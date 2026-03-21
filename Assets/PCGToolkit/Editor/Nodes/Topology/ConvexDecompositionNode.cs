using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;
using MIConvexHull;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 凸分解（对标碰撞体生成需求）
    /// 使用 MIConvexHull 库计算真实凸包，
    /// 按连通分量拆分后对每个分量独立计算凸包，每个凸包作为独立面组输出。
    /// </summary>
    public class ConvexDecompositionNode : PCGNodeBase
    {
        public override string Name => "ConvexDecomposition";
        public override string DisplayName => "Convex Decomposition";
        public override string Description => "将网格分解为多个凸包";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("maxHulls", PCGPortDirection.Input, PCGPortType.Int,
                "Max Hulls", "最大凸包数量", 16),
            new PCGParamSchema("maxVerticesPerHull", PCGPortDirection.Input, PCGPortType.Int,
                "Max Vertices Per Hull", "每个凸包最大顶点数（0=无限制）", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "分解后的几何体（每个凸包为独立面组）"),
        };

        private class Vertex3 : IVertex
        {
            public double[] Position { get; set; }
            public int OriginalIndex;

            public Vertex3(Vector3 v, int idx)
            {
                Position = new double[] { v.x, v.y, v.z };
                OriginalIndex = idx;
            }
        }

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            int maxHulls = GetParamInt(parameters, "maxHulls", 16);
            int maxVertsPerHull = GetParamInt(parameters, "maxVerticesPerHull", 0);

            if (geo.Points.Count < 4)
            {
                ctx.LogWarning("ConvexDecomposition: 输入点数不足");
                return SingleOutput("geometry", geo.Clone());
            }

            // 按连通分量拆分
            var pieces = SplitByConnectivity(geo);
            if (pieces.Count > maxHulls)
                pieces = pieces.Take(maxHulls).ToList();

            var result = new PCGGeometry();
            int hullIdx = 0;

            foreach (var piece in pieces)
            {
                if (piece.Count < 4) continue;

                var vertices = piece.Select((p, i) => new Vertex3(p, i)).ToList();

                var hullResult = ConvexHull.Create<Vertex3>(vertices);
                if (hullResult.Result == null)
                {
                    ctx.LogWarning($"ConvexDecomposition: 凸包计算失败 (piece {hullIdx})");
                    continue;
                }

                var hullPoints = hullResult.Result.Points.ToList();
                var hullFaces = hullResult.Result.Faces.ToList();

                if (maxVertsPerHull > 0 && hullPoints.Count > maxVertsPerHull)
                    hullPoints = hullPoints.Take(maxVertsPerHull).ToList();

                // 建立顶点映射
                int baseIdx = result.Points.Count;
                var vertMap = new Dictionary<Vertex3, int>();
                foreach (var v in hullPoints)
                {
                    vertMap[v] = result.Points.Count;
                    result.Points.Add(new Vector3(
                        (float)v.Position[0],
                        (float)v.Position[1],
                        (float)v.Position[2]));
                }

                // 添加面
                string groupName = $"hull_{hullIdx}";
                result.PrimGroups[groupName] = new HashSet<int>();

                foreach (var face in hullFaces)
                {
                    var faceVerts = face.Vertices;
                    if (faceVerts.Length < 3) continue;

                    // 检查所有顶点是否在映射中
                    bool valid = true;
                    var triIndices = new int[faceVerts.Length];
                    for (int i = 0; i < faceVerts.Length; i++)
                    {
                        if (!vertMap.TryGetValue(faceVerts[i], out int idx))
                        {
                            valid = false;
                            break;
                        }
                        triIndices[i] = idx;
                    }

                    if (valid)
                    {
                        int primIdx = result.Primitives.Count;
                        result.Primitives.Add(triIndices);
                        result.PrimGroups[groupName].Add(primIdx);
                    }
                }

                hullIdx++;
            }

            ctx.Log($"ConvexDecomposition: {pieces.Count} pieces -> {hullIdx} convex hulls, {result.Points.Count} pts, {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }

        private List<List<Vector3>> SplitByConnectivity(PCGGeometry geo)
        {
            int n = geo.Points.Count;
            int[] parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                a = Find(a); b = Find(b);
                if (a != b) parent[a] = b;
            }

            foreach (var prim in geo.Primitives)
                for (int i = 1; i < prim.Length; i++)
                    Union(prim[0], prim[i]);

            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int root = Find(i);
                if (!groups.ContainsKey(root))
                    groups[root] = new List<int>();
                groups[root].Add(i);
            }

            var result = new List<List<Vector3>>();
            foreach (var kvp in groups)
            {
                var points = kvp.Value.Select(i => geo.Points[i]).ToList();
                if (points.Count >= 4)
                    result.Add(points);
            }

            if (result.Count == 0)
                result.Add(new List<Vector3>(geo.Points));

            return result;
        }
    }
}