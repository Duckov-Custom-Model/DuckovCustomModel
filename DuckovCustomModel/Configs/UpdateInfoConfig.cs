using System;
using System.Collections.Generic;

namespace DuckovCustomModel.Configs
{
    public class DownloadLinkInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class UpdateInfoConfig : ConfigBase
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string LatestReleaseName { get; set; } = string.Empty;
        public DateTime? LastCheckTime { get; set; }
        public DateTimeOffset? LatestPublishedAt { get; set; }
        public bool HasUpdate { get; set; }
        public string? LatestChangelog { get; set; }
        public List<DownloadLinkInfo> LatestDownloadLinks { get; set; } = [];

        public override void LoadDefault()
        {
            LatestVersion = string.Empty;
            LatestReleaseName = string.Empty;
            LastCheckTime = null;
            LatestPublishedAt = null;
            HasUpdate = false;
            LatestChangelog = null;
            LatestDownloadLinks = [];
        }

        public override bool Validate()
        {
            return false;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not UpdateInfoConfig otherConfig) return;
            LatestVersion = otherConfig.LatestVersion;
            LatestReleaseName = otherConfig.LatestReleaseName;
            LastCheckTime = otherConfig.LastCheckTime;
            LatestPublishedAt = otherConfig.LatestPublishedAt;
            HasUpdate = otherConfig.HasUpdate;
            LatestChangelog = otherConfig.LatestChangelog;
            LatestDownloadLinks = otherConfig.LatestDownloadLinks ?? [];
        }
    }
}
