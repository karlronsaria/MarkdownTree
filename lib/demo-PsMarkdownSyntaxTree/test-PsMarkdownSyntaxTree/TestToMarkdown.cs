using MarkdownTree.Parse;

namespace test_PsMarkdownSyntaxTree;

public class TestToMarkdown
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test_Unfold_MergeOnProperty_ConvertToMarkdown()
    {
        IList<(IList<string>, IList<string>)> data = [
            (
                [
                    "# sched: est",
                    "- when: sat",
                    "- where: home",
                    "# sched: uan",
                    "- when: sun",
                    "- where: home",
                    "# sched: sin",
                    "- when: mon",
                    "- where: work",
                ],
                [
                    "# sched",
                    "",
                    "- est",
                    "  - when",
                    "    - sat",
                    "  - where",
                    "    - home",
                    "- uan",
                    "  - when",
                    "    - sun",
                    "  - where",
                    "    - home",
                    "- sin",
                    "  - when",
                    "    - mon",
                    "  - where",
                    "    - work",
                ]
            ),
        ];

        int mockIndex = 0;

        foreach ((IList<string> mock, IList<string> expected) in data)
        {
            IList<ITree> forest =
                [.. from o in Outline.Get(mock)
                    where o is Outline
                    select ((Outline)o).CascadeUnfold() as ITree];

            forest = Outline.Merge(forest);

            forest =
                [.. from o in forest
                    where o is Outline
                    select ((Outline)o).MergeChildren(c => ((Outline)c).Name == "sched")];

            IList<string> actual =
                [.. from tree in forest
                    where tree is IMarkdownWritable
                    from string s in ((IMarkdownWritable)tree).ToMarkdown()
                    select s];

            Assert.That(actual, Has.Count.EqualTo(expected.Count), $"ToMarkdown Item Count {mockIndex}");

            for (int i = 0; i < actual.Count; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"ToMarkdown {mockIndex} Line {i}");

            mockIndex++;
        }
    }

    [Test]
    public void TestOutlineToMarkdown()
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
            ),
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
