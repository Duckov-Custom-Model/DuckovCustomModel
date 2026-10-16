using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Motions
{
    internal sealed class MotionAudio : IDisposable
    {
        private Sound _sound;
        private FMOD.Studio.Bus _bus;
        private ChannelGroup _group;
        private bool _busLocked, _spatial;
        private FMOD.Channel _channel;
        private uint _length;
        private bool _disposed;
        private int _cycle = -1;

        internal static async Task<MotionAudio> Load(string path, bool spatial, Func<bool> cancelled)
        {
            var audio = new MotionAudio { _spatial = spatial };
            try
            {
                Check(RuntimeManager.CoreSystem.createStream(path,
                    MODE.NONBLOCKING | (spatial ? MODE._3D : MODE._2D), out audio._sound));
                while (true)
                {
                    if (cancelled()) throw new OperationCanceledException();
                    Check(audio._sound.getOpenState(out var state, out _, out _, out _));
                    if (state == OPENSTATE.READY) break;
                    if (state == OPENSTATE.ERROR) throw new IOException("Audio could not be decoded.");
                    await UniTask.Yield();
                }

                Check(audio._sound.getLength(out audio._length, TIMEUNIT.MS));
                Check(RuntimeManager.StudioSystem.getBus("bus:/", out audio._bus));
                Check(audio._bus.lockChannelGroup());
                audio._busLocked = true;
                var waiting = 0;
                while (audio._bus.getChannelGroup(out audio._group) != RESULT.OK)
                {
                    if (cancelled()) throw new OperationCanceledException();
                    if (++waiting > 120) throw new IOException("Game audio mixer is unavailable.");
                    await UniTask.Yield();
                }

                return audio;
            }
            catch
            {
                audio.Dispose();
                throw;
            }
        }

        internal void Sync(double seconds, int cycle, float speed, float volume, Vector3 position, bool paused)
        {
            if (_disposed) return;
            var milliseconds = seconds * 1000;
            if (milliseconds < 0 || milliseconds >= _length)
            {
                if (_channel.hasHandle()) _channel.setPaused(true);
                return;
            }

            var reset = cycle != _cycle;
            _channel.isPlaying(out var playing);
            if (!_channel.hasHandle() || !playing)
            {
                if (!reset) return;
                Check(RuntimeManager.CoreSystem.playSound(_sound, _group, true, out _channel));
                if (_spatial) _channel.set3DMinMaxDistance(1, 30);
            }

            _cycle = cycle;
            _channel.getPosition(out var actual, TIMEUNIT.MS);
            if (reset || Math.Abs(actual - milliseconds) > 80)
                _channel.setPosition((uint)milliseconds, TIMEUNIT.MS);
            _channel.setPitch(Mathf.Max(.01f, speed));
            _channel.setVolume(volume);
            if (_spatial)
            {
                var point = new VECTOR { x = position.x, y = position.y, z = position.z };
                var velocity = default(VECTOR);
                _channel.set3DAttributes(ref point, ref velocity);
            }

            _channel.setPaused(paused);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_channel.hasHandle()) _channel.stop();
            _channel.clearHandle();
            if (_sound.hasHandle()) _sound.release();
            _sound.clearHandle();
            if (_busLocked) _bus.unlockChannelGroup();
            _busLocked = false;
        }

        private static void Check(RESULT result)
        {
            if (result != RESULT.OK) throw new IOException("Motion audio: " + result);
        }
    }
}
