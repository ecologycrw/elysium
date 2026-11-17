using System.Reflection;
using ArchUnitNET.Loader;
using ArchModel = ArchUnitNET.Domain.Architecture;

namespace Elysium.Architecture.Tests;

/// <summary>
/// Finds every engine and game project (src/ and samples/) and loads its assembly for the architecture rules.
/// </summary>
internal static class EngineModel
{
    private static readonly Lazy<IReadOnlyList<string>> LazyProjectNames = new(FindProjectNames);
    private static readonly Lazy<ArchModel> LazyModel = new(BuildModel);

    public static IReadOnlyList<string> ProjectNames => LazyProjectNames.Value;

    public static ArchModel Model => LazyModel.Value;

    public static Assembly? TryLoad(string assemblyName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns every dependency from a type whose namespace matches <paramref name="isSource"/>
    /// to a type whose namespace matches <paramref name="isForbiddenTarget"/>.
    /// </summary>
    public static List<string> ForbiddenDependencies(Func<string, bool> isSource, Func<string, bool> isForbiddenTarget)
    {
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in Model.Types)
        {
            if (!isSource(type.Namespace.FullName))
            {
                continue;
            }

            foreach (var dependency in type.Dependencies)
            {
                if (isForbiddenTarget(dependency.Target.Namespace.FullName))
                {
                    violations.Add($"{type.FullName} -> {dependency.Target.FullName}");
                }
            }
        }

        return [.. violations];
    }

    private static ArchModel BuildModel()
    {
        var assemblies = ProjectNames.Select(TryLoad).OfType<Assembly>().ToArray();
        return new ArchLoader().LoadAssemblies(assemblies).Build();
    }

    private static List<string> FindProjectNames()
    {
        var root = FindRepositoryRoot();
        var names = new List<string>();
        foreach (var folder in new[] { "src", "samples" })
        {
            var path = Path.Combine(root, folder);
            if (!Directory.Exists(path))
            {
                continue;
            }

            names.AddRange(Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)
                .Where(file => !IsBuildOutput(file))
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>());
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private static bool IsBuildOutput(string file)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var segments = file.Split(separators);
        return segments.Contains("bin") || segments.Contains("obj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elysium.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find the repository root (Elysium.slnx).");
    }
}
