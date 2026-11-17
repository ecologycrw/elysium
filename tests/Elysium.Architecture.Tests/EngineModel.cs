using System.Reflection;
using ArchUnitNET.Loader;
using IType = ArchUnitNET.Domain.IType;
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
    /// to a type whose namespace matches <paramref name="isForbiddenTarget"/>, in every engine and game project.
    /// </summary>
    public static List<string> ForbiddenDependencies(Func<string, bool> isSource, Func<string, bool> isForbiddenTarget) =>
        ForbiddenDependencies(Model, isSource, isForbiddenTarget);

    /// <summary>
    /// Same as above, in any <paramref name="model"/>. A dependency without a namespace is skipped: it is never an
    /// Elysium type, only something the compiler generates, like the type parameters of the inline array helpers
    /// behind <c>Task.WhenAll(first, second)</c> (P-0004). If reading a type fails for any other reason, the error
    /// names that type.
    /// </summary>
    public static List<string> ForbiddenDependencies(ArchModel model, Func<string, bool> isSource, Func<string, bool> isForbiddenTarget)
    {
        ArgumentNullException.ThrowIfNull(model);
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in model.Types)
        {
            try
            {
                AddForbiddenDependencies(type, isSource, isForbiddenTarget, violations);
            }
            catch (Exception exception)
            {
                // Broad on purpose: whatever went wrong, the message must say which type it was (P-0004).
                throw new InvalidOperationException($"Could not check the dependencies of {type.FullName}: {exception.Message}", exception);
            }
        }

        return [.. violations];
    }

    private static void AddForbiddenDependencies(
        IType type, Func<string, bool> isSource, Func<string, bool> isForbiddenTarget, SortedSet<string> violations)
    {
        if (type.Namespace?.FullName is not { } sourceNamespace || !isSource(sourceNamespace))
        {
            return;
        }

        foreach (var dependency in type.Dependencies)
        {
            if (dependency.Target.Namespace?.FullName is { } targetNamespace && isForbiddenTarget(targetNamespace))
            {
                violations.Add($"{type.FullName} -> {dependency.Target.FullName}");
            }
        }
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
