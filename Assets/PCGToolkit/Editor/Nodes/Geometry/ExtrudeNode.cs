using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 挤出面（对标 Houdini PolyExtrude SOP）
    /// </summary>
    public class ExtrudeNode : PCGNodeBase
    {
        public override string Name => "Extrude";
        public override string DisplayName => "Extrude";
        public override string Description => "沿法线方向挤出几何体的面";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要挤出的面分组（留空=全部面）", ""),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "挤出距离", 0.5f),
            new PCGParamSchema("inset", PCGPortDirection.Input, PCGPortType.Float,
                "Inset", "内缩距离", 0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "挤出方向的分段数", 1),
            new PCGParamSchema("outputFront", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Front", "是否输出顶面", true),
            new PCGParamSchema("outputSide", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Side", "是否输出侧面", true),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");
            float distance = GetParamFloat(parameters, "distance", 0.5f);
            float inset = GetParamFloat(parameters, "inset", 0f);
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            bool outputFront = GetParamBool(parameters, "outputFront", true);
            bool outputSide = GetParamBool(parameters, "outputSide", true);

            if (geo.Primitives.Count == 0)
            {
                ctx.LogWarning("Extrude: 输入几何体没有面");
                return SingleOutput("geometry", geo);
            }

            var result = new PCGGeometry();

            // 第一步：复制所有原始顶点到 result（保持 1:1 索引映射）
            // 这样非挤出面的原始索引在 result 中仍然有效
            for (int i = 0; i < geo.Points.Count; i++)
            {
                result.Points.Add(geo.Points[i]);
            }

            // 确定要挤出的面
            HashSet<int> primsToExtrude = new HashSet<int>();
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var groupPrims))
            {
                primsToExtrude = groupPrims;
            }
            else
            {
                for (int i = 0; i < geo.Primitives.Count; i++)
                    primsToExtrude.Add(i);
            }

            // 第二步：添加未挤出的原始面（索引仍然有效，因为原始顶点已 1:1 复制到 result）
            for (int i = 0; i < geo.Primitives.Count; i++)
            {
                if (!primsToExtrude.Contains(i))
                {
                    result.Primitives.Add((int[])geo.Primitives[i].Clone());
                }
            }

            // 第三步：处理挤出面
            foreach (int primIdx in primsToExtrude)
            {
                var prim = geo.Primitives[primIdx];
                if (prim.Length < 3) continue;

                Vector3 normal = CalculateFaceNormal(geo.Points, prim);

                Vector3 center = Vector3.zero;
                foreach (int idx in prim) center += geo.Points[idx];
                center /= prim.Length;

                // 第一层使用原始顶点索引（已在 result.Points 中，索引 0 ~ geo.Points.Count-1）
                int[] prevLayerVertices = (int[])prim.Clone();

                for (int d = 1; d <= divisions; d++)
                {
                    float t = (float)d / divisions;
                    float offset = distance * t;
                    float insetAmount = inset * t;

                    int[] layerVertices = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                    {
                        Vector3 origPos = geo.Points[prim[i]];
                        Vector3 toCenter = (center - origPos);
                        float toCenterMag = toCenter.magnitude;
                        Vector3 toCenterDir = toCenterMag > 0.0001f ? toCenter / toCenterMag : Vector3.zero;
                        Vector3 newPos = origPos + normal * offset + toCenterDir * insetAmount;

                        int newIdx = result.Points.Count;
                        result.Points.Add(newPos);
                        layerVertices[i] = newIdx;
                    }

                    // 创建侧面
                    if (outputSide)
                    {
                        for (int i = 0; i < prim.Length; i++)
                        {
                            int next = (i + 1) % prim.Length;
                            result.Primitives.Add(new int[]
                            {
                                prevLayerVertices[i], prevLayerVertices[next],
                                layerVertices[next], layerVertices[i]
                            });
                        }
                    }

                    prevLayerVertices = layerVertices;
                }

                // 输出顶面
                if (outputFront)
                {
                    // 使用最后一层的顶点，反转绕序使法线朝外
                    int[] frontPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                    {
                        frontPrim[i] = prevLayerVertices[i];
                    }
                    result.Primitives.Add(frontPrim);
                }
            }

            return SingleOutput("geometry", result);
        }

        private Vector3 CalculateFaceNormal(List<Vector3> points, int[] prim)
        {
            if (prim.Length < 3) return Vector3.up;
            Vector3 v0 = points[prim[0]];
            Vector3 v1 = points[prim[1]];
            Vector3 v2 = points[prim[2]];
            return Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }
    }
}