using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成平面网格几何体（对标 Houdini Grid SOP）
    /// </summary>
    public class GridNode : PCGNodeBase
    {
        public override string Name => "Grid";
        public override string DisplayName => "Grid";
        public override string Description => "生成一个平面网格几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("sizeX", PCGPortDirection.Input, PCGPortType.Float,
                "Size X", "X 方向尺寸", 10f),
            new PCGParamSchema("sizeY", PCGPortDirection.Input, PCGPortType.Float,
                "Size Y", "Y 方向尺寸", 10f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "行数", 10),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "列数", 10),
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
            ctx.Log("Grid: 生成平面网格 (TODO)");

            float sizeX = GetParamFloat(parameters, "sizeX", 10f);
            float sizeY = GetParamFloat(parameters, "sizeY", 10f);
            int rows = GetParamInt(parameters, "rows", 10);
            int columns = GetParamInt(parameters, "columns", 10);

            ctx.Log($"Grid: size=({sizeX}, {sizeY}), rows={rows}, columns={columns}");

            var geo = new PCGGeometry();
            // TODO: 生成网格顶点和四边形面
            return SingleOutput("geometry", geo);
        }
    }
}
