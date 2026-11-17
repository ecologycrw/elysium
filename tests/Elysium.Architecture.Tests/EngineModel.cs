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
    /// Returns every dependency from a type of an assembly that matches <paramref name="isSource"/> to a type of an
    /// assembly that matches <paramref name="isForbiddenTarget"/>, in every engine and game project. Both receive the
    /// assembly name, which is the project name (for example <c>Elysium.Classic.Client</c>).
    /// </summary>
    public static List<string> ForbiddenDependencies(Func<string, bool> isSource, Func<string, bool> isForbiddenTarget) =>
        ForbiddenDependencies(Model, isSource, isForbiddenTarget);

    /// <summary>
    /// Same as above, in any <paramref name="model"/>. Everything is identified by the assembly it comes from, not by
    /// its namespace (P-0005), and checked twice:
    /// <list type="bullet">
    /// <item>each type's dependencies, which name the type that breaks a rule. A dependency whose assembly is unknown
    /// is skipped: it is never an Elysium type, only something the compiler generates (P-0004);</item>
    /// <item>each assembly's references to other assemblies. ArchUnitNET leaves out the methods the compiler
    /// generates, among them the one that holds the top-level statements of a <c>Program.cs</c>; but the compiler
    /// only references an assembly whose types the code uses, so a forbidden use there still shows up.</item>
    /// </list>
    /// If reading a type or an assembly fails for any other reason, the error names it.
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

        foreach (var assembly in model.Assemblies)
        {
            try
            {
                AddForbiddenReferences(assembly, isSource, isForbiddenTarget, violations);
            }
            catch (Exception exception)
            {
                // Broad on purpose, as above: the message must say which assembly it was.
                throw new InvalidOperationException($"Could not check the references of {assembly.Name}: {exception.Message}", exception);
            }
        }

        return [.. violations];
    }

    private static void AddForbiddenReferences(
        ArchUnitNET.Domain.Assembly assembly, Func<string, bool> isSource, Func<string, bool> isForbiddenTarget, SortedSet<string> violations)
    {
        var source = AssemblyName(assembly.Name);
        if (assembly.IsOnlyReferenced || !isSource(source))
        {
            return;
        }

        foreach (var reference in assembly.ReferencedAssemblyNames.Select(AssemblyName))
        {
            if (isForbiddenTarget(reference))
            {
                violations.Add($"{source} (assembly) -> {reference}");
            }
        }
    }

    private static void AddForbiddenDependencies(
        IType type, Func<string, bool> isSource, Func<string, bool> isForbiddenTarget, SortedSet<string> violations)
    {
        if (type.Assembly?.Name is not { } sourceAssembly || !isSource(AssemblyName(sourceAssembly)))
        {
            return;
        }

        foreach (var dependency in type.Dependencies)
        {
            if (dependency.Target.Assembly?.Name is { } targetAssembly && isForbiddenTarget(AssemblyName(targetAssembly)))
            {
                violations.Add($"{type.FullName} -> {dependency.Target.FullName}");
            }
        }
    }

    /// <summary>The simple name of an assembly ("Elysium.Core"), whether ArchUnitNET gives it simple or in full ("Elysium.Core, Version=...").</summary>
    private static string AssemblyName(string name) => new AssemblyName(name).Name ?? name;

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
