using System.IO;
using Newtonsoft.Json;

namespace DuckovCustomModel.Data
{
    public class ModelBundleInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;

        public ModelInfo[] Models { get; set; } = [];

        [JsonIgnore] public string DirectoryPath { get; internal set; } = string.Empty;

        public static ModelBundleInfo? LoadFromDirectory(string directoryPath)
        {
            var infoFilePath = Path.Combine(directoryPath, "bundleinfo.json");
            if (!File.Exists(infoFilePath)) return null;
            var json = File.ReadAllText(infoFilePath);
            var info = JsonConvert.DeserializeObject<ModelBundleInfo>(json, Constant.JsonSettings);
            if (info != null) info.DirectoryPath = directoryPath;
            return info;
        }

        public ModelBundleInfo CreateFilteredCopy(ModelInfo[] filteredModels)
        {
            var copy = new ModelBundleInfo
            {
                BundleName = BundleName,
                BundlePath = BundlePath,
                Models = filteredModels,
                DirectoryPath = DirectoryPath,
            };
            return copy;
        }
    }
}