using System.IO;
using System.Reflection;

namespace GtfobinsOffline;

public static class GtfobinsDataLoader
{
    public static IReadOnlyList<GtfobinEntry> Load()
    {
        var assembly = typeof(GtfobinsDataLoader).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("GtfobinsData/", StringComparison.Ordinal) && !name.EndsWith("LICENSE", StringComparison.Ordinal))
            .Select(name => ParseEntry(name["GtfobinsData/".Length..], ReadResource(assembly, name)))
            .Where(entry => entry.Variants.Count > 0)
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing embedded data: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static GtfobinEntry ParseEntry(string name, string document)
    {
        var lines = document.Replace("\r\n", "\n").Split('\n');
        var entry = new GtfobinEntry { Name = name };
        string? currentFunction = null;
        VariantBuilder? currentVariant = null;

        void FinishVariant()
        {
            if (currentVariant is null || string.IsNullOrWhiteSpace(currentVariant.Code)) return;
            foreach (var context in currentVariant.Contexts)
            {
                var code = currentVariant.ContextCodes.TryGetValue(context, out var overrideCode) ? overrideCode : currentVariant.Code;
                entry.Variants.Add(new CommandVariant(currentVariant.Function, context, code, currentVariant.Comment));
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var indent = CountIndent(line);
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            if (indent == 0 && trimmed.StartsWith("alias:", StringComparison.Ordinal))
            {
                entry = new GtfobinEntry { Name = entry.Name, Alias = trimmed["alias:".Length..].Trim(), Comment = entry.Comment };
                continue;
            }
            if (indent == 0 && trimmed == "comment: |-")
            {
                entry = new GtfobinEntry { Name = entry.Name, Alias = entry.Alias, Comment = ReadBlock(lines, ref i, 2) };
                continue;
            }
            if (indent == 2 && !trimmed.StartsWith("- ", StringComparison.Ordinal) && trimmed.EndsWith(':'))
            {
                FinishVariant();
                currentVariant = null;
                currentFunction = trimmed[..^1];
                continue;
            }
            if (indent == 2 && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                FinishVariant();
                currentVariant = currentFunction is null ? null : new VariantBuilder(currentFunction);
                if (currentVariant is not null && trimmed == "- code: |-") currentVariant.Code = ReadBlock(lines, ref i, 6);
                continue;
            }
            if (currentVariant is null) continue;

            if (indent == 4 && trimmed == "code: |-") { currentVariant.Code = ReadBlock(lines, ref i, 6); continue; }
            if (indent == 4 && trimmed == "comment: |-") { currentVariant.Comment = ReadBlock(lines, ref i, 6); continue; }
            if (indent == 4 && trimmed == "contexts:")
            {
                ReadContexts(lines, ref i, currentVariant);
            }
        }
        FinishVariant();
        return entry;
    }

    private static void ReadContexts(string[] lines, ref int index, VariantBuilder variant)
    {
        string? activeContext = null;
        for (var i = index + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            var indent = CountIndent(line);
            if (indent <= 4) { index = i - 1; return; }
            if (indent == 6 && trimmed.EndsWith(':'))
            {
                activeContext = trimmed[..^1];
                variant.Contexts.Add(activeContext);
                continue;
            }
            if (indent == 8 && trimmed == "code: |-" && activeContext is not null)
            {
                variant.ContextCodes[activeContext] = ReadBlock(lines, ref i, 10);
            }
        }
        index = lines.Length - 1;
    }

    private static string ReadBlock(string[] lines, ref int index, int contentIndent)
    {
        var content = new List<string>();
        for (var i = index + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length != 0 && CountIndent(line) < contentIndent) { index = i - 1; break; }
            if (line.Trim().Length == 0) content.Add(string.Empty);
            else content.Add(line.Length >= contentIndent ? line[contentIndent..] : line.TrimStart());
            if (i == lines.Length - 1) index = i;
        }
        return string.Join(Environment.NewLine, content).TrimEnd();
    }

    private static int CountIndent(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ') count++;
        return count;
    }

    private sealed class VariantBuilder(string function)
    {
        public string Function { get; } = function;
        public string? Code { get; set; }
        public string? Comment { get; set; }
        public List<string> Contexts { get; } = [];
        public Dictionary<string, string> ContextCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
