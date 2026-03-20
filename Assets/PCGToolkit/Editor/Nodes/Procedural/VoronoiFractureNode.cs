using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// Voronoi 碎裂（对标 Houdini Voronoi Fracture SOP）
    /// </summary>
    public class VoronoiFractureNode : PCGNodeBase
    {
        public override string Name => "VoronoiFracture";
        public override string DisplayName => "Voronoi Fracture";
        public override string Description => "使用 Voronoi 图对几何体进行碎裂分割";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "要碎裂的几何体", null, required: true),
            new PCGParamSchema("points", PCGPortDirection.Input, PCGPortType.Geometry,
                "Scatter Points", "Voronoi 种子点（可选，留空则自动散布）", null),
            new PCGParamSchema("numPoints", PCGPortDirection.Input, PCGPortType.Int,
                "Num Points", "自动散布的种子点数（无 points 输入时使用）", 20),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("createInterior", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Interior", "是否生成内部截面", true),
            new PCGParamSchema("interiorGroup", PCGPortDirection.Input, PCGPortType.String,
                "Interior Group", "内部截面的分组名", "inside"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "碎裂后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var inputGeo = GetInputGeometry(inputGeometries, "input");

            if (inputGeo.Points.Count == 0)
            {
                ctx.LogWarning("VoronoiFracture: 输入几何体为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            var pointsGeo = GetInputGeometry(inputGeometries, "points");
            int numPoints = GetParamInt(parameters, "numPoints", 20);
            int seed = GetParamInt(parameters, "seed", 0);
            bool createInterior = GetParamBool(parameters, "createInterior", true);
            string interiorGroup = GetParamString(parameters, "interiorGroup", "inside");

            var rng = new System.Random(seed);

            // 计算输入几何体的包围盒
            Vector3 min = inputGeo.Points[0];
            Vector3 max = inputGeo.Points[0];
            foreach (var p in inputGeo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            // 生成或使用种子点
            var seedPoints = new List<Vector3>();
            if (pointsGeo != null && pointsGeo.Points.Count > 0)
            {
                seedPoints.AddRange(pointsGeo.Points);
            }
            else
            {
                // 在包围盒内随机生成种子点
                for (int i = 0; i < numPoints; i++)
                {
                    float x = min.x + (float)rng.NextDouble() * size.x;
                    float y = min.y + (float)rng.NextDouble() * size.y;
                    float z = min.z + (float)rng.NextDouble() * size.z;
                    seedPoints.Add(new Vector3(x, y, z));
                }
            }

            // 为每个 Voronoi 单元生成碎片几何体
            var geo = new PCGGeometry();
            var allPoints = new List<Vector3>();
            var allPrimitives = new List<int[]>();

            for (int i = 0; i < seedPoints.Count; i++)
            {
                Vector3 seedPoint = seedPoints[i];

                // 计算这个种子点的 Voronoi 单元
                // 简化实现：生成一个基于到其他种子点距离的凸多面体
                var cellVertices = ComputeVoronoiCell(seedPoint, seedPoints, min, max, i);

                if (cellVertices.Count < 4) continue;

                // 创建碎片（凸包三角化）
                int baseIdx = allPoints.Count;
                allPoints.AddRange(cellVertices);

                // 简单扇形三角化（从中心点）
                Vector3 cellCenter = Vector3.zero;
                foreach (var v in cellVertices) cellCenter += v;
                cellCenter /= cellVertices.Count;

                // 添加中心点
                int centerIdx = allPoints.Count;
                allPoints.Add(cellCenter);

                // 计算凸包面（简化：遍历所有三角形）
                for (int j = 0; j < cellVertices.Count; j++)
                {
                    int j1 = baseIdx + j;
                    int j2 = baseIdx + (j + 1) % cellVertices.Count;

                    // 检查三角形是否面向外部
                    Vector3 v0 = allPoints[j1];
                    Vector3 v1 = allPoints[j2];
                    Vector3 v2 = cellCenter;

                    Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);

                    // 只添加面向种子点的三角形
                    if (Vector3.Dot(faceNormal, seedPoint - cellCenter) > 0)
                    {
                        allPrimitives.Add(new int[] { j1, j2, centerIdx });
                    }
                    else
                    {
                        allPrimitives.Add(new int[] { j2, j1, centerIdx });
                    }
                }

                // 创建碎片分组
                string groupName = $"piece_{i}";
                if (!geo.PrimGroups.ContainsKey(groupName))
                    geo.PrimGroups[groupName] = new HashSet<int>();

                int primStart = allPrimitives.Count - cellVertices.Count;
                for (int p = primStart; p < allPrimitives.Count; p++)
                {
                    geo.PrimGroups[groupName].Add(p);
                }
            }

            geo.Points = allPoints;
            geo.Primitives = allPrimitives;

            ctx.Log($"VoronoiFracture: seeds={seedPoints.Count}, pieces={seedPoints.Count}, output={allPoints.Count}pts, {allPrimitives.Count}faces");
            return SingleOutput("geometry", geo);
        }

        private List<Vector3> ComputeVoronoiCell(Vector3 seed, List<Vector3> allSeeds, Vector3 min, Vector3 max, int seedIndex)
        {
            // 简化的 Voronoi 单元计算
            // 通过裁剪包围盒的每个面来得到单元顶点

            var vertices = new List<Vector3>();

            // 从包围盒的 8 个角点开始
            vertices.Add(new Vector3(min.x, min.y, min.z));
            vertices.Add(new Vector3(max.x, min.y, min.z));
            vertices.Add(new Vector3(max.x, max.y, min.z));
            vertices.Add(new Vector3(min.x, max.y, min.z));
            vertices.Add(new Vector3(min.x, min.y, max.z));
            vertices.Add(new Vector3(max.x, min.y, max.z));
            vertices.Add(new Vector3(max.x, max.y, max.z));
            vertices.Add(new Vector3(min.x, max.y, max.z));

            // 用每个相邻种子点的中垂面裁剪
            foreach (var other in allSeeds)
            {
                if (other == seed) continue;

                // 中垂面
                Vector3 midpoint = (seed + other) * 0.5f;
                Vector3 normal = (other - seed).normalized;

                // 裁剪顶点列表
                var newVertices = new List<Vector3>();

                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 v0 = vertices[i];
                    Vector3 v1 = vertices[(i + 1) % vertices.Count];

                    float d0 = Vector3.Dot(v0 - midpoint, normal);
                    float d1 = Vector3.Dot(v1 - midpoint, normal);

                    if (d0 <= 0) newVertices.Add(v0);

                    if ((d0 > 0 && d1 < 0) || (d0 < 0 && d1 > 0))
                    {
                        // 交点
                        float t = d0 / (d0 - d1);
                        Vector3 intersection = v0 + t * (v1 - v0);
                        newVertices.Add(intersection);
                    }
                }

                vertices = newVertices;
                if (vertices.Count < 4) break;
            }

            return vertices;
        }
    }
}