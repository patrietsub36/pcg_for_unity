using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成圆形（多边形或线）几何体（对标 Houdini Circle SOP）
    /// </summary>
    public class CircleNode : PCGNodeBase
    {
        public override string Name => "Circle";
        public override string DisplayName => "Circle";
        public override string Description => "生成一个圆形几何体（多边形或线）";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "半径", 1.0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "分段数", 16),
            new PCGParamSchema("arc", PCGPortDirection.Input, PCGPortType.Float,
                "Arc", "弧度角（360 = 完整圆）", 360f),
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
            ctx.Log("Circle: 生成圆形 (TODO)");

            float radius = GetParamFloat(parameters, "radius", 1.0f);
            int divisions = GetParamInt(parameters, "divisions", 16);
            float arc = GetParamFloat(parameters, "arc", 360f);

            ctx.Log($"Circle: radius={radius}, divisions={divisions}, arc={arc}");

            var geo = new PCGGeometry();
            // TODO: 生成圆形顶点
            return SingleOutput("geometry", geo);
        }
    }
}
