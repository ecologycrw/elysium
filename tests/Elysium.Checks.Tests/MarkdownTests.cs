namespace Elysium.Checks.Tests;

public sealed class MarkdownTests
{
    [Theory]
    [InlineData("Regra de três", "regra-de-três")]
    [InlineData("Conteúdo de terceiros", "conteúdo-de-terceiros")]
    [InlineData("Propostas: como as necessidades aparecem", "propostas-como-as-necessidades-aparecem")]
    [InlineData("A escada: um erro repetido vira garantia", "a-escada-um-erro-repetido-vira-garantia")]
    [InlineData("Specs (`spec.md`)", "specs-specmd")]
    public void Slug_follows_GitHub_rules(string heading, string expected)
    {
        Assert.Equal(expected, Markdown.Slug(heading));
    }

    [Fact]
    public void Repeated_headings_get_numbered_anchors()
    {
        var anchors = Markdown.Anchors("## Notas\n\ntext\n\n## Notas\n");

        Assert.Contains("notas", anchors);
        Assert.Contains("notas-1", anchors);
    }

    [Fact]
    public void Links_and_headings_inside_code_are_ignored()
    {
        const string Text = "Real [a](a.md) and `[b](b.md)`.\n\n```markdown\n[c](c.md)\n## Not a heading\n```\n";

        var targets = Markdown.Links(Text).Select(link => link.Target).ToList();

        Assert.Equal(["a.md"], targets);
        Assert.DoesNotContain("not-a-heading", Markdown.Anchors(Text));
    }

    [Fact]
    public void Front_matter_is_read_and_comments_are_ignored()
    {
        var frontMatter = Markdown.ReadFrontMatter("---\nid: 0001\nstatus: aceita   # proposta | aceita\nvazio:\n---\n# Title\n");

        Assert.Equal("0001", frontMatter["id"]);
        Assert.Equal("aceita", frontMatter["status"]);
        Assert.Equal(string.Empty, frontMatter["vazio"]);
    }

    [Fact]
    public void A_file_without_front_matter_has_none()
    {
        Assert.Empty(Markdown.ReadFrontMatter("# Title\nid: 1\n"));
    }
}
