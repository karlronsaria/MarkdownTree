using MarkdownTree.Parse;

namespace test_PsMarkdownSyntaxTree;

public class TestOutlinePathOfAll
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test_PathOfAll_Hyperlinks_In_CascadeUnfolded_Outline()
    {
        foreach (var path in
            from Outline tree in Outline.Scan([
                "# howto: ImageMagick Auto-trim",
                "",
                "Remove edges by color of each corner pixel",
                "",
                "## link",
                "",
                "- url: <https://www.imagemagick.org/script/command-line-options.php#trim>",
                "- retrieved: 2023-12-12",
                "",
                "## example",
                "",
                "```shell",
                "magick convert -trim +repage *.png",
                "```",
            ])
            from path in tree.CascadeUnfold().CascadeMerge().PathOfAll(b =>
            {
                if (b is Outline branch)
                    return branch.Content.WhereAll(s => s.TokenType == MarkdownTree.Lex.TokenType.Hyperlink).ToList().Count != 0;

                return false;
            })
            select path
        ) {
            string actual = string.Join(" ", path);
            Assert.That(actual, Is.EqualTo("0 1 0 0"));
        }
    }
}

