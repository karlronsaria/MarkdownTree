using MarkdownTree;
using MarkdownTree.Lex;

namespace test_PsMarkdownSyntaxTree;

public class TestToken
{
    private static string TestString(LineClass lex)
    {
        string content = lex.Capture?.Value ?? string.Empty;

        string status = !lex.Actionable || lex.Status == -1
            ? string.Empty
            : lex.Status == 1
                ? "(completed)"
                : "(todo)";

        return $"{lex.Type}{status}[{content}]";
    }

    private static string TestString(Token token) => $"{token.Type}[{token.Content}]";

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestTokenize()
    {
        IList<(string, IList<string>)> data = [
            (
                "    - [ ] what: the ~~est uan sin~~ ter ius ira ``veh eme nti`` [est uan sin](<ter ius ira>) \"(veh <eme [nti\"",
                [
                    "UnorderedList(todo)[    - ]",
                    "Text[what]",
                    "Colon[: ]",
                    "Text[the ]",
                    "Strike[est uan sin]",
                    "Text[ ter ius ira ]",
                    "InlineCode[veh eme nti]",
                    "Text[ ]",
                    "OpenBox[[]",
                    "Text[est uan sin]",
                    "CloseBox[]]",
                    "OpenLink[(]",
                    "Hyperlink[ter ius ira]",
                    "CloseLink[)]",
                    "Text[ ]",
                    "String[(veh <eme [nti]",
                    "EndOfLine[]",
                ]
            ),
            (
                "# est uan sin",
                [
                    "Heading[# ]",
                    "Text[est uan sin]",
                    "EndOfLine[]",
                ]
            ),
            (
                "It's all I have to bring today",
                [
                    "Paragraph[It's all I have to bring today]",
                    "Text[It's all I have to bring today]",
                    "EndOfLine[]",
                ]
            ),
            (
                "- replay <",
                [
                    "UnorderedList[- ]",
                    "Text[replay ]",
                    "Text[<]",
                    "EndOfLine[]",
                ]
            ),
            (
                "- replay <- what the heck is this?",
                [
                    "UnorderedList[- ]",
                    "Text[replay ]",
                    "Text[<- what the heck is this?]",
                    "EndOfLine[]",
                ]
            ),
            (
                "![2025-04-06-183044](./res/2025-04-06-183044.png)",
                [
                    "ImageMacro[![]",
                    "Text[2025-04-06-183044]",
                    "CloseBox[]]",
                    "OpenLink[(]",
                    "Text[./res/2025-04-06-183044.png]",
                    "CloseLink[)]",
                    "EndOfLine[]",
                ]
            ),
            (
                "![2025-04-06-183044](<./res/2025-04-06-183044.png>)",
                [
                    "ImageMacro[![]",
                    "Text[2025-04-06-183044]",
                    "CloseBox[]]",
                    "OpenLink[(]",
                    "Hyperlink[./res/2025-04-06-183044.png]",
                    "CloseLink[)]",
                    "EndOfLine[]",
                ]
            ),
            (
                "- ``/Python31X/``",
                [
                    "UnorderedList[- ]",
                    "InlineCode[/Python31X/]",
                    "EndOfLine[]",
                ]
            ),
        ];

        foreach ((string mock, var expected) in data)
        {
            int index = 0;
            var lex = LineClass.Get(mock);

            string content = TestString(lex);
            Assert.That(content, Is.EqualTo(expected[index++]));

            foreach (var token in Token.Tokenize(mock, lex.Type, lex.Length))
                Assert.That(TestString(token), Is.EqualTo(expected[index++]));
        }
    }
}

