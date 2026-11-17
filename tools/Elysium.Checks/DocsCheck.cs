using System.Globalization;
using System.Text.RegularExpressions;

namespace Elysium.Checks;

/// <summary>
/// Keeps the documentation aligned: links and anchors, specs, STATUS, ADRs, TECH_DEBT and PROPOSALS.
/// The rules come from docs/WORKFLOW.md and docs/adr/0010-documentacao.md.
/// </summary>
internal static partial class DocsCheck
{
    private const string Name = "docs";

    private static readonly string[] SpecStatuses = ["rascunho", "aprovada", "em-andamento", "concluida"];
    private static readonly string[] AdrStatuses = ["proposta", "aceita", "substituida", "rejeitada"];
    private static readonly string[] ProposalStatuses = ["aberta", "aceita", "descartada"];

    public static List<Finding> Run(string root)
    {
        var findings = new List<Finding>();
        CheckLinks(root, findings);
        CheckSpecs(root, findings);
        CheckStatus(root, findings);
        CheckAdrs(root, findings);
        CheckTechDebt(root, findings);
        CheckProposals(root, findings);
        return findings;
    }

    private static void CheckLinks(string root, List<Finding> findings)
    {
        foreach (var file in RepositoryRoot.Files(root, "*.md"))
        {
            var relative = RepositoryRoot.Relative(root, file);
            foreach (var (target, line) in Markdown.Links(File.ReadAllText(file)))
            {
                if (target.Contains("://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    continue;
                }

                var hash = target.IndexOf('#', StringComparison.Ordinal);
                var pathPart = hash >= 0 ? target[..hash] : target;
                var anchor = hash >= 0 ? Uri.UnescapeDataString(target[(hash + 1)..]) : null;

                var resolved = file;
                if (pathPart.Length > 0)
                {
                    var decoded = Uri.UnescapeDataString(pathPart);
                    resolved = decoded.StartsWith('/')
                        ? Path.GetFullPath(Path.Combine(root, decoded.TrimStart('/')))
                        : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded));
                }

                if (!RepositoryRoot.IsInside(root, resolved))
                {
                    findings.Add(new(Name, relative, $"line {line}: link '{target}' points outside the repository"));
                    continue;
                }

                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    findings.Add(new(Name, relative, $"line {line}: broken link '{target}'"));
                    continue;
                }

                if (!string.IsNullOrEmpty(anchor) && File.Exists(resolved)
                    && resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    && !Markdown.Anchors(File.ReadAllText(resolved)).Contains(anchor))
                {
                    findings.Add(new(Name, relative, $"line {line}: anchor '#{anchor}' does not exist in '{RepositoryRoot.Relative(root, resolved)}'"));
                }
            }
        }
    }

    private static void CheckSpecs(string root, List<Finding> findings)
    {
        var specsDirectory = Path.Combine(root, "specs");
        if (!Directory.Exists(specsDirectory))
        {
            return;
        }

        var testSources = new Lazy<string>(() => string.Join('\n',
            RepositoryRoot.Files(Path.Combine(root, "tests"), "*.cs").Select(File.ReadAllText)));

        foreach (var directory in Directory.EnumerateDirectories(specsDirectory).Order(StringComparer.Ordinal))
        {
            var folder = Path.GetFileName(directory);
            var relative = "specs/" + folder;
            if (!SpecFolderPattern().IsMatch(folder))
            {
                findings.Add(new(Name, relative, "spec folders must be named NNNN-name-in-kebab-case"));
                continue;
            }

            var specFile = Path.Combine(directory, "spec.md");
            if (!File.Exists(specFile))
            {
                findings.Add(new(Name, relative, "spec.md is missing"));
                continue;
            }

            var number = folder[..4];
            var specRelative = relative + "/spec.md";
            var specText = File.ReadAllText(specFile);
            var frontMatter = Markdown.ReadFrontMatter(specText);

            if (frontMatter.GetValueOrDefault("id") != number)
            {
                findings.Add(new(Name, specRelative, $"front matter 'id' must be {number}"));
            }

            if (string.IsNullOrWhiteSpace(frontMatter.GetValueOrDefault("titulo")))
            {
                findings.Add(new(Name, specRelative, "front matter 'titulo' is missing"));
            }

            var status = frontMatter.GetValueOrDefault("status") ?? string.Empty;
            if (!SpecStatuses.Contains(status))
            {
                findings.Add(new(Name, specRelative, $"front matter 'status' must be one of: {string.Join(", ", SpecStatuses)}"));
                continue;
            }

            foreach (var required in RequiredSpecFiles(status))
            {
                if (!File.Exists(Path.Combine(directory, required)))
                {
                    findings.Add(new(Name, relative, $"status '{status}' requires {required}"));
                }
            }

            var criteria = Markdown.ProseLines(specText)
                .Select(line => CriterionPattern().Match(line))
                .Where(match => match.Success)
                .Select(match => match.Groups["id"].Value)
                .ToList();

            foreach (var duplicate in criteria.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                findings.Add(new(Name, specRelative, $"acceptance criterion {duplicate.Key} is defined more than once"));
            }

            if (status != "rascunho" && criteria.Count == 0)
            {
                findings.Add(new(Name, specRelative, "an approved spec needs at least one acceptance criterion (- CA-01: ...)"));
            }

            var reportFile = Path.Combine(directory, "report.md");
            if (status == "concluida" && File.Exists(reportFile))
            {
                CheckCriteriaProof(number, relative + "/report.md", criteria.Distinct(StringComparer.Ordinal),
                    Markdown.SplitLines(File.ReadAllText(reportFile)), testSources, findings);
            }
        }
    }

    private static void CheckCriteriaProof(string number, string reportRelative, IEnumerable<string> criteria,
        string[] reportLines, Lazy<string> testSources, List<Finding> findings)
    {
        foreach (var criterion in criteria)
        {
            var mentions = reportLines.Where(line => line.Contains(criterion, StringComparison.Ordinal)).ToList();
            if (mentions.Count == 0)
            {
                findings.Add(new(Name, reportRelative, $"{criterion} is not mentioned in the report"));
                continue;
            }

            var traitId = $"{number}-{criterion}";
            var traitPattern = new Regex(@"\[Trait\(\s*""CA""\s*,\s*""" + Regex.Escape(traitId) + @"""\s*\)\]", RegexOptions.CultureInvariant);
            var hasTest = traitPattern.IsMatch(testSources.Value);
            var hasManualCheck = mentions.Any(line => line.Contains("manual", StringComparison.OrdinalIgnoreCase));
            if (!hasTest && !hasManualCheck)
            {
                findings.Add(new(Name, reportRelative,
                    $"{criterion} has no proof: no test with [Trait(\"CA\", \"{traitId}\")] and no manual verification in the report"));
            }
        }
    }

    private static string[] RequiredSpecFiles(string status) => status switch
    {
        "em-andamento" => ["spec.md", "plan.md", "tasks.md"],
        "concluida" => ["spec.md", "plan.md", "tasks.md", "report.md"],
        _ => ["spec.md", "plan.md"],
    };

    private static void CheckStatus(string root, List<Finding> findings)
    {
        const string Relative = "docs/STATUS.md";
        var file = Path.Combine(root, "docs", "STATUS.md");
        if (!File.Exists(file))
        {
            findings.Add(new(Name, Relative, "the file is missing"));
            return;
        }

        var frontMatter = Markdown.ReadFrontMatter(File.ReadAllText(file));
        if (string.IsNullOrWhiteSpace(frontMatter.GetValueOrDefault("fase")))
        {
            findings.Add(new(Name, Relative, "front matter 'fase' is missing"));
        }

        var spec = frontMatter.GetValueOrDefault("spec-atual") ?? string.Empty;
        if (spec.Length == 0)
        {
            return;
        }

        if (!SpecNumberPattern().IsMatch(spec))
        {
            findings.Add(new(Name, Relative, "'spec-atual' must be a four-digit number or empty"));
            return;
        }

        var specsDirectory = Path.Combine(root, "specs");
        if (!Directory.Exists(specsDirectory) || !Directory.EnumerateDirectories(specsDirectory, spec + "-*").Any())
        {
            findings.Add(new(Name, Relative, $"'spec-atual' is {spec}, but there is no specs/{spec}-* folder"));
        }
    }

    private static void CheckAdrs(string root, List<Finding> findings)
    {
        var directory = Path.Combine(root, "docs", "adr");
        if (!Directory.Exists(directory))
        {
            return;
        }

        var adrs = new SortedDictionary<int, string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.md").Order(StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(file);
            if (fileName == "README.md")
            {
                continue;
            }

            var match = AdrFilePattern().Match(fileName);
            if (!match.Success)
            {
                findings.Add(new(Name, "docs/adr/" + fileName, "ADR files must be named NNNN-name-in-kebab-case.md"));
                continue;
            }

            if (!adrs.TryAdd(int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture), fileName))
            {
                findings.Add(new(Name, "docs/adr/" + fileName, "another ADR already uses this number"));
            }
        }

        var expected = 1;
        foreach (var (number, fileName) in adrs)
        {
            var relative = "docs/adr/" + fileName;
            if (number != expected)
            {
                findings.Add(new(Name, relative, $"ADR numbers must have no gaps: expected {expected:D4}"));
            }

            expected = number + 1;
            var frontMatter = Markdown.ReadFrontMatter(File.ReadAllText(Path.Combine(directory, fileName)));
            if (frontMatter.GetValueOrDefault("id") != fileName[..4])
            {
                findings.Add(new(Name, relative, $"front matter 'id' must be {fileName[..4]}"));
            }

            if (string.IsNullOrWhiteSpace(frontMatter.GetValueOrDefault("titulo")))
            {
                findings.Add(new(Name, relative, "front matter 'titulo' is missing"));
            }

            var status = frontMatter.GetValueOrDefault("status") ?? string.Empty;
            if (!AdrStatuses.Contains(status))
            {
                findings.Add(new(Name, relative, $"front matter 'status' must be one of: {string.Join(", ", AdrStatuses)}"));
            }
            else if (status == "substituida")
            {
                var replacement = frontMatter.GetValueOrDefault("substituida-por") ?? string.Empty;
                if (!int.TryParse(replacement, NumberStyles.None, CultureInfo.InvariantCulture, out var replacementNumber)
                    || !adrs.ContainsKey(replacementNumber))
                {
                    findings.Add(new(Name, relative, "a replaced ADR needs 'substituida-por' with the number of an existing ADR"));
                }
            }
        }

        var indexFile = Path.Combine(directory, "README.md");
        var index = File.Exists(indexFile) ? File.ReadAllText(indexFile) : null;
        foreach (var fileName in adrs.Values)
        {
            if (index is null || !index.Contains($"({fileName})", StringComparison.Ordinal))
            {
                findings.Add(new(Name, "docs/adr/README.md", $"the ADR index does not link to {fileName}"));
            }
        }
    }

    private static void CheckTechDebt(string root, List<Finding> findings)
    {
        const string Relative = "docs/TECH_DEBT.md";
        var file = Path.Combine(root, "docs", "TECH_DEBT.md");
        if (!File.Exists(file))
        {
            return;
        }

        var lines = Markdown.ProseLines(File.ReadAllText(file));
        int? destinationColumn = null;
        var rowInTable = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith('|'))
            {
                destinationColumn = null;
                rowInTable = 0;
                continue;
            }

            rowInTable++;
            var cells = trimmed.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            if (rowInTable == 1)
            {
                var index = Array.FindIndex(cells, cell => cell.Equals("Destino", StringComparison.OrdinalIgnoreCase));
                destinationColumn = index >= 0 ? index : null;
                continue;
            }

            if (rowInTable == 2 || destinationColumn is not { } column)
            {
                continue;
            }

            var destination = column < cells.Length ? cells[column] : string.Empty;
            if (!DebtDestinationPattern().IsMatch(destination))
            {
                findings.Add(new(Name, Relative, $"line {i + 1}: every item needs a destination like 'fatia 3' (found '{destination}')"));
            }
        }
    }

    private static void CheckProposals(string root, List<Finding> findings)
    {
        const string Relative = "docs/PROPOSALS.md";
        var file = Path.Combine(root, "docs", "PROPOSALS.md");
        if (!File.Exists(file))
        {
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        string? current = null;
        var hasStatus = false;

        void Close()
        {
            if (current is not null && !hasStatus)
            {
                findings.Add(new(Name, Relative, $"{current} needs a '- Status:' line with one of: {string.Join(", ", ProposalStatuses)}"));
            }
        }

        foreach (var line in Markdown.ProseLines(File.ReadAllText(file)))
        {
            var heading = ProposalHeadingPattern().Match(line);
            if (heading.Success)
            {
                Close();
                current = heading.Groups["id"].Value;
                hasStatus = false;
                if (!ids.Add(current))
                {
                    findings.Add(new(Name, Relative, $"{current} is used more than once"));
                }

                continue;
            }

            if (line.TrimStart().StartsWith('#'))
            {
                Close();
                current = null;
                continue;
            }

            var status = ProposalStatusPattern().Match(line);
            if (current is not null && status.Success && ProposalStatuses.Contains(status.Groups["value"].Value))
            {
                hasStatus = true;
            }
        }

        Close();
    }

    [GeneratedRegex(@"^\d{4}-[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SpecFolderPattern();

    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex SpecNumberPattern();

    [GeneratedRegex(@"^\s*[-*]\s+\**(?<id>CA-\d{2})\**\s*:")]
    private static partial Regex CriterionPattern();

    [GeneratedRegex(@"^(?<number>\d{4})-[a-z0-9]+(?:-[a-z0-9]+)*\.md$")]
    private static partial Regex AdrFilePattern();

    [GeneratedRegex(@"^fatia \d+$", RegexOptions.IgnoreCase)]
    private static partial Regex DebtDestinationPattern();

    [GeneratedRegex(@"^###\s+(?<id>P-\d{4})\b")]
    private static partial Regex ProposalHeadingPattern();

    [GeneratedRegex(@"^\s*[-*]\s+Status:\s*(?<value>[\p{L}-]+)")]
    private static partial Regex ProposalStatusPattern();
}
