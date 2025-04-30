using MarkdownTree.Parse;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace PsMarkdownTree;

/*
 * Generated Keywords
 * - _LineType
 * - _Content
 * - _Completed
 * - Language
 * - Lines
 * - Children
 * - Table
 * - Value
 * - _MissingName
 */

[Cmdlet(VerbsCommunications.Write, "MarkdownTree")]
public class WriteMarkdownTreeCommand : Cmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public object[] InputObject = [];

    protected void Write(IEnumerable<string> lines)
    {
        foreach (string line in lines)
            WriteObject(line);
    }

    protected void
    Write(
        PSObject tree,
        int indentSize = MarkdownTree.Parse.Outline.DEFAULT_INDENT_SIZE,
        int level = 0
    ) {
        string space = string.Concat(Enumerable.Repeat(" ", level * indentSize));

        string box = tree.Properties.Match("_Completed").Count != 0
            ? (bool)tree.Properties["_Completed"].Value
                ? "[x] "
                : "[ ] "
            : string.Empty;

        foreach (var prop in
            from p in tree.Properties
            where p.Name != "_Completed"
            select p
        ) {
            WriteObject($"{space}- {box}{prop.Name}");
            space = string.Concat(Enumerable.Repeat(" ", (level + 1) * indentSize));

            if (prop.Value is PSObject branch)
                Write(branch, indentSize, level + 1);
            else if (prop.Value is string str)
                WriteObject($"{space}- {str}");
            else if (prop.Value is object[] list)
                foreach (var item in list)
                    WriteObject($"{space}- {item}");
        }
    }

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        foreach (object item in InputObject)
        {
            if (item is IMarkdownWritable tree)
            {
                Write(tree.ToMarkdown());
            }
            else if (item is PSObject psobject)
            {
                var collection = psobject.Properties.Match("_Content");

                if (collection.Count > 0)
                {
                    foreach (var subitem in collection)
                    {
                        if (subitem.Value is IMarkdownWritable contentTree)
                        {
                            Write(contentTree.ToMarkdown());
                        }
                        else
                        {
                            Write([subitem.Value.ToString()?.Trim() ?? string.Empty]);
                        }
                    }
                }
                else
                {
                    Write(psobject);
                }
            }
        }
    }
}

[Cmdlet(VerbsCommon.Get, "MarkdownTree")]
public class GetMarkdownTreeCommand : Cmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public string[] InputObject = [];

    [Parameter()]
    public SwitchParameter Full;

    [Parameter()]
    public SwitchParameter AsMarkdown;

    protected IList<string> _markdown = [];

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        _markdown = [];
    }

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        foreach (var item in InputObject)
            _markdown.Add(item);
    }

    protected override void EndProcessing()
    {
        base.EndProcessing();

        foreach (var tree in Outline.Get(_markdown))
        {
            ITree worktree = tree is Outline outline
                ? outline.Unfold().Merge()
                : tree;

            var obj = new PSObject();
            AddProperty(obj, worktree);
            WriteObject(obj);
        }
    }

    public const string MISSING_NAME_MESSAGE = "_MissingName";

    private void AddTreeProperty(PSObject obj, IParent tree, string propertyName)
    {
        PSObject subobj = new();

        foreach (ITree subtree in tree.Children)
            AddProperty(subobj, subtree);

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            if (tree.Children.Count == 0)
                return;

            propertyName = MISSING_NAME_MESSAGE;
        }

        obj.Members.Add(new PSNoteProperty(propertyName, subobj));
    }

    private void AddOutlineProperty(PSObject obj, Outline outline)
    {
        AddTreeProperty(obj, outline, outline.Name);
        obj.Properties.Add(new PSNoteProperty("_LineType", outline.LineType));
        obj.Properties.Add(new PSNoteProperty("_Content", outline.Content));
    }

    private static bool ConvertLeafToString(PSObject obj, Outline outline)
    {
        if (outline.Children.Count == 1)
        {
            var child = outline.Children[0];

            if (child is IParent p && p.Children.Count == 0)
            {
                string value = child is Outline o
                    ? o.Name
                    : child.ToString()?.Trim() ?? MISSING_NAME_MESSAGE;

                obj.Properties.Add(new PSNoteProperty(outline.Name, value));
                return true;
            }
            else if (child is ISegment s)
            {
                string value = s.ToString()?.Trim() ?? MISSING_NAME_MESSAGE;
                obj.Properties.Add(new PSNoteProperty(outline.Name, value));
                return true;
            }
        }

        return false;
    }

    private void AddProperty(PSObject obj, ITree tree)
    {
        if (tree is Outline outline)
        {
            if (ConvertLeafToString(obj, outline))
                return;

            if (Full.IsPresent)
                AddOutlineProperty(obj, outline);
            else
                AddTreeProperty(obj, outline, outline.Name);
        }

        if (tree is ActionItem actionItem)
        {
            obj.Properties.Add(new PSNoteProperty("_Completed", actionItem.Completed));
            return;
        }

        if (tree is CodeBlock codeBlock)
        {
            obj.Members.Add(new PSNoteProperty("Language", codeBlock.Language));
            obj.Members.Add(new PSNoteProperty("Lines", codeBlock.Lines));
            AddTreeProperty(obj, codeBlock, "Children");
            return;
        }

        if (tree is Table table)
        {
            var subobj = new PSObject();

            IList<string> headings =
                [.. from h in table.Headings
                    select h.ToString().Trim()];

            foreach (var row in table.Rows)
            {
                IList<string> cells =
                    [.. from c in row
                        select c.ToString().Trim()];

                for (int i = 0; i < headings.Count; ++i)
                    subobj.Members.Add(new PSNoteProperty(headings[i], cells[i]));
            }

            obj.Members.Add(new PSNoteProperty("Table", subobj));
            AddTreeProperty(obj, table, "Children");
            return;
        }

        if (tree is ISegment segment)
        {
            obj.Members.Add(new PSNoteProperty("Value", segment.ToString().Trim()));
            obj.Members.Add(new PSNoteProperty("_Content", segment));
        }
    }
}

