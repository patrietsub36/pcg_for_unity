using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成管状/圆柱体几何体（对标 Houdini Tube SOP）
    /// </summary>
    public class TubeNode : PCGNodeBase
    {
        public override string Name => "Tube";
        public override string DisplayName => "Tube";
        public override string Description => "生成一个管状/圆柱体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radiusOuter", PCGPortDirection.Input, PCGPortType.Float,
                "Outer Radius", "外半径", 0.5f),
            new PCGParamSchema("radiusInner", PCGPortDirection.Input, PCGPortType.Float,
                "Inner Radius", "内半径（0 时为实心圆柱）", 0f),
            new PCGParamSchema("height", PCGPortDirection.Input, PCGPortType.Float,
                "Height", "高度", 1.0f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "高度方向的分段数", 1),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "圆周方向的分段数", 16),
            new PCGParamSchema("endCaps", PCGPortDirection.Input, PCGPortType.Bool,
                "End Caps", "是否封口", true),
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
            ctx.Log("Tube: 生成管状/圆柱体 (TODO)");

            float radiusOuter = GetParamFloat(parameters, "radiusOuter", 0.5f);
            float radiusInner = GetParamFloat(parameters, "radiusInner", 0f);
            float height = GetParamFloat(parameters, "height", 1.0f);
            int rows = GetParamInt(parameters, "rows", 1);
            int columns = GetParamInt(parameters, "columns", 16);
            bool endCaps = GetParamBool(parameters, "endCaps", true);

            ctx.Log($"Tube: radiusOuter={radiusOuter}, radiusInner={radiusInner}, height={height}");

            var geo = new PCGGeometry();
            // TODO: 生成管状/圆柱体顶点和面
            return SingleOutput("geometry", geo);
        }
    }
}
