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
            int origVertCount = geo.Points.Count;
            int origFaceCount = geo.Primitives.Count;

            // ---- Step 1: 构建邻接信息 ----

            // 边 key -> 相邻面索引列表
            var edgeFaces = new Dictionary<(int, int), List<int>>();
            // 顶点 -> 相邻面索引列表
            var vertFaces = new Dictionary<int, List<int>>();
            // 顶点 -> 相邻边 key 列表
            var vertEdges = new Dictionary<int, List<(int, int)>>();

            (int, int) EdgeKey(int a, int b) => a < b ? (a, b) : (b, a);

            for (int fi = 0; fi < origFaceCount; fi++)
            {
                var prim = geo.Primitives[fi];
                for (int i = 0; i < prim.Length; i++)
                {
                    int v = prim[i];
                    if (!vertFaces.ContainsKey(v))
                        vertFaces[v] = new List<int>();
                    vertFaces[v].Add(fi);

                    int next = prim[(i + 1) % prim.Length];
                    var ek = EdgeKey(v, next);
                    if (!edgeFaces.ContainsKey(ek))
                        edgeFaces[ek] = new List<int>();
                    edgeFaces[ek].Add(fi);

                    if (!vertEdges.ContainsKey(v))
                        vertEdges[v] = new List<(int, int)>();
                    if (!vertEdges[v].Contains(ek))
                        vertEdges[v].Add(ek);
                }
            }

            // ---- Step 2: 计算面点 (Face Points) ----
            // 每个面的所有顶点的平均值
            var facePoints = new Vector3[origFaceCount];
            for (int fi = 0; fi < origFaceCount; fi++)
            {
                var prim = geo.Primitives[fi];
                Vector3 sum = Vector3.zero;
                foreach (int v in prim)
                    sum += geo.Points[v];
                facePoints[fi] = sum / prim.Length;
            }

            // ---- Step 3: 计算边点 (Edge Points) ----
            // 内部边: (V1 + V2 + F1 + F2) / 4
            // 边界边: (V1 + V2) / 2
            var edgePoints = new Dictionary<(int, int), Vector3>();
            var boundaryEdges = new HashSet<(int, int)>();
            foreach (var kvp in edgeFaces)
            {
                var ek = kvp.Key;
                var faces = kvp.Value;
                Vector3 v1 = geo.Points[ek.Item1];
                Vector3 v2 = geo.Points[ek.Item2];

                if (faces.Count == 1)
                {
                    // 边界边
                    edgePoints[ek] = (v1 + v2) * 0.5f;
                    boundaryEdges.Add(ek);
                }
                else
                {
                    // 内部边: (V1 + V2 + F1 + F2) / 4
                    Vector3 f1 = facePoints[faces[0]];
                    Vector3 f2 = faces.Count >= 2 ? facePoints[faces[1]] : f1;
                    edgePoints[ek] = (v1 + v2 + f1 + f2) * 0.25f;
                }
            }

            // ---- Step 4: 计算新的顶点位置 (Vertex Points) ----
            // 内部顶点: (F + 2R + (n-3)P) / n
            //   F = 相邻面点的平均值
            //   R = 相邻边中点的平均值
            //   P = 原始位置
            //   n = 相邻面数（valence）
            // 边界顶点: (R + P) / 2，R = 相邻边界边中点的平均值
            var newVertPositions = new Vector3[origVertCount];
            var isBoundaryVert = new bool[origVertCount];

            for (int vi = 0; vi < origVertCount; vi++)
            {
                Vector3 P = geo.Points[vi];

                if (!vertFaces.ContainsKey(vi) || !vertEdges.ContainsKey(vi))
                {
                    newVertPositions[vi] = P;
                    continue;
                }

                // 检查是否为边界顶点
                var adjBoundary = new List<(int, int)>();
                foreach (var ek in vertEdges[vi])
                {
                    if (boundaryEdges.Contains(ek))
                        adjBoundary.Add(ek);
                }

                if (adjBoundary.Count >= 2)
                {
                    // 边界顶点
                    isBoundaryVert[vi] = true;
                    Vector3 R = Vector3.zero;
                    foreach (var ek in adjBoundary)
                        R += (geo.Points[ek.Item1] + geo.Points[ek.Item2]) * 0.5f;
                    R /= adjBoundary.Count;
                    newVertPositions[vi] = (R + P) * 0.5f;
                }
                else
                {
                    // 内部顶点
                    int n = vertFaces[vi].Count;
                    Vector3 F = Vector3.zero;
                    foreach (int fi in vertFaces[vi])
                        F += facePoints[fi];
                    F /= n;

                    Vector3 R = Vector3.zero;
                    foreach (var ek in vertEdges[vi])
                        R += (geo.Points[ek.Item1] + geo.Points[ek.Item2]) * 0.5f;
                    R /= vertEdges[vi].Count;

                    newVertPositions[vi] = (F + 2f * R + (n - 3f) * P) / n;
                }
            }

            // ---- Step 5: 组装结果 ----
            var result = new PCGGeometry();

            // 添加更新后的原始顶点 [0, origVertCount)
            for (int vi = 0; vi < origVertCount; vi++)
                result.Points.Add(newVertPositions[vi]);

            // 添加面点 [origVertCount, origVertCount + origFaceCount)
            int facePointBase = result.Points.Count;
            for (int fi = 0; fi < origFaceCount; fi++)
                result.Points.Add(facePoints[fi]);

            // 添加边点
            int edgePointBase = result.Points.Count;
            var edgePointIndex = new Dictionary<(int, int), int>();
            foreach (var kvp in edgePoints)
            {
                edgePointIndex[kvp.Key] = result.Points.Count;
                result.Points.Add(kvp.Value);
            }

            // 为每个原始面生成子面
            for (int fi = 0; fi < origFaceCount; fi++)
            {
                var prim = geo.Primitives[fi];
                int fpIdx = facePointBase + fi;

                for (int i = 0; i < prim.Length; i++)
                {
                    int v = prim[i];
                    int vNext = prim[(i + 1) % prim.Length];
                    int vPrev = prim[(i + prim.Length - 1) % prim.Length];

                    var ekNext = EdgeKey(v, vNext);
                    var ekPrev = EdgeKey(vPrev, v);

                    int epNext = edgePointIndex[ekNext];
                    int epPrev = edgePointIndex[ekPrev];

                    // 子四边形: [原始顶点, 下一边的边点, 面点, 前一边的边点]
                    result.Primitives.Add(new int[] { v, epNext, fpIdx, epPrev });
                }
            }

            return result;
        }
    }
}