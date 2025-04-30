using System.Management.Automation;
using MarkdownTree.Parse;

namespace demo_PsMarkdownSyntaxTree;

public class TestOutline
{
    public static IEnumerable<string> GetStrings(IParent tree, int level = 0, int indent = 2)
    {
        string space = string.Concat(Enumerable.Repeat(" ", level * indent));
        
        if (tree is CodeBlock e)
            foreach (string str in e.Lines)
                yield return $"{space}{e.Language}({str})";
        else if (tree is Table t)
        {
            yield return      $"{space}TableHead ({t.Headings})";

            foreach (Row row in t.Rows)
                yield return  $"{space}TableRow  ({row})";
        }
        else if (tree is Outline o)
            yield return $"{space}{o.LineType}({o.Name})";

        foreach (var child in tree.Children)
            if (child is IParent p)
                foreach (string str in GetStrings(p, level + 1, indent))
                    yield return str;
    }

    public static void Write(IEnumerable<string> lines)
    {
        foreach (string line in lines)
            Console.WriteLine(line);
    }

    public static void
    Write(
        PSObject tree,
        int indentSize = MarkdownTree.Parse.Outline.DEFAULT_INDENT_SIZE,
        int level = 0
    ) {
        string space = string.Concat(Enumerable.Repeat(" ", level * indentSize));

        string box = tree.Properties.Match("_Completed").Count != 0
            ? (bool)tree.Properties["_Completed"].Value
                ? "[x] " : "[ ] "
            : string.Empty;

        foreach (var prop in
            from p in tree.Properties
            where p.Name != "Completed"
            select p
        ) {
            Console.WriteLine($"{space}- {box}{prop.Name}");

            if (prop.Value is PSObject branch)
                Write(branch, indentSize, level + 1);
            else if (prop.Value is string str)
                Console.WriteLine($"{space}- {str}");
            else if (prop.Value is object[] list)
                foreach (var item in list)
                    Console.WriteLine($"{space}- {item}");
        }
    }

    public static void ProcessRecord(object[] InputObject)
    {
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

    public bool Full = false;

    IList<string> _markdown = [];

    public void BeginProcessing()
    {
        _markdown = [];
    }

    public void ProcessRecordFrom(object[] InputObject)
    {
        foreach (var item in InputObject)
            _markdown.Add((string)item);
    }

    public void EndProcessing()
    {
        foreach (var tree in Outline.Get(_markdown))
        {
            ITree worktree = tree is Outline outline
                ? outline.Unfold().Merge()
                : tree;

            var obj = new PSObject();
            AddProperty(obj, worktree);
            Console.WriteLine(obj);
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

    public void AddProperty(PSObject obj, ITree tree)
    {
        if (tree is Outline outline)
        {
            if (ConvertLeafToString(obj, outline))
                return;

            if (Full)
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

