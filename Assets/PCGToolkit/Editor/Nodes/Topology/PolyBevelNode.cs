using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 多边形倒角（对标 Houdini PolyBevel SOP）
    /// </summary>
    public class PolyBevelNode : PCGNodeBase
    {
        public override string Name => "PolyBevel";
        public override string DisplayName => "Poly Bevel";
        public override string Description => "对多边形的边或顶点进行倒角";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Float,
                "Offset", "倒角偏移距离", 0.1f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "倒角分段数", 1),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "倒角模式（edges/vertices）", "edges"),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组倒角", ""),
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
                ctx.LogWarning("PolyBevel: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float offset = GetParamFloat(parameters, "offset", 0.1f);
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            string mode = GetParamString(parameters, "mode", "edges").ToLower();
            string group = GetParamString(parameters, "group", "");

            // 构建边邻接信息
            var edgeFaces = new Dictionary<(int, int), List<int>>();
            for (int primIdx = 0; primIdx < geo.Primitives.Count; primIdx++)
            {
                var prim = geo.Primitives[primIdx];
                for (int i = 0; i < prim.Length; i++)
                {
                    int v0 = prim[i];
                    int v1 = prim[(i + 1) % prim.Length];
                    var edge = v0 < v1 ? (v0, v1) : (v1, v0);

                    if (!edgeFaces.TryGetValue(edge, out var faces))
                    {
                        faces = new List<int>();
                        edgeFaces[edge] = faces;
                    }
                    faces.Add(primIdx);
                }
            }

            // 找到需要倒角的边（只连接一个面的边，即边界边）
            var edgesToBevel = new List<(int, int)>();
            foreach (var kvp in edgeFaces)
            {
                if (kvp.Value.Count == 1) // 边界边
                {
                    edgesToBevel.Add(kvp.Key);
                }
            }

            if (edgesToBevel.Count == 0)
            {
                ctx.Log("PolyBevel: 没有找到可倒角的边");
                return SingleOutput("geometry", geo);
            }

            // 为倒角边生成新点
            var newPoints = new List<Vector3>(geo.Points);
            var newPrimitives = new List<int[]>(geo.Primitives);

            foreach (var edge in edgesToBevel)
            {
                int v0 = edge.Item1;
                int v1 = edge.Item2;
                Vector3 p0 = geo.Points[v0];
                Vector3 p1 = geo.Points[v1];
                Vector3 edgeDir = (p1 - p0).normalized;

                // 找到这条边所属的面
                int primIdx = edgeFaces[edge][0];
                var prim = geo.Primitives[primIdx];

                // 计算面的法线
                Vector3 normal = CalculateFaceNormal(geo.Points, prim);

                // 计算偏移方向（垂直于边，在面内）
                Vector3 offsetDir = Vector3.Cross(normal, edgeDir).normalized;

                // 为每个原始点生成两个新点
                // 这里简化处理：只在边的两端各生成一个点
                if (divisions == 1)
                {
                    // 简单倒角：用新点替换原边
                    int newPointIdx0 = newPoints.Count;
                    newPoints.Add(p0 + offsetDir * offset);

                    int newPointIdx1 = newPoints.Count;
                    newPoints.Add(p1 + offsetDir * offset);

                    // 修改面，添加倒角四边形
                    // 找到边在面中的索引
                    int edgeIdxInPrim = -1;
                    for (int i = 0; i < prim.Length; i++)
                    {
                        if ((prim[i] == v0 && prim[(i + 1) % prim.Length] == v1) ||
                            (prim[i] == v1 && prim[(i + 1) % prim.Length] == v0))
                        {
                            edgeIdxInPrim = i;
                            break;
                        }
                    }

                    if (edgeIdxInPrim >= 0)
                    {
                        // 添加倒角面
                        var bevelFace = new int[] { v0, v1, newPointIdx1, newPointIdx0 };
                        newPrimitives.Add(bevelFace);
                    }
                }
            }

            geo.Points = newPoints;
            geo.Primitives = newPrimitives;

            ctx.Log($"PolyBevel: offset={offset}, beveled={edgesToBevel.Count} edges, output={newPoints.Count}pts, {newPrimitives.Count}faces");
            return SingleOutput("geometry", geo);
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