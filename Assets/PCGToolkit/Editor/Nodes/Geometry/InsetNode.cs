using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 面内缩（对标 Houdini PolyExtrude Inset 模式）
    /// 在每个面内部生成一圈缩小的面，并用四边形侧面带连接内外环。
    /// </summary>
    public class InsetNode : PCGNodeBase
    {
        public override string Name => "Inset";
        public override string DisplayName => "Inset";
        public override string Description => "对面进行内缩，生成环形侧面带";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要内缩的面分组（留空=全部）", ""),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "内缩距离", 0.1f),
            new PCGParamSchema("outputInner", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Inner", "是否输出内缩后的中心面", true),
            new PCGParamSchema("outputSide", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Side", "是否输出侧面带", true),
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
            string group = GetParamString(parameters, "group", "");
            float distance = GetParamFloat(parameters, "distance", 0.1f);
            bool outputInner = GetParamBool(parameters, "outputInner", true);
            bool outputSide = GetParamBool(parameters, "outputSide", true);

            if (geo.Primitives.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            var result = new PCGGeometry();
            // 复制所有原始顶点
            result.Points.AddRange(geo.Points);

            HashSet<int> primsToInset = new HashSet<int>();
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var grp))
                primsToInset = grp;
            else
                for (int i = 0; i < geo.Primitives.Count; i++) primsToInset.Add(i);

            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                if (!primsToInset.Contains(pi))
                {
                    result.Primitives.Add((int[])geo.Primitives[pi].Clone());
                    continue;
                }

                var prim = geo.Primitives[pi];
                if (prim.Length < 3) continue;

                // 计算面中心
                Vector3 center = Vector3.zero;
                for (int i = 0; i < prim.Length; i++)
                    center += geo.Points[prim[i]];
                center /= prim.Length;

                // 为每个顶点创建内缩后的新顶点
                int[] innerVerts = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                {
                    Vector3 orig = geo.Points[prim[i]];
                    Vector3 toCenter = center - orig;
                    float mag = toCenter.magnitude;
                    if (mag < 0.0001f)
                    {
                        innerVerts[i] = result.Points.Count;
                        result.Points.Add(orig);
                        continue;
                    }
                    float actualDist = Mathf.Min(distance, mag * 0.999f);
                    Vector3 newPos = orig + toCenter.normalized * actualDist;
                    innerVerts[i] = result.Points.Count;
                    result.Points.Add(newPos);
                }

                // 侧面带：外环 -> 内环 四边形
                if (outputSide)
                {
                    for (int i = 0; i < prim.Length; i++)
                    {
                        int next = (i + 1) % prim.Length;
                        result.Primitives.Add(new int[]
                        {
                            prim[i], prim[next],
                            innerVerts[next], innerVerts[i]
                        });
                    }
                }

                // 内缩面
                if (outputInner)
                {
                    result.Primitives.Add((int[])innerVerts.Clone());
                }
            }

            return SingleOutput("geometry", result);
        }
    }
}
