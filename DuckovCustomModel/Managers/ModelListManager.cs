using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Managers
{
    public static class ModelListManager
    {
        private static CancellationTokenSource? _refreshCancellationTokenSource;
        private static UniTaskCompletionSource? _refreshCompletionSource;

        private static HashSet<string>? _currentRefreshingBundles;

        public static bool IsRefreshing { get; private set; }

        public static event Action? OnRefreshStarted;
        public static event Action? OnRefreshCompleted;
        public static event Action<ModelChangedEventArgs>? OnModelChanged;

        #region 事件通知

        public static void NotifyModelChanged(ModelHandler handler, bool isRestored)
        {
            if (handler == null) return;

            OnModelChanged?.Invoke(new ModelChangedEventArgs
            {
                Handler = handler,
                TargetTypeId = handler.GetTargetTypeId(),
                ModelID = handler.CurrentModelInfo?.ModelID,
                ModelName = handler.CurrentModelInfo?.Name,
                IsRestored = isRestored,
#pragma warning disable CS0618
                Target = ModelTargetExtensions.FromTargetTypeId(handler.GetTargetTypeId()) ?? ModelTarget.Character,
                AICharacterNameKey = ModelTargetType.IsAICharacterTargetType(handler.GetTargetTypeId())
                    ? ModelTargetType.ExtractAICharacterName(handler.GetTargetTypeId())
                    : null,
                HandlerCount = 0,
                Success = true,
#pragma warning restore CS0618
            });
        }

        #endregion

        #region 私有方法

        private static async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts, UniTaskCompletionSource? completionSource,
            IEnumerable<string>? priorityModelIDs)
        {
            IsRefreshing = true;

            foreach (var handler in ModelManager.GetAllHandlers())
                if (handler.IsHiddenOriginalModel)
                    handler.CleanupCustomModel();

            OnRefreshStarted?.Invoke();

            var priorityModels = new List<(ModelBundleInfo bundle, ModelInfo model)>();

            if (priorityModelIDs != null)
                foreach (var priorityModelID in priorityModelIDs)
                {
                    if (string.IsNullOrEmpty(priorityModelID)) continue;
                    if (ModelManager.FindModelByID(priorityModelID, out var bundleInfo, out var modelInfo))
                        priorityModels.Add((bundleInfo, modelInfo));
                }

            try
            {
                var bundlesToReload = ModelManager.UpdateModelBundles();
                _currentRefreshingBundles = bundlesToReload;

                if (bundlesToReload.Count > 0)
                {
                    if (priorityModels.Count > 0)
                        foreach (var (priorityBundle, _) in priorityModels)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await AssetBundleManager.GetOrLoadAssetBundleAsync(priorityBundle, false,
                                cancellationToken);
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }

                    var count = 0;

                    if (priorityModels.Count > 0)
                        foreach (var (priorityBundle, priorityModel) in priorityModels)
                        {
                            var priorityBundleInList =
                                ModelManager.ModelBundles.FirstOrDefault(b => b == priorityBundle);
                            var priorityModelInList =
                                priorityBundleInList?.Models.FirstOrDefault(m => m == priorityModel);
                            if (priorityModelInList == null) continue;
                            cancellationToken.ThrowIfCancellationRequested();
                            if (priorityBundleInList != null)
                                await AssetBundleManager.CheckBundleStatusAsync(priorityBundleInList,
                                    priorityModelInList,
                                    cancellationToken);
                            count++;
                            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        }

                    foreach (var bundle in ModelManager.ModelBundles)
                    foreach (var model in bundle.Models)
                    {
                        if (priorityModels.Any(pm => pm.bundle == bundle && pm.model == model))
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();
                        await AssetBundleManager.CheckBundleStatusAsync(bundle, model, cancellationToken);
                        count++;

                        if (count % 10 != 0) continue;
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                RefreshAndApplyAllModels();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error during model list refresh: {ex.Message}");
                ModLogger.LogException(ex);
            }
            finally
            {
                IsRefreshing = false;
                _currentRefreshingBundles = null;
                completionSource?.TrySetResult();
                _refreshCompletionSource = null;
                linkedCts?.Dispose();
                OnRefreshCompleted?.Invoke();
            }
        }

        #endregion

        #region 模型加载和应用

        public static void RefreshAndApplyAllModels()
        {
            if (ModEntry.UsingModel == null) return;

            foreach (var handler in ModelManager.GetAllHandlers())
                handler.UpdateModelPriorityList();
        }

        #endregion

        #region 配置管理

        public static void SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;

            ModEntry.UsingModel.SetModelID(targetTypeId, modelID);

            if (saveConfig)
                ConfigManager.SaveConfigToFile(ModEntry.UsingModel, "UsingModel.json");

            RefreshAndApplyAllModels();
        }

        public static void SetModelInConfigForAICharacter(string nameKey, string modelID, bool saveConfig = true)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrEmpty(nameKey)) return;

            var targetTypeId = ModelTargetType.CreateAICharacterTargetType(nameKey);
            SetModelInConfig(targetTypeId, modelID, saveConfig);
        }

        #endregion

        #region 刷新模型列表

        public static void RefreshModelList(IEnumerable<string>? priorityModelIDs = null)
        {
            if (IsRefreshing) return;

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                CancellationToken.None
            );

            _refreshCompletionSource = new();
            RefreshModelListAsync(linkedCts.Token, linkedCts, _refreshCompletionSource, priorityModelIDs).Forget();
        }

        public static void CancelRefresh()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
        }

        #endregion

        #region 过时成员（向后兼容）

        [Obsolete("Use SetModelInConfig(string targetTypeId, string modelID, bool saveConfig) instead.")]
        public static void ApplyModelToTargetType(string targetTypeId, string modelID, bool forceReapply = false)
        {
            SetModelInConfig(targetTypeId, modelID);
        }

        [Obsolete("Use RefreshAndApplyAllModels() instead.")]
        public static void ApplyAllModelsFromConfig(bool forceReapply = false)
        {
            RefreshAndApplyAllModels();
        }

        [Obsolete("Use SetModelInConfigForAICharacter(string nameKey, string modelID, bool saveConfig) instead.")]
        public static void ApplyModelToAICharacter(string nameKey, string modelID, bool forceReapply = false)
        {
            SetModelInConfigForAICharacter(nameKey, modelID);
        }

        [Obsolete("Use RefreshAndApplyAllModels() or directly call ModelHandler.UpdateModelPriorityList() instead. This method is kept for backward compatibility.")]
        public static void RestoreOriginalModelForTargetType(string targetTypeId)
        {
            if (string.IsNullOrWhiteSpace(targetTypeId)) return;

            if (targetTypeId == ModelTargetType.AllAICharacters)
                foreach (var handler in ModelManager.GetAllHandlers())
                {
                    if (!ModelTargetType.IsAICharacterTargetType(handler.TargetTypeId)) continue;
                    handler.UpdateModelPriorityList();
                }
            else
                foreach (var handler in ModelManager.GetAllModelHandlersByTargetType(targetTypeId))
                    handler.UpdateModelPriorityList();
        }

        [Obsolete("Use ApplyModelToTargetType(string targetTypeId, string modelID, bool forceReapply) instead.")]
        public static void ApplyModelToTarget(ModelTarget target, string modelID, bool forceReapply = false)
        {
            var targetTypeId = target.ToTargetTypeId();
            ApplyModelToTargetType(targetTypeId, modelID, forceReapply);
        }

        [Obsolete("Use RestoreOriginalModelForTargetType(string targetTypeId) instead.")]
        public static void RestoreOriginalModelForTarget(ModelTarget target)
        {
            var targetTypeId = target.ToTargetTypeId();
            RestoreOriginalModelForTargetType(targetTypeId);
        }

        [Obsolete(
            "Use ApplyModelToTargetType(string targetTypeId, string modelID, bool forceReapply) with ModelTargetType.CreateAICharacterTargetType instead.")]
        public static void ApplyModelToTargetAfterRefresh(ModelTarget target, string modelID,
            IReadOnlyCollection<string>? bundlesToReload = null)
        {
            if (ModEntry.UsingModel == null) return;
            if (string.IsNullOrEmpty(modelID)) return;

            var targetTypeId = target.ToTargetTypeId();
            if (!ModelManager.FindModelByID(modelID, out var bundleInfo, out var modelInfo))
            {
                ModLogger.LogWarning($"Model '{modelID}' not found for {targetTypeId}");
                return;
            }

            var needsReapply = false;
            if (bundlesToReload is { Count: > 0 })
                if (bundlesToReload.Contains(bundleInfo.BundleName))
                    needsReapply = true;

            if (!needsReapply)
            {
                var handlers = ModelManager.GetAllModelHandlersByTargetType(targetTypeId);
                var allApplied = handlers.All(handler => handler.IsHiddenOriginalModel);
                var needsAudioRestore = handlers.Any(handler =>
                    !handler.HasAnySounds() && modelInfo.CustomSounds is { Length: > 0 });

                if (allApplied && !needsAudioRestore) return;
            }

            ApplyModelToTargetType(targetTypeId, modelID, true);
        }

        #endregion
    }
}
