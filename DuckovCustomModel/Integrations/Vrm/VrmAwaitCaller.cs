using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal sealed class VrmAwaitCaller : IAwaitCaller
    {
        private static int _frame = -1;
        private static long _deadline;

        public Task NextFrame()
        {
            var now = Stopwatch.GetTimestamp();
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                _deadline = now + Stopwatch.Frequency / 250;
            }

            return now < _deadline ? Task.CompletedTask : UniTask.NextFrame().AsTask();
        }

        public Task NextFrameIfTimedOut()
        {
            return NextFrame();
        }

        public async Task Run(Action action)
        {
            await NextFrameIfTimedOut();
            action();
            await NextFrameIfTimedOut();
        }

        public Task<T> Run<T>(Func<T> action)
        {
            return Task.Run(action);
        }
    }
}
