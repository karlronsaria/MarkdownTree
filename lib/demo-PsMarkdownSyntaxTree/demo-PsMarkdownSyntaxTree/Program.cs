// See https://aka.ms/new-console-template for more information
using demo_PsMarkdownSyntaxTree;
using MarkdownTree;
using MarkdownTree.Lex;
using MarkdownTree.Parse;

using System.Management.Automation;

// Console.WriteLine("Hello, world!");

IList<string> doc;

doc = [
    "# 2025-03-27",
    "",
    "- [ ] emp: CodeNinjas: task",
    "  - every: day",
    "  - with journal",
    "  - download 1 lesson from CsFirst",
    "  - link",
    "    - url: <https://csfirst.withgoogle.com/c/cs-first/en/curriculum.html>",
    "    - login",
    "      - mail: <cnladera@gmail.com>",
    "    - retrieved: 2025-03-27",
    "  - ~~define~~"
];

// doc = [
//     "# est",
//     "![image](<./res/image.png>)",
//     "- what",
//     "- the",
//     "  1. est uan sin",
//     "     ![image](<./res/image.png>)",
//     "  2. [ ] ter ius ira",
//     "     ![image2](<./res/image2.png>)",
//     "     ![image3](<./res/image3.png>)",
//     "  3. veh eme nti",
//     "     ![image4](<./res/image4.png>)",
//     "  4. sep hir oth",
//     "- he",
//     "  1. est uan sin",
//     "  2. ter ius ira",
//     "     | est | uan | sin |",
//     "     | --- | --- | --- |",
//     "     | ter | ius | ira |",
//     "     | veh | eme | nti |",
//     "     ![image](<./res/image.png>)",
//     "- it",
//     "- just",
//     "- works",
//     "## uan",
//     "![image](<./res/image.png>)",
// ];

foreach (var str in 
    from o in Outline.Get(doc)
    where o is Outline
    from string s in ((Outline)o) .Unfold() .Merge() .ToMarkdown()
    select s
) {
    Console.WriteLine(str);
}

// foreach (var tree in Outline.Get(doc))
// {
//     ITree worktree = tree is Outline outline
//         ? outline.Unfold().Merge()
//         : tree;
// 
//     var obj = new PSObject();
//     (new TestOutline()).AddProperty(obj, worktree);
//     TestOutline.Write(obj);
// }

// foreach (var str in
//     from o in Outline.Get(doc)
//     where o is Outline
//     from string s in TestOutline.GetStrings(((Outline)o) .Unfold() .Merge() ) // .Fold() .Unfold() )
//     select s
// ) {
//     Console.WriteLine(str);
// }

