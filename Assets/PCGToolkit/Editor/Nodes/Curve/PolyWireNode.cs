using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 曲线/线段转管状网格（对标 Houdini PolyWire SOP）
    /// 比 SweepNode 更轻量，固定圆形截面，沿线段序列生成管状网格。
    /// </summary>
    public class PolyWireNode : PCGNodeBase
    {
        public override string Name => "PolyWire";
        public override string DisplayName => "Poly Wire";
        public override string Description => "将曲线/线段转换为管状网格（固定圆形截面）";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线/线段几何体（点序列）", null, required: true),
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "管半径", 0.1f),
            new PCGParamSchema("sides", PCGPortDirection.Input, PCGPortType.Int,
                "Sides", "截面边数", 8),
            new PCGParamSchema("capEnds", PCGPortDirection.Input, PCGPortType.Bool,
                "Cap Ends", "是否封口两端", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "管状网格"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            float radius = Mathf.Max(0.001f, GetParamFloat(parameters, "radius", 0.1f));
            int sides = Mathf.Max(3, GetParamInt(parameters, "sides", 8));
            bool capEnds = GetParamBool(parameters, "capEnds", true);

            if (geo.Points.Count < 2)
            {
                ctx.LogWarning("PolyWire: 至少需要 2 个点");
                return SingleOutput("geometry", geo.Clone());
            }

            var result = new PCGGeometry();

            // 把点序列当作一条折线
            var points = geo.Points;
            int segCount = points.Count;

            // 为每个点生成一圈截面顶点
            // ring[i] = 第 i 个点处的截面起始索引
            int[] ringStart = new int[segCount];

            for (int i = 0; i < segCount; i++)
            {
                // 计算局部坐标系 (tangent, normal, binormal)
                Vector3 tangent;
                if (i == 0)
                    tangent = (points[1] - points[0]).normalized;
                else if (i == segCount - 1)
                    tangent = (points[i] - points[i - 1]).normalized;
                else
                    tangent = (points[i + 1] - points[i - 1]).normalized;

                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = Vector3.up;

                // 找一个不平行的向量来构建局部坐标系
                Vector3 up = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) < 0.99f
                    ? Vector3.up : Vector3.right;
                Vector3 normal = Vector3.Cross(tangent, up).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

                ringStart[i] = result.Points.Count;

                for (int s = 0; s < sides; s++)
                {
                    float angle = s * Mathf.PI * 2f / sides;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    Vector3 offset = (normal * cos + binormal * sin) * radius;
                    result.Points.Add(points[i] + offset);
                }
            }

            // 生成侧面四边形：连接相邻两圈
            for (int i = 0; i < segCount - 1; i++)
            {
                int r0 = ringStart[i];
                int r1 = ringStart[i + 1];
                for (int s = 0; s < sides; s++)
                {
                    int s1 = (s + 1) % sides;
                    result.Primitives.Add(new int[]
                    {
                        r0 + s, r0 + s1,
                        r1 + s1, r1 + s
                    });
                }
            }

            // 封口
            if (capEnds)
            {
                // 起始端封口（N-gon，反向绕序）
                var startCap = new int[sides];
                for (int s = 0; s < sides; s++)
                    startCap[s] = ringStart[0] + (sides - 1 - s);
                result.Primitives.Add(startCap);

                // 末端封口（N-gon）
                var endCap = new int[sides];
                for (int s = 0; s < sides; s++)
                    endCap[s] = ringStart[segCount - 1] + s;
                result.Primitives.Add(endCap);
            }

            ctx.Log($"PolyWire: {segCount} segments, r={radius}, {sides} sides, {result.Points.Count} pts, {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }
    }
}
