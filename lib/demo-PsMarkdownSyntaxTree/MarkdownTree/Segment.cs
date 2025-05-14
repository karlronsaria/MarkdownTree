using MarkdownTree.Lex;
using System.Text.RegularExpressions;

namespace MarkdownTree.Parse;

public interface IEnumerator<T> : System.Collections.Generic.IEnumerator<T>, ICloneable
{
    public int Index();
}

public interface ITree;

public interface ISegment : ITree
{
    public delegate bool Predicate(Token token);
    public string ToString();
    public Token? WhereFirst(Predicate predicate);
    public IEnumerable<Token> WhereAll(Predicate predicate);
}

public class WhiteSpaceLine : ISegment
{
    public override string ToString() => string.Empty;
    public Token? WhereFirst(ISegment.Predicate predicate) => null;
    public IEnumerable<Token> WhereAll(ISegment.Predicate predicate) => [];
}

public class Leaf : Token, ISegment
{
    public Leaf() : base() { }
    public Leaf(Token token) : base(token) { }
    public override string ToString() => base.ToString() ?? string.Empty;
    public Token ToToken() => new(this);

    public Token? WhereFirst(ISegment.Predicate predicate) =>
        predicate(this) ? ToToken() : null;

    public IEnumerable<Token> WhereAll(ISegment.Predicate predicate) =>
        predicate(this) ? [ToToken()] : [];
}

public class Sequence : List<ISegment>, ISegment
{
    public Sequence() { }
    public Sequence(IList<ISegment> trees) : base(trees) { }

    public override string ToString() =>
        string.Join(string.Empty, from i in this select i.ToString());

    public Token? WhereFirst(ISegment.Predicate predicate)
    {
        foreach (ISegment i in this)
            return i.WhereFirst(predicate);

        return null;
    }

    public IEnumerable<Token> WhereAll(ISegment.Predicate predicate)
    {
        foreach (ISegment i in this)
            foreach (Token t in i.WhereAll(predicate))
                yield return t;
    }
}

public class Row : Sequence
{
    public override string ToString() =>
        string.Join('|', from i in this select i.ToString());
}

public class Colon : ISegment
{
    public required ISegment Left { get; set; }
    public required ISegment Right { get; set; }
    public override string ToString() => $"{Left.ToString()}: {Right.ToString()}";

    public Token? WhereFirst(ISegment.Predicate predicate) =>
        Left.WhereFirst(predicate) ?? Right.WhereFirst(predicate);

    public IEnumerable<Token> WhereAll(ISegment.Predicate predicate)
    {
        foreach (Token t in Left.WhereAll(predicate))
            yield return t;

        foreach (Token t in Right.WhereAll(predicate))
            yield return t;
    }
}

public class LinkText : ISegment
{
    public required ISegment Box { get; set; }
    public required ISegment Link { get; set; }
    public override string ToString() => $"[{Box.ToString()}]({Link.ToString()})";

    public Token? WhereFirst(ISegment.Predicate predicate) =>
        Box.WhereFirst(predicate) ?? Link.WhereFirst(predicate);

    public IEnumerable<Token> WhereAll(ISegment.Predicate predicate)
    {
        foreach (Token t in Box.WhereAll(predicate))
            yield return t;

        foreach (Token t in Link.WhereAll(predicate))
            yield return t;
    }
}

public class ImageMacro : LinkText
{
    public override string ToString() => $"!{base.ToString()}";
}

public class Text : Leaf
{
    public Text() : base() { }
    public Text(Token token) : base(token) { }
    public override string ToString() => Content;
}

public static partial class Segments
{
    public static ISegment Get(LineType lineType, IEnumerator<Token> tokens)
    {
        ISegment? head;

        switch (lineType)
        {
            case LineType.TableRow:
                (head, _) = GetRow(tokens);
                break;
            case LineType.WhiteSpace:
                head = new WhiteSpaceLine();
                break;
            case LineType.ImageMacro:
                _ = tokens.MoveNext();
                (head, _) = GetLinkText(tokens, moveNext: false); // todo

                if (head is LinkText linkText)
                    head = new ImageMacro
                    {
                        Box = linkText.Box,
                        Link = linkText.Link,
                    };

                break;
            default:
                head = Get(tokens);
                break;
        }

        return head ?? new Leaf();
    }

    public delegate bool Predicate(Token token);

    public static ISegment
    Add(ISegment? root, ISegment branch)
    {
        if (root is null)
            return root = branch;

        if (root is not Sequence)
            root = new Sequence([root]);

        ((Sequence) root).Add(branch);
        return root;
    }

    public static ISegment?
    Add(ISegment? root, IEnumerable<ISegment> branches)
    {
        foreach (ISegment branch in branches)
            root = Add(root, branch);

        return root;
    }

    public static ISegment?
    Get(IEnumerator<Token> tokens)
    {
        (_, ISegment? head) = Get(tokens, t => true);
        return head;
    }

    public static bool
    IsRowSeparator(IEnumerator<Token> tokens)
    {
        (ISegment segment, _) = GetRow(tokens);

        if (segment is null)
            return false;

        if (segment is Row row)
            foreach (ISegment cell in row)
                if (!TableVinculum().IsMatch(cell.ToString()))
                    return false;

        return true;
    }

    public static (ISegment, IEnumerator<Token>?)
    GetRow(IEnumerator<Token> tokens)
    {
        Row row = [];
        Sequence cell = [];

        (bool success, ISegment? branch) = Get(tokens, t => t.Type != TokenType.Bar);

        while (success) // &= tokens.MoveNext())
        {
            if (branch is ISegment inline)
                cell.Add(inline);

            if (tokens.Current.Type == TokenType.Bar && cell.Count > 0)
            {
                row.Add(cell);
                cell = [];
            }

            (success, branch) = Get(tokens, t => t.Type != TokenType.Bar);
        }

        return (row, tokens);
    }

    public static (ISegment, IEnumerator<Token>?)
    GetLinkText(IEnumerator<Token> tokens, bool moveNext = true)
    {
        ISegment fail = new Leaf(tokens.Current);
        ((Leaf)fail).Type = TokenType.Text;

        IList<ISegment> box = [];
        bool any = !moveNext || tokens.MoveNext();

        while (any && tokens.Current.Type != TokenType.CloseBox)
        {
            box.Add(new Leaf(tokens.Current));
            any = tokens.MoveNext();
        }

        if (!any)
            return (fail, null);

        any = tokens.MoveNext();

        if (!any || tokens.Current.Type != TokenType.OpenLink)
            return (fail, null);

        IList<ISegment> link = [];
        any = tokens.MoveNext();

        while (any && tokens.Current.Type != TokenType.CloseLink)
        {
            link.Add(new Leaf(tokens.Current));
            any = tokens.MoveNext();
        }

        if (!any)
            return (fail, null);

        return (
            new LinkText
            {
                Box = new Sequence(box),
                Link = new Sequence(link),
            },
            null
        );
    }

    public static (bool, ISegment?)
    Get(IEnumerator<Token> tokens, Predicate predicate)
    {
        IList<Token> texts = [];
        ISegment? head = null;
        Token current = new();
        bool fail = true;

        while (tokens.MoveNext() && (fail = predicate(tokens.Current)))
        {
            current = tokens.Current;

            switch (current.Type)
            {
                case TokenType.Text:
                    texts.Add(current);
                    break;
                default:
                    if (texts.Count > 0)
                    {
                        head = Add(head, [new Text(texts.First())
                        {
                            Content = string.Join("", texts.Select(x => x.Content)),
                            End = texts.Last().End,
                        }]);

                        texts = [];
                    }

                    break;
            }

            switch (current.Type)
            {
                case TokenType.Colon:
                    (_, ISegment? tempHead) = Get(tokens, predicate);

                    head = new Colon
                    {
                        Left = head ?? new Leaf(),
                        Right = tempHead ?? new Leaf(),
                    };

                    break;
                case TokenType.Box:
                    (ISegment branch, var e) = GetLinkText((IEnumerator<Token>)tokens.Clone());

                    if (e is IEnumerator<Token> f)
                    {
                        tokens = f;
                        head = Add(head, [branch]);
                    }

                    break;
                case TokenType.Text:
                    break;
                default:
                    head = Add(head, [new Leaf(current)]);
                    break;
            }
        }

        if (texts.Count > 0)
        {
            ISegment token = new Text(texts.First())
            {
                Content = string.Join("", texts.Select(x => x.Content)),
                End = texts.Last().End,
            };

            head = Add(head, [token]);
        }

        return (!fail, head);
    }

    [GeneratedRegex(@"\s*:?\s*\-+\s*:?\s*")]
    private static partial Regex TableVinculum();
}

