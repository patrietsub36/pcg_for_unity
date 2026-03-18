using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 凸包分解（使用 MIConvexHull）
    /// </summary>
    public class ConvexDecompositionNode : PCGNodeBase
    {
        public override string Name => "ConvexDecomposition";
        public override string DisplayName => "Convex Decomposition";
        public override string Description => "将几何体分解为多个凸包";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("maxConvexHulls", PCGPortDirection.Input, PCGPortType.Int,
                "Max Hulls", "最大凸包数量", 16),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "体素化分辨率", 100000),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "分解后的凸包几何体（每个凸包作为一个 Primitive Group）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("ConvexDecomposition: 凸包分解 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            int maxHulls = GetParamInt(parameters, "maxConvexHulls", 16);
            int resolution = GetParamInt(parameters, "resolution", 100000);

            ctx.Log($"ConvexDecomposition: maxHulls={maxHulls}, resolution={resolution}");

            // TODO: 使用 MIConvexHull 计算凸包/Voronoi 分解
            return SingleOutput("geometry", geo);
        }
    }
}
