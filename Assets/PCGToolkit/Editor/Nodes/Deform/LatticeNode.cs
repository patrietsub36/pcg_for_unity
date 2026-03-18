using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 晶格变形（对标 Houdini Lattice SOP）
    /// </summary>
    public class LatticeNode : PCGNodeBase
    {
        public override string Name => "Lattice";
        public override string DisplayName => "Lattice";
        public override string Description => "使用晶格控制点对几何体进行自由变形（FFD）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("lattice", PCGPortDirection.Input, PCGPortType.Geometry,
                "Lattice", "晶格控制点（变形后的）", null, required: true),
            new PCGParamSchema("restLattice", PCGPortDirection.Input, PCGPortType.Geometry,
                "Rest Lattice", "晶格控制点（变形前的，可选）", null),
            new PCGParamSchema("divisionsX", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions X", "X 方向晶格分段", 2),
            new PCGParamSchema("divisionsY", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions Y", "Y 方向晶格分段", 2),
            new PCGParamSchema("divisionsZ", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions Z", "Z 方向晶格分段", 2),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "变形后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Lattice: 晶格变形 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            int divX = GetParamInt(parameters, "divisionsX", 2);
            int divY = GetParamInt(parameters, "divisionsY", 2);
            int divZ = GetParamInt(parameters, "divisionsZ", 2);

            ctx.Log($"Lattice: divisions=({divX}, {divY}, {divZ})");

            // TODO: 计算每个点在晶格中的参数坐标，应用 FFD 变形
            return SingleOutput("geometry", geo);
        }
    }
}
