using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Physics
{
    [AddComponentMenu("Duckov Custom Model/Spring Bone Collider")]
    public sealed class ModelSpringCollider : MonoBehaviour
    {
        public enum ColliderShape
        {
            Sphere,
            Capsule
        }

        [Tooltip("Sphere or capsule used by referencing spring bones only; does not affect game physics.")]
        public ColliderShape Shape;

        public Vector3 Offset;
        [Min(0)] public float Radius = .05f;
        public Vector3 Tail = new(0, .1f, 0);

        public float WorldRadius => Mathf.Max(0, Radius) * Mathf.Max(Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));

        private void OnDrawGizmosSelected()
        {
            var color = Gizmos.color;
            Gizmos.color = new Color(.3f, .85f, 1);
            var head = transform.TransformPoint(Offset);
            Gizmos.DrawWireSphere(head, WorldRadius);
            if (Shape == ColliderShape.Capsule)
            {
                var tail = transform.TransformPoint(Tail);
                Gizmos.DrawWireSphere(tail, WorldRadius);
                var axis = (tail - head).normalized;
                var side = Vector3.Cross(axis, Vector3.up).normalized;
                if (side.sqrMagnitude < .001f) side = Vector3.Cross(axis, Vector3.right).normalized;
                var other = Vector3.Cross(axis, side);
                foreach (var direction in new[] { side, -side, other, -other })
                    Gizmos.DrawLine(head + direction * WorldRadius, tail + direction * WorldRadius);
            }

            Gizmos.color = color;
        }
    }
}
