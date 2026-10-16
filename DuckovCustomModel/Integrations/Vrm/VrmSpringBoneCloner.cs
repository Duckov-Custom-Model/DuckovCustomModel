using System;
using System.Collections.Generic;
using System.Linq;
using UniVRM10;
using UnityEngine;

namespace DuckovCustomModel.Integrations.Vrm
{
    internal static class VrmSpringBoneCloner
    {
        internal static Transform[] Copy(Vrm10Instance source, Vrm10Instance target)
        {
            var originals = source.GetComponentsInChildren<Transform>(true);
            var copies = target.GetComponentsInChildren<Transform>(true);
            if (originals.Length != copies.Length)
                throw new InvalidOperationException("Cloned VRM hierarchy differs from its template.");
            var transforms = new Dictionary<Transform, Transform>(originals.Length);
            for (var i = 0; i < originals.Length; i++)
            {
                if (i > 0 && (originals[i].name != copies[i].name ||
                              !transforms.TryGetValue(originals[i].parent, out var parent) ||
                              copies[i].parent != parent))
                    throw new InvalidOperationException("Cloned VRM hierarchy order differs from its template.");
                transforms.Add(originals[i], copies[i]);
            }

            var joints = MapComponents<VRM10SpringBoneJoint>();
            foreach (var pair in joints)
            {
                var original = pair.Key;
                var copy = pair.Value;
                copy.m_stiffnessForce = original.m_stiffnessForce;
                copy.m_gravityPower = original.m_gravityPower;
                copy.m_gravityDir = original.m_gravityDir;
                copy.m_dragForce = original.m_dragForce;
                copy.m_jointRadius = original.m_jointRadius;
                copy.m_anglelimitType = original.m_anglelimitType;
                copy.m_limitSpaceOffset = original.m_limitSpaceOffset;
                copy.m_pitch = original.m_pitch;
                copy.m_yaw = original.m_yaw;
            }

            var colliders = MapComponents<VRM10SpringBoneCollider>();
            foreach (var pair in colliders)
            {
                pair.Value.ColliderType = pair.Key.ColliderType;
                pair.Value.Offset = pair.Key.Offset;
                pair.Value.Radius = pair.Key.Radius;
                pair.Value.Tail = pair.Key.Tail;
                pair.Value.Normal = pair.Key.Normal;
            }

            var groups = MapComponents<VRM10SpringBoneColliderGroup>();
            foreach (var pair in groups)
            {
                pair.Value.Name = pair.Key.Name;
                pair.Value.Colliders = pair.Key.Colliders.Select(collider => colliders[collider]).ToList();
            }

            target.SpringBone = new Vrm10InstanceSpringBone
            {
                ColliderGroups = source.SpringBone.ColliderGroups.Select(group => groups[group]).ToList(),
                Springs = source.SpringBone.Springs.Select(spring => new Vrm10InstanceSpringBone.Spring(spring.Name)
                {
                    Center = spring.Center == null ? null : transforms[spring.Center],
                    Joints = spring.Joints.Select(joint => joints[joint]).ToList(),
                    ColliderGroups = spring.ColliderGroups.Select(group => groups[group]).ToList(),
                }).ToList(),
            };
            return copies;

            Dictionary<T, T> MapComponents<T>() where T : Component
            {
                var originalComponents = source.GetComponentsInChildren<T>(true);
                var copyComponents = target.GetComponentsInChildren<T>(true);
                if (originalComponents.Length != copyComponents.Length)
                    throw new InvalidOperationException("Cloned VRM component count differs: " + typeof(T).Name);
                var map = new Dictionary<T, T>(originalComponents.Length);
                for (var i = 0; i < originalComponents.Length; i++)
                {
                    if (copyComponents[i].transform != transforms[originalComponents[i].transform])
                        throw new InvalidOperationException("Cloned VRM component order differs: " + typeof(T).Name);
                    map.Add(originalComponents[i], copyComponents[i]);
                }

                return map;
            }
        }
    }
}
