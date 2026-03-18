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
                "Geometry", "碎裂后的几何体（每个碎片作为 Primitive Group）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("VoronoiFracture: Voronoi 碎裂 (TODO)");

            var inputGeo = GetInputGeometry(inputGeometries, "input");
            int numPoints = GetParamInt(parameters, "numPoints", 20);
            int seed = GetParamInt(parameters, "seed", 0);
            bool createInterior = GetParamBool(parameters, "createInterior", true);

            ctx.Log($"VoronoiFracture: numPoints={numPoints}, seed={seed}, createInterior={createInterior}");

            // TODO: 使用 MIConvexHull 计算 Voronoi 图
            // 用 Voronoi 单元裁剪输入几何体
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
