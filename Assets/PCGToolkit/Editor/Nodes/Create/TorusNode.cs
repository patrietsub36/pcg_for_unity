using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成环面几何体（对标 Houdini Torus SOP）
    /// </summary>
    public class TorusNode : PCGNodeBase
    {
        public override string Name => "Torus";
        public override string DisplayName => "Torus";
        public override string Description => "生成一个环面（甜甜圈）几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radiusMajor", PCGPortDirection.Input, PCGPortType.Float,
                "Major Radius", "主半径（环心到管心的距离）", 1.0f),
            new PCGParamSchema("radiusMinor", PCGPortDirection.Input, PCGPortType.Float,
                "Minor Radius", "次半径（管的截面半径）", 0.25f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "管截面方向的分段数", 16),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "环周方向的分段数", 32),
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
            ctx.Log("Torus: 生成环面 (TODO)");

            float radiusMajor = GetParamFloat(parameters, "radiusMajor", 1.0f);
            float radiusMinor = GetParamFloat(parameters, "radiusMinor", 0.25f);
            int rows = GetParamInt(parameters, "rows", 16);
            int columns = GetParamInt(parameters, "columns", 32);

            ctx.Log($"Torus: radiusMajor={radiusMajor}, radiusMinor={radiusMinor}, rows={rows}, columns={columns}");

            var geo = new PCGGeometry();
            // TODO: 生成环面顶点和四边形面
            return SingleOutput("geometry", geo);
        }
    }
}
