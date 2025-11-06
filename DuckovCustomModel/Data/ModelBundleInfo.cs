using System.IO;
using Newtonsoft.Json;

namespace DuckovCustomModel.Data
{
    public class ModelBundleInfo
    {
        public string BundleName { get; set; } = string.Empty;
        public string BundlePath { get; set; } = string.Empty;

        public ModelInfo[] Models { get; set; } = [];

        public static ModelBundleInfo? LoadFromDirectory(string directoryPath)
        {
            var infoFilePath = Path.Combine(directoryPath, "bundleinfo.json");
            if (!File.Exists(infoFilePath)) return null;
            var json = File.ReadAllText(infoFilePath);
            return JsonConvert.DeserializeObject<ModelBundleInfo>(json);
        }
    }
}