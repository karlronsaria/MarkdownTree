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
            yield return     $"{space}TableHead ({t.Headings})";

            foreach (Row row in t.Rows)
                yield return $"{space}TableRow  ({row})";
        }
        else if (tree is Outline o)
            yield return $"{space}{o.LineType}({o.Name})";

        foreach (var child in tree.Children)
            if (child is IParent p)
                foreach (string str in GetStrings(p, level + 1, indent))
                    yield return str;
    }
}

