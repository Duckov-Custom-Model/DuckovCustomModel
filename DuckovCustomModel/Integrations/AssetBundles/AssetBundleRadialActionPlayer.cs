using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Data;
using UnityEngine;

namespace DuckovCustomModel.Integrations.AssetBundles
{
    public sealed class AssetBundleRadialActionPlayer
    {
        private static readonly HashSet<int> DrivenParameters =
            new(CustomAnimatorHash.GetAllParams().Select(parameter => parameter.Hash));

        private readonly RadialActionInfo[] _actions;

        private readonly Animator _animator;
        private RadialActionInfo? _activeBoolAction;

        public AssetBundleRadialActionPlayer(Animator animator, ModelInfo modelInfo)
        {
            _animator = animator;
            var parameters = animator.parameters.ToDictionary(parameter => parameter.nameHash);
            var buffParameters = modelInfo.BuffAnimatorParams == null
                ? new HashSet<int>()
                : new HashSet<int>(modelInfo.BuffAnimatorParams.Keys.Select(Animator.StringToHash));
            _actions = (modelInfo.RadialActions ?? [])
                .Where(action => action != null && action.Validate())
                .Where(action =>
                {
                    var hash = Animator.StringToHash(action.Parameter);
                    if (DrivenParameters.Contains(hash) || buffParameters.Contains(hash)
                                                        || !parameters.TryGetValue(hash, out var parameter))
                        return false;
                    return parameter.type == (action.Mode == RadialActionInfo.BoolMode
                        ? AnimatorControllerParameterType.Bool
                        : AnimatorControllerParameterType.Trigger);
                })
                .ToArray();
        }

        public IReadOnlyList<RadialActionInfo> Actions => _actions;
        public string? CurrentActionId => _activeBoolAction?.Id;

        public bool TryPlay(string actionId)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(actionId)) return false;
            var action = Array.Find(_actions, candidate =>
                string.Equals(candidate.Id, actionId, StringComparison.OrdinalIgnoreCase));
            if (action == null) return false;

            Stop();
            var hash = Animator.StringToHash(action.Parameter);
            if (action.Mode == RadialActionInfo.BoolMode)
            {
                _animator.SetBool(hash, true);
                _activeBoolAction = action;
            }
            else
            {
                _animator.SetTrigger(hash);
            }

            return true;
        }

        public bool Stop()
        {
            if (_activeBoolAction == null) return false;
            if (_animator != null)
                _animator.SetBool(Animator.StringToHash(_activeBoolAction.Parameter), false);
            _activeBoolAction = null;
            return true;
        }
    }
}
