using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.Integrations.Motions;
using DuckovCustomModel.MonoBehaviours;
using DuckovCustomModel.Utils;
using DuckovCustomModel.Localizations;
using ModelRuntime;
using Newtonsoft.Json;
using UnityEngine;

namespace DuckovCustomModel.Integrations.AssetBundles
{
    public sealed class AssetBundleRadialActionPlayer
    {
        private static readonly HashSet<int> DrivenParameters =
            new(CustomAnimatorHash.GetAllParams().Select(parameter => parameter.Hash));

        private RadialActionInfo[] _actions;
        private readonly RadialActionInfo[] _configuredActions;
        private readonly RadialMenuEntry[] _configuredEntries;
        private IReadOnlyList<RadialActionInfo>? _librarySnapshot;
        private RadialMenuEntry[] _entries;
        private readonly Dictionary<int, AnimatorControllerParameter> _parameters;
        private readonly Dictionary<string, RadialFormInfo[]> _forms = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _formValues = new(StringComparer.Ordinal);
        private readonly string _targetTypeId;
        private readonly string _modelId;
        private bool _formsDirty;

        private readonly Animator _animator;
        private RadialActionInfo? _activeAction;
        private readonly ExternalMotionPlayer? _motionPlayer;
        private readonly ModelHandler _handler;
        private bool _movementLocked;

        public bool MovementLocked
        {
            get => _motionPlayer?.MovementLocked ?? _movementLocked;
            set
            {
                _movementLocked = value;
                if (_motionPlayer != null) _motionPlayer.MovementLocked = value;
            }
        }

        public bool IsMotionControllingPose => _motionPlayer != null && _motionPlayer.IsControllingPose;

        public AssetBundleRadialActionPlayer(Animator animator, ModelInfo modelInfo, string targetTypeId,
            ModelHandler handler, ModelBundleInfo bundle)
        {
            _animator = animator;
            _handler = handler;
            _targetTypeId = targetTypeId;
            _modelId = modelInfo.ModelID;
            _parameters = animator.parameters.ToDictionary(parameter => parameter.nameHash);
            var buffParameters = modelInfo.BuffAnimatorParams == null
                ? new HashSet<int>()
                : new HashSet<int>(modelInfo.BuffAnimatorParams.Keys.Select(Animator.StringToHash));
            if (animator.isHuman)
            {
                _motionPlayer = animator.gameObject.GetComponent<ExternalMotionPlayer>() ??
                                animator.gameObject.AddComponent<ExternalMotionPlayer>();
                _motionPlayer.Initialize(handler, animator, bundle);
            }

            var actions = new List<RadialActionInfo>();
            var entries = BuildMenus(modelInfo.RadialMenus);
            _configuredEntries = entries.ToArray();
            _configuredActions = actions.ToArray();
            _entries = _configuredEntries;
            _actions = _configuredActions;
            RefreshLibrary();

            List<RadialMenuEntry> BuildMenus(RadialMenuInfo[]? menus)
            {
                var entries = new List<RadialMenuEntry>();
                foreach (var menu in menus ?? [])
                {
                    if (menu == null) continue;
                    var children = new List<RadialMenuEntry>();
                    foreach (var action in menu.Actions ?? [])
                    {
                        if (action == null || !action.Validate()) continue;
                        if (action.Motion.Length > 0)
                        {
                            if (_motionPlayer != null)
                            {
                                actions.Add(action);
                                children.Add(new RadialMenuEntry(action.Id, action.Name));
                            }

                            continue;
                        }

                        var hash = Animator.StringToHash(action.Parameter);
                        if (DrivenParameters.Contains(hash) || buffParameters.Contains(hash)
                                                            || !_parameters.TryGetValue(hash, out var parameter) ||
                                                            parameter.type != ActionType(action.Mode)) continue;
                        actions.Add(action);
                        var validForms = new List<RadialFormInfo>();
                        foreach (var form in action.Forms ?? [])
                        {
                            var formHash = Animator.StringToHash(form.Parameter);
                            if (DrivenParameters.Contains(formHash) || buffParameters.Contains(formHash) ||
                                !_parameters.TryGetValue(formHash, out var formParameter) ||
                                formParameter.type != ActionType(form.Mode) || formHash == hash) continue;
                            validForms.Add(form);
                        }

                        if (validForms.Count > 0) _forms[action.Id] = validForms.ToArray();
                        children.Add(new(action.Id, action.Name, hasConfiguration: validForms.Count > 0));
                    }

                    children.AddRange(BuildMenus(menu.Menus));
                    if (children.Count > 0)
                        entries.Add(new(menu.Id, menu.Name, RadialMenuEntryKind.Classification, children));
                }

                return entries;
            }

            try
            {
                var stored = ModelRuntimeDataManager.LoadRuntimeData(_targetTypeId, _modelId)
                    .GetValue<string>("AssetBundleFormValues");
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    var values = JsonConvert.DeserializeObject<Dictionary<string, double>>(stored);
                    if (values != null)
                        foreach (var pair in values)
                            _formValues[pair.Key] = pair.Value;
                }
            }
            catch (Exception exception)
            {
                ModLogger.LogWarning($"AssetBundle form settings '{_modelId}': {exception.Message}");
            }

            foreach (var pair in _forms)
            foreach (var form in pair.Value)
                if (_formValues.TryGetValue(FormKey(pair.Key, form.Id), out var value))
                    SetFormValue(pair.Key, form.Id, value, false);
        }

        public IReadOnlyList<RadialMenuEntry> Entries
        {
            get
            {
                RefreshLibrary();
                return _entries;
            }
        }

        public string? CurrentActionId => _motionPlayer?.CurrentActionId ?? _activeAction?.Id;

        public IReadOnlyList<RadialFormInfo> GetForms(string actionId) =>
            _forms.TryGetValue(actionId, out var forms) ? forms : Array.Empty<RadialFormInfo>();

        public double GetFormValue(RadialFormInfo form)
        {
            var hash = Animator.StringToHash(form.Parameter);
            return form.Mode switch
            {
                RadialActionInfo.BoolMode => _animator.GetBool(hash) ? 1 : 0,
                RadialActionInfo.IntMode => _animator.GetInteger(hash),
                _ => _animator.GetFloat(hash)
            };
        }

        public bool SetFormValue(string actionId, string formId, double value) =>
            SetFormValue(actionId, formId, value, true);

        private bool SetFormValue(string actionId, string formId, double value, bool record)
        {
            if (_animator == null || double.IsNaN(value) || double.IsInfinity(value) ||
                !_forms.TryGetValue(actionId, out var forms)) return false;
            var form = Array.Find(forms, candidate => candidate.Id == formId);
            if (form == null) return false;
            var hash = Animator.StringToHash(form.Parameter);
            switch (form.Mode)
            {
                case RadialActionInfo.BoolMode:
                    value = value > 0 ? 1 : 0;
                    _animator.SetBool(hash, value > 0);
                    break;
                case RadialActionInfo.IntMode:
                    if (form.Options.Length > 0)
                    {
                        if (!Array.Exists(form.Options, option => option.Value == value)) return false;
                    }
                    else value = Math.Max(form.Min, Math.Min(form.Max, Math.Round(value)));

                    _animator.SetInteger(hash, (int)value);
                    break;
                case RadialActionInfo.FloatMode:
                    value = Math.Max(form.Min, Math.Min(form.Max, value));
                    if (form.Step > 0)
                        value = Math.Max(form.Min, Math.Min(form.Max,
                            form.Min + Math.Round((value - form.Min) / form.Step) * form.Step));
                    _animator.SetFloat(hash, (float)value);
                    break;
            }

            if (record)
            {
                _formValues[FormKey(actionId, form.Id)] = value;
                _formsDirty = true;
            }

            return true;
        }

        public void SaveForms()
        {
            if (!_formsDirty || _targetTypeId.Length == 0 || _modelId.Length == 0) return;
            _formsDirty = false;
            var data = ModelRuntimeDataManager.LoadRuntimeData(_targetTypeId, _modelId);
            if (_formValues.Count == 0)
            {
                if (!data.RemoveValue("AssetBundleFormValues")) return;
                if (data.Data.Count == 0)
                {
                    ModelRuntimeDataManager.ClearRuntimeData(_targetTypeId, _modelId);
                    return;
                }
            }
            else data.SetValue("AssetBundleFormValues", JsonConvert.SerializeObject(_formValues));

            ModelRuntimeDataManager.SaveRuntimeData(_targetTypeId, _modelId, data);
        }

        public void ResetForms()
        {
            foreach (var pair in _forms)
            foreach (var form in pair.Value)
            {
                var hash = Animator.StringToHash(form.Parameter);
                var parameter = _parameters[hash];
                switch (form.Mode)
                {
                    case RadialActionInfo.BoolMode:
                        _animator.SetBool(hash, parameter.defaultBool);
                        break;
                    case RadialActionInfo.IntMode:
                        _animator.SetInteger(hash, parameter.defaultInt);
                        break;
                    case RadialActionInfo.FloatMode:
                        _animator.SetFloat(hash, parameter.defaultFloat);
                        break;
                }
            }

            _formValues.Clear();
            _formsDirty = true;
            SaveForms();
        }

        private static string FormKey(string actionId, string formId) => actionId + ":" + formId;

        private static AnimatorControllerParameterType ActionType(string mode)
        {
            return mode switch
            {
                RadialActionInfo.BoolMode => AnimatorControllerParameterType.Bool,
                RadialActionInfo.IntMode => AnimatorControllerParameterType.Int,
                RadialActionInfo.FloatMode => AnimatorControllerParameterType.Float,
                _ => AnimatorControllerParameterType.Trigger
            };
        }

        private void RefreshLibrary()
        {
            if (_motionPlayer == null || ReferenceEquals(_librarySnapshot, MotionLibrary.Actions)) return;
            _librarySnapshot = MotionLibrary.Actions;
            var extra = _librarySnapshot.Where(action => !_configuredActions.Any(existing => existing.Id == action.Id))
                .ToArray();
            _actions = _configuredActions.Concat(extra).ToArray();
            _entries = extra.Length == 0
                ? _configuredEntries
                : _configuredEntries.Concat(new[]
                {
                    new RadialMenuEntry("dcm:motions", Localization.ExternalMotions, RadialMenuEntryKind.Classification,
                        extra.Select(action => new RadialMenuEntry(action.Id, action.Name)).ToArray())
                }).ToArray();
        }

        public bool TryPlay(string actionId)
        {
            RefreshLibrary();
            if (_animator == null || string.IsNullOrWhiteSpace(actionId)) return false;
            var action = Array.Find(_actions, candidate =>
                string.Equals(candidate.Id, actionId, StringComparison.OrdinalIgnoreCase));
            if (action == null) return false;

            Stop();
            if (action.Motion.Length > 0) return _motionPlayer?.TryPlay(action) == true;
            var hash = Animator.StringToHash(action.Parameter);
            if (action.Mode == RadialActionInfo.BoolMode)
            {
                _animator.SetBool(hash, true);
            }
            else if (action.Mode == RadialActionInfo.IntMode)
                _animator.SetInteger(hash, (int)action.Value);
            else if (action.Mode == RadialActionInfo.FloatMode)
                _animator.SetFloat(hash, action.Value);
            else
            {
                _animator.SetTrigger(hash);
            }

            _activeAction = action.Mode == RadialActionInfo.TriggerMode ? null : action;

            return true;
        }

        public void Update()
        {
            if (_activeAction != null && !MovementLocked && _handler.CharacterMainControl != null &&
                RadialActionInput.MovementRequested(_handler.CharacterMainControl)) Stop();
        }

        public void Dispose()
        {
            SaveForms();
            _motionPlayer?.Stop(true);
        }

        public void SetMotionPaused(bool paused) => _motionPlayer?.SetPaused(paused);

        public bool Stop()
        {
            var motionStopped = _motionPlayer?.Stop() == true;
            if (_activeAction == null) return motionStopped;
            var action = _activeAction;
            _activeAction = null;
            if (_animator != null)
            {
                var hash = Animator.StringToHash(action.Parameter);
                var parameter = _parameters[hash];
                if (action.Mode == RadialActionInfo.BoolMode)
                    _animator.SetBool(hash, parameter.defaultBool);
                else if (action.Mode == RadialActionInfo.IntMode)
                    _animator.SetInteger(hash, parameter.defaultInt);
                else if (action.Mode == RadialActionInfo.FloatMode)
                    _animator.SetFloat(hash, parameter.defaultFloat);
            }

            return true;
        }
    }
}
