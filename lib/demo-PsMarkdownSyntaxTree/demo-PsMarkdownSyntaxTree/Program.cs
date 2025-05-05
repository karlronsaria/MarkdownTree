// See https://aka.ms/new-console-template for more information
using demo_PsMarkdownSyntaxTree;
using MarkdownTree;
using MarkdownTree.Lex;
using MarkdownTree.Parse;

using System.Management.Automation;
using System.Management.Automation.Runspaces;

IList<string> doc;

doc = [
    "# est",
    "- uan",
    "  - sin",
    "    - est",
    "      - It's all",
    "  - est",
    "    - I have",

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

powershell.AddScript("$input").Invoke();
powershell.Commands.Clear();
powershell.AddCommand("Find-MarkdownTree");
powershell.AddParameter("PropertyName", new List<string> { "est" });

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




