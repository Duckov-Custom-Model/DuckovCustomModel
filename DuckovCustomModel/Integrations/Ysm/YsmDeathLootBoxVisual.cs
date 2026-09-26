using System;
using DuckovCustomModel.Core.Data;
using ModelRuntime;
using ModelRuntime.Media;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed class YsmDeathLootBoxVisual : MonoBehaviour
    {
        private YsmModelLease? _lease;
        private DuckovYsmRenderBackend? _renderer;
        private StandardTexturePixelSource? _pixels;

        public static GameObject? Create(ModelBundleInfo bundle, ModelInfo model)
        {
            if (string.IsNullOrWhiteSpace(model.DeathLootBoxYsmPath)) return null;
            YsmModelLease? lease = null;
            DuckovYsmRenderBackend? renderer = null;
            StandardTexturePixelSource? pixels = null;
            GameObject? root = null;
            try
            {
                lease = YsmModelSource.Acquire(bundle, new ModelInfo
                {
                    SourceKind = "Ysm", SourcePath = model.DeathLootBoxYsmPath
                });
                var context = new MolangContext
                {
                    Dialect = MolangDialect.Ysm,
                    ValueCompatibility = lease.Profile.ValueCompatibility
                };
                var simulation = new ModelSimulation(lease.Package, lease.Profile.ModelTarget, context);
                if (!string.IsNullOrWhiteSpace(model.DeathLootBoxAnimation) &&
                    !simulation.Document.Animations.ContainsKey(model.DeathLootBoxAnimation))
                    throw new InvalidOperationException("YSM death loot box animation was not found: " +
                                                        model.DeathLootBoxAnimation);
                simulation.Sample(model.DeathLootBoxAnimation, 0);
                root = new("DeathLootBox_CustomModel");
                var scale = simulation.Document.DisplayScale;
                root.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z) * lease.Profile.Scale;
                pixels = new();
                renderer = new(root.transform, lease.Package, pixels);
                renderer.LoadSelected(simulation.Document);
                renderer.Render(simulation.Pose);
                var visual = root.AddComponent<YsmDeathLootBoxVisual>();
                visual._lease = lease;
                visual._renderer = renderer;
                visual._pixels = pixels;
                return root;
            }
            catch (Exception exception)
            {
                renderer?.Dispose();
                pixels?.Clear();
                lease?.Dispose();
                if (root != null) Object.Destroy(root);
                ModLogger.LogWarning($"YSM death loot box could not load: {exception.Message}");
                return null;
            }
        }

        public void MatchLayer(int layer)
        {
            gameObject.layer = layer;
            if (_renderer == null) return;
            foreach (var renderer in _renderer.Renderers)
                if (renderer != null)
                    renderer.gameObject.layer = layer;
        }

        private void OnDestroy()
        {
            _renderer?.Dispose();
            _pixels?.Clear();
            _lease?.Dispose();
        }
    }
}
