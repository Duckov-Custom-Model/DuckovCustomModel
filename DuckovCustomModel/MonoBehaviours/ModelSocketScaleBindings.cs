using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    internal sealed class ModelSocketScaleBindings : MonoBehaviour
    {
        private readonly Dictionary<(string name, Transform source), Transform> _bindings = [];
        private IReadOnlyDictionary<string, float> _scales = new Dictionary<string, float>();

        internal Transform Resolve(string name, Transform source)
        {
            var key = (name, source);
            if (_bindings.TryGetValue(key, out var binding) && binding != null) return binding;
            binding = new GameObject("SocketScale_" + name).transform;
            binding.SetParent(source, false);
            binding.localScale = Vector3.one * (_scales.TryGetValue(name, out var scale) ? scale : 1);
            _bindings[key] = binding;
            return binding;
        }

        internal void Apply(IReadOnlyDictionary<string, float> scales)
        {
            _scales = scales;
            foreach (var pair in _bindings)
                if (pair.Value != null)
                    pair.Value.localScale =
                        Vector3.one * (scales.TryGetValue(pair.Key.name, out var scale) ? scale : 1);
        }
    }
}
