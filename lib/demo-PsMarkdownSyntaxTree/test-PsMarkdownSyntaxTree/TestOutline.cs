using MarkdownTree.Parse;

namespace test_PsMarkdownSyntaxTree;

public class TestOutline
{
    [SetUp]
    public void Setup()
    {
    }

    private static IEnumerable<string> GetStrings(Branching tree, int level = 0, int indent = 2)
    {
        string space = string.Concat(Enumerable.Repeat(" ", level * indent));
        
        if (tree is CodeBlock e)
            foreach (string str in e.Lines)
                yield return $"{space}{e.Language}({str})";
        else if (tree is Table t)
        {
            yield return     $"{space}TableHead ({t.Headings})";

            foreach (Row row in t.Rows)
                yield return $"{space}TableRow  ({row})";
        }
        else if (tree is Outline o)
            yield return $"{space}{o.LineType}({o.Name})";

        foreach (var child in tree.Children)
            if (child is Branching p)
                foreach (string str in GetStrings(p, level + 1, indent))
                    yield return str;
    }

    [Test]
    public void TestOutlineConstructor()
    {
        IList<(IList<string>, IList<string>, IList<string>, IList<string>, IList<string>, IList<string>)>
        data = [
            (
                [
                    "# est uan sin",
                    "  ",
                    "It's all I have to bring today",
                    "  ",
                    "- ter ius ira",
                    "  - veh eme nti",
                    "    - [ ] est: uan: sin",
                    "      This and my heart beside",
                    "      This and my heart and all the fields",
                    "  - ter ius ira",
                    "  ",
                    "## veh: eme nti",
                    "  ",
                    "And all the meadows wide",
                    "  ",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est: uan: sin)",
                    "        Paragraph(This and my heart beside)",
                    "        Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh: eme nti)",
                    "    Paragraph(And all the meadows wide)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti: est: uan: sin)",
                    "      Paragraph(This and my heart beside)",
                    "      Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh: eme nti)",
                    "    Paragraph(And all the meadows wide)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                ]
            ),
            (
                [
                    "  ",
                    "# est uan sin",
                    "- est: uan: sin: ter",
                    "  Why",
                    "  ",
                    "- est: uan: sin: ter",
                    "  do",
                    "  ",
                    "- est: uan: sin: ter",
                    "  they",
                    "  ",
                    "- est: uan: sin: ter",
                    "  call",
                    "  ",
                    "- est: uan: sin: ter",
                    "  it",
                    "  ",
                    "- est: uan: sin: ter",
                    "  oven",
                    "  ",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(Why)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(do)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(they)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(call)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(it)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    Paragraph(oven)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(Why)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(do)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(they)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(call)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(it)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(oven)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(Why)",
                    "          Paragraph(do)",
                    "          Paragraph(they)",
                    "          Paragraph(call)",
                    "          Paragraph(it)",
                    "          Paragraph(oven)",
                ],
                [
                    "Heading(est uan sin: est: uan: sin: ter)",
                    "  Paragraph(Why)",
                    "  Paragraph(do)",
                    "  Paragraph(they)",
                    "  Paragraph(call)",
                    "  Paragraph(it)",
                    "  Paragraph(oven)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          Paragraph(Why)",
                    "          Paragraph(do)",
                    "          Paragraph(they)",
                    "          Paragraph(call)",
                    "          Paragraph(it)",
                    "          Paragraph(oven)",
                ]
            ),
            (
                [
                    "# est uan sin",
                    "  ",
                    "- est: uan: sin: ter",
                    "  - ius: ira: veh",
                    "    - eme: nti",
                    "      Hello",
                    "  ",
                    "- est: uan: sin: ter",
                    "  - ius: ira: veh",
                    "    - eme: nti",
                    "      Mario",
                    "  ",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    UnorderedList(ius: ira: veh)",
                    "      UnorderedList(eme: nti)",
                    "        Paragraph(Hello)",
                    "  UnorderedList(est: uan: sin: ter)",
                    "    UnorderedList(ius: ira: veh)",
                    "      UnorderedList(eme: nti)",
                    "        Paragraph(Mario)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          UnorderedList(ius)",
                    "            UnorderedList(ira)",
                    "              UnorderedList(veh)",
                    "                UnorderedList(eme)",
                    "                  UnorderedList(nti)",
                    "                    Paragraph(Hello)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          UnorderedList(ius)",
                    "            UnorderedList(ira)",
                    "              UnorderedList(veh)",
                    "                UnorderedList(eme)",
                    "                  UnorderedList(nti)",
                    "                    Paragraph(Mario)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          UnorderedList(ius)",
                    "            UnorderedList(ira)",
                    "              UnorderedList(veh)",
                    "                UnorderedList(eme)",
                    "                  UnorderedList(nti)",
                    "                    Paragraph(Hello)",
                    "                    Paragraph(Mario)",
                ],
                [
                    "Heading(est uan sin: est: uan: sin: ter: ius: ira: veh: eme: nti)",
                    "  Paragraph(Hello)",
                    "  Paragraph(Mario)",
                ],
                [
                    "Heading(est uan sin)",
                    "  UnorderedList(est)",
                    "    UnorderedList(uan)",
                    "      UnorderedList(sin)",
                    "        UnorderedList(ter)",
                    "          UnorderedList(ius)",
                    "            UnorderedList(ira)",
                    "              UnorderedList(veh)",
                    "                UnorderedList(eme)",
                    "                  UnorderedList(nti)",
                    "                    Paragraph(Hello)",
                    "                    Paragraph(Mario)",
                ]
            ),
            (
                [
                    "# est uan sin",
                    "It's all I have to bring today",
                    "- ter ius ira",
                    "             ",
                    "  - veh eme nti",
                    "    - [ ] est: uan: sin",
                    "             ",
                    "      This and my heart beside",
                    "      This and my heart and all the fields",
                    "             ",
                    "  - ter ius ira",
                    "             ",
                    "## veh: eme nti",
                    "             ",
                    "And all the meadows wide",
                    "             ",
                    "  ```javascript",
                    "  function what() {",
                    "      console.log(\"what\")",
                    "  }",
                    "  ```",
                    "  ",
                    "  - upgrade complete",
                    "- research complete",
                    "  ",
                    "  | est | uan | sin |",
                    "  | --- | --- | --- |",
                    "  | ter | ius | ira |",
                    "  | veh | eme | nti |",
                    "  | sep | hir | oth |",
                    "  ",
                    "- summoning is complete",
                    "  - we demand additional lumber",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est: uan: sin)",
                    "        Paragraph(This and my heart beside)",
                    "        Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh: eme nti)",
                    "    Paragraph(And all the meadows wide)",
                    "      javascript(function what() {)",
                    "      javascript(    console.log(\"what\"))",
                    "      javascript(})",
                    "      UnorderedList(upgrade complete)",
                    "    UnorderedList(research complete)",
                    "      TableHead ( est | uan | sin )",
                    "      TableRow  ( ter | ius | ira )",
                    "      TableRow  ( veh | eme | nti )",
                    "      TableRow  ( sep | hir | oth )",
                    "    UnorderedList(summoning is complete)",
                    "      UnorderedList(we demand additional lumber)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                    "        javascript(function what() {)",
                    "        javascript(    console.log(\"what\"))",
                    "        javascript(})",
                    "        UnorderedList(upgrade complete)",
                    "      UnorderedList(research complete)",
                    "        TableHead ( est | uan | sin )",
                    "        TableRow  ( ter | ius | ira )",
                    "        TableRow  ( veh | eme | nti )",
                    "        TableRow  ( sep | hir | oth )",
                    "      UnorderedList(summoning is complete)",
                    "        UnorderedList(we demand additional lumber)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                    "        javascript(function what() {)",
                    "        javascript(    console.log(\"what\"))",
                    "        javascript(})",
                    "        UnorderedList(upgrade complete)",
                    "      UnorderedList(research complete)",
                    "        TableHead ( est | uan | sin )",
                    "        TableRow  ( ter | ius | ira )",
                    "        TableRow  ( veh | eme | nti )",
                    "        TableRow  ( sep | hir | oth )",
                    "      UnorderedList(summoning is complete)",
                    "        UnorderedList(we demand additional lumber)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti: est: uan: sin)",
                    "      Paragraph(This and my heart beside)",
                    "      Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh: eme nti)",
                    "    Paragraph(And all the meadows wide)",
                    "      javascript(function what() {)",
                    "      javascript(    console.log(\"what\"))",
                    "      javascript(})",
                    "      UnorderedList(upgrade complete)",
                    "    UnorderedList(research complete)",
                    "      TableHead ( est | uan | sin )",
                    "      TableRow  ( ter | ius | ira )",
                    "      TableRow  ( veh | eme | nti )",
                    "      TableRow  ( sep | hir | oth )",
                    "    UnorderedList(summoning is complete: we demand additional lumber)",
                ],
                [
                    "Heading(est uan sin)",
                    "  Paragraph(It's all I have to bring today)",
                    "  UnorderedList(ter ius ira)",
                    "    UnorderedList(veh eme nti)",
                    "      UnorderedList(est)",
                    "        UnorderedList(uan)",
                    "          UnorderedList(sin)",
                    "            Paragraph(This and my heart beside)",
                    "            Paragraph(This and my heart and all the fields)",
                    "    UnorderedList(ter ius ira)",
                    "  Heading(veh)",
                    "    UnorderedList(eme nti)",
                    "      Paragraph(And all the meadows wide)",
                    "        javascript(function what() {)",
                    "        javascript(    console.log(\"what\"))",
                    "        javascript(})",
                    "        UnorderedList(upgrade complete)",
                    "      UnorderedList(research complete)",
                    "        TableHead ( est | uan | sin )",
                    "        TableRow  ( ter | ius | ira )",
                    "        TableRow  ( veh | eme | nti )",
                    "        TableRow  ( sep | hir | oth )",
                    "      UnorderedList(summoning is complete)",
                    "        UnorderedList(we demand additional lumber)",
                ]
            ),
            (
                [
                    "# ``est``: uan sin",
                    "- ``est``: uan sin",
                    "  - ``est``: uan sin",
                    "- ``est``: uan sin",
                    "## ``est``: uan sin",
                    "- ``est``: uan sin",
                    "  - ``est``: uan sin",
                    "- ``est``: uan sin",
                ],
                [
                    "Heading(``est``: uan sin)",
                    "  UnorderedList(``est``: uan sin)",
                    "    UnorderedList(``est``: uan sin)",
                    "  UnorderedList(``est``: uan sin)",
                    "  Heading(``est``: uan sin)",
                    "    UnorderedList(``est``: uan sin)",
                    "      UnorderedList(``est``: uan sin)",
                    "    UnorderedList(``est``: uan sin)",
                ],
                [
                    "Heading(``est``)",
                    "  UnorderedList(uan sin)",
                    "    UnorderedList(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "    UnorderedList(``est``)",
                    "      UnorderedList(uan sin)",
                    "    Heading(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "            UnorderedList(``est``)",
                    "              UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                ],
                [
                    "Heading(``est``)",
                    "  UnorderedList(uan sin)",
                    "    UnorderedList(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "    Heading(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "            UnorderedList(``est``)",
                    "              UnorderedList(uan sin)",
                ],
                [
                    "Heading(``est``: uan sin)",
                    "  UnorderedList(``est``: uan sin: ``est``: uan sin)",
                    "  Heading(``est``: uan sin: ``est``: uan sin: ``est``: uan sin)",
                ],
                [
                    "Heading(``est``)",
                    "  UnorderedList(uan sin)",
                    "    UnorderedList(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "    Heading(``est``)",
                    "      UnorderedList(uan sin)",
                    "        UnorderedList(``est``)",
                    "          UnorderedList(uan sin)",
                    "            UnorderedList(``est``)",
                    "              UnorderedList(uan sin)",
                ]
            )
        ];

        int mockIndex = 0;

        foreach ((
            var mock,
            var expected,
            var expectedUnfolded,
            var expectedMerged,
            var expectedRefolded,
            var expectedReunfolded
        ) in data
        ) {
            IList<string> actual =
                [.. from o in Outline.Get(mock)
                where o is Outline
                from string s in GetStrings(((Outline)o))
                select s];

            Assert.That(actual, Has.Count.EqualTo(expected.Count), $"Outline tree count {mockIndex}");

            for (int i = 0; i < (int)Math.Min(actual.Count, expected.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"Outline tree Line {i}");

            actual =
                [.. from o in Outline.Get(mock)
                where o is Outline
                from string s in GetStrings(((Outline)o).CascadeUnfold())
                select s];

            Assert.That(actual, Has.Count.EqualTo(expectedUnfolded.Count), $"Unfolded outline tree count {mockIndex}");

            for (int i = 0; i < (int)Math.Min(actual.Count, expectedUnfolded.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expectedUnfolded[i]), $"Unfolded outline tree Line {i}");

            actual =
                [.. from o in Outline.Get(mock)
                where o is Outline
                from string s in GetStrings(((Outline)o).CascadeUnfold().CascadeMerge())
                select s];

            Assert.That(actual, Has.Count.EqualTo(expectedMerged.Count), $"Merged outline tree count");

            for (int i = 0; i < (int)Math.Min(actual.Count, expectedMerged.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expectedMerged[i]), $"Merged outline tree Line {i}");

            actual =
                [.. from o in Outline.Get(mock)
                where o is Outline
                from string s in GetStrings(((Outline)o).CascadeUnfold().CascadeMerge().CascadeFold())
                select s];

            Assert.That(actual, Has.Count.EqualTo(expectedRefolded.Count), $"Refolded merged outline tree count {mockIndex}");

            for (int i = 0; i < (int)Math.Min(actual.Count, expectedRefolded.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expectedRefolded[i]), $"Refolded merged outline tree Line {i}");

            actual =
                [.. from o in Outline.Get(mock)
                where o is Outline
                from string s in GetStrings(((Outline)o).CascadeUnfold().CascadeMerge().CascadeFold().CascadeUnfold())
                select s];

            Assert.That(actual, Has.Count.EqualTo(expectedReunfolded.Count), $"Unfolded, merged, folded, and unfolded outline tree count {mockIndex}");

            for (int i = 0; i < (int)Math.Min(actual.Count, expectedReunfolded.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expectedReunfolded[i]), $"Unfolded, merged, folded, and unfolded outline tree Line {i}");

            mockIndex++;
        }
    }

    [Test]
    public void TestConvertToMarkdown()
    {
        IList<(IList<string>, IList<string>)>
        data = [
            (
                [
                    "# 2025-03-27",
                    "",
                    "- [ ] emp: CompanyName: task",
                    "  - every: day",
                    "  - with journal",
                    "  - download 1 lesson from CsFirst",
                    "  - link",
                    "    - url: <https://csfirst.withgoogle.com/c/cs-first/en/curriculum.html>",
                    "    - login",
                    "      - mail: <me.me@gmail.com>",
                    "    - retrieved: 2025-03-27",
                    "  - ~~define~~"
                ],
                [
                    "# 2025-03-27",
                    "",
                    "- [ ] emp: CompanyName: task",
                    "  - every: day",
                    "  - with journal",
                    "  - download 1 lesson from CsFirst",
                    "  - link",
                    "    - url: <https://csfirst.withgoogle.com/c/cs-first/en/curriculum.html>",
                    "    - login",
                    "      - mail: <me.me@gmail.com>",
                    "    - retrieved: 2025-03-27",
                    "  - ~~define~~",
                ]
            )
        ];

        int mockIndex = 0;

        foreach ((IList<string> mock, IList<string> expected) in data)
        {
            IList<string> actual =
                [.. from o in Outline.Get(mock)
                    where o is Outline
                    from string s in ((Outline)o).ToMarkdown()
                    select s];

            Assert.That(actual, Has.Count.EqualTo(expected.Count), $"ToMarkdown count {mockIndex}");

            for (int i = 0; i < (int)Math.Min(actual.Count, expected.Count); ++i)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"ToMarkdown {i}");

            mockIndex++;
        }
    }

    /*
    [Test]
    public void TestDropBranch()
    {
        IList<(IList<string>, IList<string>)>
        data = [
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
                ]
            )
        ];

    }
    */
}






