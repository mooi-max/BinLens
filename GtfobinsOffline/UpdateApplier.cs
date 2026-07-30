using System.Diagnostics;
using System.IO;

namespace GtfobinsOffline;

public static class UpdateApplier
{
    public static bool TryLaunch(string downloadedApplication)
    {
        var currentApplication = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentApplication) || !File.Exists(downloadedApplication)) return false;
        var helperPath = Path.Combine(Path.GetTempPath(), $"BinLens-Updater-{Guid.NewGuid():N}.exe");
        try
        {
            File.Copy(currentApplication, helperPath, true);
            Process.Start(new ProcessStartInfo(helperPath, $"--apply-update \"{downloadedApplication}\" \"{currentApplication}\" {Environment.ProcessId}") { UseShellExecute = false });
            return true;
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
        {
            UpdateService.TryDelete(helperPath);
            return false;
        }
    }

    public static bool IsApplyRequest(string[] args) => args.Length == 4 && args[0] == "--apply-update";

    public static void Apply(string[] args)
    {
        var source = args[1];
        var target = args[2];
        if (!int.TryParse(args[3], out var processId) || !File.Exists(source)) return;
        try
        {
            using var oldProcess = Process.GetProcessById(processId);
            oldProcess.WaitForExit(30_000);
        }
        catch (ArgumentException) { }

        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                File.Copy(source, target, true);
                Process.Start(new ProcessStartInfo(target, $"--cleanup-helper \"{Environment.ProcessPath}\"") { UseShellExecute = true });
                UpdateService.TryDelete(source);
                return;
            }
            catch (IOException) { Thread.Sleep(500); }
        }
    }

    public static bool TryGetCleanupPath(string[] args, out string? helperPath)
    {
        helperPath = null;
        if (args.Length != 2 || args[0] != "--cleanup-helper") return false;
        try
        {
            var fullPath = Path.GetFullPath(args[1]);
            var tempPath = Path.GetFullPath(Path.GetTempPath());
            var name = Path.GetFileName(fullPath);
            if (!fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) || !name.StartsWith("BinLens-Updater-", StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return false;
            helperPath = fullPath;
            return true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException) { return false; }
    }
}
