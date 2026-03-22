using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 填充孔洞（对标 Houdini PolyFill SOP）
    /// </summary>
    public class PolyFillNode : PCGNodeBase
    {
        public override string Name => "PolyFill";
        public override string DisplayName => "Poly Fill";
        public override string Description => "填充几何体中的孔洞";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("fillMode", PCGPortDirection.Input, PCGPortType.String,
                "Fill Mode", "填充模式（triangulate/fan/center）", "triangulate")
            {
                EnumOptions = new[] { "triangulate", "fan", "center" }
            },
            new PCGParamSchema("reverse", PCGPortDirection.Input, PCGPortType.Bool,
                "Reverse", "反转填充面法线", false),
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
                ctx.LogWarning("PolyFill: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            string fillMode = GetParamString(parameters, "fillMode", "triangulate").ToLower();
            bool reverse = GetParamBool(parameters, "reverse", false);

            // 找边界边环（孔洞）
            var boundaryLoops = FindBoundaryLoops(geo);

            if (boundaryLoops.Count == 0)
            {
                ctx.Log("PolyFill: 没有找到孔洞");
                return SingleOutput("geometry", geo);
            }

            var newPrimitives = new List<int[]>(geo.Primitives);

            foreach (var loop in boundaryLoops)
            {
                if (loop.Count < 3) continue;

                switch (fillMode)
                {
                    case "fan":
                        // 扇形填充：从中心点到每个边界点
                        var fanFaces = CreateFanFill(geo.Points, loop, reverse);
                        newPrimitives.AddRange(fanFaces);
                        break;

                    case "center":
                        // 单个多边形填充
                        var centerFace = CreateCenterFill(geo.Points, loop, reverse);
                        if (centerFace != null)
                            newPrimitives.Add(centerFace);
                        break;

                    default: // triangulate
                        // 三角化填充
                        var triangles = TriangulateLoop(geo.Points, loop, reverse);
                        newPrimitives.AddRange(triangles);
                        break;
                }
            }

            geo.Primitives = newPrimitives;

            ctx.Log($"PolyFill: filled {boundaryLoops.Count} holes, mode={fillMode}, output={newPrimitives.Count}faces");
            return SingleOutput("geometry", geo);
        }

        private List<List<int>> FindBoundaryLoops(PCGGeometry geo)
        {
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

            var boundaryEdges = new HashSet<(int, int)>();
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1) boundaryEdges.Add(kvp.Key);
            }

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

        private List<int[]> CreateFanFill(List<Vector3> points, List<int> loop, bool reverse)
        {
            var faces = new List<int[]>();

            // 计算环的中心点（在原有点中的索引或创建新点）
            // 简化版：使用第一个点作为锚点
            int anchor = loop[0];

            for (int i = 1; i < loop.Count - 1; i++)
            {
                int v0 = loop[i];
                int v1 = loop[i + 1];

                var face = reverse ? new int[] { anchor, v1, v0 } : new int[] { anchor, v0, v1 };
                faces.Add(face);
            }

            return faces;
        }

        private int[] CreateCenterFill(List<Vector3> points, List<int> loop, bool reverse)
        {
            var face = new int[loop.Count];
            for (int i = 0; i < loop.Count; i++)
            {
                face[i] = reverse ? loop[loop.Count - 1 - i] : loop[i];
            }
            return face;
        }

        private List<int[]> TriangulateLoop(List<Vector3> points, List<int> loop, bool reverse)
        {
            var triangles = new List<int[]>();

            // 简单的耳切法三角化
            var indices = new List<int>(loop);

            while (indices.Count > 3)
            {
                bool found = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = (i - 1 + indices.Count) % indices.Count;
                    int next = (i + 1) % indices.Count;

                    int i0 = indices[prev];
                    int i1 = indices[i];
                    int i2 = indices[next];

                    if (IsEar(points, indices, i))
                    {
                        if (reverse)
                            triangles.Add(new int[] { i0, i2, i1 });
                        else
                            triangles.Add(new int[] { i0, i1, i2 });

                        indices.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found) break; // 防止无限循环
            }

            // 最后剩余的三角形
            if (indices.Count == 3)
            {
                if (reverse)
                    triangles.Add(new int[] { indices[0], indices[2], indices[1] });
                else
                    triangles.Add(new int[] { indices[0], indices[1], indices[2] });
            }

            return triangles;
        }

        private bool IsEar(List<Vector3> points, List<int> indices, int earIndex)
        {
            int prev = (earIndex - 1 + indices.Count) % indices.Count;
            int next = (earIndex + 1) % indices.Count;

            Vector3 p0 = points[indices[prev]];
            Vector3 p1 = points[indices[earIndex]];
            Vector3 p2 = points[indices[next]];

            // 检查角度是否是凸的（内角 < 180度）
            Vector3 v1 = p1 - p0;
            Vector3 v2 = p2 - p1;

            // 简化：假设所有都是凸的（完整实现需要检查多边形方向和点是否在三角形内）
            float cross = Vector3.Cross(v1, v2).magnitude;
            return cross > 0.0001f;
        }
    }
}