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

public static class Tables
{
    public static PSObject[] ToPSTable(MarkdownTree.Parse.Table table)
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
            {
                string cell = i < cells.Count ? cells[i] : string.Empty;
                subobj.Members.Add(new PSNoteProperty(headings[i], cell));
            }

            rows.Add(subobj);
        }

        return [.. rows];
    }
}

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

        bool confirm = result is not null
            && result.Count > 0
            && LanguagePrimitives.IsTrue(result[0]);

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
    public delegate string[] WriteTableCallback(IList<PSObject> rows, int level, int indent);

    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public object[] InputObject = [];

    [Parameter()]
    public int HeadingLevels = 0;

    [Parameter()]
    public WriteTableCallback? WriteTable;
    // public ScriptBlock? WriteTable;

    private bool _followsBlank = true;

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
                        level: 0,
                        headingLevels: HeadingLevels
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
        string space = string.Concat(Enumerable.Repeat(' ', level * indentSize));

        foreach (string line in lines)
            WriteObject($"{space}{line}");
    }

    protected void
    Write(
        PSObject psobject,
        int indentSize = IMarkdownWritable.DEFAULT_INDENT_SIZE,
        int level = 0,
        int headingLevels = 0
    ) {
        string box = psobject.Properties.Any(p => p.Name == "_Completed")
            ? (bool)psobject.Properties["_Completed"].Value
                ? "[x] "
                : "[ ] "
            : string.Empty;

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

            var lineNumberCapture = psobject.Properties.Match("_LineNumber");

            int lineNumber = lineNumberCapture.Count != 0
                ? (int)lineNumberCapture.First().Value : -1;

            var codeBlock = new CodeBlock(lineNumber)
            {
                Language = langCapture.First().Value.ToString() ?? string.Empty,
                Lines = lines,
            };

            if (!_followsBlank)
                WriteObject("");

            Write(codeBlock.ToMarkdown(
                level: level + 1,
                nextIndent: (level + 1) * indentSize
            ));

            WriteObject("");
            _followsBlank = true;
        }

        IList<string> keywords = [
            "_Completed",
            "_Language",
            "_Lines",
        ];

        bool isHeading = level < headingLevels;

        string lead = isHeading
            ? $"{string.Concat(Enumerable.Repeat('#', level + 1))} "
            : $"{string.Concat(Enumerable.Repeat(' ', (level - headingLevels) * indentSize))}- ";

        int mdLevel = level - headingLevels;

        foreach (var prop in
            from p in psobject.Properties
            where !keywords.Contains(p.Name)
            select p
        ) {
            if (prop.Name == "_Table")
            {
                IList<PSObject> myList = [];

                if (prop.Value is object[] objects)
                    myList = [.. from o in objects select o as PSObject];
                else if (prop.Value is IList<PSObject> tableList)
                    myList = tableList;
                else if (prop.Value is PSObject psObject)
                    myList = [psObject];

                if (!_followsBlank)
                    WriteObject("");

                if (WriteTable != null)
                    foreach (var row in WriteTable.Invoke(myList, mdLevel, indentSize))
                        WriteObject(row);
                else
                    foreach (Table table in ToTable(myList))
                        Write(table.ToMarkdown(
                            level: mdLevel,
                            nextIndent: mdLevel * indentSize
                        ));

                WriteObject("");
                _followsBlank = true;
                continue;
            }

            _followsBlank = false;

            // // todo: remove
            // if (level > 0 && level <= headingLevels)
            //     WriteObject(string.Empty);

            WriteObject($"{lead}{box}{prop.Name}");

            if (isHeading)
            {
                WriteObject("");
                _followsBlank = true;
            }

            if (prop.Value is PSObject branch)
                Write(
                    psobject: branch,
                    level: level + 1,
                    indentSize: indentSize,
                    headingLevels: headingLevels
                );

            else if (prop.Value is IMarkdownWritable contentTree)
                Write(contentTree.ToMarkdown(
                    level: mdLevel + 1,
                    indent: (mdLevel + 1) * indentSize
                ));

            else if (prop.Value is IList<PSObject> objList)
                foreach (Table table in ToTable(objList))
                    Write(table.ToMarkdown(
                        level: mdLevel + 1,
                        nextIndent: (mdLevel + 1) * indentSize
                    ));

            else if (prop.Value is IList<string> strList)
                foreach (var item in strList)
                    WriteLeaf(
                        inputObject: item,
                        indentSize: indentSize,
                        level: level + 1,
                        headingLevels: headingLevels
                    );

            else if (prop.Value is string str)
                WriteLeaf(
                    inputObject: str,
                    indentSize: indentSize,
                    level: level + 1,
                    headingLevels: headingLevels
                );

            else if (prop.Value is object[] array)
                foreach (var item in array)
                    WriteLeaf(
                        inputObject: item,
                        indentSize: indentSize,
                        level: level + 1,
                        headingLevels: headingLevels
                    );

            else
                WriteLeaf(
                    inputObject: prop.Value,
                    indentSize: indentSize,
                    level: level + 1,
                    headingLevels: headingLevels
                );
        }
    }

    protected void
    WriteLeaf(
        object inputObject,
        int indentSize = IMarkdownWritable.DEFAULT_INDENT_SIZE,
        int level = 0,
        int headingLevels = 0
    ) {
        bool isHeading = level < headingLevels;

        string lead = isHeading
            ? $"{string.Concat(Enumerable.Repeat('#', level + 1))} "
            : $"{string.Concat(Enumerable.Repeat(' ', (level - headingLevels) * indentSize))}- ";

        WriteObject($"{lead}{inputObject.ToString()?.Trim() ?? string.Empty}");

        if (isHeading)
        {
            WriteObject("");
            _followsBlank = true;
        }
    }

    private static bool
    ListsEqual<T>(IList<T> first, IList<T> secnd)
        where T : IEquatable<T>
    {
        if (first.Count != secnd.Count)
            return false;

        for (int i = 0; i < first.Count; ++i)
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
        int lineNumber = -1;

        foreach (PSObject item in items)
        {
            var lineNumberCapture = item.Properties.Match("_LineNumber");

            lineNumber = lineNumberCapture.Count != 0
                ? (int)lineNumberCapture.First().Value : -1;

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

                yield return new Table(lineNumber)
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

        yield return new Table(lineNumber)
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

        var forest = Outline.Scan(_markdown, c => !MuteProperty.Contains(((Outline)c).Name));

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

            if (AsMarkdown.IsPresent)
                WriteObject(worktree);
            else
            {
                var obj = new PSObject();
                AddProperty(obj, worktree);
                WriteObject(obj);
            }
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

    // (karlr 2026-09-21): It seems I'm getting two different concepts confused.
    // I'm checking to see if this node is a trivial branch, but I'm also looking for
    // leaf branches. They may seem like the same path, but are actually different.
    // A branch can have multiple leaves.

    private static bool IsLeaf(ITree child)
    {
        return (child is Outline outline && outline.Children.Count == 0) || (child is ISegment);
    }

    private static object? ConvertLeafToString(Outline outline)
    {
        IList<string> list = [];

        foreach (ITree child in outline.Children)
        {
            if (!IsLeaf(child))
                return null;

            string value = child.ToString()?.Trim() ?? MISSING_NAME_MESSAGE;
            list.Add(value);
        }

        if (!list.Any())
            return null;

        if (list.Count == 1)
            return (object)list[0];

        return (object)list;
    }

    private void AddProperty(PSObject obj, ITree tree)
    {
        if (tree is Outline outline)
        {
            var leaves = ConvertLeafToString(outline);

            if (leaves != null)
            {
                obj.Properties.Add(new PSNoteProperty(outline.Name, leaves));
                return;
            }

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
            obj.Properties.Add(new PSNoteProperty("_Table", Tables.ToPSTable(table)));

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

