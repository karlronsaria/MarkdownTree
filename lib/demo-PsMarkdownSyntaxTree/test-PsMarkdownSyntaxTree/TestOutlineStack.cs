using MarkdownTree.Parse;

namespace test_PsMarkdownSyntaxTree;

public class TestOutlineStack
{
    public class TestTree(int lineNumber) : Branching(lineNumber)
    {
        public required string Name { get; set; }

        public IEnumerable<string> Names()
        {
            yield return Name;

            foreach (var name in
                from TestTree child in Children
                from name in child.Names()
                select name
            )
                yield return name;

            yield break;
        }

        public override string ToString() =>
            $"{Name}: ({string.Join(", ", from c in Children select c.ToString())})";

        public override Needle? FindFirstSegment(ISegment.Predicate predicate) => null;
        public override IEnumerable<Needle> FindAllSegments(ISegment.Predicate predicate) => [];
    }

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void OutlineStack()
    {
        IList<(string, int)> mock = [
            ("est", 0),
            ("uan", 1),
            ("sin", 1),
            ("ter", 2),
            ("ius", 2),
            ("ira", 2),
            ("veh", 3),
            ("eme", 3),
            ("nti", 3),
            ("sep", 1),
            ("hir", 1),
            ("oth", 1),
        ];

        IList<string> expected = [
            "est: (uan: (), sin: (ter: (), ius: (), ira: (veh: (), eme: (), nti: ())), sep: (), hir: (), oth: ())"
        ];

        var myStack = new OutlineStack();

        foreach ((string word, int index) in mock)
            myStack.Put(
                new TestTree(0) { Name = word },
                index
            );

        (var forest, _) = myStack.Flush();

        for (int i = 0; i < forest.Count; i++)
            Assert.That(forest[i].ToString(), Is.EqualTo(expected[i]), $"Outline stack");
    }
}
