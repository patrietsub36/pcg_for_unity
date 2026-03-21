using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// N边形转三角形（对标 Houdini Divide SOP triangulate模式）
    /// </summary>
    public class TriangulateNode : PCGNodeBase
    {
        public override string Name => "Triangulate";
        public override string DisplayName => "Triangulate";
        public override string Description => "将四边形和N边形统一转换为三角形";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（全三角形）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            var result = new PCGGeometry();
            result.Points.AddRange(geo.Points);

            // 复制点属性
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                newAttr.Values.AddRange(attr.Values);
            }

            // 复制点分组
            foreach (var grp in geo.PointGroups)
                result.PointGroups[grp.Key] = new HashSet<int>(grp.Value);

            // 三角化每个面
            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length < 3) continue;

                if (prim.Length == 3)
                {
                    result.Primitives.Add((int[])prim.Clone());
                }
                else
                {
                    // 扇形三角化
                    for (int j = 1; j < prim.Length - 1; j++)
                    {
                        result.Primitives.Add(new int[] { prim[0], prim[j], prim[j + 1] });
                    }
                }

                // 把原面所属的 PrimGroup 映射到新三角形
                foreach (var grpKvp in geo.PrimGroups)
                {
                    if (grpKvp.Value.Contains(pi))
                    {
                        if (!result.PrimGroups.ContainsKey(grpKvp.Key))
                            result.PrimGroups[grpKvp.Key] = new HashSet<int>();

                        int triCount = prim.Length == 3 ? 1 : prim.Length - 2;
                        int baseIdx = result.Primitives.Count - triCount;
                        for (int t = 0; t < triCount; t++)
                            result.PrimGroups[grpKvp.Key].Add(baseIdx + t);
                    }
                }
            }

            ctx.Log($"Triangulate: {geo.Primitives.Count} 面 -> {result.Primitives.Count} 三角形");
            return SingleOutput("geometry", result);
        }
    }
}
