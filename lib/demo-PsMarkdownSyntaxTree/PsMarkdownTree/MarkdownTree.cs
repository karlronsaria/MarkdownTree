using MarkdownTree.Parse;
using System.Management.Automation;

namespace PsMarkdownTree;

/*
 * Generated Keywords
 * - _LineType
 * - _Content
 * - _Completed
 * - _Language
 * - _Lines
 * - _Children
 * - _Table
 * - _Value
 * - _MissingName
 */

[Cmdlet(
    VerbsCommon.Find, "MarkdownTree",
    DefaultParameterSetName = "ByPropertyName"
)]
public class FindMarkdownTreeCommand : PSCmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public object[] InputObject = [];

    [Parameter(Position = 1)]
    [Parameter(ParameterSetName = "ByScriptBlock")]
    public ScriptBlock? Where;

    [Parameter(Position = 1)]
    [Parameter(ParameterSetName = "ByPropertyName")]
    public string[] PropertyName = [];

    protected override void ProcessRecord()
    {
        switch (ParameterSetName)
        {
            case "ByPropertyName":
                foreach (var item in InputObject)
                {
                    var list = item switch
                    {
                        PSObject psobject => FindSubtree(psobject, PropertyName),

                        Branching root => root.WhereAll(x => x switch
                        {
                            Outline o => PropertyName.Contains(o.Name),
                            ITree t => PropertyName.Contains(t.ToString()?.Trim() ?? string.Empty),
                        }),

                        _ => [],
                    };

                    foreach (var subtree in list)
                        WriteObject(subtree);
                }

                break;
            case "ByScriptBlock":
                Where ??= ScriptBlock.Create("return $true");

                foreach (object item in InputObject)
                {
                    IEnumerable<object> list = item switch
                    {
                        PSObject psobject => FindSubtree(psobject, Where),
                        Branching root => root.WhereAll(x => InvokeAsPredicate(x, Where)),
                        _ => InvokeAsPredicate(item, Where) ? [item] : [],
                    };

                    foreach (var subtree in list)
                        WriteObject(subtree);
                }

                break;
        }
    }

    private static bool
    InvokeAsPredicate(object psobject, ScriptBlock scriptBlock)
    {
        var result = scriptBlock.Invoke(psobject);
        bool confirm = result is not null && result.Count > 0 && LanguagePrimitives.IsTrue(result[0]);
        return confirm;
    }

    private static IEnumerable<object>
    FindSubtree(PSObject psobject, ScriptBlock predicate)
    {
        foreach (var property in psobject.Properties)
        {
            if (InvokeAsPredicate(property.Name, predicate))
                yield return property.Value;

            if (property.Value is PSObject subtree)
                foreach (var value in FindSubtree(subtree, predicate))
                    yield return value;
        }
    }

    private static IEnumerable<object>
    FindSubtree(PSObject psobject, string[] propertyName)
    {
        foreach (var property in psobject.Properties)
        {
            if (propertyName.Contains(property.Name))
                yield return property.Value;

            if (property.Value is PSObject subtree)
                foreach (var value in FindSubtree(subtree, propertyName))
                    yield return value;
        }
    }
}

[Cmdlet(VerbsCommunications.Write, "MarkdownTree")]
public class WriteMarkdownTreeCommand : Cmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public object[] InputObject = [];

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        foreach (object item in InputObject)
        {
            if (item is IMarkdownWritable tree)
            {
                Write(tree.ToMarkdown());
                continue;
            }

            if (item is PSObject psobject)
            {
                var collection = psobject
                    .Properties
                    .Where(p => p.Name == "_Content");

                if (collection.Any())
                {
                    foreach (var collectionItem in collection)
                        if (collectionItem.Value is IMarkdownWritable markdown)
                            Write(markdown.ToMarkdown());
                }
                else
                {
                    Write(
                        psobject: psobject,
                        indentSize: IMarkdownWritable.DEFAULT_INDENT_SIZE,
                        level: 0
                    );
                }
            }
        }
    }

    protected void
    Write(
        IEnumerable<string> lines,
        int indentSize = IMarkdownWritable.DEFAULT_INDENT_SIZE,
        int level = 0
    ) {
        string space = string.Concat(Enumerable.Repeat(" ", level * indentSize));

        foreach (string line in lines)
            WriteObject($"{space}{line}");
    }

    protected void
    Write(
        PSObject psobject,
        int indentSize = IMarkdownWritable.DEFAULT_INDENT_SIZE,
        int level = 0
    ) {
        string box = psobject.Properties.Any(p => p.Name == "_Completed")
            ? (bool)psobject.Properties["_Completed"].Value
                ? "[x] "
                : "[ ] "
            : string.Empty;

        string firstSpace = string.Concat(Enumerable.Repeat(" ", level * indentSize));
        string secndSpace = string.Concat(Enumerable.Repeat(" ", (level + 1) * indentSize));

        var langCapture = psobject.Properties.Match("_Language");
        var linesCapture = psobject.Properties.Match("_Lines");

        if (langCapture.Count != 0 && linesCapture.Count != 0)
        {
            IList<string> lines = linesCapture.First().Value switch
            {
                IList<string> strlist => strlist,

                IList<object> objectList =>
                    [.. from o in objectList
                        select o.ToString() ?? string.Empty],

                string mystr => [mystr],
                object other => [other.ToString() ?? string.Empty],
            };

            var codeBlock = new CodeBlock
            {
                Language = langCapture.First().Value.ToString() ?? string.Empty,
                Lines = lines,
            };

            Write(codeBlock.ToMarkdown(
                level: level + 1,
                nextIndent: (level + 1) * indentSize
            ));
        }

        IList<string> keywords = [
            "_Completed",
            "_Language",
            "_Lines",
        ];

        foreach (var prop in
            from p in psobject.Properties
            where !keywords.Contains(p.Name)
            select p
        ) {
            if (prop.Name == "_Table" && prop.Value is IList<PSObject> tableList)
            {
                foreach (Table table in ToTable(tableList))
                    Write(table.ToMarkdown(
                        level: level,
                        nextIndent: level * indentSize
                    ));

                continue;
            }

            WriteObject($"{firstSpace}- {box}{prop.Name}");

            if (prop.Value is PSObject branch)
                Write(
                    psobject: branch,
                    level: level + 1,
                    indentSize: indentSize
                );

            else if (prop.Value is IMarkdownWritable contentTree)
                Write(contentTree.ToMarkdown(
                    level: level + 1,
                    indent: (level + 1) * indentSize
                ));

            else if (prop.Value is IList<PSObject> list)
                foreach (Table table in ToTable(list))
                    Write(table.ToMarkdown(
                        level: level + 1,
                        nextIndent: (level + 1) * indentSize
                    ));

            else if (prop.Value is string str)
                WriteObject($"{secndSpace}- {str}");

            else if (prop.Value is object[] array)
                foreach (var item in array)
                    WriteObject($"{secndSpace}- {item}");

            else
                WriteObject($"{secndSpace}- {prop.Value.ToString()?.Trim() ?? string.Empty}");
        }
    }

    private static bool
    ListsEqual<T>(IList<T> first, IList<T> secnd)
        where T : IEquatable<T>
    {
        if (first.Count != secnd.Count)
            return false;

        int count = int.Min(first.Count, secnd.Count);

        for (int i = 0; i < count; ++i)
            if (!first[i].Equals(secnd[i]))
                return false;

        return true;
    }

    protected static IEnumerable<Table>
    ToTable(IList<PSObject> items)
    {
        IList<string> headings = [];
        IList<Row> rows = [];
        Row headingRow = [];

        foreach (PSObject item in items)
        {
            IList<string> newHeadings =
                [.. from p in item.Properties
                    select p.Name];

            Row row =
                [.. from p in item.Properties
                    select (p.Value switch {
                        ISegment segment => segment,
                        string str => new Text { Content = str },
                        object obj => new Text { Content = obj.ToString() ?? string.Empty },
                    })];

            rows.Add(row);

            if (headings.Count == 0)
            {
                headings = newHeadings;
            }
            else if (!ListsEqual(headings, newHeadings))
            {
                headingRow =
                    [.. from h in newHeadings
                        select new Text { Content = h }];

                yield return new Table
                {
                    Headings = headingRow,
                    Rows = rows,
                };

                headings = newHeadings;
                rows = [];
            }
        }

        headingRow =
            [.. from h in headings
                select new Text { Content = h }];

        yield return new Table
        {
            Headings = headingRow,
            Rows = rows,
        };
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

    [Parameter()]
    public string[] MuteProperty = [];

    [Parameter()]
    public string[] MergeProperty = [];

    [Parameter()]
    public string[] FoldProperty = [];

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

        var forest = Outline.Get(_markdown, c => !MuteProperty.Contains(((Outline)c).Name));

        if (MergeProperty.Length > 0)
        {
            forest = Outline.Merge([.. forest], c => MergeProperty.Contains(((Outline)c).Name));

            forest =
                [.. from t in forest
                    where t is Outline
                    select ((Outline)t).CascadeMerge(c => MergeProperty.Contains(((Outline)c).Name))];
        }

        if (FoldProperty.Length > 0)
        {
            forest =
                from t in forest
                where t is Outline
                select ((Outline)t).Fold(c => FoldProperty.Contains(((Outline)c).Name));
        }

        foreach (var tree in forest)
        {
            ITree worktree = tree is Outline outline
                ? outline.CascadeUnfold().CascadeMerge()
                : tree;

            var obj = new PSObject();
            AddProperty(obj, worktree);
            WriteObject(obj);
        }
    }

    public const string MISSING_NAME_MESSAGE = "_MissingName";

    private void AddTreeProperty(PSObject obj, Branching tree, string propertyName)
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

        if (!obj.Properties.Any(p => p.Name == propertyName))
        {
            // branch
            obj.Members.Add(new PSNoteProperty(propertyName, subobj));
        }
        else
        {
            var value = obj.Properties[propertyName].Value;

            if (value is IList<object> list)
            {
                list.Add(subobj);
                obj.Properties[propertyName].Value = list;
            }
            else
            {
                IList<object> newList = [];
                newList.Add(value);
                newList.Add(subobj);
                obj.Properties[propertyName].Value = newList;
            }
        }
    }

    private void AddOutlineProperty(PSObject obj, Outline outline)
    {
        AddTreeProperty(obj, outline, outline.Name);
        obj.Properties.Add(new PSNoteProperty("_LineType", outline.LineType));
        obj.Properties.Add(new PSNoteProperty("_Content", outline));
    }

    private static bool ConvertLeafToString(PSObject obj, Outline outline)
    {
        if (outline.Children.Count != 1)
            return false;

        var child = outline.Children[0];

        // [!] note: Branching type accepts code blocks, tables, and action items
        if (child is Outline p && p.Children.Count == 0)
        {
            string value = child is Outline o
                ? o.Name
                : child.ToString()?.Trim() ?? MISSING_NAME_MESSAGE;

            obj.Properties.Add(new PSNoteProperty(outline.Name, value));
            return true;
        }

        if (child is ISegment s)
        {
            string value = s.ToString()?.Trim() ?? MISSING_NAME_MESSAGE;
            obj.Properties.Add(new PSNoteProperty(outline.Name, value));
            return true;
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

            if (outline is ActionItem actionItem)
                obj.Properties.Add(new PSNoteProperty("_Completed", actionItem.Completed));

            return;
        }

        if (tree is CodeBlock codeBlock)
        {
            obj.Members.Add(new PSNoteProperty("_Language", codeBlock.Language));
            obj.Members.Add(new PSNoteProperty("_Lines", codeBlock.Lines));

            if (codeBlock.Children.Count > 0)
                AddTreeProperty(obj, codeBlock, "_Children");

            return;
        }

        if (tree is Table table)
        {
            IList<string> headings =
                [.. from h in table.Headings
                    select h.ToString().Trim()];

            IList<PSObject> rows = [];

            foreach (var row in table.Rows)
            {
                IList<string> cells =
                    [.. from c in row
                        select c.ToString().Trim()];

                var subobj = new PSObject();

                for (int i = 0; i < headings.Count; ++i)
                    subobj.Members.Add(new PSNoteProperty(headings[i], cells[i]));

                rows.Add(subobj);
            }

            obj.Properties.Add(new PSNoteProperty("_Table", rows));

            if (table.Children.Count > 0)
                AddTreeProperty(obj, table, "_Children");

            return;
        }

        if (tree is ISegment segment)
        {
            obj.Members.Add(new PSNoteProperty("_Value", segment.ToString().Trim()));
            obj.Members.Add(new PSNoteProperty("_Content", segment));
        }
    }
}

