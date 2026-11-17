namespace Elysium.Checks;

/// <summary>
/// Finds the repository root and walks its files, skipping build output and tool folders.
/// </summary>
internal static class RepositoryRoot
{
    public const string MarkerFile = "Elysium.slnx";

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "bin", "obj", "node_modules", "TestResults", "StrykerOutput",
    };

    public static string? Find(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, MarkerFile)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>Path relative to the root, always with forward slashes.</summary>
    public static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    public static bool IsInside(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>Every file matching the pattern, sorted, without build output and tool folders.</summary>
    public static List<string> Files(string directory, string pattern)
    {
        var files = new List<string>();
        Collect(directory, pattern, files);
        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private static void Collect(string directory, string pattern, List<string> files)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        files.AddRange(Directory.EnumerateFiles(directory, pattern));
        foreach (var subdirectory in Directory.EnumerateDirectories(directory))
        {
            if (!IgnoredDirectories.Contains(Path.GetFileName(subdirectory)))
            {
                Collect(subdirectory, pattern, files);
            }
        }
    }
}
