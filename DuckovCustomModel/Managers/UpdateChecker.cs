using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Configs;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DuckovCustomModel.Managers
{
    public class UpdateChecker : MonoBehaviour
    {
        private const string UpdateUrl =
            "https://duckov-custom-model-release-version.ritsukage.com/";

        private const float CheckIntervalHours = 1f;
        private CancellationTokenSource? _periodicCheckCts;
        private UpdateInfoConfig? _updateInfoConfig;

        public static UpdateChecker? Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadUpdateInfo();
            StartPeriodicCheck();
            CheckForUpdateAsync().Forget();
        }

        private void OnDestroy()
        {
            _periodicCheckCts?.Cancel();
            _periodicCheckCts?.Dispose();
            _periodicCheckCts = null;
        }

        public static event Action<bool, string?>? OnUpdateCheckCompleted;

        private void LoadUpdateInfo()
        {
            try
            {
                _updateInfoConfig = ConfigManager.LoadConfigFromFile<UpdateInfoConfig>("UpdateInfoConfig.json");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to load update info config: {ex.Message}");
                _updateInfoConfig = new UpdateInfoConfig();
            }
        }

        private void SaveUpdateInfo()
        {
            if (_updateInfoConfig == null) return;
            try
            {
                ConfigManager.SaveConfigToFile(_updateInfoConfig, "UpdateInfoConfig.json");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Failed to save update info config: {ex.Message}");
            }
        }

        private void StartPeriodicCheck()
        {
            _periodicCheckCts?.Cancel();
            _periodicCheckCts?.Dispose();
            _periodicCheckCts = new CancellationTokenSource();
            PeriodicCheckAsync(_periodicCheckCts.Token).Forget();
        }

        private async UniTaskVoid PeriodicCheckAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromHours(CheckIntervalHours), cancellationToken: cancellationToken);
                if (!cancellationToken.IsCancellationRequested) CheckForUpdateAsync().Forget();
            }
        }

        public void CheckForUpdate()
        {
            CheckForUpdateAsync().Forget();
        }

        private async UniTaskVoid CheckForUpdateAsync()
        {
            const int maxRetries = 3;
            const int retryDelaySeconds = 2;
            const int requestTimeout = 60;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
                try
                {
                    using var request = UnityWebRequest.Get(UpdateUrl);
                    request.timeout = requestTimeout;

                    await request.SendWebRequest().ToUniTask();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var json = request.downloadHandler.text;
                        var releaseInfo = JsonConvert.DeserializeObject<ReleaseInfo>(json);

                        if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.Version))
                        {
                            ModLogger.LogWarning("Invalid release info received");
                            OnUpdateCheckCompleted?.Invoke(false, null);
                            return;
                        }

                        var latestVersion = NormalizeVersion(releaseInfo.Version);
                        var currentVersion = NormalizeVersion(Constant.ModVersion);
                        var hasUpdate = CompareVersions(currentVersion, latestVersion) < 0;

                        _updateInfoConfig ??= new UpdateInfoConfig();

                        _updateInfoConfig.LatestVersion = latestVersion;
                        _updateInfoConfig.LatestReleaseName = releaseInfo.ReleaseName ?? latestVersion;
                        _updateInfoConfig.LastCheckTime = DateTime.Now;
                        _updateInfoConfig.HasUpdate = hasUpdate;
                        _updateInfoConfig.LatestChangelog = releaseInfo.Changelog;

                        // 转换下载链接
                        _updateInfoConfig.LatestDownloadLinks = [];
                        if (releaseInfo.DownloadLinks != null)
                            foreach (var link in releaseInfo.DownloadLinks)
                                _updateInfoConfig.LatestDownloadLinks.Add(new DownloadLinkInfo
                                {
                                    Name = link.Name,
                                    Url = link.Url,
                                });

                        if (DateTimeOffset.TryParse(releaseInfo.PublishedAt, CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind, out var publishedAt))
                            _updateInfoConfig.LatestPublishedAt = publishedAt;

                        SaveUpdateInfo();

                        ModLogger.Log(
                            $"Update check completed. Current: {Constant.ModVersion}, Latest: {releaseInfo.Version}, HasUpdate: {hasUpdate}");
                        OnUpdateCheckCompleted?.Invoke(hasUpdate, latestVersion);
                        return;
                    }

                    var errorMessage = request.error ?? "Unknown error";
                    var isTimeout = errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                                    errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase);

                    ModLogger.LogWarning(
                        isTimeout
                            ? $"Update check timeout (attempt {attempt}/{maxRetries}). This may be due to network issues."
                            : $"Failed to check for updates (attempt {attempt}/{maxRetries}): {errorMessage}");

                    if (attempt < maxRetries)
                    {
                        var delaySeconds = retryDelaySeconds * attempt;
                        ModLogger.Log($"Retrying update check in {delaySeconds} seconds...");
                        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    else
                    {
                        ModLogger.LogWarning("Update check failed after all retry attempts.");
                        OnUpdateCheckCompleted?.Invoke(false, null);
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.LogWarning(
                        $"Error checking for updates (attempt {attempt}/{maxRetries}): {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(retryDelaySeconds * attempt));
                    }
                    else
                    {
                        ModLogger.LogError($"Failed to check for updates after {maxRetries} attempts: {ex.Message}");
                        OnUpdateCheckCompleted?.Invoke(false, null);
                    }
                }
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            version = version.Trim();

            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase)) version = version[1..];

            return version.Trim();
        }

        private static int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2))
                return 0;
            if (string.IsNullOrEmpty(version1))
                return -1;
            if (string.IsNullOrEmpty(version2))
                return 1;

            var v1DashIndex = version1.IndexOf('-');
            var v2DashIndex = version2.IndexOf('-');

            var v1Main = v1DashIndex >= 0 ? version1[..v1DashIndex] : version1;
            var v2Main = v2DashIndex >= 0 ? version2[..v2DashIndex] : version2;

            var v1Suffix = v1DashIndex >= 0 ? version1[(v1DashIndex + 1)..] : string.Empty;
            var v2Suffix = v2DashIndex >= 0 ? version2[(v2DashIndex + 1)..] : string.Empty;

            var v1Parts = v1Main.Split('.');
            var v2Parts = v2Main.Split('.');

            var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

            for (var i = 0; i < maxLength; i++)
            {
                var v1Part = i < v1Parts.Length ? ParseVersionPart(v1Parts[i]) : 0;
                var v2Part = i < v2Parts.Length ? ParseVersionPart(v2Parts[i]) : 0;

                if (v1Part < v2Part)
                    return -1;
                if (v1Part > v2Part)
                    return 1;
            }

            return CompareVersionSuffix(v1Suffix, v2Suffix);
        }

        private static int CompareVersionSuffix(string suffix1, string suffix2)
        {
            if (string.IsNullOrEmpty(suffix1) && string.IsNullOrEmpty(suffix2))
                return 0;
            if (string.IsNullOrEmpty(suffix1))
                return -1;
            if (string.IsNullOrEmpty(suffix2))
                return 1;

            suffix1 = suffix1.Trim();
            suffix2 = suffix2.Trim();

            var v1Match = Regex.Match(suffix1, @"^fix(\d+)", RegexOptions.IgnoreCase);
            var v2Match = Regex.Match(suffix2, @"^fix(\d+)", RegexOptions.IgnoreCase);

            if (!v1Match.Success || !v2Match.Success)
                return string.Compare(suffix1, suffix2, StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(v1Match.Groups[1].Value, out var v1Fix) &&
                int.TryParse(v2Match.Groups[1].Value, out var v2Fix))
                return v1Fix.CompareTo(v2Fix);

            return string.Compare(suffix1, suffix2, StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseVersionPart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return 0;

            part = part.Trim();
            var match = Regex.Match(part, @"^\d+");
            if (match.Success && int.TryParse(match.Value, out var result))
                return result;

            return 0;
        }

        public bool HasUpdate()
        {
            return _updateInfoConfig?.HasUpdate ?? false;
        }

        public string? GetLatestVersion()
        {
            return _updateInfoConfig?.LatestVersion;
        }

        public string? GetLatestReleaseName()
        {
            return _updateInfoConfig?.LatestReleaseName;
        }

        public DateTime? GetLastCheckTime()
        {
            return _updateInfoConfig?.LastCheckTime;
        }

        public DateTimeOffset? GetLatestPublishedAt()
        {
            return _updateInfoConfig?.LatestPublishedAt;
        }

        public string? GetLatestChangelog()
        {
            return _updateInfoConfig?.LatestChangelog;
        }

        public List<DownloadLinkInfo> GetLatestDownloadLinks()
        {
            return _updateInfoConfig?.LatestDownloadLinks ?? [];
        }

        private class ReleaseInfo
        {
            [JsonProperty("version")] public string Version { get; set; } = string.Empty;

            [JsonProperty("release_name")] public string? ReleaseName { get; set; }

            [JsonProperty("published_at")] public string? PublishedAt { get; set; }

            [JsonProperty("changelog")] public string? Changelog { get; set; }

            [JsonProperty("download_links")] public List<DownloadLink>? DownloadLinks { get; set; }
        }

        private class DownloadLink
        {
            [JsonProperty("name")] public string Name { get; set; } = string.Empty;

            [JsonProperty("url")] public string Url { get; set; } = string.Empty;
        }
    }
}
