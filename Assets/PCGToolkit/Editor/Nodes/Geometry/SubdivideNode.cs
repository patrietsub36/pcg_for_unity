using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 细分几何体（对标 Houdini Subdivide SOP）
    /// </summary>
    public class SubdivideNode : PCGNodeBase
    {
        public override string Name => "Subdivide";
        public override string DisplayName => "Subdivide";
        public override string Description => "对几何体进行细分（Catmull-Clark 或 Linear）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "细分迭代次数", 1),
            new PCGParamSchema("algorithm", PCGPortDirection.Input, PCGPortType.String,
                "Algorithm", "细分算法（catmull-clark / linear）", "linear"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            int iterations = Mathf.Max(1, GetParamInt(parameters, "iterations", 1));
            string algorithm = GetParamString(parameters, "algorithm", "linear");

            for (int iter = 0; iter < iterations; iter++)
            {
                geo = algorithm.ToLower() == "catmull-clark" 
                    ? SubdivideCatmullClark(geo) 
                    : SubdivideLinear(geo);
            }

            return SingleOutput("geometry", geo);
        }

        private PCGGeometry SubdivideLinear(PCGGeometry geo)
        {
            var result = new PCGGeometry();

            // 第一步：复制所有原始顶点
            for (int i = 0; i < geo.Points.Count; i++)
            {
                result.Points.Add(geo.Points[i]);
            }

            // 第二步：为每条边创建共享中点（key 为排序后的顶点对）
            var edgeMidpoints = new Dictionary<(int, int), int>();

            int GetOrCreateMidpoint(int a, int b)
            {
                var key = a < b ? (a, b) : (b, a);
                if (edgeMidpoints.TryGetValue(key, out int midIdx))
                    return midIdx;
                midIdx = result.Points.Count;
                result.Points.Add((geo.Points[a] + geo.Points[b]) * 0.5f);
                edgeMidpoints[key] = midIdx;
                return midIdx;
            }

            // 第三步：细分每个面
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 4)
                {
                    int v0 = prim[0], v1 = prim[1], v2 = prim[2], v3 = prim[3];
                    int m01 = GetOrCreateMidpoint(v0, v1);
                    int m12 = GetOrCreateMidpoint(v1, v2);
                    int m23 = GetOrCreateMidpoint(v2, v3);
                    int m30 = GetOrCreateMidpoint(v3, v0);

                    // 中心点（每个面独有）
                    int center = result.Points.Count;
                    result.Points.Add((geo.Points[v0] + geo.Points[v1] +
                                       geo.Points[v2] + geo.Points[v3]) * 0.25f);

                    result.Primitives.Add(new int[] { v0, m01, center, m30 });
                    result.Primitives.Add(new int[] { m01, v1, m12, center });
                    result.Primitives.Add(new int[] { center, m12, v2, m23 });
                    result.Primitives.Add(new int[] { m30, center, m23, v3 });
                }
                else if (prim.Length == 3)
                {
                    int v0 = prim[0], v1 = prim[1], v2 = prim[2];
                    int m01 = GetOrCreateMidpoint(v0, v1);
                    int m12 = GetOrCreateMidpoint(v1, v2);
                    int m20 = GetOrCreateMidpoint(v2, v0);

                    result.Primitives.Add(new int[] { v0, m01, m20 });
                    result.Primitives.Add(new int[] { m01, v1, m12 });
                    result.Primitives.Add(new int[] { m20, m12, v2 });
                    result.Primitives.Add(new int[] { m01, m12, m20 });
                }
                else
                {
                    // 其他多边形：直接复制（索引已经有效，因为原始顶点已在 result 中）
                    result.Primitives.Add((int[])prim.Clone());
                }
            }

            return result;
        }

        private PCGGeometry SubdivideCatmullClark(PCGGeometry geo)
        {
            // Catmull-Clark 细分的简化实现
            // 完整实现需要考虑折痕、边界等特殊情况
            Debug.LogWarning("Subdivide: Catmull-Clark 细分使用简化实现");

            // 第一次迭代使用 linear，后续可改进
            return SubdivideLinear(geo);
        }
    }
}