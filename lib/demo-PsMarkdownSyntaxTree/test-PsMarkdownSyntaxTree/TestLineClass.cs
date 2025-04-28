using MarkdownTree;

namespace test_PsMarkdownSyntaxTree;

public class TestLineClass
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void LineLex_LineClass()
    {
        IList<(string, object)> mock = [
            ("# Hello, Mario",               new { indent = 0, type = LineType.Heading, hashes = 1 }),
            ("Hello, Mario",                 new { indent = 0, type = LineType.Paragraph }),
            ("- Hello, Mario",               new { indent = 0, type = LineType.UnorderedList }),
            ("  - Hello, Mario",             new { indent = 2, type = LineType.UnorderedList }),
            ("    1. Hello, Mario",          new { indent = 4, type = LineType.OrderedList }),
            ("       ![est](./res/est.png)", new { indent = 7, type = LineType.Local }),
            ("       ```javascript",         new { indent = 7, type = LineType.CodeBlock, language = "javascript" }),
            ("       ```",                   new { indent = 7, type = LineType.CodeBlock, language = "" }),
            ("    2. Hello, Mario",          new { indent = 4, type = LineType.OrderedList }),
            ("    3. [ ] Hello, Mario?",     new { indent = 4, type = LineType.OrderedList, status = false }),
            ("    4. [x] Hello, Mario?",     new { indent = 4, type = LineType.OrderedList, status = true }),
            ("  - [ ] Hello, Mario?",        new { indent = 2, type = LineType.UnorderedList, status = false }),
            ("  - [d] Hello, Mario?",        new { indent = 2, type = LineType.UnorderedList }),
            ("  | est | uan | sin |",        new { indent = 2, type = LineType.TableRow }),
            ("===",                          new { indent = 0, type = LineType.Vinculum }),
            ("***",                          new { indent = 0, type = LineType.Vinculum }),
            ("---",                          new { indent = 0, type = LineType.Vinculum }),
            ("___",                          new { indent = 0, type = LineType.Vinculum }),
            ("## Hello, Mario",              new { indent = 0, type = LineType.Heading, hashes = 2 }),
            ("### Hello, Mario",             new { indent = 0, type = LineType.Heading, hashes = 3 }),
            ("#### Hello, Mario",            new { indent = 0, type = LineType.Heading, hashes = 4 }),
        ];

        foreach (var (text, expected) in mock)
        {
            var sublex = LineClass.Get(text);

            var indent = expected
                .GetType()
                .GetProperty("indent")?
                .GetValue(expected) ?? -1;

            var type = expected
                .GetType()
                .GetProperty("type")?
                .GetValue(expected) ?? LineType.None;

            Assert.Multiple(() =>
            {
                Assert.That(
                    sublex.Capture?.Groups["indent"].Value.Length ?? -1,
                    Is.EqualTo(indent),
                    $"indent of [\"{text}\"]"
                );

                Assert.That(
                    sublex.Type,
                    Is.EqualTo(type),
                    $"type of [\"{text}\"]"
                );

                switch (sublex.Type)
                {
                    case LineType.Heading:
                        var hashes = expected
                            .GetType()
                            .GetProperty("hashes")?
                            .GetValue(expected) ?? -1;

                        Assert.That(
                            sublex.Capture?.Groups["hashes"].Value.Length,
                            Is.EqualTo(hashes),
                            $"hashes of [\"{text}\"]"
                        );

                        break;
                    case LineType.CodeBlock:
                        var language = expected
                            .GetType()
                            .GetProperty("language")?
                            .GetValue(expected) ?? string.Empty;

                        Assert.That(
                            ((CodeBlockLineClass)sublex).Language,
                            Is.EqualTo(language)
                        );

                        break;
                    default:
                        break;
                }

                string content = text[(sublex.Capture?.Length ?? 0)..];

                if (sublex.Actionable)
                {
                    (int statusNumber, var boxCapture) = LineClass.GetStatus(content);

                    if (boxCapture.Success)
                    {
                        bool? expectedStatus = (bool?)expected
                            .GetType()
                            .GetProperty("status")?
                            .GetValue(expected);

                        bool? status = statusNumber == -1
                            ? null
                            : statusNumber == 1;

                        Assert.That(
                            status,
                            Is.EqualTo(expectedStatus),
                            $"status of [\"{text}\"]"
                        );

                        content = content[boxCapture.Length..];
                    }
                }
            });
        }
    }
}

