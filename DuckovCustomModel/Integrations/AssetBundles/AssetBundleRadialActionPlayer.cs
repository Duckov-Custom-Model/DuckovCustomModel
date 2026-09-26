using System;
using System.Collections.Generic;
using System.Linq;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using ModelRuntime;
using Newtonsoft.Json;
using UnityEngine;

namespace DuckovCustomModel.Integrations.AssetBundles
{
    public sealed class AssetBundleRadialActionPlayer
    {
        private static readonly HashSet<int> DrivenParameters =
            new(CustomAnimatorHash.GetAllParams().Select(parameter => parameter.Hash));

        private readonly RadialActionInfo[] _actions;
        private readonly RadialMenuEntry[] _entries;
        private readonly Dictionary<int, AnimatorControllerParameter> _parameters;
        private readonly Dictionary<string, RadialFormInfo[]> _forms = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _formValues = new(StringComparer.Ordinal);
        private readonly string _targetTypeId;
        private readonly string _modelId;
        private bool _formsDirty;

        private readonly Animator _animator;
        private RadialActionInfo? _activeAction;

        public AssetBundleRadialActionPlayer(Animator animator, ModelInfo modelInfo, string targetTypeId)
        {
            _animator = animator;
            _targetTypeId = targetTypeId;
            _modelId = modelInfo.ModelID;
            _parameters = animator.parameters.ToDictionary(parameter => parameter.nameHash);
            var buffParameters = modelInfo.BuffAnimatorParams == null
                ? new HashSet<int>()
                : new HashSet<int>(modelInfo.BuffAnimatorParams.Keys.Select(Animator.StringToHash));
            var actions = new List<RadialActionInfo>();
            _entries = BuildMenus(modelInfo.RadialMenus).ToArray();
            _actions = actions.ToArray();

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
                        foreach (var pair in values) _formValues[pair.Key] = pair.Value;
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

        public IReadOnlyList<RadialMenuEntry> Entries => _entries;
        public string? CurrentActionId => _activeAction?.Id;

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
                    if (form.Step > 0) value = Math.Max(form.Min, Math.Min(form.Max,
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

        public bool Stop()
        {
            if (_activeAction == null) return false;
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
