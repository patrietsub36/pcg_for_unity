using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 布局/排列（对标 Houdini UVLayout SOP）
    /// </summary>
    public class UVLayoutNode : PCGNodeBase
    {
        public override string Name => "UVLayout";
        public override string DisplayName => "UV Layout";
        public override string Description => "重新排列 UV 岛以优化空间利用率";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("padding", PCGPortDirection.Input, PCGPortType.Float,
                "Padding", "UV 岛之间的间距", 0.01f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "布局分辨率", 1024),
            new PCGParamSchema("rotateIslands", PCGPortDirection.Input, PCGPortType.Bool,
                "Rotate Islands", "是否允许旋转 UV 岛以优化排列", true),
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
            ctx.Log("UVLayout: UV 布局 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float padding = GetParamFloat(parameters, "padding", 0.01f);
            int resolution = GetParamInt(parameters, "resolution", 1024);

            ctx.Log($"UVLayout: padding={padding}, resolution={resolution}");

            // TODO: 重新排列 UV 岛
            return SingleOutput("geometry", geo);
        }
    }
}
