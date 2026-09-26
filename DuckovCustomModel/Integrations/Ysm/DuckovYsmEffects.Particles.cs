using System;
using System.Collections.Generic;
using ModelRuntime;
using ModelRuntime.Adapters;
using UnityEngine;
using NMatrix = System.Numerics.Matrix4x4;
using NVector = System.Numerics.Vector3;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace DuckovCustomModel.Integrations.Ysm
{
    public sealed partial class DuckovYsmEffects
    {
        private readonly List<ParticleEmitter> bursts = new();
        private readonly Dictionary<string, ParticleMapping> particleMappings = new(StringComparer.Ordinal);
        private readonly Random particleRandom = new();

        public void RegisterParticle(string resource, ParticleSystem prefab, float maximumLifetimeSeconds = 30)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuckovYsmEffects));
            if (string.IsNullOrEmpty(resource))
                throw new ArgumentException("A resource ID is required.", nameof(resource));
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            if (float.IsNaN(maximumLifetimeSeconds) || float.IsInfinity(maximumLifetimeSeconds) ||
                maximumLifetimeSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumLifetimeSeconds));
            particleMappings[resource] = new(prefab, maximumLifetimeSeconds);
        }

        private ParticleMapping? FindParticle(string resource)
        {
            if (particleMappings.TryGetValue(resource, out var mapping) && mapping.Prefab) return mapping;
            WarnOnce("particle:" + resource,
                "Particle resource '" + resource +
                "' is unsupported until explicitly mapped to a Unity ParticleSystem prefab.");
            return null;
        }

        private void PlayBurst(ParticleCommand command)
        {
            var mapping = FindParticle(command.Resource);
            if (mapping == null || !root) return;
            if (bursts.Count >= 64)
            {
                WarnOnce("burst-budget", "The per-model limit of 64 active particle bursts was reached.");
                return;
            }

            var system = Object.Instantiate(mapping.Prefab);
            try
            {
                system.gameObject.name = "YSM particles " + command.Resource;
                system.gameObject.SetActive(true);
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = system.emission;
                emission.enabled = false;
                var main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var count = Math.Min(ParticleEmission.Count(command), 1024);
                main.maxParticles = Math.Max(main.maxParticles, count);
                var world = modelToWorld();
                var origin = world.Translation;
                var lifetime = Math.Min(mapping.Lifetime, Math.Max(0, command.LifetimeTicks / 20f));
                for (var i = 0; i < count; i++)
                {
                    var spawn = ParticleEmission.Sample(command, i, NVector.Zero, 0, particleRandom);
                    var offset = command.AbsoluteRotation
                        ? new(spawn.Position.X, spawn.Position.Y, -spawn.Position.Z)
                        : NVector.TransformNormal(spawn.Position * 16, world);
                    var velocity = new Vector3(spawn.Velocity.X, spawn.Velocity.Y, -spawn.Velocity.Z);
                    var parameters = new ParticleSystem.EmitParams
                    {
                        position = ToUnity(origin + offset),
                        velocity = velocity,
                        startLifetime = lifetime,
                    };
                    system.Emit(parameters, 1);
                }

                bursts.Add(new(system, Math.Max(.1f, lifetime), true));
            }
            catch
            {
                Object.Destroy(system.gameObject);
                throw;
            }
        }

        private void UpdateBursts(double deltaSeconds)
        {
            for (var i = bursts.Count - 1; i >= 0; i--)
            {
                var burst = bursts[i];
                burst.Advance(deltaSeconds);
                if (burst.IsAlive) continue;
                bursts.RemoveAt(i);
                burst.Dispose();
            }
        }

        private void ClearBursts()
        {
            foreach (var burst in bursts) burst.Dispose();
            bursts.Clear();
        }

        private sealed class ParticleMapping
        {
            internal readonly float Lifetime;
            internal readonly ParticleSystem Prefab;

            internal ParticleMapping(ParticleSystem prefab, float lifetime)
            {
                Prefab = prefab;
                Lifetime = lifetime;
            }
        }

        private sealed class EmitterBackend : IParticleEmitterBackend
        {
            private readonly DuckovYsmEffects host;

            internal EmitterBackend(DuckovYsmEffects host)
            {
                this.host = host;
            }

            public IParticleEmitter? Create(string resource, ModelPackage package, MolangContext context)
            {
                var mapping = host.FindParticle(resource);
                if (mapping == null || !host.root) return null;
                var system = Object.Instantiate(mapping.Prefab);
                try
                {
                    system.gameObject.name = "YSM emitter " + resource;
                    system.gameObject.SetActive(true);
                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    return new ParticleEmitter(system, mapping.Lifetime);
                }
                catch
                {
                    Object.Destroy(system.gameObject);
                    throw;
                }
            }
        }

        private sealed class ParticleEmitter : IParticleEmitter
        {
            private readonly Vector3 prefabScale;
            private double remaining;
            private bool started;
            private ParticleSystem? system;

            internal ParticleEmitter(ParticleSystem system, double lifetime, bool alreadyEmitted = false)
            {
                this.system = system;
                remaining = lifetime;
                started = alreadyEmitted;
                prefabScale = system.transform.localScale;
                system.Pause(true);
            }

            public bool IsAlive => system is not null && system && remaining > 0 && (!started || system.IsAlive(true));

            public void SetTransform(NMatrix matrix)
            {
                if (system is null || !system) return;
                if (!NMatrix.Decompose(matrix, out var scale, out var rotation, out var position))
                    throw new InvalidOperationException("The particle emitter transform cannot be decomposed.");
                system.transform.SetPositionAndRotation(ToUnity(position),
                    new(rotation.X, rotation.Y, rotation.Z, rotation.W));
                system.transform.localScale = Vector3.Scale(prefabScale, ToUnity(scale * 16));
            }

            public void Advance(double deltaSeconds)
            {
                if (system is null || !system) return;
                remaining -= deltaSeconds;
                if (!started)
                {
                    started = true;
                    system.Play(true);
                    system.Pause(true);
                }

                system.Simulate((float)deltaSeconds, true, false, true);
                system.Pause(true);
            }

            public void Dispose()
            {
                if (system is null || !system) return;
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.gameObject.SetActive(false);
                Object.Destroy(system.gameObject);
                system = null;
            }
        }
    }
}
