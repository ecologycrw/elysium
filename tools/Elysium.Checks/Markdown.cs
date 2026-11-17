using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Elysium.Checks;

/// <summary>
/// Small Markdown helpers: front matter, links, headings and GitHub-style anchors.
/// </summary>
internal static partial class Markdown
{
    public static string[] SplitLines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    /// <summary>
    /// Reads "key: value" lines between the two "---" lines at the top of the file.
    /// Anything after " #" on a line is a comment.
    /// </summary>
    public static Dictionary<string, string> ReadFrontMatter(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = SplitLines(text);
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return result;
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim() == "---")
            {
                break;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }

            var value = line[(colon + 1)..];
            var comment = value.IndexOf(" #", StringComparison.Ordinal);
            if (comment >= 0)
            {
                value = value[..comment];
            }

            result[line[..colon].Trim()] = value.Trim();
        }

        return result;
    }

    /// <summary>
    /// The lines of the file with fenced code blocks blanked out and inline code removed,
    /// so that examples inside code are never treated as real links or definitions.
    /// Line numbers are preserved.
    /// </summary>
    public static string[] ProseLines(string text) => BlankFences(text, removeInlineCode: true);

    public static IEnumerable<(string Target, int Line)> Links(string text)
    {
        var lines = ProseLines(text);
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (Match match in LinkPattern().Matches(lines[i]))
            {
                yield return (match.Groups["target"].Value, i + 1);
            }
        }
    }

    /// <summary>
    /// The anchors GitHub generates for the headings of a file. Repeated headings get "-1", "-2"...
    /// </summary>
    public static HashSet<string> Anchors(string text)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in BlankFences(text, removeInlineCode: false))
        {
            var match = HeadingPattern().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var slug = Slug(match.Groups["text"].Value);
            if (anchors.Add(slug))
            {
                continue;
            }

            var suffix = 1;
            while (!anchors.Add(string.Create(CultureInfo.InvariantCulture, $"{slug}-{suffix}")))
            {
                suffix++;
            }
        }

        return anchors;
    }

    /// <summary>
    /// GitHub's heading slug: lower case, punctuation removed (letters with accents are kept),
    /// spaces turned into hyphens.
    /// </summary>
    public static string Slug(string heading)
    {
        var text = LinkTextPattern().Replace(heading, "${text}").Replace("`", string.Empty, StringComparison.Ordinal);
#pragma warning disable CA1308 // GitHub lower-cases anchors; upper-casing would give a different anchor.
        text = text.Trim().ToLowerInvariant();
#pragma warning restore CA1308
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == ' ')
            {
                builder.Append('-');
            }
            else if (char.IsLetterOrDigit(c) || c is '-' or '_' || IsMark(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static bool IsMark(char c) => CharUnicodeInfo.GetUnicodeCategory(c)
        is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    private static string[] BlankFences(string text, bool removeInlineCode)
    {
        var lines = SplitLines(text);
        var result = new string[lines.Length];
        string? fence = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (fence is null && (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal)))
            {
                fence = trimmed[..3];
                result[i] = string.Empty;
                continue;
            }

            if (fence is not null)
            {
                if (trimmed.StartsWith(fence, StringComparison.Ordinal))
                {
                    fence = null;
                }

                result[i] = string.Empty;
                continue;
            }

            result[i] = removeInlineCode ? InlineCodePattern().Replace(lines[i], string.Empty) : lines[i];
        }

        return result;
    }

    [GeneratedRegex(@"!?\[(?:[^\]\\]|\\.)*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+(?<text>.+?)\s*#*\s*$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"\[(?<text>[^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkTextPattern();
}
