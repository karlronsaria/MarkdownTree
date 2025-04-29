// See https://aka.ms/new-console-template for more information
using demo_PsMarkdownSyntaxTree;
using MarkdownTree;
using MarkdownTree.Lex;
using MarkdownTree.Parse;

// Console.WriteLine("Hello, world!");

IList<string> doc;

doc = [
    "# ``est``: uan sin",
    "- ``est``: uan sin",
    "  - ``est``: uan sin",
    "- ``est``: uan sin",
    "## ``est``: uan sin",
    "- ``est``: uan sin",
    "  - ``est``: uan sin",
    "- ``est``: uan sin",
];

foreach (var str in
    from o in Outline.Get(doc)
    where o is Outline
    from string s in TestOutline.GetStrings(((Outline)o) .Unfold() .Merge() .Fold() .Unfold() )
    select s
) {
    Console.WriteLine(str);
}

