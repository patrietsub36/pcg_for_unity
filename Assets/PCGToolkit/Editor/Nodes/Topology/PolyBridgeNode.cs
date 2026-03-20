using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 桥接面（对标 Houdini PolyBridge SOP）
    /// </summary>
    public class PolyBridgeNode : PCGNodeBase
    {
        public override string Name => "PolyBridge";
        public override string DisplayName => "Poly Bridge";
        public override string Description => "在两个边界边环之间创建桥接面";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "桥接分段数", 1),
            new PCGParamSchema("twist", PCGPortDirection.Input, PCGPortType.Float,
                "Twist", "扭转角度", 0f),
            new PCGParamSchema("taper", PCGPortDirection.Input, PCGPortType.Float,
                "Taper", "锥度（0~1）", 1.0f),
            new PCGParamSchema("reverse", PCGPortDirection.Input, PCGPortType.Bool,
                "Reverse", "反转连接方向", false),
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

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
            {
                ctx.LogWarning("PolyBridge: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            float twist = GetParamFloat(parameters, "twist", 0f);
            float taper = GetParamFloat(parameters, "taper", 1.0f);
            bool reverse = GetParamBool(parameters, "reverse", false);

            // 找到边界边环
            var boundaryLoops = FindBoundaryLoops(geo);

            if (boundaryLoops.Count < 2)
            {
                ctx.LogWarning("PolyBridge: 需要至少两个边界环");
                return SingleOutput("geometry", geo);
            }

            // 取前两个边界环进行桥接
            var loop0 = boundaryLoops[0];
            var loop1 = boundaryLoops[1];

            // 确保两个环点数相同（或可对齐）
            if (loop0.Count != loop1.Count)
            {
                // 重采样使其点数相同
                int targetCount = Mathf.Max(loop0.Count, loop1.Count);
                loop0 = ResampleLoop(geo.Points, loop0, targetCount);
                loop1 = ResampleLoop(geo.Points, loop1, targetCount);
            }

            int pointCount = loop0.Count;
            if (pointCount < 3)
            {
                ctx.LogWarning("PolyBridge: 边界环点数不足");
                return SingleOutput("geometry", geo);
            }

            var newPoints = new List<Vector3>(geo.Points);
            var newPrimitives = new List<int[]>(geo.Primitives);

            // 计算两个环的中心
            Vector3 center0 = Vector3.zero;
            Vector3 center1 = Vector3.zero;
            foreach (int idx in loop0) center0 += geo.Points[idx];
            foreach (int idx in loop1) center1 += geo.Points[idx];
            center0 /= pointCount;
            center1 /= pointCount;

            // 对齐环的方向（使相对的点尽可能接近）
            float minDist = float.MaxValue;
            int bestOffset = 0;
            for (int offset = 0; offset < pointCount; offset++)
            {
                float totalDist = 0f;
                for (int i = 0; i < pointCount; i++)
                {
                    int j = (i + offset) % pointCount;
                    if (reverse) j = (pointCount - j) % pointCount;
                    totalDist += Vector3.Distance(geo.Points[loop0[i]], geo.Points[loop1[j]]);
                }
                if (totalDist < minDist)
                {
                    minDist = totalDist;
                    bestOffset = offset;
                }
            }

            // 生成桥接面
            for (int seg = 0; seg < pointCount; seg++)
            {
                int i0 = seg;
                int i1 = (seg + 1) % pointCount;
                int j0 = (seg + bestOffset) % pointCount;
                int j1 = (seg + bestOffset + 1) % pointCount;

                if (reverse)
                {
                    j0 = (pointCount - j0) % pointCount;
                    j1 = (pointCount - j1) % pointCount;
                }

                int v0 = loop0[i0];
                int v1 = loop0[i1];
                int v2 = loop1[j1];
                int v3 = loop1[j0];

                if (divisions == 1)
                {
                    // 简单四边形
                    newPrimitives.Add(new int[] { v0, v1, v2, v3 });
                }
                else
                {
                    // 多段桥接
                    var interpPoints = new List<int>();
                    interpPoints.Add(v0);

                    for (int d = 1; d < divisions; d++)
                    {
                        float t = (float)d / divisions;
                        Vector3 p0 = geo.Points[v0];
                        Vector3 p1 = geo.Points[v3];

                        // 应用锥度
                        float localTaper = Mathf.Lerp(1f, taper, t);
                        Vector3 midPoint = Vector3.Lerp(p0, p1, t);

                        // 应用扭转
                        float angle = twist * t;
                        Vector3 toCenter = midPoint - Vector3.Lerp(center0, center1, t);
                        midPoint = Vector3.Lerp(center0, center1, t) + Quaternion.Euler(0, angle, 0) * toCenter * localTaper;

                        int newIdx = newPoints.Count;
                        newPoints.Add(midPoint);
                        interpPoints.Add(newIdx);
                    }

                    interpPoints.Add(v3);

                    // 类似地为另一边
                    var interpPoints1 = new List<int>();
                    interpPoints1.Add(v1);

                    for (int d = 1; d < divisions; d++)
                    {
                        float t = (float)d / divisions;
                        Vector3 p0 = geo.Points[v1];
                        Vector3 p1 = geo.Points[v2];

                        float localTaper = Mathf.Lerp(1f, taper, t);
                        Vector3 midPoint = Vector3.Lerp(p0, p1, t);

                        float angle = twist * t;
                        Vector3 toCenter = midPoint - Vector3.Lerp(center0, center1, t);
                        midPoint = Vector3.Lerp(center0, center1, t) + Quaternion.Euler(0, angle, 0) * toCenter * localTaper;

                        int newIdx = newPoints.Count;
                        newPoints.Add(midPoint);
                        interpPoints1.Add(newIdx);
                    }

                    interpPoints1.Add(v2);

                    // 生成面
                    for (int d = 0; d < divisions; d++)
                    {
                        newPrimitives.Add(new int[] {
                            interpPoints[d], interpPoints1[d],
                            interpPoints1[d + 1], interpPoints[d + 1]
                        });
                    }
                }
            }

            geo.Points = newPoints;
            geo.Primitives = newPrimitives;

            ctx.Log($"PolyBridge: bridged {pointCount} points, divisions={divisions}, output={newPoints.Count}pts");
            return SingleOutput("geometry", geo);
        }

        private List<List<int>> FindBoundaryLoops(PCGGeometry geo)
        {
            // 找边界边
            var edgeCount = new Dictionary<(int, int), int>();
            foreach (var prim in geo.Primitives)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int v0 = prim[i];
                    int v1 = prim[(i + 1) % prim.Length];
                    var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                    if (!edgeCount.ContainsKey(edge)) edgeCount[edge] = 0;
                    edgeCount[edge]++;
                }
            }

            // 边界边只出现一次
            var boundaryEdges = new HashSet<(int, int)>();
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1) boundaryEdges.Add(kvp.Key);
            }

            // 组装成环
            var loops = new List<List<int>>();
            var used = new HashSet<(int, int)>();

            foreach (var edge in boundaryEdges)
            {
                if (used.Contains(edge)) continue;

                var loop = new List<int>();
                int current = edge.Item1;
                loop.Add(current);

                while (true)
                {
                    int next = -1;
                    foreach (var e in boundaryEdges)
                    {
                        if (used.Contains(e)) continue;
                        if (e.Item1 == current || e.Item2 == current)
                        {
                            next = e.Item1 == current ? e.Item2 : e.Item1;
                            used.Add(e);
                            break;
                        }
                    }

                    if (next == -1 || next == edge.Item1) break;
                    loop.Add(next);
                    current = next;
                }

                if (loop.Count >= 3)
                    loops.Add(loop);
            }

            return loops;
        }

        private List<int> ResampleLoop(List<Vector3> points, List<int> loop, int targetCount)
        {
            if (loop.Count == targetCount) return loop;

            var result = new List<int>();

            // 简单实现：均匀采样原始点索引
            for (int i = 0; i < targetCount; i++)
            {
                float t = (float)i / targetCount;
                int idx = Mathf.FloorToInt(t * loop.Count);
                result.Add(loop[idx % loop.Count]);
            }

            return result;
        }
    }
}