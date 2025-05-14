using MarkdownTree.Lex;
using System.Text.RegularExpressions;

namespace MarkdownTree.Parse;

public interface IMarkdownWritable
{
    public const int DEFAULT_INDENT_SIZE = 2;

    public IEnumerable<string> ToMarkdown();
    public IEnumerable<string> ToMarkdown(int level, int indent);
}

public interface IHaystack
{
    public enum Type
    {
        String,
        Text,
        Hyperlink,
        Strike,
        InlineCode,
    }

    public IHaystack? FindFirst(ISegment.Predicate predicate);
    public IEnumerable<IHaystack> FindAll(ISegment.Predicate predicate);

    public IEnumerable<IHaystack> IsA(Type type) =>
        FindAll(type switch
        {
            Type.String => (i => i.Type == TokenType.String),
            Type.Text => (i => i.Type == TokenType.Text),
            Type.Hyperlink => (i => i.Type == TokenType.Hyperlink),
            Type.Strike => (i => i.Type == TokenType.Strike),
            Type.InlineCode => (i => i.Type == TokenType.InlineCode),
            _ => (_ => false),
        });
}

public class Needle : IHaystack
{
    public required int LineNumber { get; set; }
    public required int ColumnNumber { get; set; }
    public required Branching Branch { get; set; }
    public required Token Token { get; set; }

    public IHaystack? FindFirst(ISegment.Predicate predicate) =>
        predicate(Token) 
            ? this
            : Branch.FindFirst(predicate);

    public IEnumerable<IHaystack> FindAll(ISegment.Predicate predicate)
    {
        if (predicate(Token))
            yield return this;

        foreach (IHaystack needle in Branch.FindAll(predicate))
            yield return needle;
    }
}

public abstract class Branching(int lineNumber) : ITree, IHaystack
{
    public IList<ITree> Children { get; set; } = [];
    public int LineNumber { get; set; } = lineNumber;

    public abstract Needle? FindFirstSegment(ISegment.Predicate predicate);
    public abstract IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate);

    public IHaystack? FindFirst(ISegment.Predicate predicate)
    {
        if (FindFirstSegment(predicate) is Needle needle)
            return needle;

        foreach (ITree child in Children)
            if (child is Branching branch && branch.FindFirst(predicate) is Needle n)
                return n;
            else if (child is ISegment e && e.WhereFirst(predicate) is Token g)
                return new Needle
                {
                    LineNumber = LineNumber,
                    ColumnNumber = g.Start,
                    Branch = this,
                    Token = g,
                };

        return null;
    }

    public IEnumerable<IHaystack> FindAll(ISegment.Predicate predicate)
    {
        foreach (Needle needle in FindAllSegments(predicate))
            yield return needle;

        foreach (ITree child in Children)
            if (child is Branching branch)
                foreach (IHaystack n in branch.FindAll(predicate))
                    yield return n;
            else if (child is ISegment e)
                foreach (Token g in e.WhereAll(predicate))
                    yield return new Needle
                    {
                        LineNumber = LineNumber,
                        ColumnNumber = g.Start,
                        Branch = this,
                        Token = g,
                    };

        yield break;
    }

    public delegate bool Predicate(ITree tree);

    public ITree? WhereFirst(Predicate predicate)
    {
        if (predicate(this))
            return this;

        foreach (ITree child in Children)
            if (predicate(child))
                return child;

        return null;
    }

    public IEnumerable<ITree> WhereAll(Predicate predicate)
    {
        if (predicate(this))
            yield return this;

        foreach (ITree child in Children)
            if (child is Branching branch)
                foreach (ITree subchild in branch.WhereAll(predicate))
                    yield return subchild;
    }

    public delegate T Callback<T>(ITree tree);

    public IEnumerable<T> ForEach<T>(Callback<T> callback)
    {
        yield return callback(this);

        foreach (ITree child in Children)
            yield return callback(child);
    }
}

public class Outline(int lineNumber) : Branching(lineNumber), IMarkdownWritable
{
    public LineType LineType { get; set; } = LineType.Paragraph;
    public ISegment Content { get; set; } = new Leaf();

    public string Name => Content.ToString().Trim();

    public override Needle? FindFirstSegment(ISegment.Predicate predicate) =>
        Content.WhereFirst(predicate) switch
        {
            Token t => new Needle
            {
                LineNumber = LineNumber,
                ColumnNumber = t.Start,
                Branch = this,
                Token = t,
            },
            _ => null,
        };

    public override IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate) =>
        from what in Content.WhereAll(predicate)
        where what is Token t
        select new Needle
        {
            LineNumber = LineNumber,
            ColumnNumber = what.Start,
            Branch = this,
            Token = what,
        };

    public static IEnumerable<string> HeadingToMarkdown(int level, ITree content)
    {
        yield return $"{string.Concat(Enumerable.Repeat('#', level))} {content.ToString()?.Trim()}";
        yield return string.Empty;
    }

    public IEnumerable<string>
    ContentAsMarkdown(int level = 1, int indent = 0)
    {
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
            // // (karlr 2025-05-04): This conflicts with inline ImageMacro
            // LineType.ImageMacro => "![",
            _ => string.Empty,
        };

        string space = string.Concat(Enumerable.Repeat(' ', indent));
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
                    o.LineType == LineType.ImageMacro && prevType != LineType.ImageMacro ||
                    o.LineType != LineType.ImageMacro && prevType == LineType.ImageMacro)
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
                        // // (karlr 2025-05-04): This conflicts with inline ImageMacro
                        // LineType.ImageMacro => "![",
                        _ => string.Empty,
                    };
                }

                if (o is ActionItem a)
                    lead = $"{lead}[{(a.Completed ? 'x' : ' ')}] ";

                if (parentLineType == LineType.Heading)
                    indent = 0;

                string space = string.Concat(Enumerable.Repeat(' ', indent));
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

        if (prevType == LineType.ImageMacro)
            yield return string.Empty;
    }

    public IEnumerable<string> ToMarkdown() => ToMarkdown(1, IMarkdownWritable.DEFAULT_INDENT_SIZE);

    public IEnumerable<string> ToMarkdown(int level, int indentSize)
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

    // public delegate bool Predicate(Outline outline);

    public static IList<ITree> Merge(IList<ITree> forest, Predicate predicate)
    {
        IList<ITree> buckets = [];

        foreach (ITree child in forest)
        {
            if (child is Outline outline && predicate(outline))
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

        return buckets;
    }

    public static IList<ITree> Merge(IList<ITree> forest) => Merge(forest, _ => true);

    public Outline MergeChildren(Predicate predicate)
    {
        Children = Merge(Children, predicate);
        return this;
    }

    public Outline MergeChildren()
    {
        Children = Merge(Children);
        return this;
    }

    public Outline CascadeMerge(Predicate predicate)
    {
        _ = MergeChildren(predicate);

        foreach (ITree child in Children)
            if (child is Outline outline)
                outline.CascadeMerge(predicate);

        return this;
    }

    public Outline CascadeMerge()
    {
        _ = MergeChildren();

        foreach (ITree child in Children)
            if (child is Outline outline)
                outline.CascadeMerge();

        return this;
    }

    public bool IsNonLeaf() =>
        Children.Count > 0 || LineType == LineType.UnorderedList;

    public Outline Fold(Predicate predicate)
    {
        ISegment left = Content;
        ITree rootPtr = this;

        while (
            predicate(this) &&
            Children.Count == 1 &&
            Children[0] is Outline trivial &&
            trivial.IsNonLeaf()
        ) {
            left = new Colon
            {
                Left = left is Colon c
                    ? c.Right
                    : left,
                Right = trivial.Content,
            };

            if (rootPtr is Outline o)
                o.Content = left;
            else if (rootPtr is Colon d)
                d.Right = left;

            rootPtr = left;
            Children = trivial.Children;
        }

        return this;
    }

    public Outline Fold() => Fold(_ => true);

    public Outline CascadeFold()
    {
        _ = Fold();

        foreach (var child in Children)
            if (child is Outline o)
                _ = o.CascadeFold();

        return this;
    }

    public Outline CascadeFold(Predicate predicate)
    {
        _ = Fold(predicate);

        foreach (var child in Children)
            if (child is Outline o)
                _ = o.CascadeFold(predicate);

        return this;
    }

    public Outline Unfold(Predicate predicate)
    {
        Outline tail = this;
        ISegment cursor = Content;
        var tempChildren = Children;

        while (predicate(tail) && cursor is Colon c)
        {
            tail.Content = c.Left;

            Outline branch = new(LineNumber)
            {
                LineType = LineType.UnorderedList,
            };

            tail.Children = [branch];
            tail = branch;
            cursor = c.Right;
        }

        tail.Content = cursor;
        tail.Children = tempChildren;
        return tail;
    }

    public Outline Unfold() => Unfold(_ => true);

    public Outline CascadeUnfold(Predicate predicate)
    {
        Outline tail = Unfold(predicate);

        foreach (var child in tail.Children)
            if (child is Outline o)
                _ = o.CascadeUnfold(predicate);

        return this;
    }

    public Outline CascadeUnfold()
    {
        Outline tail = Unfold();

        foreach (var child in tail.Children)
            if (child is Outline o)
                _ = o.CascadeUnfold();

        return this;
    }

    public static IEnumerable<ITree> Get(IEnumerable<string> lines) => Get(lines, _ => true);

    public static IEnumerable<ITree> Get(IEnumerable<string> lines, Predicate whereOutline)
    {
        TreeDepth depth = new();
        OutlineStack stack = new();
        IEnumerator<string> e = new NonStandardEnumerator<string>([.. lines], () => string.Empty);

        // Advance enumerator
        while (e.MoveNext())
        {
            // Enumerator Current used
            string line = e.Current;

            // Line class
            Line lineClass = Line.Get(line);

            if (lineClass is WhiteSpaceLineClass)
                continue;

            Branching branch;

            if (lineClass is CodeBlockLineClass codeBlockLine)
            {
                // Enumerator passed
                (branch, e) = GetCodeBlock((IEnumerator<string>)e.Clone(), codeBlockLine);

                if (branch is Malformed malformed)
                {
                    yield return malformed;
                    yield break;
                }
            }
            else if (lineClass.Type == LineType.TableRow)
            {
                // Enumerator passed
                (branch, e) = GetTable((IEnumerator<string>)e.Clone(), lineClass);

                if (branch is Malformed malformed)
                {
                    yield return malformed;
                    yield break;
                }
            }
            else
            {
                // Branch created
                branch = lineClass.Actionable && lineClass.Status > -1
                    ? new ActionItem(e.Index())
                    {
                        LineType = lineClass.Type,
                        Completed = lineClass.Status == 1,
                    }
                    : new Outline(e.Index())
                    {
                        LineType = lineClass.Type,
                    };

                var tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);

                if (branch is Outline o)
                {
                    o.Content = Segments.Get(lineClass.Type, new Enumerator<Token>([.. tokens]));

                    if (!whereOutline(o))
                        continue;
                }
            }

            if (lineClass is HeadingLineClass c)
            {
                depth.Set(c.Level);
            }
            else if (!depth.Next(lineClass.Indent))
            {
                // Branch created
                yield return new Malformed(e.Index()) { Children = [branch] };
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

    public static (Branching, IEnumerator<string>)
    GetTable(IEnumerator<string> lines, Line lineClass)
    {
        string line = lines.Current;
        var tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
        (ISegment s, _) = Segments.GetRow(new Enumerator<Token>([.. tokens]));

        if (s is not Row)
            // Branch created
            return (new Malformed(lines.Index()) { Children = [s] }, lines);

        Row headings = (Row)s;

        // Advance enumerator
        _ = lines.MoveNext();

        // Enumerator Current used
        line = lines.Current;

        // Line class
        lineClass = Line.Get(line);
        tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
        bool isRowSeparator = Segments.IsRowSeparator(new Enumerator<Token>([.. tokens]));

        // Advance enumerator
        while (!isRowSeparator && lines.MoveNext())
        {
            // Enumerator Current used
            line = lines.Current;

            // Line class
            lineClass = Line.Get(line);

            if (
                lineClass.Type != LineType.TableRow
                && lineClass.Type != LineType.WhiteSpace
            )
                // Branch created
                return (new Malformed(lines.Index()) { Children = [s] }, lines);

            tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
            isRowSeparator = Segments.IsRowSeparator(new Enumerator<Token>([.. tokens]));
        }

        bool isTablePart = true;
        IEnumerator<string> backtracker;
        IList<Row> rows = [];

        // Advance enumerator
        while ((backtracker = (IEnumerator<string>)lines.Clone()).MoveNext() && isTablePart)
        {
            // Enumerator Current used
            line = backtracker.Current;

            // Line class
            lineClass = Line.Get(line);

            if (lineClass.Type == LineType.TableRow)
            {
                tokens = Token.Tokenize(line, lineClass.Type, lineClass.Length);
                (s, _) = Segments.GetRow(new Enumerator<Token>([.. tokens]));

                if (s is Row row)
                    rows.Add(row);
                else
                    // Branch created
                    return (new Malformed(lines.Index()) { Children = [s] }, lines);
            }

            isTablePart = lineClass.Type == LineType.TableRow
                || lineClass.Type == LineType.WhiteSpace;

            if (isTablePart)
                lines = backtracker;
        }

        // Branch created
        Branching branch = new Table(lines.Index())
        {
            Headings = headings,
            Rows = rows,
        };

        return (branch, lines);
    }

    public static (Branching, IEnumerator<string>)
    GetCodeBlock(IEnumerator<string> lines, CodeBlockLineClass lineClass)
    {
        int firstIndent = lineClass.Indent;
        IList<string> codeLines = [];
        Branching codeBlock;

        // Advance enumerator
        while (lines.MoveNext())
        {
            // No line class identified
            // All lines are treated as code until an EndCodeBlock line is found

            string line = lines.Current;
            Match indentCapture = new Regex($"^ {{0,{firstIndent}}}").Match(line);
            int leadIndent = indentCapture.Length;
            string code = line[leadIndent..];

            if (leadIndent == firstIndent && code == "```")
            {
                // Branch created
                codeBlock = new CodeBlock(lines.Index())
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

        // Branch created
        codeBlock = new Malformed(lines.Index())
        {
            Children = [
                new CodeBlock(lines.Index())
                {
                    Language = lineClass.Language,
                    Lines = codeLines,
                }
            ],
        };

        return (codeBlock, lines);
    }
}

public class ActionItem(int lineNumber) : Outline(lineNumber)
{
    public bool Completed = false;
}

public class CodeBlock(int lineNumber) : Branching(lineNumber), IMarkdownWritable
{
    public string Language { get; set; } = string.Empty;
    public IList<string> Lines { get; set; } = [];

    public override Needle? FindFirstSegment(ISegment.Predicate predicate) => null;
    public override IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate) => [];

    public IEnumerable<string> ToMarkdown() => ToMarkdown(1, IMarkdownWritable.DEFAULT_INDENT_SIZE);

    public IEnumerable<string> ToMarkdown(int level, int nextIndent)
    {
        string space = string.Concat(Enumerable.Repeat(' ', nextIndent));

        yield return string.Empty;
        yield return $"{space}```{Language}";

        foreach (string line in Lines)
            yield return $"{space}{line}";

        yield return $"{space}```";
        yield return string.Empty;
    }
}

public class Table(int lineNumber) : Branching(lineNumber), IMarkdownWritable
{
    public Row Headings { get; set; } = [];
    public IList<Row> Rows { get; set; } = [];

    public override Needle? FindFirstSegment(ISegment.Predicate predicate)
    {
        if (Headings.WhereFirst(predicate) is Token t)
            return new Needle
            {
                LineNumber = LineNumber,
                ColumnNumber = t.Start,
                Branch = this,
                Token = t,
            };

        for (int i = 0; i < Rows.Count; i++)
            if (Rows[i].WhereFirst(predicate) is Token s)
                return new Needle
                {
                    LineNumber = LineNumber + i + 1,
                    ColumnNumber = s.Start,
                    Branch = this,
                    Token = s,
                };

        return null;
    }

    public override IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate)
    {
        foreach (Token t in Headings.WhereAll(predicate))
            yield return new Needle
            {
                LineNumber = LineNumber,
                ColumnNumber = t.Start,
                Branch = this,
                Token = t,
            };

        for (int i = 0; i < Rows.Count; i++)
            foreach (Token s in Rows[i].WhereAll(predicate))
                yield return new Needle
                {
                    LineNumber = LineNumber + i + 1,
                    ColumnNumber = s.Start,
                    Branch = this,
                    Token = s,
                };

        yield break;
    }

    public IEnumerable<string> ToMarkdown() => ToMarkdown(1, IMarkdownWritable.DEFAULT_INDENT_SIZE);

    public IEnumerable<string> ToMarkdown(int level, int nextIndent)
    {
        string space = string.Concat(Enumerable.Repeat(' ', nextIndent));

        yield return string.Empty;
        yield return $"{space}|{Headings}|";

        string vinculum =
            string.Join("|", [.. from h in Headings
            select string.Concat(Enumerable.Repeat('-', h.ToString().Length))]);

        yield return $"{space}|{vinculum}|";

        foreach (Row row in Rows)
            yield return $"{space}|{row}|";

        yield return string.Empty;
    }
}

public class Malformed(int lineNumber) : Branching(lineNumber)
{
    public override Needle? FindFirstSegment(ISegment.Predicate predicate) => null;
    public override IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate) => [];
}

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
            if (prevTail is Branching parent)
            {
                foreach (var item in list)
                    parent.Children.Add(item);

                base.Push((prevList, prevTail));
            }
            else
            {
                base.Push((prevList, prevTail));

                int lineNumber = list.First() is Branching b
                    ? b.LineNumber : -1;

                // return malformed branch
                return ([], new Malformed(lineNumber) { Children = list });
            }
        }

        return ([], null);
    }
}

