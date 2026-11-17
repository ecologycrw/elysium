using System.Globalization;

namespace Elysium.Checks.Tests;

/// <summary>
/// A throwaway repository in the temp folder. Fixtures are written here, never into the real repository,
/// so the real checks never see the broken examples.
/// Spec numbers in fixtures use 9999 so they can never be confused with real specs.
/// </summary>
internal sealed class TempRepository : IDisposable
{
    public TempRepository()
    {
        Root = Path.Combine(Path.GetTempPath(), "elysium-checks-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Root);
        Write("Elysium.slnx", "<Solution>\n</Solution>\n");
    }

    public string Root { get; }

    /// <summary>A repository that passes every docs check.</summary>
    public static TempRepository WithValidDocs() => new TempRepository()
        .Write("docs/STATUS.md", "---\nfase: preparacao\nspec-atual:\n---\n\n# Status\n");

    public TempRepository Write(string relativePath, string content)
    {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return this;
    }

    public List<Finding> Docs() => DocsCheck.Run(Root);

    public List<Finding> Structure() => StructureCheck.Run(Root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless.
        }
    }
}
