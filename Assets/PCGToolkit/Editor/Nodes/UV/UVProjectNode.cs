using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 投影（对标 Houdini UVProject SOP）
    /// </summary>
    public class UVProjectNode : PCGNodeBase
    {
        public override string Name => "UVProject";
        public override string DisplayName => "UV Project";
        public override string Description => "对几何体进行 UV 投影（平面/柱面/球面/立方体）";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("projectionType", PCGPortDirection.Input, PCGPortType.String,
                "Projection Type", "投影类型（planar/cylindrical/spherical/cubic）", "planar"),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组投影（留空=全部）", ""),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "UV 缩放", Vector3.one),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Vector3,
                "Offset", "UV 偏移", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带 UV 属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("UVProject: UV 投影 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string projectionType = GetParamString(parameters, "projectionType", "planar");

            ctx.Log($"UVProject: type={projectionType}");

            // TODO: 根据投影类型计算 UV 坐标并写入 Vertex 属性 "uv"
            return SingleOutput("geometry", geo);
        }
    }
}
