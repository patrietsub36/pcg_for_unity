using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成立方体几何体（对标 Houdini Box SOP）
    /// </summary>
    public class BoxNode : PCGNodeBase
    {
        public override string Name => "Box";
        public override string DisplayName => "Box";
        public override string Description => "生成一个立方体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("sizeX", PCGPortDirection.Input, PCGPortType.Float,
                "Size X", "X 轴尺寸", 1.0f),
            new PCGParamSchema("sizeY", PCGPortDirection.Input, PCGPortType.Float,
                "Size Y", "Y 轴尺寸", 1.0f),
            new PCGParamSchema("sizeZ", PCGPortDirection.Input, PCGPortType.Float,
                "Size Z", "Z 轴尺寸", 1.0f),
            new PCGParamSchema("center", PCGPortDirection.Input, PCGPortType.Vector3,
                "Center", "中心位置", Vector3.zero),
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
            // TODO: 实现 Box 几何体生成
            ctx.Log("Box: 生成立方体 (TODO)");

            float sizeX = GetParamFloat(parameters, "sizeX", 1.0f);
            float sizeY = GetParamFloat(parameters, "sizeY", 1.0f);
            float sizeZ = GetParamFloat(parameters, "sizeZ", 1.0f);
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            ctx.Log($"Box: size=({sizeX}, {sizeY}, {sizeZ}), center={center}");

            var geo = new PCGGeometry();
            // TODO: 生成 8 个顶点 + 6 个四边形面
            return SingleOutput("geometry", geo);
        }
    }
}
