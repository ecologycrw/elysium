namespace Elysium.Checks.Tests;

/// <summary>
/// Runs the real checks on the real repository, so `dotnet test` also proves the repository is aligned.
/// </summary>
public sealed class RepositoryTests
{
    [Fact]
    public void The_repository_passes_every_check()
    {
        var root = RepositoryRoot.Find(AppContext.BaseDirectory);
        Assert.NotNull(root);

        var findings = DocsCheck.Run(root).Concat(StructureCheck.Run(root)).Select(finding => finding.ToString());

        Assert.Empty(findings);
    }
}
