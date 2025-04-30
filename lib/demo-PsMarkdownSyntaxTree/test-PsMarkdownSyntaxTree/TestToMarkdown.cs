using MarkdownTree.Parse;

namespace test_PsMarkdownSyntaxTree;

public class TestToMarkdown
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        IList<(IList<string>, IList<string>)> data = [
            (
                [
                    "# est",
                    "![image](<./res/image.png>)",
                    "- what",
                    "- the",
                    "  1. est uan sin",
                    "     ![image](<./res/image.png>)",
                    "  2. [ ] ter ius ira",
                    "     ![image2](<./res/image2.png>)",
                    "     ![image3](<./res/image3.png>)",
                    "  3. veh eme nti",
                    "     ![image4](<./res/image4.png>)",
                    "  4. sep hir oth",
                    "- he",
                    "  1. est uan sin",
                    "  2. ter ius ira",
                    "     | est | uan | sin |",
                    "     | --- | --- | --- |",
                    "     | ter | ius | ira |",
                    "     | veh | eme | nti |",
                    "     ![image](<./res/image.png>)",
                    "- it",
                    "- just",
                    "- works",
                    "## uan",
                    "![image](<./res/image.png>)",
                ],
                [
                    "# est",
                    "",
                    "![image](<./res/image.png>)",
                    "",
                    "- what",
                    "- the",
                    "  1. est uan sin",
                    "",
                    "     ![image](<./res/image.png>)",
                    "",
                    "  2. [ ] ter ius ira",
                    "",
                    "     ![image2](<./res/image2.png>)",
                    "     ![image3](<./res/image3.png>)",
                    "",
                    "  3. veh eme nti",
                    "",
                    "     ![image4](<./res/image4.png>)",
                    "",
                    "  4. sep hir oth",
                    "- he",
                    "  1. est uan sin",
                    "  2. ter ius ira",
                    "",
                    "     | est | uan | sin |",
                    "     |-----|-----|-----|",
                    "     | ter | ius | ira |",
                    "     | veh | eme | nti |",
                    "",
                    "     ![image](<./res/image.png>)",
                    "",
                    "- it",
                    "- just",
                    "- works",
                    "",
                    "## uan",
                    "",
                    "![image](<./res/image.png>)",
                    "",
                ]
            )
        ];

        foreach ((IList<string> mock, IList<string> expected) in data)
        {
            IList<string> actual = [..
                from o in Outline.Get(mock)
                from string s in ((Outline)o).ToMarkdown()
                select s
            ];

            Assert.That(actual, Has.Count.EqualTo(expected.Count), $"ToMarkdown Item Count");

            for (int i = 0; i < actual.Count; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"ToMarkdown Line {i}");
        }
    }
}
