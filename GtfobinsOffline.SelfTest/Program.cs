using GtfobinsOffline;
using System.IO;
using System.Reflection;

var entries = GtfobinsDataLoader.Load();
Assert(entries.Count == 458, "Expected every current GTFOBins executable entry to load.");
Assert(entries.All(entry => !string.IsNullOrWhiteSpace(entry.Name) && entry.Variants.All(variant => !string.IsNullOrWhiteSpace(variant.Code))), "Every loaded entry must have a name and non-empty command text.");
VerifyEmbeddedDataIsUnchanged();
Assert(CommandCopyService.ShouldCopyFullCommand(0, 0, 0), "A simple click must copy the full command.");
Assert(!CommandCopyService.ShouldCopyFullCommand(1, 0, 0), "A selected text range must not be overwritten by full-command copy.");
Assert(!CommandCopyService.ShouldCopyFullCommand(0, 5, 0), "A pointer drag must not trigger full-command copy.");
string? copiedCommand = null;
Assert(CommandCopyService.TryCopy("sudo find .", text => copiedCommand = text) && copiedCommand == "sudo find .", "The copy service must pass the full command to the clipboard action.");
Assert(!CommandCopyService.TryCopy("", _ => throw new InvalidOperationException("Empty commands must not be copied.")), "Empty commands must not invoke the clipboard action.");
copiedCommand = null;
Assert(CommandCopyService.TryCopyIfClick(0, 2, 1, "sudo id", text => copiedCommand = text) && copiedCommand == "sudo id", "A code-card click must copy the command.");
Assert(!CommandCopyService.TryCopyIfClick(0, 5, 0, "sudo id", _ => throw new InvalidOperationException("A drag must not copy.")), "A code-card drag must preserve text selection.");
var find = entries.SingleOrDefault(entry => entry.Name == "find");
Assert(find is not null, "Expected find entry.");
Assert(find!.Variants.Any(variant => variant.Context == "sudo" && variant.Code.Contains("find", StringComparison.Ordinal)), "Expected Sudo command for find.");

const string sudoOutput = """
Matching Defaults entries for alice on target:
    env_reset, secure_path=/usr/local/sbin\:/usr/local/bin\:/usr/sbin\:/usr/bin\:/sbin\:/bin

User alice may run the following commands on target:
    (ALL) NOPASSWD: /usr/bin/find
    (ALL) NOPASSWD: /usr/bin/python3
    (ALL) NOPASSWD: /bin/kill, !/bin/su
""";
var matches = SudoParser.Parse(sudoOutput, entries);
Assert(matches.Count == 4, "Expected four command-rule matches, excluding secure_path.");
Assert(matches.Single(match => match.CommandName == "find").Kind == MatchKind.Exact, "Expected exact find match.");
Assert(matches.Single(match => match.CommandName == "python3").Kind == MatchKind.Family, "Expected Python version-family match.");
Assert(matches.Single(match => match.CommandName == "su").IsForbidden, "Expected forbidden su rule.");
Assert(!matches.Any(match => match.Path.Contains("secure_path", StringComparison.OrdinalIgnoreCase)), "Defaults path must not be parsed as a command.");

const string alternateSudoOutput = """
任意本地化标题：
    (root) NOPASSWD: /usr/bin/find --exec /bin/sh \;
    (www-data : www-data) SETENV: /usr/bin/python3, /bin/kill
""";
var alternateMatches = SudoParser.Parse(alternateSudoOutput, entries);
Assert(alternateMatches.Count == 3, "Expected commands from localized and multi-command sudo output.");
Assert(alternateMatches.Single(match => match.CommandName == "find").RunAs == "root", "Expected RunAs metadata to be retained.");
Assert(alternateMatches.Single(match => match.CommandName == "python3").Tags == "SETENV", "Expected sudo tags to be retained.");
Assert(alternateMatches.Single(match => match.CommandName == "kill").Kind == MatchKind.NotFound, "Expected non-listed commands to remain visible.");

const string suidOutput = """
/usr/bin/find
/usr/bin/su
/usr/bin/python3.11
/usr/local/bin/binsnitch
""";
var suidMatches = SudoParser.ParseSuid(suidOutput, entries);
Assert(suidMatches.Count == 4, "Expected four SUID path matches.");
Assert(suidMatches.All(match => match.IsSuidAnalysis), "SUID matches must be flagged as SUID analysis.");
Assert(suidMatches.Single(match => match.CommandName == "find").Kind == MatchKind.Exact, "Expected exact SUID match for find.");
Assert(suidMatches.Single(match => match.CommandName == "find").Entry!.Variants.Any(variant => variant.Context == "suid"), "Exact SUID match must carry official suid variants.");
Assert(suidMatches.Single(match => match.CommandName == "su").Kind == MatchKind.NoSuid, "Expected no-SUID-usage match for su.");
Assert(suidMatches.Single(match => match.CommandName == "python3.11").Kind == MatchKind.Family, "Expected version-family SUID match for python3.11.");
Assert(suidMatches.Single(match => match.CommandName == "binsnitch").Kind == MatchKind.NotFound, "Expected non-listed SUID path to remain visible.");
Assert(suidMatches.Single(match => match.CommandName == "find").Path == "/usr/bin/find", "SUID match must retain the original path.");
Assert(SudoParser.ParseSuid("", entries).Count == 0, "Empty SUID input must yield no matches.");
Assert(SudoParser.ParseSuid("not-a-path\n/usr/bin/sudo\n", entries).Count == 1, "Non-path noise lines must be ignored by SUID analysis.");

var autoSudo = SudoParser.ParseAuto(sudoOutput, entries);
Assert(autoSudo.Count == matches.Count && autoSudo.All(match => !match.IsSuidAnalysis), "Auto-detect must route sudo -l output to sudo parsing.");
Assert(autoSudo.Any(match => match.CommandName == "su" && match.IsForbidden), "Auto-detect must preserve sudo continuation lines (forbidden rules without a RunAs group).");
var autoSuid = SudoParser.ParseAuto(suidOutput, entries);
Assert(autoSuid.Count == suidMatches.Count && autoSuid.All(match => match.IsSuidAnalysis), "Auto-detect must route plain path lists to SUID parsing.");

var installedVersion = typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);
Assert(UpdateService.IsNewer($"{installedVersion.Major}.{installedVersion.Minor}.{installedVersion.Build + 1}"), "Expected semantic update comparison.");
Assert(!UpdateApplier.IsApplyRequest([]), "Normal startup must not invoke the updater.");
Assert(!UpdateApplier.TryGetCleanupPath(["--cleanup-helper", "C:\\Windows\\System32\\notepad.exe"], out _), "Updater cleanup must not accept paths outside the temp directory.");
var validHelperPath = Path.Combine(Path.GetTempPath(), "BinLens-Updater-test.exe");
Assert(UpdateApplier.TryGetCleanupPath(["--cleanup-helper", validHelperPath], out var cleanupPath) && cleanupPath == Path.GetFullPath(validHelperPath), "Updater cleanup must accept its own temporary helper path.");

Console.WriteLine($"Self-test passed: {entries.Count} embedded entries, source data integrity, and {matches.Count + alternateMatches.Count} sudo matches.");

static void VerifyEmbeddedDataIsUnchanged()
{
    var root = FindWorkspaceRoot();
    var sourceDirectory = Path.Combine(root, "upstream", "_gtfobins");
    Assert(Directory.Exists(sourceDirectory), "The official GTFOBins source directory must be present for the integrity audit.");

    var assembly = typeof(GtfobinsDataLoader).Assembly;
    var resources = assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith("GtfobinsData/", StringComparison.Ordinal) && name != "GtfobinsData/LICENSE")
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
    var sourceFiles = Directory.GetFiles(sourceDirectory).OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).ToArray();
    Assert(resources.Length == sourceFiles.Length, "Every official GTFOBins entry file must be embedded.");

    foreach (var resourceName in resources)
    {
        var fileName = resourceName["GtfobinsData/".Length..];
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        Assert(File.Exists(sourcePath), $"Missing official source file: {fileName}");
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        Assert(memory.ToArray().SequenceEqual(File.ReadAllBytes(sourcePath)), $"Embedded command data was changed: {fileName}");
    }
}

static string FindWorkspaceRoot()
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "GtfobinsOffline", "GtfobinsOffline.csproj"))) return directory.FullName;
    }
    throw new DirectoryNotFoundException("Workspace root was not found.");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
