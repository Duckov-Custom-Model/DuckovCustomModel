using System;
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
        private const string UpdateUrl = "https://duckov-custom-model-release-version.ritsukage.com/";
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

                        _updateInfoConfig.LatestVersion = releaseInfo.Version;
                        _updateInfoConfig.LatestReleaseName = releaseInfo.ReleaseName ?? releaseInfo.Version;
                        _updateInfoConfig.LastCheckTime = DateTime.Now;
                        _updateInfoConfig.HasUpdate = hasUpdate;

                        if (DateTime.TryParse(releaseInfo.PublishedAt, out var publishedAt))
                            _updateInfoConfig.LatestPublishedAt = publishedAt;

                        SaveUpdateInfo();

                        ModLogger.Log(
                            $"Update check completed. Current: {Constant.ModVersion}, Latest: {releaseInfo.Version}, HasUpdate: {hasUpdate}");
                        OnUpdateCheckCompleted?.Invoke(hasUpdate, releaseInfo.Version);
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

            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase)) version = version.Substring(1);

            var dashIndex = version.IndexOf('-');
            if (dashIndex >= 0) version = version.Substring(0, dashIndex);

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

            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

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

            return 0;
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

        public DateTime? GetLatestPublishedAt()
        {
            return _updateInfoConfig?.LatestPublishedAt;
        }

        private class ReleaseInfo
        {
            [JsonProperty("version")] public string Version { get; set; } = string.Empty;

            [JsonProperty("release_name")] public string? ReleaseName { get; set; }

            [JsonProperty("published_at")] public string? PublishedAt { get; set; }
        }
    }
}
