using System.Text.RegularExpressions;

namespace Elysium.Checks;

/// <summary>
/// Checks project names, folders, layers, references between projects and the solution file.
/// The rules come from docs/ARCHITECTURE.md ("Estrutura de projetos") and docs/CONVENTIONS.md.
/// </summary>
internal static partial class StructureCheck
{
    private const string Name = "structure";
    private const string Prefix = "Elysium.";

    private enum Layer
    {
        Unknown,
        Core,
        Classic,
        Game,
        Tool,
        Test,
    }

    private enum Side
    {
        Neutral,
        Shared,
        Server,
        Client,
    }

    private sealed record Project(string Name, string FullPath, string RelativePath, Layer Layer);

    public static List<Finding> Run(string root)
    {
        var findings = new List<Finding>();
        var projects = RepositoryRoot.Files(root, "*.csproj")
            .Select(file =>
            {
                var relative = RepositoryRoot.Relative(root, file);
                return new Project(Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file), relative, LayerOf(relative));
            })
            .ToList();

        foreach (var project in projects)
        {
            CheckNaming(project, findings);
        }

        CheckSolution(root, projects, findings);
        CheckReferences(projects, findings);
        return findings;
    }

    private static Layer LayerOf(string relativePath)
    {
        var segments = relativePath.Split('/');
        return segments[0] switch
        {
            "src" when segments.Length == 4 && segments[1] == "core" => Layer.Core,
            "src" when segments.Length == 4 && segments[1] == "framework" => Layer.Classic,
            "samples" when segments.Length == 3 => Layer.Game,
            "tools" when segments.Length == 3 => Layer.Tool,
            "tests" when segments.Length == 3 => Layer.Test,
            _ => Layer.Unknown,
        };
    }

    private static Side SideOf(string projectName)
    {
        var segments = projectName.Split('.');
        if (segments.Contains("Server"))
        {
            return Side.Server;
        }

        if (segments.Contains("Client"))
        {
            return Side.Client;
        }

        return segments.Contains("Shared") ? Side.Shared : Side.Neutral;
    }

    private static void CheckNaming(Project project, List<Finding> findings)
    {
        void Add(string message) => findings.Add(new(Name, project.RelativePath, message));

        if (!project.Name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            Add($"project names must start with '{Prefix}'");
        }

        var segments = project.RelativePath.Split('/');
        if (segments.Length < 2 || segments[^2] != project.Name)
        {
            Add("the project folder must have the same name as the project");
        }

        var isTestName = project.Name.EndsWith(".Tests", StringComparison.Ordinal);
        switch (project.Layer)
        {
            case Layer.Unknown:
                Add("projects must live in src/core/<Name>, src/framework/<Name>, samples/<Name>, tools/<Name> or tests/<Name>");
                break;
            case Layer.Test when !isTestName:
                Add("test projects must end with '.Tests'");
                break;
            case not Layer.Test when isTestName:
                Add("projects ending with '.Tests' must live in tests/");
                break;
            case Layer.Core when !IsNamed(project.Name, "Elysium.Core"):
                Add("projects in src/core must be named Elysium.Core or Elysium.Core.*");
                break;
            case Layer.Classic when !IsNamed(project.Name, "Elysium.Classic"):
                Add("projects in src/framework must be named Elysium.Classic or Elysium.Classic.*");
                break;
            case Layer.Game when IsNamed(project.Name, "Elysium.Core") || IsNamed(project.Name, "Elysium.Classic"):
                Add("game projects must not use the Elysium.Core or Elysium.Classic names");
                break;
            default:
                break;
        }
    }

    private static bool IsNamed(string name, string baseName) =>
        name == baseName || name.StartsWith(baseName + ".", StringComparison.Ordinal);

    private static void CheckSolution(string root, List<Project> projects, List<Finding> findings)
    {
        var solutionFile = Path.Combine(root, RepositoryRoot.MarkerFile);
        var listed = SolutionProjectPattern().Matches(File.ReadAllText(solutionFile))
            .Select(match => match.Groups["path"].Value.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var project in projects.Where(project => !listed.Contains(project.RelativePath)))
        {
            findings.Add(new(Name, project.RelativePath, $"the project is not listed in {RepositoryRoot.MarkerFile}"));
        }

        foreach (var path in listed.Order(StringComparer.Ordinal).Where(path => !File.Exists(Path.Combine(root, path))))
        {
            findings.Add(new(Name, RepositoryRoot.MarkerFile, $"lists '{path}', which does not exist"));
        }
    }

    private static void CheckReferences(List<Project> projects, List<Finding> findings)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var byPath = projects.ToDictionary(project => project.FullPath, comparer);

        foreach (var project in projects)
        {
            var directory = Path.GetDirectoryName(project.FullPath)!;
            foreach (Match match in ProjectReferencePattern().Matches(File.ReadAllText(project.FullPath)))
            {
                var include = match.Groups["path"].Value.Replace('\\', '/');
                var target = Path.GetFullPath(Path.Combine(directory, include));
                if (!byPath.TryGetValue(target, out var referenced))
                {
                    findings.Add(new(Name, project.RelativePath, $"references '{include}', which does not exist"));
                    continue;
                }

                var problem = ReferenceProblem(project, referenced);
                if (problem is not null)
                {
                    findings.Add(new(Name, project.RelativePath, problem));
                }
            }
        }
    }

    private static string? ReferenceProblem(Project from, Project to)
    {
        var isProductCode = from.Layer is Layer.Core or Layer.Classic or Layer.Game;
        if (!isProductCode)
        {
            return null;
        }

        if (to.Layer is Layer.Test or Layer.Tool or Layer.Unknown)
        {
            return $"engine and game projects must not reference {to.Name}";
        }

        if (from.Layer == Layer.Core && to.Layer is Layer.Classic or Layer.Game)
        {
            return $"the core must not reference {to.Name} (INV-02)";
        }

        if (from.Layer == Layer.Classic && to.Layer == Layer.Game)
        {
            return $"Classic must not reference the game project {to.Name} (INV-02)";
        }

        var fromSide = SideOf(from.Name);
        var toSide = SideOf(to.Name);
        if (fromSide is Side.Shared or Side.Neutral && toSide is Side.Server or Side.Client)
        {
            return $"shared code must not reference {to.Name}";
        }

        if ((fromSide == Side.Server && toSide == Side.Client) || (fromSide == Side.Client && toSide == Side.Server))
        {
            return $"server and client code must not reference each other ({to.Name})";
        }

        return null;
    }

    [GeneratedRegex(@"<Project\s+Path=""(?<path>[^""]+)""")]
    private static partial Regex SolutionProjectPattern();

    [GeneratedRegex(@"<ProjectReference\s+Include=""(?<path>[^""]+)""")]
    private static partial Regex ProjectReferencePattern();
}
