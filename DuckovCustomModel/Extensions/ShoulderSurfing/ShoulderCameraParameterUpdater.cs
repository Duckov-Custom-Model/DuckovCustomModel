using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DuckovCustomModel.Core.Data;
using DuckovCustomModel.Managers;
using DuckovCustomModel.MonoBehaviours;

namespace DuckovCustomModel.Extensions.ShoulderSurfing
{
    public class ShoulderCameraParameterUpdater : IAnimatorParameterUpdater, IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        public ShoulderCameraParameterUpdater()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            UpdateLoop(_cancellationTokenSource.Token).Forget();
        }

        public void UpdateParameters(CustomAnimatorControl control)
        {
            if (control.CharacterMainControl == null || !control.CharacterMainControl.IsMainCharacter)
                return;

            control.SetParameterFloat(CustomAnimatorHash.ModShoulderSurfingCameraPitch,
                ShoulderCameraCompat.CameraPitch);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            ShoulderCameraCompat.Cleanup();

            _disposed = true;
        }

        private static async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ShoulderCameraCompat.UpdateState();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
