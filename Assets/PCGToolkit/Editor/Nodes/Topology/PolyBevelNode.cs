using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 多边形倒角（对标 Houdini PolyBevel SOP）
    /// 支持按 Group 选择内部边和边界边进行倒角
    /// </summary>
    public class PolyBevelNode : PCGNodeBase
    {
        public override string Name => "PolyBevel";
        public override string DisplayName => "Poly Bevel";
        public override string Description => "对多边形的边进行倒角，支持按 Group 选择";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Float,
                "Offset", "倒角偏移距离", 0.1f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "倒角分段数", 1),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定 PrimGroup 内的边倒角（留空=所有边）", ""),
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
            float offset = GetParamFloat(parameters, "offset", 0.1f);
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            string group = GetParamString(parameters, "group", "");

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            // 构建边 -> 相邻面的映射
            var edgeFaces = new Dictionary<(int, int), List<int>>();
            for (int fi = 0; fi < geo.Primitives.Count; fi++)
            {
                var prim = geo.Primitives[fi];
                for (int i = 0; i < prim.Length; i++)
                {
                    var ek = EdgeKey(prim[i], prim[(i + 1) % prim.Length]);
                    if (!edgeFaces.ContainsKey(ek))
                        edgeFaces[ek] = new List<int>();
                    edgeFaces[ek].Add(fi);
                }
            }

            // 确定需要倒角的面集合
            HashSet<int> groupPrims = null;
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var grp))
                groupPrims = grp;

            // 收集需要倒角的边：
            // 有 group -> 至少一侧面在 group 中的边
            // 无 group -> 所有共享边（内部边，2个面共享）
            var edgesToBevel = new HashSet<(int, int)>();
            foreach (var kvp in edgeFaces)
            {
                if (groupPrims != null)
                {
                    // 至少一侧在 group 中
                    if (kvp.Value.Any(fi => groupPrims.Contains(fi)))
                        edgesToBevel.Add(kvp.Key);
                }
                else
                {
                    // 默认倒角所有共享边（内部边）
                    if (kvp.Value.Count >= 2)
                        edgesToBevel.Add(kvp.Key);
                }
            }

            if (edgesToBevel.Count == 0)
            {
                ctx.Log("PolyBevel: 没有找到可倒角的边");
                return SingleOutput("geometry", geo.Clone());
            }

            // 对每个需要倒角的边，在它的两个端点处各生成一个新点（沿边方向偏移 offset）
            // 边 -> (newVert_near_v0, newVert_near_v1)
            var edgeNewVerts = new Dictionary<(int, int), (int, int)>();
            var newPoints = new List<Vector3>(geo.Points);

            foreach (var ek in edgesToBevel)
            {
                Vector3 p0 = geo.Points[ek.Item1];
                Vector3 p1 = geo.Points[ek.Item2];
                Vector3 dir = p1 - p0;
                float len = dir.magnitude;
                if (len < 0.00001f) continue;
                dir /= len;

                float clampedOffset = Mathf.Min(offset, len * 0.49f);

                int nv0 = newPoints.Count;
                newPoints.Add(p0 + dir * clampedOffset);
                int nv1 = newPoints.Count;
                newPoints.Add(p1 - dir * clampedOffset);

                edgeNewVerts[ek] = (nv0, nv1);
            }

            // 重建面：对每个面，用新点替换被倒角边的端点
            var newPrimitives = new List<int[]>();
            var bevelFaces = new List<int[]>();

            for (int fi = 0; fi < geo.Primitives.Count; fi++)
            {
                var prim = geo.Primitives[fi];
                var expanded = new List<int>();

                for (int i = 0; i < prim.Length; i++)
                {
                    int v = prim[i];
                    int vNext = prim[(i + 1) % prim.Length];
                    var ek = EdgeKey(v, vNext);

                    if (edgeNewVerts.TryGetValue(ek, out var nv))
                    {
                        // 这条边被倒角了，用新点替换
                        int nearV = ek.Item1 == v ? nv.Item1 : nv.Item2;
                        int nearVNext = ek.Item1 == vNext ? nv.Item1 : nv.Item2;
                        expanded.Add(nearV);
                        expanded.Add(nearVNext);
                    }
                    else
                    {
                        expanded.Add(v);
                    }
                }

                if (expanded.Count >= 3)
                    newPrimitives.Add(expanded.ToArray());
            }

            // 为每条倒角边添加倒角面（连接两个新点和原始两端点）
            foreach (var kvp in edgeNewVerts)
            {
                var ek = kvp.Key;
                int v0 = ek.Item1, v1 = ek.Item2;
                int nv0 = kvp.Value.Item1, nv1 = kvp.Value.Item2;

                if (divisions <= 1)
                {
                    // 单段倒角面
                    bevelFaces.Add(new int[] { v0, nv0, nv1, v1 });
                }
                else
                {
                    // 多段弧形倒角
                    Vector3 p0 = geo.Points[v0];
                    Vector3 pN0 = newPoints[nv0];
                    Vector3 pN1 = newPoints[nv1];
                    Vector3 p1 = geo.Points[v1];

                    int prevA = v0;
                    int prevB = v1;
                    for (int d = 1; d <= divisions; d++)
                    {
                        float t = (float)d / divisions;
                        int curA, curB;
                        if (d == divisions)
                        {
                            curA = nv0;
                            curB = nv1;
                        }
                        else
                        {
                            curA = newPoints.Count;
                            newPoints.Add(Vector3.Lerp(p0, pN0, t));
                            curB = newPoints.Count;
                            newPoints.Add(Vector3.Lerp(p1, pN1, t));
                        }
                        bevelFaces.Add(new int[] { prevA, curA, curB, prevB });
                        prevA = curA;
                        prevB = curB;
                    }
                }
            }

            newPrimitives.AddRange(bevelFaces);

            var result = new PCGGeometry();
            result.Points = newPoints;
            result.Primitives = newPrimitives;

            ctx.Log($"PolyBevel: beveled {edgesToBevel.Count} edges, output {result.Points.Count} pts, {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }

        private static (int, int) EdgeKey(int a, int b) => a < b ? (a, b) : (b, a);
    }
}