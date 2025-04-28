using MarkdownTree.Parse;
using System.Management.Automation;

namespace PsMarkdownTree;

/*
 * Generated Keywords
 * - _LineType
 * - _Content
 * - Completed
 * - Language
 * - Lines
 * - Children
 * - Table
 * - Value
 */

[Cmdlet(VerbsCommon.New, "MarkdownTree")]
public class NewMarkdownTreeCommand : Cmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public string[] InputObject = [];

    private void AddParentProperty(PSObject obj, IParent parent, string propertyName)
    {
        if (parent.Children.Count == 0)
            return;

        PSObject subobj = new();

        foreach (ITree subtree in parent.Children)
            AddProperty(subobj, subtree);

        obj.Members.Add(new PSNoteProperty(propertyName, subobj));
    }

    private void AddOutlineProperty(PSObject obj, Outline outline)
    {
        AddParentProperty(obj, outline, outline.Name);
        obj.Properties.Add(new PSNoteProperty("_LineType", outline.LineType));
        obj.Properties.Add(new PSNoteProperty("_Content", outline.Content));
    }

    private void AddProperty(PSObject obj, ITree tree)
    {
        if (tree is Outline outline)
            AddOutlineProperty(obj, outline);

        if (tree is ActionItem actionItem)
            obj.Properties.Add(new PSNoteProperty("Completed", actionItem.Completed));

        if (tree is CodeBlock codeBlock)
        {
            obj.Members.Add(new PSNoteProperty("Language", codeBlock.Language));
            obj.Members.Add(new PSNoteProperty("Lines", codeBlock.Lines));
            AddParentProperty(obj, codeBlock, "Children");
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
            AddParentProperty(obj, table, "Children");
            return;
        }

        if (tree is ISegment segment)
        {
            obj.Members.Add(new PSNoteProperty("Value", segment.ToString().Trim()));
            obj.Members.Add(new PSNoteProperty("_Content", segment));
        }
    }

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
}

