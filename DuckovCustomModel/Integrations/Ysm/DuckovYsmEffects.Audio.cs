using System;
using System.Collections.Generic;
using ModelRuntime;
using ModelRuntime.Adapters;
using ModelRuntime.Media;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmEffects
    {
        private const int MaxDecodedClipBytes = 32 * 1024 * 1024;
        private const long MaxCachedClipBytes = 64L * 1024 * 1024;
        private readonly Dictionary<(ModelPackage, string), AudioClip> audioClips = new();
        private readonly HashSet<SoundVoice> voices = new();
        private long cachedClipBytes;

        private AudioClip? GetAudioClip(string resource, ModelPackage package)
        {
            var key = (package, resource);
            if (audioClips.TryGetValue(key, out var cached)) return cached;
            var folder = ((string?)package.Manifest["files"]?["sound_path"] ?? "sounds").TrimEnd('/', '\\') + "/";
            if (!package.Resources.TryGetValue(folder + resource, out var bytes) &&
                !package.Resources.TryGetValue(folder + resource + ".wav", out bytes) &&
                !package.Resources.TryGetValue(folder + resource + ".ogg", out bytes))
            {
                WarnOnce("audio-missing:" + resource, "No packaged sound resource for '" + resource + "'.");
                return null;
            }

            var wave = new WaveAudioDecoder();
            var vorbis = new VorbisAudioDecoder();
            IAudioDecoder? decoder = wave.CanDecode(bytes) ? wave : vorbis.CanDecode(bytes) ? vorbis : null;
            if (decoder == null)
            {
                WarnOnce("audio-format:" + resource,
                    "Unsupported sound codec for '" + resource + "'; WAV PCM and Ogg Vorbis are supported.");
                return null;
            }

            var budget = (int)Math.Min(MaxDecodedClipBytes, Math.Max(0, MaxCachedClipBytes - cachedClipBytes));
            var pcm = decoder.Decode(bytes, budget);
            if (pcm.Frames == 0) return null;
            var clip = AudioClip.Create(resource, pcm.Frames, pcm.Channels, pcm.SampleRate, false);
            try
            {
                if (!clip.SetData(pcm.Samples, 0))
                    throw new InvalidOperationException("Unity rejected the decoded PCM audio.");
                audioClips.Add(key, clip);
                cachedClipBytes += pcm.Samples.LongLength * sizeof(float);
                return clip;
            }
            catch
            {
                Object.Destroy(clip);
                throw;
            }
        }

        private sealed class SoundBackend : ISoundBackend
        {
            private readonly DuckovYsmEffects host;

            internal SoundBackend(DuckovYsmEffects host)
            {
                this.host = host;
            }

            public ISoundVoice? Create(string entityId, string resource, ModelPackage package, bool global)
            {
                if (host.voices.Count >= 64)
                {
                    host.WarnOnce("voice-budget", "The per-model limit of 64 simultaneous sounds was reached.");
                    return null;
                }

                var clip = host.GetAudioClip(resource, package);
                if (!clip) return null;
                var instance = new GameObject("YSM sound");
                try
                {
                    instance.transform.SetParent(host.root, false);
                    var source = instance.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.clip = clip;
                    source.spatialBlend = 1;
                    source.rolloffMode = AudioRolloffMode.Logarithmic;
                    source.minDistance = 1;
                    source.maxDistance = 30;
                    source.dopplerLevel = 0;
                    var voice = new SoundVoice(host, source);
                    host.voices.Add(voice);
                    return voice;
                }
                catch
                {
                    Object.Destroy(instance);
                    throw;
                }
            }
        }

        private sealed class SoundVoice : ISpatialSoundVoice
        {
            private readonly DuckovYsmEffects host;
            private AudioSource? source;
            private float volume;

            internal SoundVoice(DuckovYsmEffects host, AudioSource source)
            {
                this.host = host;
                this.source = source;
            }

            public bool IsPlaying => source is not null && source && source.isPlaying;

            public void SetSpatial(SoundSpatialState state)
            {
                if (source is null || !source) return;
                source.spatialBlend = state.ListenerRelative ? 0 : 1;
                source.transform.position = ToUnity(state.Position);
            }

            public void Play(bool loop, float requestedVolume, float pitch)
            {
                if (source is null || !source) return;
                volume = requestedVolume;
                source.loop = loop;
                source.pitch = Mathf.Clamp(pitch, -3, 3);
                RefreshVolume();
                source.Play();
            }

            public void Dispose()
            {
                host.voices.Remove(this);
                if (source is null || !source) return;
                source.Stop();
                source.clip = null;
                Object.Destroy(source.gameObject);
                source = null;
            }

            internal void RefreshVolume()
            {
                if (source is null || !source) return;
                source.mute = !host.enabled || !host.handler || !host.handler.IsModelAudioEnabled;
                source.volume = Mathf.Clamp01(volume * (host.handler ? host.handler.ModelAudioVolume : 0));
            }
        }
    }
}
