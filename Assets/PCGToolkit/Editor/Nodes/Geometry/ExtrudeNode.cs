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
            new PCGParamSchema("individual", PCGPortDirection.Input, PCGPortType.Bool,
                "Individual", "是否独立挤出每个面（避免共享顶点拉扯）", false),
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
            bool individual = GetParamBool(parameters, "individual", false);

            if (geo.Primitives.Count == 0)
            {
                ctx.LogWarning("Extrude: 输入几何体没有面");
                return SingleOutput("geometry", geo);
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

            // individual 模式：先对面进行独立化处理
            if (individual)
            {
                return ExecuteIndividual(geo, primsToExtrude, distance, inset, divisions, outputFront, outputSide, ctx);
            }

            // 正常模式
            return ExecuteNormal(geo, primsToExtrude, distance, inset, divisions, outputFront, outputSide);
        }

        /// <summary>
        /// 正常挤出模式（共享顶点）
        /// </summary>
        private Dictionary<string, PCGGeometry> ExecuteNormal(
            PCGGeometry geo, HashSet<int> primsToExtrude,
            float distance, float inset, int divisions,
            bool outputFront, bool outputSide)
        {
            var result = new PCGGeometry();

            // 第一步：复制所有原始顶点到 result（保持 1:1 索引映射）
            for (int i = 0; i < geo.Points.Count; i++)
            {
                result.Points.Add(geo.Points[i]);
            }

            // 第二步：添加未挤出的原始面
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

                if (outputFront)
                {
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

        /// <summary>
        /// 独立挤出模式（每个面独立化后挤出，避免共享顶点拉扯）
        /// </summary>
        private Dictionary<string, PCGGeometry> ExecuteIndividual(
            PCGGeometry geo, HashSet<int> primsToExtrude,
            float distance, float inset, int divisions,
            bool outputFront, bool outputSide, PCGContext ctx)
        {
            var result = new PCGGeometry();

            // 复制所有原始顶点到 result（保持 1:1 索引映射）
            for (int i = 0; i < geo.Points.Count; i++)
                result.Points.Add(geo.Points[i]);

            // 添加未挤出的面（直接使用原始索引，无需重映射）
            for (int i = 0; i < geo.Primitives.Count; i++)
            {
                if (!primsToExtrude.Contains(i))
                    result.Primitives.Add((int[])geo.Primitives[i].Clone());
            }

            // 对每个挤出面创建独立的顶点副本并挤出
            foreach (int primIdx in primsToExtrude)
            {
                var prim = geo.Primitives[primIdx];
                if (prim.Length < 3) continue;

                Vector3 normal = CalculateFaceNormal(geo.Points, prim);

                Vector3 center = Vector3.zero;
                foreach (int idx in prim) center += geo.Points[idx];
                center /= prim.Length;

                // 为这个面创建独立的顶点副本
                int[] baseVertices = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                {
                    baseVertices[i] = result.Points.Count;
                    result.Points.Add(geo.Points[prim[i]]);
                }

                int[] prevLayerVertices = baseVertices;

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

                if (outputFront)
                {
                    int[] frontPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        frontPrim[i] = prevLayerVertices[i];
                    result.Primitives.Add(frontPrim);
                }
            }

            ctx.Log($"Extrude Individual: 挤出了 {primsToExtrude.Count} 个独立面");
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