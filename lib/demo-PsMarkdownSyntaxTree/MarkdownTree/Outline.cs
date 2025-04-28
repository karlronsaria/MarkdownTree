using MarkdownTree.Lex;
using System.Text.RegularExpressions;

namespace MarkdownTree.Parse;

public class IParent : ITree
{
    public IList<ITree> Children { get; set; } = [];
}

public class Outline : IParent
{
    public LineType LineType { get; set; } = LineType.Paragraph;
    public ISegment Content { get; set; } = new Leaf();

    public string Name => Content.ToString().Trim();

    // note
    // - two outline items having the same line type and name but different overall types
    //   (eg one is an action item and another is not) is an example of a malformed tree
    public bool HeadEquivalent(Outline outline) =>
        LineType == outline.LineType && Name == outline.Name;

    public Outline Merge()
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

public class CodeBlock : IParent
{
    public string Language { get; set; } = string.Empty;
    public IList<string> Lines { get; set; } = [];
}

public class Table : IParent
{
    public Row Headings { get; set; } = [];
    public IList<Row> Rows { get; set; } = [];
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

