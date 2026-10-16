using System;
using DuckovCustomModel.Core.Data;

namespace DuckovCustomModel.Integrations.Motions
{
    internal interface IMotionSampler : IDisposable
    {
        double Duration { get; }
        bool ControlsBody { get; }
        void Apply(double seconds, float weight, MotionPlaybackSettings settings);
        void Restore();
    }
}
