using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 重新网格化（对标 Houdini Remesh SOP）
    /// 使用简单的迭代边缘翻转和顶点平滑
    /// </summary>
    public class RemeshNode : PCGNodeBase
    {
        public override string Name => "Remesh";
        public override string DisplayName => "Remesh";
        public override string Description => "重新生成均匀的三角形网格";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("targetEdgeLength", PCGPortDirection.Input, PCGPortType.Float,
                "Target Edge Length", "目标边长", 0.5f),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "迭代次数", 3),
            new PCGParamSchema("smoothing", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothing", "平滑系数", 0.5f),
            new PCGParamSchema("preserveBoundary", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Boundary", "保持边界不变", true),
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
                ctx.LogWarning("Remesh: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float targetLength = GetParamFloat(parameters, "targetEdgeLength", 0.5f);
            int iterations = GetParamInt(parameters, "iterations", 3);
            float smoothing = GetParamFloat(parameters, "smoothing", 0.5f);
            bool preserveBoundary = GetParamBool(parameters, "preserveBoundary", true);

            // 确保所有面都是三角形
            var triangles = new List<int[]>();
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 3)
                {
                    triangles.Add(prim);
                }
                else
                {
                    // 简单扇形三角化
                    for (int i = 1; i < prim.Length - 1; i++)
                    {
                        triangles.Add(new int[] { prim[0], prim[i], prim[i + 1] });
                    }
                }
            }
            geo.Primitives = triangles;

            // 找到边界顶点
            var boundaryVertices = new HashSet<int>();
            if (preserveBoundary)
            {
                var edgeCount = new Dictionary<(int, int), int>();
                foreach (var tri in triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int v0 = tri[i];
                        int v1 = tri[(i + 1) % 3];
                        var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                        if (!edgeCount.ContainsKey(edge)) edgeCount[edge] = 0;
                        edgeCount[edge]++;
                    }
                }

                foreach (var kvp in edgeCount)
                {
                    if (kvp.Value == 1)
                    {
                        boundaryVertices.Add(kvp.Key.Item1);
                        boundaryVertices.Add(kvp.Key.Item2);
                    }
                }
            }

            // 迭代处理
            for (int iter = 0; iter < iterations; iter++)
            {
                // 1. 边分割（边太长）
                var newPoints = new List<Vector3>(geo.Points);
                var newTriangles = new List<int[]>();

                foreach (var tri in geo.Primitives)
                {
                    var newTris = ProcessTriangle(tri, geo.Points, newPoints, targetLength);
                    newTriangles.AddRange(newTris);
                }

                geo.Points = newPoints;
                geo.Primitives = newTriangles;

                // 2. 边翻转（改善三角形质量）
                for (int flipIter = 0; flipIter < 3; flipIter++)
                {
                    FlipEdges(geo);
                }

                // 3. 顶点平滑
                SmoothVertices(geo, smoothing, boundaryVertices);
            }

            ctx.Log($"Remesh: targetLength={targetLength}, iterations={iterations}, output={geo.Points.Count}pts, {geo.Primitives.Count}tris");
            return SingleOutput("geometry", geo);
        }

        private List<int[]> ProcessTriangle(int[] tri, List<Vector3> points, List<Vector3> newPoints, float targetLength)
        {
            // 检查是否需要分割
            float maxEdge = 0f;
            int maxEdgeIdx = -1;

            for (int i = 0; i < 3; i++)
            {
                float len = Vector3.Distance(points[tri[i]], points[tri[(i + 1) % 3]]);
                if (len > maxEdge)
                {
                    maxEdge = len;
                    maxEdgeIdx = i;
                }
            }

            if (maxEdge > targetLength * 1.5f)
            {
                // 分割最长的边
                int v0 = tri[maxEdgeIdx];
                int v1 = tri[(maxEdgeIdx + 1) % 3];
                int v2 = tri[(maxEdgeIdx + 2) % 3];

                Vector3 midPoint = (points[v0] + points[v1]) * 0.5f;
                int midIdx = newPoints.Count;
                newPoints.Add(midPoint);

                return new List<int[]>
                {
                    new int[] { v0, midIdx, v2 },
                    new int[] { midIdx, v1, v2 }
                };
            }

            return new List<int[]> { tri };
        }

        private void FlipEdges(PCGGeometry geo)
        {
            // 构建边到面的映射
            var edgeTris = new Dictionary<(int, int), List<int>>();
            for (int triIdx = 0; triIdx < geo.Primitives.Count; triIdx++)
            {
                var tri = geo.Primitives[triIdx];
                for (int i = 0; i < 3; i++)
                {
                    int v0 = tri[i];
                    int v1 = tri[(i + 1) % 3];
                    var edge = v0 < v1 ? (v0, v1) : (v1, v0);
                    if (!edgeTris.ContainsKey(edge)) edgeTris[edge] = new List<int>();
                    edgeTris[edge].Add(triIdx);
                }
            }

            // 检查每个边是否需要翻转
            foreach (var kvp in edgeTris)
            {
                if (kvp.Value.Count != 2) continue;

                var tri0 = geo.Primitives[kvp.Value[0]];
                var tri1 = geo.Primitives[kvp.Value[1]];

                // 找到共享边和相对顶点
                var shared = FindSharedEdge(tri0, tri1);
                if (shared == null) continue;

                int a = shared.Value.a;
                int b = shared.Value.b;
                int c = shared.Value.c; // tri0 的相对顶点
                int d = shared.Value.d; // tri1 的相对顶点

                // 计算翻转前后的最小角度
                float minAngleBefore = MinAngle(geo.Points[a], geo.Points[b], geo.Points[c], geo.Points[d]);
                float minAngleAfter = MinAngle(geo.Points[c], geo.Points[d], geo.Points[a], geo.Points[b]);

                if (minAngleAfter > minAngleBefore)
                {
                    // 翻转边
                    geo.Primitives[kvp.Value[0]] = new int[] { a, c, d };
                    geo.Primitives[kvp.Value[1]] = new int[] { b, d, c };
                }
            }
        }

        private (int a, int b, int c, int d)? FindSharedEdge(int[] tri0, int[] tri1)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (tri0[i] == tri1[(j + 1) % 3] && tri0[(i + 1) % 3] == tri1[j])
                    {
                        return (tri0[i], tri0[(i + 1) % 3], tri0[(i + 2) % 3], tri1[(j + 2) % 3]);
                    }
                }
            }
            return null;
        }

        private float MinAngle(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float angle1 = Vector3.Angle(b - a, c - a);
            float angle2 = Vector3.Angle(a - b, c - b);
            float angle3 = Vector3.Angle(a - b, d - b);
            float angle4 = Vector3.Angle(b - a, d - a);
            return Mathf.Min(angle1, angle2, angle3, angle4);
        }

        private void SmoothVertices(PCGGeometry geo, float strength, HashSet<int> boundaryVertices)
        {
            // 构建邻接关系
            var neighbors = new Dictionary<int, HashSet<int>>();
            foreach (var tri in geo.Primitives)
            {
                for (int i = 0; i < 3; i++)
                {
                    int v0 = tri[i];
                    int v1 = tri[(i + 1) % 3];
                    int v2 = tri[(i + 2) % 3];

                    if (!neighbors.ContainsKey(v0)) neighbors[v0] = new HashSet<int>();
                    neighbors[v0].Add(v1);
                    neighbors[v0].Add(v2);
                }
            }

            var newPoints = new List<Vector3>(geo.Points);

            foreach (var kvp in neighbors)
            {
                int v = kvp.Key;

                // 跳过边界顶点
                if (boundaryVertices.Contains(v)) continue;

                Vector3 centroid = Vector3.zero;
                foreach (int n in kvp.Value)
                    centroid += geo.Points[n];
                centroid /= kvp.Value.Count;

                newPoints[v] = Vector3.Lerp(geo.Points[v], centroid, strength);
            }

            geo.Points = newPoints;
        }
    }
}