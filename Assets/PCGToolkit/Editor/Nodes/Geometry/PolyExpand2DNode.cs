using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;
using Clipper2Lib;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 2D 多边形偏移/内缩（对标 Houdini PolyExpand2D SOP）
    /// 使用 Clipper2 库的 ClipperOffset 实现。
    /// 将几何体的面投影到 XZ 平面进行偏移，再写回 Y 坐标。
    /// </summary>
    public class PolyExpand2DNode : PCGNodeBase
    {
        public override string Name => "PolyExpand2D";
        public override string DisplayName => "PolyExpand2D";
        public override string Description => "2D 多边形偏移/内缩（基于 Clipper2）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        private const double SCALE = 100000.0;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Float,
                "Offset", "偏移量（正=膨胀，负=收缩）", 0.1f),
            new PCGParamSchema("joinType", PCGPortDirection.Input, PCGPortType.String,
                "Join Type", "拐角类型（round/miter/square）", "round"),
            new PCGParamSchema("miterLimit", PCGPortDirection.Input, PCGPortType.Float,
                "Miter Limit", "Miter 模式的尖角限制", 2.0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "偏移后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            float offset = GetParamFloat(parameters, "offset", 0.1f);
            string joinTypeStr = GetParamString(parameters, "joinType", "round").ToLower();
            float miterLimit = GetParamFloat(parameters, "miterLimit", 2.0f);

            if (geo.Primitives.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            JoinType joinType = joinTypeStr switch
            {
                "miter" => JoinType.Miter,
                "square" => JoinType.Square,
                "bevel" => JoinType.Bevel,
                _ => JoinType.Round
            };

            // 计算输入面的平均 Y 值（保留高度信息）
            float avgY = 0f;
            int count = 0;
            foreach (var p in geo.Points) { avgY += p.y; count++; }
            if (count > 0) avgY /= count;

            // 转换为 Clipper2 路径（XZ -> XY 投影）
            var paths = new Paths64();
            foreach (var prim in geo.Primitives)
            {
                var path = new Path64();
                foreach (int idx in prim)
                {
                    Vector3 p = geo.Points[idx];
                    path.Add(new Point64(p.x * SCALE, p.z * SCALE));
                }
                paths.Add(path);
            }

            // 执行偏移
            var co = new ClipperOffset(miterLimit);
            co.AddPaths(paths, joinType, EndType.Polygon);
            var result64 = new Paths64();
            co.Execute(offset * SCALE, result64);

            // 转换回 PCGGeometry
            var result = new PCGGeometry();
            foreach (var path in result64)
            {
                if (path.Count < 3) continue;

                int baseIdx = result.Points.Count;
                foreach (var pt in path)
                {
                    result.Points.Add(new Vector3(
                        (float)(pt.X / SCALE),
                        avgY,
                        (float)(pt.Y / SCALE)
                    ));
                }

                // 创建面
                int[] prim = new int[path.Count];
                for (int i = 0; i < path.Count; i++)
                    prim[i] = baseIdx + i;
                result.Primitives.Add(prim);
            }

            ctx.Log($"PolyExpand2D: {geo.Primitives.Count} 面偏移 {offset} -> {result.Primitives.Count} 面");
            return SingleOutput("geometry", result);
        }
    }
}
