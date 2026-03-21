using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 将点沿目标表面"爬行"变形（对标 Houdini Creep SOP）
    /// 将输入几何体的每个点投射到目标表面最近的三角形上。
    /// </summary>
    public class CreepNode : PCGNodeBase
    {
        public override string Name => "Creep";
        public override string DisplayName => "Creep";
        public override string Description => "将点投射到目标表面上（沿最近面爬行）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "要变形的几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target", "目标表面几何体", null, required: true),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Float,
                "Offset", "投射后沿法线偏移距离", 0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "变形后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            var target = GetInputGeometry(inputGeometries, "target");
            float offset = GetParamFloat(parameters, "offset", 0f);

            if (geo.Points.Count == 0 || target.Primitives.Count == 0)
            {
                ctx.LogWarning("Creep: 输入或目标几何体为空");
                return SingleOutput("geometry", geo);
            }

            // 预计算目标面的三角形数据
            var tris = new List<(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)>();
            foreach (var prim in target.Primitives)
            {
                if (prim.Length < 3) continue;
                // 对 N 边形做扇形拆分
                for (int j = 1; j < prim.Length - 1; j++)
                {
                    Vector3 a = target.Points[prim[0]];
                    Vector3 b = target.Points[prim[j]];
                    Vector3 c = target.Points[prim[j + 1]];
                    Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                    tris.Add((a, b, c, n));
                }
            }

            // 对每个输入点找最近三角形并投射
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i];
                float bestDist = float.MaxValue;
                Vector3 bestProj = p;
                Vector3 bestNormal = Vector3.up;

                foreach (var tri in tris)
                {
                    Vector3 proj = ClosestPointOnTriangle(p, tri.a, tri.b, tri.c);
                    float dist = (p - proj).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestProj = proj;
                        bestNormal = tri.normal;
                    }
                }

                geo.Points[i] = bestProj + bestNormal * offset;
            }

            ctx.Log($"Creep: {geo.Points.Count} points projected onto {tris.Count} triangles");
            return SingleOutput("geometry", geo);
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                return a + ab * v;
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                return a + ac * w;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + (c - b) * w;
            }

            float denom = 1f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            return a + ab * v2 + ac * w2;
        }
    }
}
