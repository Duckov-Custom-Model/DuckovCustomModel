using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UniVRM10;

namespace DuckovCustomModel.Integrations.Motions
{
    internal sealed class MotionExpressions
    {
        private readonly Vrm10Instance? _vrm;
        private readonly SkinnedMeshRenderer[] _meshes;
        private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Binding> _aliases = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _mapping;

        internal MotionExpressions(Animator animator, Dictionary<string, string> mapping)
        {
            _vrm = animator.GetComponent<Vrm10Instance>();
            _meshes = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _mapping = mapping;
        }

        internal void Set(string name, float value, float blend)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException("Invalid motion expression value.");
            if (!_aliases.TryGetValue(name, out var binding))
            {
                var target = _mapping.TryGetValue(name, out var mapped)
                    ? mapped
                    : name switch
                    {
                        "あ" => "aa", "い" => "ih", "う" => "ou", "え" => "ee", "お" => "oh",
                        "まばたき" => "blink", "ウィンク" => "blinkLeft", "ウィンク右" => "blinkRight",
                        "笑い" => "happy", "怒り" => "angry", "悲しい" => "sad", _ => name
                    };
                if (_bindings.TryGetValue(target, out binding))
                {
                    _aliases.Add(name, binding);
                }
                else
                {
                    binding = new Binding();
                    if (_vrm != null)
                    {
                        binding.Key = Enum.TryParse(target, true, out ExpressionPreset preset) &&
                                      Enum.IsDefined(typeof(ExpressionPreset), preset)
                            ? ExpressionKey.CreateFromPreset(preset)
                            : ExpressionKey.CreateCustom(target);
                    }
                    else
                    {
                        foreach (var mesh in _meshes)
                        {
                            if (mesh == null || mesh.sharedMesh == null) continue;
                            var index = mesh.sharedMesh.GetBlendShapeIndex(target);
                            if (index >= 0) binding.Shapes.Add((mesh, index, 0));
                        }
                    }

                    _bindings.Add(target, binding);
                    _aliases.Add(name, binding);
                }
            }

            binding.Value += value;
            if (_vrm != null)
            {
                if (!binding.Applied) binding.Previous = _vrm.Runtime.Expression.GetWeight(binding.Key);
                _vrm.Runtime.Expression.SetWeight(binding.Key,
                    Mathf.Lerp(binding.Previous, Mathf.Clamp01(binding.Value), blend));
            }
            else
            {
                for (var i = 0; i < binding.Shapes.Count; i++)
                {
                    var (mesh, index, previous) = binding.Shapes[i];
                    if (mesh == null) continue;
                    if (!binding.Applied) previous = mesh.GetBlendShapeWeight(index);
                    binding.Shapes[i] = (mesh, index, previous);
                    mesh.SetBlendShapeWeight(index, Mathf.Lerp(previous, Mathf.Clamp01(binding.Value) * 100, blend));
                }
            }

            binding.Applied = true;
        }

        internal void Restore()
        {
            foreach (var binding in _bindings.Values)
            {
                if (!binding.Applied) continue;
                binding.Applied = false;
                binding.Value = 0;
                if (_vrm != null) _vrm.Runtime.Expression.SetWeight(binding.Key, binding.Previous);
                else
                    foreach (var (mesh, index, previous) in binding.Shapes)
                        if (mesh != null)
                            mesh.SetBlendShapeWeight(index, previous);
            }
        }

        private sealed class Binding
        {
            internal ExpressionKey Key;
            internal float Previous, Value;
            internal bool Applied;
            internal readonly List<(SkinnedMeshRenderer mesh, int index, float previous)> Shapes = [];
        }
    }
}
