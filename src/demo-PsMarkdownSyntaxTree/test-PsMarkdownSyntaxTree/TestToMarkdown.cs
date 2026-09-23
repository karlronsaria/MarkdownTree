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
                    "## est",
                    "",
                    "- when",
                    "  - sat",
                    "- where",
                    "  - home",
                    "",
                    "## uan",
                    "",
                    "- when",
                    "  - sun",
                    "- where",
                    "  - home",
                    "",
                    "## sin",
                    "",
                    "- when",
                    "  - mon",
                    "- where",
                    "  - work",
                ]
            ),
        ];

        int mockIndex = 0;

        foreach ((IList<string> mock, IList<string> expected) in data)
        {
            IList<ITree> forest =
                [.. from o in Outline.Scan(mock)
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
            (
                [
                    "# estuans",
                    "",
                    "inter",
                    ": iu-sir",
                    ": - avehe: 2026-09-22",
                    "",
                    "mentiseph",
                    ": IROT Hestuans Inter",
                    ": - iusir: 2026-07-23",
                    "",
                    "avehemen",
                    ": Tise Phirot Hestu Ansin",
                    ": Teri Usirav Ehemen Tisep Hiroth estuans",
                    ": - [inte rius](./irav/ehe/ment_-_2023-02-20_IsephiroTheStuans.in)",
                    ": - teriu: 2026-07-14",
                    "",
                    "siraveh",
                    ": Emen Tiseph Iroth estu",
                    ": Ansi Nteriu Sirav Eheme Ntisep Hirothe",
                    ": - [stua nsin](./teri/usi/rave_-_2023-02-20_HementisEphiRothe.st)",
                    ": - uansi: 2026-07-14",
                    "",
                ],
                [
                    "# estuans",
                    "",
                    "inter",
                    ": iu-sir",
                    ": - avehe: 2026-09-22",
                    "",
                    "mentiseph",
                    ": IROT Hestuans Inter",
                    ": - iusir: 2026-07-23",
                    "",
                    "avehemen",
                    ": Tise Phirot Hestu Ansin",
                    ": Teri Usirav Ehemen Tisep Hiroth estuans",
                    ": - [inte rius](./irav/ehe/ment_-_2023-02-20_IsephiroTheStuans.in)",
                    ": - teriu: 2026-07-14",
                    "",
                    "siraveh",
                    ": Emen Tiseph Iroth estu",
                    ": Ansi Nteriu Sirav Eheme Ntisep Hirothe",
                    ": - [stua nsin](./teri/usi/rave_-_2023-02-20_HementisEphiRothe.st)",
                    ": - uansi: 2026-07-14",
                    "",
                ]
            ),
            (
                [
                    "# estua",
                    "- nsin: Teriusi Raveheme",
                    "- ntis",
                    "- ephi: roth",
                    "- est",
                    "  - uan: <sinte://riu.siraveh.eme>",
                    "  - ntise: <phiroth@estua.nsi>",
                    "  - nteriusir: 2023-05-10",
                    "- aveh",
                    "  - ementise",
                    "    - [x] hir-2025-04-09",
                    "    - [x] 2023-10-04",
                    "    - [x] 2023-05-12",
                ],
                [
                    "# estua",
                    "",
                    "- nsin: Teriusi Raveheme",
                    "- ntis",
                    "- ephi: roth",
                    "- est",
                    "  - uan: <sinte://riu.siraveh.eme>",
                    "  - ntise: <phiroth@estua.nsi>",
                    "  - nteriusir: 2023-05-10",
                    "- aveh",
                    "  - ementise",
                    "    - [x] hir-2025-04-09",
                    "    - [x] 2023-10-04",
                    "    - [x] 2023-05-12",
                ]
            ),
        ];

        foreach ((IList<string> mock, IList<string> expected) in data)
        {
            IList<string> actual = [..
                from o in Outline.Scan(mock)
                from string s in ((Outline)o).ToMarkdown()
                select s
            ];

            for (int i = 0; i < actual.Count; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"ToMarkdown Line {i}");
        }
    }
}
