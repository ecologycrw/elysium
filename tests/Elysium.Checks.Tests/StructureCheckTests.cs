namespace Elysium.Checks.Tests;

public sealed class StructureCheckTests
{
    private const string EmptyProject = "<Project Sdk=\"Microsoft.NET.Sdk\">\n</Project>\n";

    [Fact]
    public void Valid_layout_has_no_findings()
    {
        using var repo = ValidLayout();

        Assert.Empty(repo.Structure());
    }

    [Fact]
    public void Project_name_without_the_prefix_is_reported()
    {
        using var repo = ValidLayout()
            .Write("tools/MyTool/MyTool.csproj", EmptyProject)
            .Write("Elysium.slnx", Solution(ValidPaths.Append("tools/MyTool/MyTool.csproj")));

        var finding = Assert.Single(repo.Structure());
        Assert.Contains("must start with 'Elysium.'", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_referencing_Classic_is_reported()
    {
        using var repo = ValidLayout()
            .Write("src/core/Elysium.Core/Elysium.Core.csproj", Project("../../framework/Elysium.Classic.Shared/Elysium.Classic.Shared.csproj"));

        Assert.Contains(repo.Structure(), finding => finding.Message.Contains("the core must not reference Elysium.Classic.Shared", StringComparison.Ordinal));
    }

    [Fact]
    public void Shared_referencing_Server_is_reported()
    {
        using var repo = ValidLayout()
            .Write("src/framework/Elysium.Classic.Shared/Elysium.Classic.Shared.csproj", Project("../../core/Elysium.Core.Server/Elysium.Core.Server.csproj"));

        Assert.Contains(repo.Structure(), finding => finding.Message.Contains("shared code must not reference Elysium.Core.Server", StringComparison.Ordinal));
    }

    [Fact]
    public void Engine_referencing_a_test_project_is_reported()
    {
        using var repo = ValidLayout()
            .Write("src/core/Elysium.Core/Elysium.Core.csproj", Project("../../../tests/Elysium.Core.Tests/Elysium.Core.Tests.csproj"));

        Assert.Contains(repo.Structure(), finding => finding.Message.Contains("must not reference Elysium.Core.Tests", StringComparison.Ordinal));
    }

    [Fact]
    public void Test_project_outside_tests_is_reported()
    {
        using var repo = ValidLayout()
            .Write("tools/Elysium.Bot.Tests/Elysium.Bot.Tests.csproj", EmptyProject)
            .Write("Elysium.slnx", Solution(ValidPaths.Append("tools/Elysium.Bot.Tests/Elysium.Bot.Tests.csproj")));

        var finding = Assert.Single(repo.Structure());
        Assert.Contains("must live in tests/", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_in_the_wrong_folder_is_reported()
    {
        using var repo = ValidLayout()
            .Write("src/Elysium.Misc/Elysium.Misc.csproj", EmptyProject)
            .Write("Elysium.slnx", Solution(ValidPaths.Append("src/Elysium.Misc/Elysium.Misc.csproj")));

        var finding = Assert.Single(repo.Structure());
        Assert.Contains("projects must live in", finding.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_missing_from_the_solution_is_reported()
    {
        using var repo = ValidLayout().Write("tools/Elysium.Bot/Elysium.Bot.csproj", EmptyProject);

        var finding = Assert.Single(repo.Structure());
        Assert.Contains("not listed in Elysium.slnx", finding.Message, StringComparison.Ordinal);
    }

    private static readonly string[] ValidPaths =
    [
        "src/core/Elysium.Core/Elysium.Core.csproj",
        "src/core/Elysium.Core.Server/Elysium.Core.Server.csproj",
        "src/framework/Elysium.Classic.Shared/Elysium.Classic.Shared.csproj",
        "tests/Elysium.Core.Tests/Elysium.Core.Tests.csproj",
    ];

    private static TempRepository ValidLayout() => new TempRepository()
        .Write("src/core/Elysium.Core/Elysium.Core.csproj", EmptyProject)
        .Write("src/core/Elysium.Core.Server/Elysium.Core.Server.csproj", Project("../Elysium.Core/Elysium.Core.csproj"))
        .Write("src/framework/Elysium.Classic.Shared/Elysium.Classic.Shared.csproj", Project("../../core/Elysium.Core/Elysium.Core.csproj"))
        .Write("tests/Elysium.Core.Tests/Elysium.Core.Tests.csproj", Project("../../src/core/Elysium.Core.Server/Elysium.Core.Server.csproj"))
        .Write("Elysium.slnx", Solution(ValidPaths));

    private static string Project(string reference) =>
        $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"{reference}\" />\n  </ItemGroup>\n</Project>\n";

    private static string Solution(IEnumerable<string> paths) =>
        "<Solution>\n" + string.Concat(paths.Select(path => $"  <Project Path=\"{path}\" />\n")) + "</Solution>\n";
}
