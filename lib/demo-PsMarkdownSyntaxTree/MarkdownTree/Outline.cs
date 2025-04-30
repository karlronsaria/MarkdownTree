using MarkdownTree.Lex;
using System.Buffers;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace MarkdownTree.Parse;

public interface IMarkdownWritable
{
    public IEnumerable<string> ToMarkdown(int level = 1, int indent = 0);
}

public class IParent : ITree
{
    public IList<ITree> Children { get; set; } = [];
}

public class Outline : IParent, IMarkdownWritable
{
    public const int DEFAULT_INDENT_SIZE = 2;
    // public static int IndentSize { get; set; } = DEFAULT_INDENT_SIZE;

    public LineType LineType { get; set; } = LineType.Paragraph;
    public ISegment Content { get; set; } = new Leaf();

    public string Name => Content.ToString().Trim();

    public static IEnumerable<string> HeadingToMarkdown(int level, ITree content)
    {
        yield return $"{string.Concat(Enumerable.Repeat("#", level))} {content.ToString()?.Trim()}";
        yield return string.Empty;
    }

    public IEnumerable<string>
    ContentAsMarkdown(int level = 1, int indent = 0) {
        foreach (string line in ContentAsMarkdown(Content, LineType, level, indent))
            yield return line;
    }

    public static IEnumerable<string>
    ContentAsMarkdown(
        ITree content,
        LineType lineType,
        int level = 1,
        int indent = 0
    ) {
        if (lineType == LineType.Heading)
        {
            foreach (string line in HeadingToMarkdown(level, content))
                yield return line;

            yield break;
        }

        string lead = lineType switch
        {
            LineType.Vinculum => "---",
            LineType.UnorderedList => "- ",
            LineType.Define => ": ",
            LineType.Local => "![",
            _ => string.Empty,
        };

        string space = string.Concat(Enumerable.Repeat(" ", indent));
        yield return $"{space}{lead}{content.ToString()?.Trim()}";
    }

    public IEnumerable<string> ChildrenAsMarkdown(int nextLevel, int indent, int indentSize)
    {
        int orderedListIndex = 0;
        LineType prevType = LineType.None;
        LineType parentLineType = LineType;
        bool lastLineWhiteSpace = LineType == LineType.Heading;

        foreach (var child in Children)
        {
            if (child is CodeBlock c)
            {
                foreach (string line in c.ToMarkdown(nextLevel, indent))
                    yield return line;

                lastLineWhiteSpace = true;
                orderedListIndex = 0;
                prevType = LineType.None;

                foreach (ITree tree in c.Children)
                    if (tree is IMarkdownWritable m)
                        foreach (string line in m.ToMarkdown(nextLevel + 1, indent))
                            yield return line;
                    else
                        foreach (string line in ContentAsMarkdown(tree, LineType.None, nextLevel + 1, indent))
                            yield return line;

                continue;
            }

            if (child is Table t)
            {
                foreach (string line in t.ToMarkdown(nextLevel, indent))
                    yield return line;

                lastLineWhiteSpace = true;
                orderedListIndex = 0;
                prevType = LineType.None;

                foreach (ITree tree in t.Children)
                    if (tree is IMarkdownWritable m)
                        foreach (string line in m.ToMarkdown(nextLevel + 1, indent))
                            yield return line;
                    else
                        foreach (string line in ContentAsMarkdown(tree, LineType.None, nextLevel + 1, indent))
                            yield return line;

                continue;
            }

            if (child is Outline o)
            {
                if (o.LineType == LineType.Heading)
                {
                    yield return string.Empty;

                    foreach (string line in HeadingToMarkdown(nextLevel, o.Content))
                        yield return line;

                    lastLineWhiteSpace = true;
                    indent = 0;
                    orderedListIndex = 0;
                    prevType = o.LineType;

                    foreach (string str in o.ChildrenAsMarkdown(nextLevel + 1, indent, indentSize))
                        yield return str;

                    continue;
                }

                if (!lastLineWhiteSpace &&
                    o.LineType == LineType.Local && prevType != LineType.Local ||
                    o.LineType != LineType.Local && prevType == LineType.Local)
                {
                    yield return string.Empty;
                }

                string lead = string.Empty;

                if (o.LineType == LineType.OrderedList)
                {
                    orderedListIndex++;
                    lead = $"{orderedListIndex}. ";
                }
                else
                {
                    orderedListIndex = 0;

                    lead = o.LineType switch
                    {
                        LineType.Vinculum => "---",
                        LineType.UnorderedList => "- ",
                        LineType.Define => ": ",
                        LineType.Local => "![",
                        _ => string.Empty,
                    };
                }

                if (o is ActionItem a)
                    lead = $"{lead}[{(a.Completed ? 'x' : ' ')}] ";

                if (parentLineType == LineType.Heading)
                    indent = 0;

                string space = string.Concat(Enumerable.Repeat(" ", indent));
                yield return $"{space}{lead}{o.Name}";

                int nextIndent = 
                    o.LineType == LineType.Heading
                        ? 0
                        : indent +
                          (o.LineType == LineType.OrderedList && indentSize < 3
                              ? 3 : indentSize);

                prevType = o.LineType;

                foreach (string str in o.ChildrenAsMarkdown(nextLevel + 1, nextIndent, indentSize))
                    yield return str;
            }

            lastLineWhiteSpace = false;
        }

        if (prevType == LineType.Local)
            yield return string.Empty;
    }

    public IEnumerable<string> ToMarkdown(int level = 1, int indentSize = DEFAULT_INDENT_SIZE)
    {
        foreach (string line in ContentAsMarkdown(level, indentSize))
            yield return line;

        int indent = 0;

        if (LineType != LineType.Heading)
            indent =
                LineType != LineType.Heading &&
                LineType == LineType.OrderedList && indentSize == 2
                    ? 3 : indentSize;

        foreach (string line in ChildrenAsMarkdown(level + 1, indent, indentSize))
            yield return line;
    }

    // note
    // - two outline items having the same line type and name but different overall types
    //   (eg one is an action item and another is not) is an example of a malformed tree
    public bool HeadEquivalent(Outline outline) =>
        LineType == outline.LineType && Name == outline.Name;

    public Outline MergeChildren()
    {
        IList<ITree> buckets = [];

        foreach (ITree child in Children)
        {
            if (child is Outline outline)
            {
                int index = 0;

                while (index < buckets.Count)
                {
                    if (buckets[index] is Outline bucket && outline.HeadEquivalent(bucket))
                    {
                        foreach (ITree grandchild in outline.Children)
                            bucket.Children.Add(grandchild);

                        break;
                    }

                    index++;
                }

                if (index == buckets.Count)
                    buckets.Add(outline);
            }
            else
            {
                buckets.Add(child);
            }
        }

        Children = buckets;
        return this;
    }

    public Outline Merge()
    {
        MergeChildren();

        foreach (ITree child in Children)
            if (child is Outline outline)
                outline.Merge();

        return this;
    }

    public bool IsNonLeaf() =>
        Children.Count > 0 || LineType == LineType.UnorderedList;

    public Outline Fold()
    {
        ISegment left = Content;
        ITree rootPtr = this;

        while (Children.Count == 1 && Children[0] is Outline trivial && trivial.IsNonLeaf())
        {
            left = new Colon
            {
                Left = left is Colon c
                    ? c.Right
                    : left,
                Right = trivial.Content,
            };

            if (rootPtr is Outline o)
            {
                o.Content = left;
            }
            else if (rootPtr is Colon d)
            {
                d.Right = left;
            }

            rootPtr = left;
            Children = trivial.Children;
        }

        foreach (var child in Children)
            if (child is Outline o)
                _ = o.Fold();

        return this;
    }

    public Outline Unfold()
    {
        Outline tail = this;
        ISegment cursor = Content;
        var tempChildren = Children;

        while (cursor is Colon c)
        {
            tail.Content = c.Left;

            Outline branch = new()
            {
                LineType = LineType.UnorderedList,
            };

            tail.Children = [branch];
            tail = branch;
            cursor = c.Right;
        }

        tail.Content = cursor;
        tail.Children = tempChildren;

        foreach (var child in tail.Children)
            if (child is Outline o)
                _ = o.Unfold();

        return this;
    }

    public static IEnumerable<ITree> Get(IEnumerable<string> lines)
    {
        TreeDepth depth = new();
        OutlineStack stack = new();
        IEnumerator<string> e = new NonStandardEnumerator<string>([.. lines], () => string.Empty);

        while (e.MoveNext())
        {
            string line = e.Current;
            LineClass lineClass = LineClass.Get(line);

            if (lineClass is WhiteSpaceLineClass)
                continue;

            IParent branch;

            if (lineClass is CodeBlockLineClass codeBlockLine)
            {
                (branch, e) = GetCodeBlock((IEnumerator<string>)e.Clone(), codeBlockLine);

                if (branch is Malformed malformed)
                {
                    yield return malformed;
                    yield break;
                }
            }
            else if (lineClass.Type == LineType.TableRow)
            {
                (branch, e) = GetTable((IEnumerator<string>)e.Clone(), lineClass);

                if (branch is Malformed malformed)
                {
                    yield return malformed;
                    yield break;
                }
            }
            else
            {
                branch = lineClass.Actionable && lineClass.Status > -1
                    ? new ActionItem
                    {
                        LineType = lineClass.Type,
                        Completed = lineClass.Status == 1,
                    }
                    : new Outline
                    {
                        LineType = lineClass.Type,
                    };

                var tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);

                if (branch is Outline o)
                    o.Content = Lines.Get(lineClass.Type, new Enumerator<Token>([.. tokens]));
            }

            if (lineClass is HeadingLineClass c)
            {
                depth.Set(c.Level);
            }
            else if (!depth.Next(lineClass.Indent))
            {
                yield return new Malformed { Children = [branch] };
                yield break;
            }

            foreach (var error in stack.Put(branch, depth.Index))
                yield return error;
        }

        (var trees, var errors) = stack.Flush();

        foreach (var error in errors)
            yield return error;

        foreach (var tree in trees)
            yield return tree;
    }

    public static (IParent, IEnumerator<string>)
    GetTable(IEnumerator<string> lines, LineClass lineClass)
    {
        string line = lines.Current;
        var tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
        (ISegment s, _) = Lines.GetRow(new Enumerator<Token>([.. tokens]));

        if (s is not Row)
            return (new Malformed { Children = [s] }, lines);

        Row headings = (Row)s;
        _ = lines.MoveNext();
        line = lines.Current;
        lineClass = LineClass.Get(line);
        tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
        bool isRowSeparator = Lines.IsRowSeparator(new Enumerator<Token>([.. tokens]));

        while (!isRowSeparator && lines.MoveNext())
        {
            line = lines.Current;
            lineClass = LineClass.Get(line);

            if (
                lineClass.Type != LineType.TableRow
                && lineClass.Type != LineType.WhiteSpace
            )
                return (new Malformed { Children = [s] }, lines);

            tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
            isRowSeparator = Lines.IsRowSeparator(new Enumerator<Token>([.. tokens]));
        }

        bool isTablePart = true;
        IEnumerator<string> backtracker;
        IList<Row> rows = [];

        while ((backtracker = (IEnumerator<string>)lines.Clone()).MoveNext() && isTablePart)
        {
            line = backtracker.Current;
            lineClass = LineClass.Get(line);

            if (lineClass.Type == LineType.TableRow)
            {
                tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
                (s, _) = Lines.GetRow(new Enumerator<Token>([.. tokens]));

                if (s is Row row)
                    rows.Add(row);
                else
                    return (new Malformed { Children = [s] }, lines);
            }

            isTablePart = lineClass.Type == LineType.TableRow
                || lineClass.Type == LineType.WhiteSpace;

            if (isTablePart)
                lines = backtracker;
        }

        IParent branch = new Table
        {
            Headings = headings,
            Rows = rows,
        };

        return (branch, lines);
    }

    public static (IParent, IEnumerator<string>)
    GetCodeBlock(IEnumerator<string> lines, CodeBlockLineClass lineClass)
    {
        int firstIndent = lineClass.Indent;
        IList<string> codeLines = [];
        IParent codeBlock;

        while (lines.MoveNext())
        {
            string line = lines.Current;
            Match indentCapture = (new Regex($"^ {{0,{firstIndent}}}")).Match(line);
            int leadIndent = indentCapture.Length;
            string code = line[leadIndent..];

            if (leadIndent == firstIndent && code == "```")
            {
                codeBlock = new CodeBlock
                {
                    Language = lineClass.Language,
                    Lines = codeLines,
                };

                return (codeBlock, lines);
            }
            else
            {
                codeLines.Add(code);
            }
        }

        codeBlock = new Malformed
        {
            Children = [
                new CodeBlock
                {
                    Language = lineClass.Language,
                    Lines = codeLines,
                }
            ],
        };

        return (codeBlock, lines);
    }
}

public class ActionItem : Outline
{
    public bool Completed = false;
}

public class CodeBlock : IParent, IMarkdownWritable
{
    public string Language { get; set; } = string.Empty;
    public IList<string> Lines { get; set; } = [];

    public IEnumerable<string> ToMarkdown(int level, int nextIndent)
    {
        string space = string.Concat(Enumerable.Repeat(" ", nextIndent));

        yield return string.Empty;
        yield return $"{space}```{Language}";

        foreach (string line in Lines)
            yield return $"{space}{line}";

        yield return $"{space}```";
        yield return string.Empty;
    }
}

public class Table : IParent, IMarkdownWritable
{
    public Row Headings { get; set; } = [];
    public IList<Row> Rows { get; set; } = [];

    public IEnumerable<string> ToMarkdown(int level, int nextIndent)
    {
        string space = string.Concat(Enumerable.Repeat(" ", nextIndent));

        yield return string.Empty;
        yield return $"{space}|{Headings}|";

        string vinculum =
            string.Join("|", [.. from h in Headings
            select string.Concat(Enumerable.Repeat("-", h.ToString().Length))]);

        yield return $"{space}|{vinculum}|";

        foreach (Row row in Rows)
            yield return $"{space}|{row}|";

        yield return string.Empty;
    }
}

public class Malformed : IParent;

public class TreeDepth()
{
    private int _level = 0;
    private int _current_indent = 0;
    private bool _increment_next = false;
    private readonly Stack<int> _stack = new();

    public int Index => _level + _stack.Count - 1;

    // Heading LineType
    public void Set(int level)
    {
        _level = level;
        _stack.Clear();
        _current_indent = 0;
        _increment_next = true;
    }

    public bool Next(int indent)
    {
        if (_increment_next)
        {
            _level++;
            _increment_next = false;
        }

        if (indent > _current_indent)
        {
            int diff = indent - _current_indent;
            _stack.Push(diff);
            _current_indent += diff;
        }
        else
        {
            while (indent < _current_indent)
                if (_stack.Count == 0)
                    return false;
                else
                    _current_indent -= _stack.Pop();

            if (indent > _current_indent)
                return false;
        }

        return true;
    }
}

public class OutlineStack : Stack<(IList<ITree>, ITree?)>
{
    public (IList<ITree>, IList<Malformed>) Flush()
    {
        IList<ITree> tree = [];
        IList<Malformed> errors = [];

        while (Count > 0)
        {
            (tree, Malformed? error) = Pop();

            if (error is not null)
                errors.Add(error);
        }

        return (tree, errors);
    }

    public IList<Malformed> Put(ITree tree, int bucket = -1)
    {
        if (bucket < 0)
        {
            Replace([tree]);
            return [];
        }

        while (bucket > Count - 1)
            base.Push(([], null));

        IList<Malformed> errors = [];

        while (bucket < Count - 1)
        {
            (_, var item) = Pop();

            if (item is not null)
                errors.Add(item);
        }

        Replace([tree]);
        return errors;
    }

    private void Replace(IList<ITree> trees)
    {
        if (Count == 0)
        {
            base.Push((trees, trees.Last()));
        }
        else
        {
            (var list, _) = base.Pop();

            foreach (var tree in trees)
                list.Add(tree);

            base.Push((list, list.Last()));
        }
    }

    private new void Push((IList<ITree>, ITree?) listAndTail) => base.Push(listAndTail);

    private new (IList<ITree>, Malformed?) Pop()
    {
        if (Count == 0)
            return ([], null);

        (var list, ITree? tail) = base.Pop();

        // return tree root
        if (Count == 0)
            return (list, null);

        (var prevList, ITree? prevTail) = base.Pop();

        if (prevList.Count == 0)
        {
            base.Push((list, tail));
        }
        else
        {
            if (prevTail is IParent parent)
            {
                foreach (var item in list)
                    parent.Children.Add(item);

                base.Push((prevList, prevTail));
            }
            else
            {
                base.Push((prevList, prevTail));

                // return malformed branch
                return ([], new Malformed { Children = list });
            }
        }

        return ([], null);
    }
}

