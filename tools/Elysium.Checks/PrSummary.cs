using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Elysium.Checks;

/// <summary>
/// Writes the safety summary of a pull request: protected files that changed and tests that were removed.
/// The text is in Portuguese because the owner of the product reads it in the PR.
/// It never fails the build; it only makes these changes impossible to miss.
/// </summary>
internal static class PrSummary
{
    private static readonly string[] ProtectedPrefixes =
    [
        ".claude/", ".githooks/", ".github/", "tools/Elysium.Checks/", "tests/Elysium.Architecture.Tests/",
    ];

    private static readonly string[] ProtectedFiles =
    [
        "AGENTS.md", "CLAUDE.md", "global.json", "src/BannedSymbols.txt", "docs/VISION.md", "docs/ROADMAP.md",
    ];

    private static readonly string[] ProtectedFileNames =
    [
        "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", ".editorconfig",
    ];

    public static int Run(string root, string baseRef, TextWriter output)
    {
        var range = baseRef + "...HEAD";
        var nameStatus = Git(root, "diff", "--name-status", range);
        var testDiff = Git(root, "diff", "-U0", range, "--", "tests/");
        if (nameStatus is null || testDiff is null)
        {
            output.WriteLine($"git diff failed for '{range}'.");
            return 1;
        }

        var markdown = Build(ParseNameStatus(nameStatus), testDiff);
        output.Write(markdown);

        var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(summaryFile))
        {
            File.AppendAllText(summaryFile, markdown);
        }

        return 0;
    }

    public static bool IsProtected(string path) =>
        ProtectedFiles.Contains(path)
        || ProtectedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal))
        || ProtectedFileNames.Contains(Path.GetFileName(path));

    public static List<(string Status, string Path)> ParseNameStatus(string text)
    {
        var changes = new List<(string Status, string Path)>();
        foreach (var line in Markdown.SplitLines(text).Where(line => line.Length > 0))
        {
            var parts = line.Split('\t');
            foreach (var path in parts.Skip(1))
            {
                changes.Add((parts[0][..1], path));
            }
        }

        return changes;
    }

    public static string Build(IReadOnlyList<(string Status, string Path)> changes, string testDiff)
    {
        var protectedChanges = changes.Where(change => IsProtected(change.Path)).ToList();
        var deletedTestFiles = changes
            .Where(change => change.Status == "D" && change.Path.StartsWith("tests/", StringComparison.Ordinal)
                && change.Path.EndsWith(".cs", StringComparison.Ordinal))
            .ToList();
        var (added, removed) = CountTestAttributes(testDiff);

        var text = new StringBuilder();
        text.AppendLine("## Resumo de segurança do PR").AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture, $"### Travas alteradas ({protectedChanges.Count})").AppendLine();
        if (protectedChanges.Count == 0)
        {
            text.AppendLine("Nenhuma.");
        }
        else
        {
            foreach (var (status, path) in protectedChanges)
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"- `{path}` ({Describe(status)})");
            }

            text.AppendLine().AppendLine("> **Atenção:** mudanças em travas exigem a aprovação explícita do dono (docs/WORKFLOW.md).");
        }

        text.AppendLine().AppendLine("### Testes").AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture, $"- Testes adicionados: {added}");
        text.AppendLine(CultureInfo.InvariantCulture, $"- Testes removidos: {removed}");
        text.AppendLine(CultureInfo.InvariantCulture, $"- Arquivos de teste apagados: {deletedTestFiles.Count}");
        foreach (var (_, path) in deletedTestFiles)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"  - `{path}`");
        }

        if (removed > 0 || deletedTestFiles.Count > 0)
        {
            text.AppendLine().AppendLine("> **Atenção:** testes foram removidos. O report da fatia precisa explicar o motivo de cada remoção.");
        }

        return text.ToString();
    }

    private static (int Added, int Removed) CountTestAttributes(string diff)
    {
        var added = 0;
        var removed = 0;
        foreach (var line in Markdown.SplitLines(diff))
        {
            if (line.StartsWith("+++", StringComparison.Ordinal) || line.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var isTest = line.Contains("[Fact", StringComparison.Ordinal) || line.Contains("[Theory", StringComparison.Ordinal);
            if (isTest && line.StartsWith('+'))
            {
                added++;
            }
            else if (isTest && line.StartsWith('-'))
            {
                removed++;
            }
        }

        return (added, removed);
    }

    private static string Describe(string status) => status switch
    {
        "A" => "criado",
        "D" => "apagado",
        "R" => "renomeado",
        _ => "alterado",
    };

    private static string? Git(string root, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        _ = errors.Result;
        return process.ExitCode == 0 ? output : null;
    }
}
