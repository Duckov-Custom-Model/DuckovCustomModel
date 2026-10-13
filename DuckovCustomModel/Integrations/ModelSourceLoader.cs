using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Integrations.Ysm;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations
{
    internal static class ModelSourceLoader
    {
        private static readonly Dictionary<string, Loader> Loaders = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AssetBundle"] = LoadPrefab,
            ["Ysm"] = LoadYsm,
        };

        internal static void Register(ModelSourceDefinition definition, Loader loader)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            ModelSourceDefinition.Register(definition);
            Loaders.Add(definition.Kind, loader);
        }

        internal static UniTask<PreparedModel> Load(ModelHandler handler, ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Loaders.TryGetValue(model.SourceKind, out var loader))
                throw new NotSupportedException("Unsupported model source: " + model.SourceKind);
            return loader(handler, bundle, model, cancellationToken);
        }

        private static UniTask<PreparedModel> LoadPrefab(ModelHandler handler, ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            var prefab = AssetBundleManager.LoadModelPrefab(bundle, model);
            if (prefab == null) throw new InvalidOperationException("Failed to load custom model prefab.");
            var instance = Object.Instantiate(prefab, handler.OriginalCharacterModel!.transform);
            instance.name = ModelHandler.CustomModelInstanceName;
            if (!instance.activeSelf) instance.SetActive(true);
            return UniTask.FromResult(new PreparedModel(instance));
        }

        private static UniTask<PreparedModel> LoadYsm(ModelHandler handler, ModelBundleInfo bundle, ModelInfo model,
            CancellationToken cancellationToken)
        {
            var runtime = new YsmCharacterRuntime(handler, bundle, model);
            return UniTask.FromResult(new PreparedModel(runtime.Root, runtime));
        }

        internal delegate UniTask<PreparedModel> Loader(ModelHandler handler, ModelBundleInfo bundle,
            ModelInfo model, CancellationToken cancellationToken);
    }
}
