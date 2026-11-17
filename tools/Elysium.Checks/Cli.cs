namespace Elysium.Checks;

/// <summary>
/// Command line entry point.
/// Usage: Elysium.Checks [docs] [structure] [all] [pr-summary --base REF] [--root PATH]
/// Exit codes: 0 = no problems, 1 = problems found, 2 = usage error.
/// </summary>
internal static class Cli
{
    public static int Run(IReadOnlyList<string> args, TextWriter output)
    {
        string? root = null;
        string? baseRef = null;
        var commands = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--root" when i + 1 < args.Count:
                    root = args[++i];
                    break;
                case "--base" when i + 1 < args.Count:
                    baseRef = args[++i];
                    break;
                case "docs" or "structure" or "all" or "pr-summary":
                    commands.Add(args[i]);
                    break;
                default:
                    output.WriteLine($"Unknown argument: {args[i]}");
                    PrintUsage(output);
                    return 2;
            }
        }

        if (commands.Count == 0)
        {
            PrintUsage(output);
            return 2;
        }

        root ??= RepositoryRoot.Find(Directory.GetCurrentDirectory());
        if (root is null)
        {
            output.WriteLine($"Could not find the repository root ({RepositoryRoot.MarkerFile}).");
            return 2;
        }

        if (commands.Contains("pr-summary"))
        {
            return PrSummary.Run(root, baseRef ?? "origin/main", output);
        }

        var all = commands.Contains("all");
        var findings = new List<Finding>();
        var ran = new List<string>();
        if (all || commands.Contains("docs"))
        {
            findings.AddRange(DocsCheck.Run(root));
            ran.Add("docs");
        }

        if (all || commands.Contains("structure"))
        {
            findings.AddRange(StructureCheck.Run(root));
            ran.Add("structure");
        }

        foreach (var finding in findings)
        {
            output.WriteLine(finding);
        }

        output.WriteLine(findings.Count == 0
            ? $"OK: {string.Join(", ", ran)}"
            : $"{findings.Count} problem(s) found.");
        return findings.Count == 0 ? 0 : 1;
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine("Usage: Elysium.Checks [docs] [structure] [all] [pr-summary --base REF] [--root PATH]");
        output.WriteLine("  docs        links, specs, STATUS, ADRs, TECH_DEBT and PROPOSALS");
        output.WriteLine("  structure   project names, folders, layers, references and Elysium.slnx");
        output.WriteLine("  all         docs + structure");
        output.WriteLine("  pr-summary  lists protected files and removed tests since REF (default origin/main)");
    }
}
