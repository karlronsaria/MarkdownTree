using System.Text.RegularExpressions;

namespace MarkdownTree;

public enum LineType
{
    Heading,
    Vinculum,
    UnorderedList,
    OrderedList,
    TableRow,
    Define,
    Local,
    CodeBlock,
    WhiteSpace,
    Paragraph,
    None, // Error type
}

public partial class LineClass
{
    public delegate Regex MatchLine();

    public static IList<MatchLine> LineMatches =>
    [
        Heading,
        Vinculum,
        UnorderedList,
        OrderedList,
        TableRow,
        Define,
        Local,
        CodeBlock,
        WhiteSpace,
        Paragraph,
    ];

    public LineType Type { get; set; } = LineType.Paragraph;
    public bool Actionable { get; set; } = false;
    public int Status { get; set; } = -1;
    public int NextIndentSize { get; set; } = 2;
    public int Length { get; set; } = 0;
    public int Indent { get; set; } = 0;
    public Match? Capture { get; set; }

    public static (int, Match) GetStatus(string text)
    {
        Match capture = Checkbox().Match(text);

        return !capture.Success
            ? (-1, capture)
            : capture.Groups["status"].Value switch
              {
                  " " => (0, capture),
                  "x" => (1, capture),
                  _ => (-1, capture),
              };
    }

    public static LineClass Get(string text)
    {
        int i = 0;
        bool success = false;
        Match? capture = null;

        while (!success && i < LineMatches.Count)
        {
            capture = LineMatches[i]().Match(text);
            success = capture.Success;
            ++i;
        }

        LineType type = (LineType)(Enum.GetValues<LineType>().GetValue(i - 1) ?? LineType.Paragraph);
        int nextStart = capture?.Length ?? 0;

        var lineClass = type switch
        {
            LineType.WhiteSpace => new WhiteSpaceLineClass
            {
                Type = type,
            },

            LineType.Heading => new HeadingLineClass
            {
                Type = type,
                Level = capture?.Groups["hashes"].Length ?? 0,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = nextStart,
                Capture = capture,
            },

            LineType.CodeBlock => new CodeBlockLineClass
            {
                Type = type,
                Language = capture?.Groups["language"].Value ?? string.Empty,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = nextStart,
                Capture = capture,
            },

            LineType.UnorderedList => new LineClass
            {
                Type = type,
                Actionable = true,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = nextStart,
                Capture = capture,
            },

            LineType.OrderedList => new LineClass
            {
                Type = type,
                Actionable = true,
                NextIndentSize = 3,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = nextStart,
                Capture = capture,
            },

            LineType.Paragraph => new LineClass
            {
                Type = type,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = capture?.Groups["indent"].Length ?? 0,
                Capture = capture,
            },

            _ => new LineClass
            {
                Type = type,
                Indent = capture?.Groups["indent"].Length ?? 0,
                Length = nextStart,
                Capture = capture,
            }
        };

        if (lineClass.Actionable)
        {
            (lineClass.Status, Match boxCapture) = GetStatus(text[lineClass.Length..]);
            lineClass.Length += boxCapture?.Length ?? 0;
        }

        return lineClass;
    }

    [GeneratedRegex(@"^\s*\[(?<status>[^\[\]])\]\s")]
    private static partial Regex Checkbox();

    [GeneratedRegex(@"^(?<hashes>#+)\s")]
    private static partial Regex Heading();

    [GeneratedRegex(@"(?<indent>^\s*)(-|=|\*|_){3}")]
    private static partial Regex Vinculum();

    [GeneratedRegex(@"^(?<indent>\s*)(?<bullet>-|\+|\*)\s")]
    private static partial Regex UnorderedList();

    [GeneratedRegex(@"^(?<indent>\s*)(?<number>[1-9][0-9]*)\.\s")]
    private static partial Regex OrderedList();

    [GeneratedRegex(@"^(?<indent>\s*)\|")]
    private static partial Regex TableRow();

    [GeneratedRegex(@"^(?<indent>\s+):\s")]
    private static partial Regex Define();

    [GeneratedRegex(@"^(?<indent>\s*)!\[")]
    private static partial Regex Local();

    [GeneratedRegex(@"^(?<indent>\s*)```(?<language>\S*)")]
    private static partial Regex CodeBlock();

    [GeneratedRegex(@"^(?<indent>\s*)$")]
    private static partial Regex WhiteSpace();

    [GeneratedRegex(@"^(?<indent>\s*).*$")]
    private static partial Regex Paragraph();
}

public class CodeBlockLineClass : LineClass
{
    public string Language { get; set; } = string.Empty;
}

public class HeadingLineClass : LineClass
{
    public int Level { get; set; }
}

public class WhiteSpaceLineClass : LineClass;

