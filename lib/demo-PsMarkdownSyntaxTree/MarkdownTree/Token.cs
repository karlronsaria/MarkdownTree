using System.Collections;

namespace MarkdownTree.Lex;

/*
 * todo
 * - [ ] "embedded link"
 * - [ ] italic and bold
 * - [ ] "refs", Chicago style footnotes
 */

public enum TokenType
{
    NewLine, // \n
    String, // "
    Escape, // \
    Bar, // |
    Strike, // ~~
    InlineCode, // ``
    Colon, // :
    Hyperlink, // <
    OpenHyperlink, // <
    CloseHyperlink, // >
    Box, // [
    OpenBox, // [
    CloseBox, // ]
    Link, // (
    OpenLink, // (
    CloseLink, // )
    WhiteSpace,
    Text,
    EndOfLine,
}

public class Token
{
    public Token() { }

    public Token(Token token)
    {
        Success = token.Success;
        Type = token.Type;
        Content = token.Content;
        Start = token.Start;
        End = token.End;
    }

    public bool Success { get; set; }
    public TokenType Type { get; set; } = TokenType.Text;
    public string Content { get; set; } = string.Empty;
    public int Start { get; set; }
    public int End { get; set; }

    public delegate bool Predicate(char c);

    public const char DEFAULT_ESCAPE_CHAR = '\\';

    public override string ToString() =>
        Type switch {
            TokenType.NewLine => "\n", // \n
            TokenType.String => $"\"{Content}\"", // "
            TokenType.Escape => Content, // \
            TokenType.Bar => "|", // |
            TokenType.Strike => $"~~{Content}~~", // ~~
            TokenType.InlineCode => $"``{Content}``", // ``
            TokenType.Colon => ": ", // :
            TokenType.Hyperlink => $"<{Content}>", // <
            TokenType.OpenHyperlink => "<", // <
            TokenType.CloseHyperlink => ">", // >
            TokenType.Box => $"[{Content}]", // [
            TokenType.OpenBox => "[", // [
            TokenType.CloseBox => "]", // ]
            TokenType.Link => $"({Content})", // (
            TokenType.OpenLink => "(", // (
            TokenType.CloseLink => ")", // )
            TokenType.WhiteSpace => Content,
            TokenType.Text => Content,
            TokenType.EndOfLine => "", // "EOL",
            _ => throw new Exception("Unidentified token type"),
        };

    public static bool Any(string input, int start) =>
        start < input.Length - 1;

    public static bool Escaped(string input, int start, char escape = '\\') =>
        start >= 1 && input[start - 1] == escape;

    public static IEnumerable<Token>
    Tokenize(
        string input,
        LineType lineType = LineType.Paragraph,
        int start = 0
    ) {
        Token? token;
        int textStart = start;

        bool canBranch =
            lineType == LineType.UnorderedList ||
            lineType == LineType.OrderedList ||
            lineType == LineType.Heading;

        while (start < input.Length) // Any(input, start))
        {
            token = input[start] switch
            {
                '\n' => new Token
                {
                    Type = TokenType.NewLine,
                    Start = start,
                    End = start + 1,
                },
                '|' => lineType == LineType.TableRow
                    ? new Token
                    {
                        Success = true,
                        Type = TokenType.Bar,
                        Start = start,
                        End = start + 1,
                    }
                    : null,
                '\\' => start < input.Length - 1
                    ? new Token
                    {
                        Success = true,
                        Type = TokenType.Escape,
                        Content = $"\\{input[start + 1]}",
                        Start = start,
                        End = start + 2,
                    }
                    : null,
                ':' => canBranch && start < input.Length - 1 && char.IsWhiteSpace(input[start + 1])
                    ? new Token
                    {
                        Success = true,
                        Type = TokenType.Colon,
                        Content = $":{input[start + 1]}",
                        Start = start,
                        End = start + 2,
                    }
                    : null,
                '"' => Sequence(input, TokenType.String, '"', start),
                '<' => Sequence(input, TokenType.Hyperlink, '>', start),
                    // // todo: remove
                    // new Token
                    // {
                    //     Success = true,
                    //     Type = TokenType.OpenHyperlink,
                    //     Content = "<",
                    //     Start = start,
                    //     End = start + 1,
                    // },
                '>' => new Token
                    {
                        Success = true,
                        Type = TokenType.CloseHyperlink,
                        Content = ">",
                        Start = start,
                        End = start + 1,
                    },
                '[' => new Token
                    {
                        Success = true,
                        Type = TokenType.OpenBox,
                        Content = "[",
                        Start = start,
                        End = start + 1,
                    },
                ']' => new Token
                    {
                        Success = true,
                        Type = TokenType.CloseBox,
                        Content = "]",
                        Start = start,
                        End = start + 1,
                    },
                '(' => new Token
                    {
                        Success = true,
                        Type = TokenType.OpenLink,
                        Content = "(",
                        Start = start,
                        End = start + 1,
                    },
                ')' => new Token
                    {
                        Success = true,
                        Type = TokenType.CloseLink,
                        Content = ")",
                        Start = start,
                        End = start + 1,
                    },
                '~' => Strike(input, start),
                '`' => InlineCode(input, start),
                _ => null,
            };

            if (token is null)
            {
                ++start;
                continue;
            }

            if (token.Success)
            {
                if (token.Start > textStart)
                {
                    yield return new Token
                    {
                        Success = true,
                        Type = TokenType.Text,
                        Content = input[textStart..token.Start],
                        Start = textStart,
                        End = token.Start + 1,
                    };
                }

                yield return token;
                textStart = token.End;
            }
            else
            {
                yield return new Token
                {
                    Success = true,
                    Type = TokenType.Text,
                    Content = token.Content,
                    Start = start,
                    End = token.End,
                };

                textStart = token.End;
            }

            start = token.End;
        }

        if (start > textStart)
        {
            yield return new Token
            {
                Success = true,
                Type = TokenType.Text,
                Content = input[textStart..start], // (start + 1)
                Start = textStart,
                End = start,
            };
        }

        yield return new Token
        {
            Success = true,
            Type = TokenType.EndOfLine,
            Start = start,
            End = start,
        };

        yield break;
    }

    public static Token InlineCode(string input, int start = 0)
    {
        TokenType type = TokenType.InlineCode;
        int next = start + 1;
        bool success = Any(input, next) && input[next] == '`';

        if (!success)
            return new Token
            {
                Success = false,
                Type = type,
                Content = "`",
                Start = start,
                End = next, // Do not consume
            };

        var token = Sequence(input, type, '`', next);

        var fail = new Token
        {
            Success = false,
            Type = type,
            Content = "``",
            Start = start,
            End = next + 1,
        };

        if (!token.Success)
            return fail;

        next = token.End;
        success = Any(input, next) && input[next] == '`';

        if (!success)
            return fail;

        return new Token
        {
            Success = success,
            Type = type,
            Content = token.Content,
            Start = start,
            End = next + 1,
        };
    }

    public static Token Strike(string input, int start = 0)
    {
        TokenType type = TokenType.Strike;
        int next = start + 1;
        bool success = Any(input, next) && input[next] == '~';

        if (!success)
            return new Token
            {
                Success = false,
                Type = type,
                Content = "~",
                Start = start,
                End = next, // Do not consume
            };

        var token = Sequence(input, TokenType.Strike, '~', next);
        var fail = new Token
        {
            Success = false,
            Type = type,
            Content = "~~",
            Start = start,
            End = next + 1,
        };

        if (!token.Success)
            return fail;

        next = token.End;
        success = Any(input, next) && input[next] == '~';

        if (!success)
            return fail;

        return new Token
        {
            Success = success,
            Type = type,
            Content = token.Content,
            Start = start,
            End = next + 1,
        };
    }

    public static Token
    Sequence(
        string input,
        TokenType type,
        char endChar,
        int start = 0
    ) {
        // <est uan sin>
        // ^^
        // sn
        int next = start + 1;

        (bool success, int end) = ConsumeUnescaped(input, next, c => c != endChar);

        return new Token
        {
            Success = success,
            Type = type,
            Content = input[next..end],
            Start = start,

            // <est uan sin>
            // ^^           ^
            // sn           e
            End = end + 1,
        };
    }

    // <est uan sin>
    // ^
    // s
    public static (bool, int)
    ConsumeUnescaped(
        string input,
        int start,
        Predicate predicate,
        char escape = '\\'
    ) {
        // <est uan sin>
        // ^^          ^
        // sn          e
        (bool success, int end) = Consume(input, start, predicate);

        while (Any(input, end) && Escaped(input, end, escape))
            (success, end) = Consume(input, end, predicate);

        return (success, end);
    }

    public static Token WhiteSpace(string input, int start)
    {
        (_, int end) = Consume(input, start + 1, char.IsWhiteSpace);

        return new Token
        {
            Success = true,
            Type = TokenType.WhiteSpace,
            Content = input[start..end],
            Start = start,
            End = end,
        };
    }

    public static (bool, int) Consume(string input, int start, Predicate predicate)
    {
        bool fail;

        while ((fail = predicate(input[start])) && Any(input, start))
            start++;

        return (!fail, start);
    }
}

// // todo: consider removing
// public class Transcript : IEnumerable<string>
// {
//     private readonly IList<string> _lines = [];
// 
//     public string this[int index]
//     {
//         get { return _lines[index]; }
//         private set { _lines[index] = value; }
//     }
// 
//     public IEnumerator<string> GetEnumerator()
//     {
//         return _lines.GetEnumerator();
//     }
// 
//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return _lines.GetEnumerator();
//     }
// }


