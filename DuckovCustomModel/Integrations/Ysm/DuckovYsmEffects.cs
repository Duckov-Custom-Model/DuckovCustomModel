using System;
using System.Collections.Generic;
using DuckovCustomModel.MonoBehaviours;
using ModelRuntime;
using ModelRuntime.Adapters;
using UnityEngine;
using NMatrix = System.Numerics.Matrix4x4;
using NVector = System.Numerics.Vector3;
using Object = UnityEngine.Object;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmEffects : IModelSoundSink, IDisposable
    {
        private readonly HashSet<string> diagnostics = new(StringComparer.Ordinal);
        private readonly ModelHandler handler;
        private readonly Func<NMatrix> modelToWorld;
        private readonly Transform root;
        private bool disposed;
        private ParticleEmitterManager emitters;
        private bool enabled;
        private ModelSimulation? simulation;
        private SoundPlaybackManager sounds;

        public DuckovYsmEffects(Transform root, ModelHandler handler, Func<NMatrix> modelToWorld)
        {
            this.root = root ? root : throw new ArgumentNullException(nameof(root));
            this.handler = handler ? handler : throw new ArgumentNullException(nameof(handler));
            this.modelToWorld = modelToWorld ?? throw new ArgumentNullException(nameof(modelToWorld));
            sounds = CreateSoundManager();
            emitters = CreateEmitterManager();
        }

        public IModelSoundSink Sink => this;

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (disposed || enabled == value) return;
                enabled = value;
                if (value) return;
                try
                {
                    sounds.Dispose();
                }
                finally
                {
                    try
                    {
                        emitters.Dispose();
                    }
                    finally
                    {
                        ClearBursts();
                        sounds = CreateSoundManager();
                        emitters = CreateEmitterManager();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            enabled = false;
            try
            {
                sounds.Dispose();
            }
            finally
            {
                try
                {
                    emitters.Dispose();
                }
                finally
                {
                    ClearBursts();
                    foreach (var clip in audioClips.Values)
                        if (clip)
                            Object.Destroy(clip);
                    audioClips.Clear();
                    particleMappings.Clear();
                    diagnostics.Clear();
                    simulation = null;
                }
            }
        }

        public bool HandleSound(string entityId, SoundCommand command, ModelPackage package)
        {
            if (disposed) return false;
            if (command.Action == SoundAction.Play && (!enabled || !root || !root.gameObject.activeInHierarchy ||
                                                       !handler.IsModelAudioEnabled)) return false;
            try
            {
                return sounds.Handle(entityId, command, package);
            }
            catch (Exception error)
            {
                WarnOnce("sound:" + command.Resource, "Sound '" + command.Resource + "' failed: " + error.Message);
                return false;
            }
        }

        public void Handle(ModelEffect effect)
        {
            if (disposed || simulation == null) return;
            if (effect.Emitter is ParticleEmitterCommand emitter)
            {
                if (emitter.Action != ParticleEmitterAction.Spawn || enabled)
                    emitters.Handle(emitter, effect.Package, simulation.Pose);
            }
            else if (enabled && effect.Particle is ParticleCommand particle)
            {
                PlayBurst(particle);
            }
        }

        public void Bind(ModelSimulation value)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuckovYsmEffects));
            if (simulation != null && !ReferenceEquals(simulation, value))
                throw new InvalidOperationException("An effect host cannot be shared between model instances.");
            simulation = value ?? throw new ArgumentNullException(nameof(value));
        }

        private SoundPlaybackManager CreateSoundManager()
        {
            return new(new SoundBackend(this))
            {
                ResolveSource = (_, _) => disposed || !enabled || !root || simulation == null
                    ? null
                    : new SoundSourceState(simulation.Pose, modelToWorld(), ToNumerics(root.position)),
            };
        }

        private ParticleEmitterManager CreateEmitterManager()
        {
            return new(new EmitterBackend(this))
            {
                MaxEmitters = 64,
                PrepareContext = PrepareEmitterContext,
            };
        }

        private void PrepareEmitterContext(MolangContext context)
        {
            if (simulation == null) return;
            var source = simulation.Context;
            context.ValueCompatibility = source.ValueCompatibility;
            context.ValueQuery = source.ValueQuery;
            context.FunctionFallback = source.FunctionFallback;
            context["query.life_time"] = simulation.Time;
            foreach (var pair in source.ValueFunctions)
                if (pair.Key.StartsWith("query.", StringComparison.Ordinal))
                    context.ValueFunctions[pair.Key] = pair.Value;
        }

        public void Tick(double deltaSeconds)
        {
            if (disposed || !enabled) return;
            if (deltaSeconds < 0 || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            sounds.UpdateSpatial(deltaSeconds);
            foreach (var voice in voices) voice.RefreshVolume();
            emitters.Update(deltaSeconds, modelToWorld());
            UpdateBursts(deltaSeconds);
        }

        private void WarnOnce(string key, string message)
        {
            if (diagnostics.Add(key)) Debug.LogWarning("[DuckovCustomModel YSM] " + message);
        }

        private static NVector ToNumerics(Vector3 value)
        {
            return new(value.x, value.y, value.z);
        }

        private static Vector3 ToUnity(NVector value)
        {
            return new(value.X, value.Y, value.Z);
        }
    }
}
