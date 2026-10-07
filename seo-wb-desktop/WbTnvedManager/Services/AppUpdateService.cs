using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WbTnvedManager.Services
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class AppUpdateService
    {
        private const string GitHubRepo = "TruongTuanDev/WPost";
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static string CurrentVersion
        {
            get
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                return ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.2.0";
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = CurrentVersion,
                HasUpdate = false
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{GitHubRepo}/releases/latest");
                request.Headers.Add("User-Agent", "WPost-AutoUpdater");

                var response = await HttpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tag_name", out var tagElem))
                {
                    var latestTag = tagElem.GetString()?.Trim() ?? string.Empty;
                    result.LatestVersion = latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? latestTag : $"v{latestTag}";
                    result.ReleaseNotes = root.TryGetProperty("body", out var bodyElem) ? bodyElem.GetString() ?? "" : "";

                    // Find download URL for WPost.exe
                    if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var asset in assetsElem.EnumerateArray())
                        {
                            var name = asset.GetProperty("name").GetString() ?? "";
                            if (name.Equals("WPost.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                result.DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(result.DownloadUrl))
                    {
                        result.DownloadUrl = $"https://github.com/{GitHubRepo}/releases/download/{latestTag}/WPost.exe";
                    }

                    // Compare versions
                    result.HasUpdate = IsNewerVersion(result.LatestVersion, result.CurrentVersion);
                }
            }
            catch
            {
                // Fallback on network failure
            }

            return result;
        }

        public async Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
                {
                    currentExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WPost.exe");
                }

                var exeDir = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var tempNewExePath = Path.Combine(exeDir, $"WPost_update_{Guid.NewGuid():N}.exe");

                // Download new exe with progress reporting
                using (var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var fileStream = new FileStream(tempNewExePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var buffer = new byte[16384];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalRead += bytesRead;

                        if (totalBytes > 0 && progress != null)
                        {
                            var percent = (int)((totalRead * 100) / totalBytes);
                            progress.Report(percent);
                        }
                    }
                }

                if (!File.Exists(tempNewExePath) || new FileInfo(tempNewExePath).Length < 100000)
                {
                    return false;
                }

                // Create a self-deleting batch script to replace the executable and restart it
                var currentPid = Process.GetCurrentProcess().Id;
                var updaterScriptPath = Path.Combine(exeDir, "wpost_updater.cmd");

                var scriptContent = $@"@echo off
chcp 65001 > nul
echo Dang cap nhat WPost len phien ban moi...
timeout /t 1 /nobreak > nul

:waitloop
tasklist /FI ""PID eq {currentPid}"" 2>NUL | find /I /N ""{currentPid}"">NUL
if ""%ERRORLEVEL%""==""0"" (
    timeout /t 1 /nobreak > nul
    goto waitloop
)

del /f /q ""{currentExePath}"" 2>NUL
move /y ""{tempNewExePath}"" ""{currentExePath}"" > nul

start """" ""{currentExePath}""
del /f /q ""%~f0"" 2>NUL
exit
";
                File.WriteAllText(updaterScriptPath, scriptContent);

                // Launch updater script hidden and exit current process
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{updaterScriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = exeDir
                };

                Process.Start(psi);
                Environment.Exit(0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNewerVersion(string latestVerStr, string currentVerStr)
        {
            try
            {
                var v1 = ParseVersion(latestVerStr);
                var v2 = ParseVersion(currentVerStr);
                return v1 > v2;
            }
            catch
            {
                return !string.Equals(latestVerStr, currentVerStr, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static Version ParseVersion(string versionString)
        {
            var clean = versionString.Trim().TrimStart('v', 'V');
            var parts = clean.Split(new[] { '.', '-', '+' }, StringSplitOptions.RemoveEmptyEntries);
            int major = parts.Length > 0 && int.TryParse(parts[0], out int mj) ? mj : 1;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int mn) ? mn : 0;
            int build = parts.Length > 2 && int.TryParse(parts[2], out int b) ? b : 0;
            return new Version(major, minor, build);
        }
    }
}
