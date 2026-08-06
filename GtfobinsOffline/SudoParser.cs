using System.Text.RegularExpressions;
using System.IO;

namespace GtfobinsOffline;

public static class SudoParser
{
    private static readonly Regex PathPattern = new(@"(?<forbidden>!)?(?<path>/[A-Za-z0-9_+.,@%=-]+(?:/[A-Za-z0-9_+.,@%=-]+)+)", RegexOptions.Compiled);
    private static readonly Regex RunAsPattern = new(@"\((?<runas>[^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex TagsPattern = new(@"\b(?<tags>(?:NO)?PASSWD|SETENV|NOSETENV|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT)\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IReadOnlyList<BatchMatch> Parse(string input, IReadOnlyList<GtfobinEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        if (input.Length > 1_048_576) throw new ArgumentException("输入内容超过 1 MB，无法安全分析。");

        var exact = entries.ToDictionary(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
        var aliases = entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Alias))
            .GroupBy(entry => entry.Alias!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var results = new List<BatchMatch>();
        var listingStarted = false;

        foreach (var raw in input.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.Contains("secure_path", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Matching Defaults", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.Contains('(') && line.Contains(')') || TagsPattern.IsMatch(line)) listingStarted = true;
            if (!listingStarted) continue;

            var runAs = RunAsPattern.Match(line) is { Success: true } runAsMatch ? runAsMatch.Groups["runas"].Value : null;
            var tags = string.Join(", ", TagsPattern.Matches(line).Select(match => match.Groups["tags"].Value.ToUpperInvariant()));
            // A sudo rule can contain more than one authorized command separated by commas.
            // Only the first path in each rule item is the executable; later paths are often arguments
            // (for example: /usr/bin/find --exec /bin/sh) and must not become false matches.
            foreach (var ruleItem in line.Split(','))
            {
                var pathMatch = PathPattern.Match(ruleItem);
                if (!pathMatch.Success) continue;
                var path = pathMatch.Groups["path"].Value;
                var name = Path.GetFileName(path);
                var forbidden = pathMatch.Groups["forbidden"].Success;
                exact.TryGetValue(name, out var entry);
                var kind = entry is not null ? MatchKind.Exact : MatchKind.NotFound;
                if (entry is null && aliases.TryGetValue(name, out var aliasEntry)) { entry = aliasEntry; kind = MatchKind.OfficialAlias; }
                if (entry is null && TryFindFamily(entries, name, out var familyEntry)) { entry = familyEntry; kind = MatchKind.Family; }
                if (forbidden) kind = MatchKind.Forbidden;

                results.Add(new BatchMatch
                {
                    OriginalLine = raw.Trim(), Path = path, CommandName = name, Entry = entry, Kind = kind,
                    IsForbidden = forbidden, RunAs = runAs, Tags = string.IsNullOrWhiteSpace(tags) ? null : tags
                });
            }
        }

        return results.DistinctBy(result => $"{result.OriginalLine}|{result.Path}").ToArray();
    }

    /// <summary>
    /// Automatically detects whether the input looks like <c>sudo -l</c> rules or a plain
    /// SUID path list and routes the whole input to the matching parser. When any line contains
    /// a RunAs group or a sudo tag such as <c>NOPASSWD:</c> the input is treated as sudo output
    /// (preserving sudo rule continuation lines); otherwise absolute paths are matched against
    /// the SUID entries.
    /// </summary>
    public static IReadOnlyList<BatchMatch> ParseAuto(string input, IReadOnlyList<GtfobinEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        if (input.Length > 1_048_576) throw new ArgumentException("输入内容超过 1 MB，无法安全分析。");

        var lines = input.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        var normalized = string.Join(Environment.NewLine, lines);
        if (lines.Any(IsSudoRuleLine)) return Parse(normalized, entries);
        if (lines.Any(line => PathPattern.IsMatch(line))) return ParseSuid(normalized, entries);
        return [];
    }

    /// <summary>
    /// Analyzes a plain list of SUID file paths (for example the output of
    /// <c>find / -perm -u=s -type f 2&gt;/dev/null</c>) and matches each path
    /// against the GTFOBins entries that have an official <c>suid</c> usage.
    /// </summary>
    public static IReadOnlyList<BatchMatch> ParseSuid(string input, IReadOnlyList<GtfobinEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        if (input.Length > 1_048_576) throw new ArgumentException("输入内容超过 1 MB，无法安全分析。");

        var exact = entries.ToDictionary(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
        var aliases = entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Alias))
            .GroupBy(entry => entry.Alias!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var results = new List<BatchMatch>();

        foreach (var raw in input.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            var pathMatch = PathPattern.Match(line);
            if (!pathMatch.Success) continue;
            var path = pathMatch.Groups["path"].Value;
            var name = Path.GetFileName(path);

            GtfobinEntry? entry = null;
            var kind = MatchKind.NotFound;
            if (exact.TryGetValue(name, out var exactEntry))
            {
                entry = exactEntry;
                kind = HasSuidUsage(entry) ? MatchKind.Exact : MatchKind.NoSuid;
            }
            else if (aliases.TryGetValue(name, out var aliasEntry))
            {
                entry = aliasEntry;
                kind = HasSuidUsage(aliasEntry) ? MatchKind.OfficialAlias : MatchKind.NoSuid;
            }
            else if (TryFindFamily(entries, name, out var familyEntry) && HasSuidUsage(familyEntry))
            {
                entry = familyEntry;
                kind = MatchKind.Family;
            }

            results.Add(new BatchMatch
            {
                OriginalLine = raw.Trim(),
                Path = path,
                CommandName = name,
                Entry = entry,
                Kind = kind,
                IsSuidAnalysis = true
            });
        }

        return results.DistinctBy(result => $"{result.OriginalLine}|{result.Path}").ToArray();
    }

    private static bool HasSuidUsage(GtfobinEntry? entry)
        => entry is not null && entry.Variants.Any(variant => string.Equals(variant.Context, "suid", StringComparison.OrdinalIgnoreCase));

    private static bool IsSudoRuleLine(string line)
        => (line.Contains('(') && line.Contains(')')) || TagsPattern.IsMatch(line);

    private static bool TryFindFamily(IReadOnlyList<GtfobinEntry> entries, string name, out GtfobinEntry? entry)
    {
        var family = Regex.Replace(name, @"\d+(?:\.\d+)*$", string.Empty);
        entry = entries.FirstOrDefault(candidate => string.Equals(candidate.Name, family, StringComparison.OrdinalIgnoreCase));
        return entry is not null && !string.Equals(family, name, StringComparison.OrdinalIgnoreCase);
    }
}
