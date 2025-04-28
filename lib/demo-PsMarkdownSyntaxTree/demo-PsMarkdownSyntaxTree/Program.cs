// See https://aka.ms/new-console-template for more information
using demo_PsMarkdownSyntaxTree;
using MarkdownTree;
using MarkdownTree.Lex;
using MarkdownTree.Parse;

// Console.WriteLine("Hello, world!");

IList<string> doc;

doc = [
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
];

foreach (var str in
    from o in Outline.Get(doc)
    where o is Outline
    from string s in TestOutline.GetStrings(((Outline)o) ) // .Unfold() .Merge() .Fold() .Unfold() )
    select s
) {
    Console.WriteLine(str);
}

