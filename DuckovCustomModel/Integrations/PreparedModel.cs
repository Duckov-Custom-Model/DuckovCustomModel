using System;
using DuckovCustomModel.Integrations.Ysm;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations
{
    internal sealed class PreparedModel(GameObject root, YsmCharacterRuntime? ysmRuntime = null) : IDisposable
    {
        private bool _disposed;
        public GameObject Root { get; } = root;
        public YsmCharacterRuntime? YsmRuntime { get; } = ysmRuntime;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            YsmRuntime?.Dispose();
            if (Root != null) Object.DestroyImmediate(Root);
        }
    }
}
