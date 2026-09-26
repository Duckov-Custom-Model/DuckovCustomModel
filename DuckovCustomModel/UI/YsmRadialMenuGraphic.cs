using UnityEngine;
using UnityEngine.UI;

namespace DuckovCustomModel.UI
{
    internal sealed class YsmRadialMenuGraphic : MaskableGraphic
    {
        internal float InnerRadius = 50;
        internal float OuterRadius = 210;
        internal int Slot;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int segments = 12;
            var start = Slot * 45f + 2f;
            var span = 41f;
            for (var i = 0; i <= segments; i++)
            {
                var angle = (start + span * i / segments) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
                vh.AddVert(direction * InnerRadius, color, Vector2.zero);
                vh.AddVert(direction * OuterRadius, color, Vector2.one);
                if (i == 0) continue;
                var n = i * 2;
                vh.AddTriangle(n - 2, n - 1, n);
                vh.AddTriangle(n, n - 1, n + 1);
            }
        }
    }
}
