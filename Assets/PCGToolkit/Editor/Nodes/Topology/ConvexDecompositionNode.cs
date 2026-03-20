using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 凸分解（Convex Decomposition）
    /// 将凹多边形分解为多个凸多边形，用于碰撞体生成
    /// </summary>
    public class ConvexDecompositionNode : PCGNodeBase
    {
        public override string Name => "ConvexDecomposition";
        public override string DisplayName => "Convex Decomposition";
        public override string Description => "将凹多边形分解为多个凸部分";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("maxHulls", PCGPortDirection.Input, PCGPortType.Int,
                "Max Hulls", "最大凸包数量", 16),
            new PCGParamSchema("voxelResolution", PCGPortDirection.Input, PCGPortType.Int,
                "Voxel Resolution", "体素分辨率", 100000),
            new PCGParamSchema("concavity", PCGPortDirection.Input, PCGPortType.Float,
                "Concavity", "凹度阈值", 0.0025f),
            new PCGParamSchema("outputMode", PCGPortDirection.Input, PCGPortType.String,
                "Output Mode", "输出模式（merge/separate）", "merge"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "分解后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count < 4 || geo.Primitives.Count == 0)
            {
                ctx.LogWarning("ConvexDecomposition: 输入几何体点数不足");
                return SingleOutput("geometry", geo);
            }

            int maxHulls = GetParamInt(parameters, "maxHulls", 16);
            string outputMode = GetParamString(parameters, "outputMode", "merge").ToLower();

            // 简化实现：计算输入网格的包围盒，然后分割成多个凸包
            // 这是一个近似方法，真正的凸分解需要 V-HACD 等复杂算法

            // 计算包围盒
            Vector3 min = geo.Points[0];
            Vector3 max = geo.Points[0];
            foreach (var p in geo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;

            // 根据主轴方向分割
            int splitAxis = 0;
            if (size.y > size.x && size.y > size.z) splitAxis = 1;
            else if (size.z > size.x) splitAxis = 2;

            // 简单分割：沿主轴切成几段
            int numSplits = Mathf.Min(maxHulls, Mathf.Max(1, Mathf.CeilToInt(size[splitAxis] / 2f)));
            float splitSize = size[splitAxis] / numSplits;

            var hulls = new List<List<Vector3>>();

            for (int i = 0; i < numSplits; i++)
            {
                float splitMin = min[splitAxis] + i * splitSize;
                float splitMax = min[splitAxis] + (i + 1) * splitSize;

                // 收集在这个分割区域的点
                var regionPoints = new List<Vector3>();
                foreach (var p in geo.Points)
                {
                    if (p[splitAxis] >= splitMin && p[splitAxis] <= splitMax)
                    {
                        regionPoints.Add(p);
                    }
                }

                if (regionPoints.Count >= 4)
                {
                    // 计算这个区域的凸包
                    var hullPoints = ComputeConvexHull(regionPoints);
                    if (hullPoints.Count >= 4)
                    {
                        hulls.Add(hullPoints);
                    }
                }
            }

            // 如果没有成功分割，使用整体凸包
            if (hulls.Count == 0)
            {
                var hullPoints = ComputeConvexHull(geo.Points);
                hulls.Add(hullPoints);
            }

            // 构建输出几何体
            var newPoints = new List<Vector3>();
            var newPrimitives = new List<int[]>();

            for (int h = 0; h < hulls.Count; h++)
            {
                var hull = hulls[h];
                int baseIdx = newPoints.Count;

                // 添加点
                newPoints.AddRange(hull);

                // 创建凸包面（简单扇形三角化）
                for (int i = 1; i < hull.Count - 1; i++)
                {
                    newPrimitives.Add(new int[] { baseIdx, baseIdx + i, baseIdx + i + 1 });
                }
            }

            // 创建分组
            for (int h = 0; h < hulls.Count; h++)
            {
                string groupName = $"hull_{h}";
                if (!geo.PrimGroups.ContainsKey(groupName))
                    geo.PrimGroups[groupName] = new HashSet<int>();

                int basePrimIdx = 0;
                for (int i = 0; i < h; i++)
                    basePrimIdx += hulls[i].Count - 2;

                for (int i = 0; i < hulls[h].Count - 2; i++)
                {
                    geo.PrimGroups[groupName].Add(basePrimIdx + i);
                }
            }

            geo.Points = newPoints;
            geo.Primitives = newPrimitives;

            ctx.Log($"ConvexDecomposition: decomposed into {hulls.Count} convex hulls, total {newPrimitives.Count} faces");
            return SingleOutput("geometry", geo);
        }

        private List<Vector3> ComputeConvexHull(List<Vector3> points)
        {
            if (points.Count < 4) return new List<Vector3>(points);

            // 简单的 3D 凸包算法（Gift Wrapping / QuickHull 的简化版本）
            // 找到最远的几个点作为凸包的初始顶点

            // 找到极值点
            int minX = 0, maxX = 0, minY = 0, maxY = 0, minZ = 0, maxZ = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].x < points[minX].x) minX = i;
                if (points[i].x > points[maxX].x) maxX = i;
                if (points[i].y < points[minY].y) minY = i;
                if (points[i].y > points[maxY].y) maxY = i;
                if (points[i].z < points[minZ].z) minZ = i;
                if (points[i].z > points[maxZ].z) maxZ = i;
            }

            var extremeIndices = new HashSet<int> { minX, maxX, minY, maxY, minZ, maxZ };
            var hullPoints = new List<Vector3>();

            foreach (int idx in extremeIndices)
            {
                hullPoints.Add(points[idx]);
            }

            // 添加更多点以形成合理的凸包
            // 找到距离已有凸包点最远的点
            for (int iter = 0; iter < 8 && hullPoints.Count < points.Count; iter++)
            {
                int farthestIdx = -1;
                float maxDist = 0f;

                for (int i = 0; i < points.Count; i++)
                {
                    if (extremeIndices.Contains(i)) continue;

                    float minDistToHull = float.MaxValue;
                    foreach (var hp in hullPoints)
                    {
                        float dist = Vector3.Distance(points[i], hp);
                        if (dist < minDistToHull) minDistToHull = dist;
                    }

                    // 找到到凸包距离最远的点
                    if (minDistToHull > maxDist)
                    {
                        maxDist = minDistToHull;
                        farthestIdx = i;
                    }
                }

                if (farthestIdx >= 0 && maxDist > 0.01f)
                {
                    hullPoints.Add(points[farthestIdx]);
                    extremeIndices.Add(farthestIdx);
                }
            }

            return hullPoints;
        }
    }
}