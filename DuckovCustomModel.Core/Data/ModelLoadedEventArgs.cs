using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.Core.Data
{
    public class ModelLoadedEventArgs
    {
        public List<LoadedModelInfo> LoadedModels { get; set; } = [];

        public class LoadedModelInfo(
            string modelID,
            ModelBundleInfo bundleInfo,
            ModelInfo modelInfo,
            GameObject modelPrefab)
        {
            public string ModelID { get; set; } = modelID;
            public ModelBundleInfo BundleInfo { get; set; } = bundleInfo;
            public ModelInfo ModelInfo { get; set; } = modelInfo;
            public GameObject ModelPrefab { get; set; } = modelPrefab;
        }
    }
}
