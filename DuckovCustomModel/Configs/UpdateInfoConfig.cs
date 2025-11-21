using System;

namespace DuckovCustomModel.Configs
{
    public class UpdateInfoConfig : ConfigBase
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string LatestReleaseName { get; set; } = string.Empty;
        public DateTime? LastCheckTime { get; set; }
        public DateTime? LatestPublishedAt { get; set; }
        public bool HasUpdate { get; set; }

        public override void LoadDefault()
        {
            LatestVersion = string.Empty;
            LatestReleaseName = string.Empty;
            LastCheckTime = null;
            LatestPublishedAt = null;
            HasUpdate = false;
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
        }
    }
}
