namespace Elysium.Checks.Tests;

public sealed class PrSummaryTests
{
    [Theory]
    [InlineData(".claude/settings.json", true)]
    [InlineData(".github/workflows/ci.yml", true)]
    [InlineData("AGENTS.md", true)]
    [InlineData("tests/.editorconfig", true)]
    [InlineData("tests/Directory.Build.props", true)]
    [InlineData("tools/Elysium.Checks/DocsCheck.cs", true)]
    [InlineData("src/core/Elysium.Core/CoreAssembly.cs", false)]
    [InlineData("docs/ARCHITECTURE.md", false)]
    public void Protected_paths_are_recognized(string path, bool expected)
    {
        Assert.Equal(expected, PrSummary.IsProtected(path));
    }

    [Fact]
    public void Summary_lists_protected_changes_and_removed_tests()
    {
        var changes = PrSummary.ParseNameStatus("M\t.claude/settings.json\nA\tsrc/core/Elysium.Core/Grid.cs\nD\ttests/Elysium.Core.Tests/OldTests.cs\n");
        const string TestDiff = "--- a/tests/X.cs\n+++ b/tests/X.cs\n-    [Fact]\n-    [Theory]\n+    [Fact]\n";

        var summary = PrSummary.Build(changes, TestDiff);

        Assert.Contains("### Travas alteradas (1)", summary, StringComparison.Ordinal);
        Assert.Contains("`.claude/settings.json` (alterado)", summary, StringComparison.Ordinal);
        Assert.Contains("Testes adicionados: 1", summary, StringComparison.Ordinal);
        Assert.Contains("Testes removidos: 2", summary, StringComparison.Ordinal);
        Assert.Contains("`tests/Elysium.Core.Tests/OldTests.cs`", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_without_risky_changes_says_so()
    {
        var summary = PrSummary.Build(PrSummary.ParseNameStatus("M\tdocs/STATUS.md\n"), string.Empty);

        Assert.Contains("### Travas alteradas (0)", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("Atenção", summary, StringComparison.Ordinal);
    }
}
