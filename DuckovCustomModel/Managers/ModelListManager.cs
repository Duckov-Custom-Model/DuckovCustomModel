using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Data;

namespace DuckovCustomModel.Managers
{
    public static class ModelListManager
    {
        private static CancellationTokenSource? _refreshCancellationTokenSource;
        private static UniTaskCompletionSource? _refreshCompletionSource;

        public static bool IsRefreshing { get; private set; }

        public static event Action? OnRefreshStarted;
        public static event Action? OnRefreshCompleted;
        public static event Action<string>? OnRefreshProgress;

        public static void RefreshModelList(IEnumerable<string>? priorityModelIDs = null)
        {
            if (IsRefreshing)
            {
                _refreshCancellationTokenSource?.Cancel();
                _refreshCancellationTokenSource?.Dispose();
            }

            _refreshCancellationTokenSource = new();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _refreshCancellationTokenSource.Token,
                CancellationToken.None
            );

            _refreshCompletionSource = new();
            RefreshModelListAsync(linkedCts.Token, linkedCts, _refreshCompletionSource, priorityModelIDs).Forget();
        }

        public static async UniTask WaitForRefreshCompletion()
        {
            if (_refreshCompletionSource != null) await _refreshCompletionSource.Task;
        }

        private static async UniTaskVoid RefreshModelListAsync(CancellationToken cancellationToken,
            CancellationTokenSource? linkedCts, UniTaskCompletionSource? completionSource,
            IEnumerable<string>? priorityModelIDs)
        {
            IsRefreshing = true;
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
                ModelManager.UpdateModelBundles();

                if (priorityModels.Count > 0)
                    foreach (var (priorityBundle, priorityModel) in priorityModels)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        OnRefreshProgress?.Invoke($"Loading priority model: {priorityModel.Name}");
                        await AssetBundleManager.GetOrLoadAssetBundleAsync(priorityBundle, false, cancellationToken);
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }

                var totalCount = ModelManager.ModelBundles.Sum(b => b.Models.Length);
                var count = 0;

                if (priorityModels.Count > 0)
                    foreach (var (priorityBundle, priorityModel) in priorityModels)
                    {
                        var priorityBundleInList = ModelManager.ModelBundles.FirstOrDefault(b => b == priorityBundle);
                        var priorityModelInList = priorityBundleInList?.Models.FirstOrDefault(m => m == priorityModel);
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
                    OnRefreshProgress?.Invoke($"Loading... ({count}/{totalCount})");
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                IsRefreshing = false;
                completionSource?.TrySetResult();
                _refreshCompletionSource = null;
                linkedCts?.Dispose();
                OnRefreshCompleted?.Invoke();
            }
        }

        public static void CancelRefresh()
        {
            _refreshCancellationTokenSource?.Cancel();
            _refreshCancellationTokenSource?.Dispose();
        }
    }
}