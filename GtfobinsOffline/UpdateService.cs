using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO;

namespace GtfobinsOffline;

public sealed record UpdateRelease(string Version, string Notes, UpdateAsset Application, UpdateAsset Checksum);
public sealed record UpdateAsset(string Name, string DownloadUrl, long Size);

public static class UpdateService
{
    public const string ApplicationAssetName = "BinLens-win-x64.exe";
    public const string ChecksumAssetName = "BinLens-win-x64.exe.sha256";

    public static string? Repository => typeof(UpdateService).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == "GitHubRepository")?.Value;

    public static async Task<UpdateRelease?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Repository)) return null;
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BinLens/0.1.0");
        using var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var assets = root.GetProperty("assets").EnumerateArray().Select(asset => new UpdateAsset(
            asset.GetProperty("name").GetString() ?? string.Empty,
            asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
            asset.GetProperty("size").GetInt64())).ToArray();
        var application = assets.SingleOrDefault(asset => asset.Name == ApplicationAssetName);
        var checksum = assets.SingleOrDefault(asset => asset.Name == ChecksumAssetName);
        if (application is null || checksum is null) throw new InvalidDataException("发布版本缺少应用或 SHA-256 校验文件。");
        return new UpdateRelease(root.GetProperty("tag_name").GetString() ?? string.Empty, root.GetProperty("body").GetString() ?? string.Empty, application, checksum);
    }

    public static bool IsNewer(string version)
    {
        var current = typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);
        return Version.TryParse(version.Trim().TrimStart('v'), out var latest) && latest > current;
    }

    public static async Task<string> DownloadAndVerifyAsync(UpdateRelease release, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BinLens/0.1.0");
        var checksumText = await client.GetStringAsync(release.Checksum.DownloadUrl, cancellationToken);
        var expectedHash = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Length != 64) throw new InvalidDataException("SHA-256 校验文件格式无效。");

        var downloadPath = Path.Combine(Path.GetTempPath(), $"BinLens-{Guid.NewGuid():N}.exe");
        try
        {
            using var response = await client.GetAsync(release.Application.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = File.Create(downloadPath);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                total += read;
                if (release.Application.Size > 0) progress?.Report((double)total / release.Application.Size);
            }
            await target.FlushAsync(cancellationToken);
            await using var hashStream = File.OpenRead(downloadPath);
            var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, cancellationToken));
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("下载文件的 SHA-256 校验失败。");
            return downloadPath;
        }
        catch
        {
            TryDelete(downloadPath);
            throw;
        }
    }

    public static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
    }
}
