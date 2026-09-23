// See https://aka.ms/new-console-template for more information
using demo_PsMarkdownSyntaxTree;
using MarkdownTree;
using MarkdownTree.Lex;
using MarkdownTree.Parse;

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Linq;

// // (karlr 2026-09-01)
// foreach (var path in
//     from Outline tree in Outline.Scan([
//         "# howto: ImageMagick Auto-trim",
//         "",
//         "Remove edges by color of each corner pixel",
//         "",
//         "## link",
//         "",
//         "- url: <https://www.imagemagick.org/script/command-line-options.php#trim>",
//         "- retrieved: 2023-12-12",
//         "",
//         "## example",
//         "",
//         "```shell",
//         "magick convert -trim +repage *.png",
//         "```",
//     ])
//     from path in tree.CascadeUnfold().CascadeMerge().PathOfAll(b =>
//     {
//         if (b is Outline branch)
//             return branch.Content.WhereAll(s => s.TokenType == TokenType.Hyperlink).ToList().Count != 0;
// 
//         return false;
//     })
//     select path
// ) {
//     Console.Write(string.Join(" ", path));
// }
// 
// Console.WriteLine();


// // (karlr 2026-08-28)
// foreach (Outline tree in Outline.Scan(["~~vscode\\~~"]))
//     foreach (var item in tree.ToMarkdown())
//         Console.WriteLine(item);


IList<string> doc;

/*
issue
- howto

  ```powershell
  $other = cat C:\note\pool_-_2024-09-30_Master.md | Get-MarkdownTree -AsMarkdown
  $other.IsA("Hyperlink")
  @($other.IsA("Hyperlink"))[1].Token.Content
  ```

- case

  ```markdown
  - actual: cannot use login <cnalisoviejo@gmail.com>
  ```

  ```markdown
  | retrieved | what | url |
  |-----------|------|-----|
  | 2025-04-09 | padlet | <https://padlet.com/cn_edu/jr-explorers-bits-bytes-h2fk29k4u37fuw4t> |
  | 2025-04-09 | slide share | <https://drive.google.com/drive/folders/1sTMmh52hH4_XfvLbFfz1hqxhgc94f_mp> |
  ```
*/

doc = [
    // "# estuans",
    // "",
    // "inter",
    // ": iu-sir",
    // ": - avehe: 2026-09-22",
    // "",
    // "mentiseph",
    // ": IROT Hestuans Inter",
    // ": - iusir: 2026-07-23",
    // "",
    // "avehemen",
    // ": Tise Phirot Hestu Ansin",
    // ": Teri Usirav Ehemen Tisep Hiroth estuans",
    // ": - [inte rius](./irav/ehe/ment_-_2023-02-20_IsephiroTheStuans.in)",
    // ": - teriu: 2026-07-14",
    // "",
    // "siraveh",
    // ": Emen Tiseph Iroth estu",
    // ": Ansi Nteriu Sirav Eheme Ntisep Hirothe",
    // ": - [stua nsin](./teri/usi/rave_-_2023-02-20_HementisEphiRothe.st)",
    // ": - uansi: 2026-07-14",
    // "",



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

    // "# est",
    // "- actual: cannot use login <cnalisoviejo@gmail.com>",

    // "| retrieved | what | url |",
    // "|-----------|------|-----|",
    // "| 2025-04-09 | padlet | <https://padlet.com/cn_edu/jr-explorers-bits-bytes-h2fk29k4u37fuw4t> |",
    // "| 2025-04-09 | slide share | <https://drive.google.com/drive/folders/1sTMmh52hH4_XfvLbFfz1hqxhgc94f_mp> |",

    // "# est",
    // "- uan",
    // "  - sin",
    // "    - est",
    // "      - It's all",
    // "  - est",
    // "    - I have",

    // "# the",
    // "- define",
    // "  - what",
    // "  - note: ``Ctl + B``: System Tray, **Show Hidden Icons**",
    // "  - issue 2025-04-06-183038",
    // "    - where: ScanSnap Home",
    // "    - actual",

    // "![2025-04-06-183044](./res/2025-04-06-183044.png)",
    // "- replay <- what the heck is this?",
    // "- replay <",
    // "- ``/Python31X/``",
    // "  ```markdown",
    // "  # todo",
    // "",
    // "  - [ ] connect: show Matt",
    // "",
    // "    ![20250220_161533.jpg](../res/20250220_161533.jpg)",
    // "    ![20250220_161533.jpg](../res/20250220_161533.jpg)",
    // "  ```",
    // "  | est | uan | sin |",
    // "  |-----|-----|-----|",
    // "  | ter | ius | ira |",
    // "  | veh | eme | nti |",


    // "- ![2025-04-06-183044](./res/2025-04-06-183044.png)",
    // "- ![2025-04-06-183129](./res/2025-04-06-183129.png)",
    // "- ![2025-04-06-183943](./res/2025-04-06-183943.png)",
    // "- ![2025-04-06-184257](./res/2025-04-06-184257.png)",

    // "    | est | uan | sin |",
    // "    |-----|-----|-----|",
    // "    | ter | ius | ira |",
    // "    | veh | eme | nti |",
    // "  - [2025-02-12](link)",
];

// IList<string> actual =
//     [.. from o in Outline.Scan(doc)
//     where o is Outline
//     from string s in TestOutline.GetStrings((Outline)o)
//     select s];
// 
// foreach (var item in actual)
//     Console.WriteLine(item);
// 
// return;




using PowerShell powershell = PowerShell.Create();
var initialSessionState = InitialSessionState.CreateDefault();

initialSessionState.Commands.Add(new SessionStateCmdletEntry(
    "Get-MarkdownTree",
    typeof(PsMarkdownTree.GetMarkdownTreeCommand),
    ""
));

initialSessionState.Commands.Add(new SessionStateCmdletEntry(
    "Write-MarkdownTree",
    typeof(PsMarkdownTree.WriteMarkdownTreeCommand),
    ""
));

initialSessionState.Commands.Add(new SessionStateCmdletEntry(
    "Find-MarkdownTree",
    typeof(PsMarkdownTree.FindMarkdownTreeCommand),
    ""
));

using var runspace = RunspaceFactory.CreateRunspace(initialSessionState);

runspace.Open();
powershell.Runspace = runspace;

powershell.AddScript("$input").Invoke();
powershell.Commands.Clear();
powershell.AddCommand("Get-MarkdownTree");

var collection = powershell.Invoke(doc);

powershell.AddScript("$input").Invoke();
powershell.Commands.Clear();
powershell.AddCommand("Write-MarkdownTree");

// powershell.AddScript("$input").Invoke();
// powershell.Commands.Clear();
// powershell.AddCommand("Find-MarkdownTree");
// powershell.AddParameter("PropertyName", new List<string> { "est" });

foreach (var line in powershell.Invoke(collection))
    Console.WriteLine(line);




/*
IList<ITree> forest =
    [.. from o in Outline.Get(doc)
//         where o is Outline
//         select ((Outline)o).CascadeUnfold() as ITree];
        select o];

// forest = Outline.Merge(forest);
// 
// forest =
//     [.. from o in forest
//         where o is Outline
//         select ((Outline)o).MergeChildren(c => ((Outline)c).Name == "sched")];

foreach (string line in
    from tree in forest
    where tree is IMarkdownWritable
    from s in ((IMarkdownWritable)tree).ToMarkdown()
    select s
) {
    Console.WriteLine(line);
}
*/




