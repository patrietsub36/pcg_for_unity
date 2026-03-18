using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成线段几何体（对标 Houdini Line SOP）
    /// </summary>
    public class LineNode : PCGNodeBase
    {
        public override string Name => "Line";
        public override string DisplayName => "Line";
        public override string Description => "生成一条线段几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "起点", Vector3.zero),
            new PCGParamSchema("direction", PCGPortDirection.Input, PCGPortType.Vector3,
                "Direction", "方向", Vector3.up),
            new PCGParamSchema("length", PCGPortDirection.Input, PCGPortType.Float,
                "Length", "长度", 1.0f),
            new PCGParamSchema("points", PCGPortDirection.Input, PCGPortType.Int,
                "Points", "点数（包含起点和终点）", 2),
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
            ctx.Log("Line: 生成线段 (TODO)");

            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 direction = GetParamVector3(parameters, "direction", Vector3.up);
            float length = GetParamFloat(parameters, "length", 1.0f);
            int points = GetParamInt(parameters, "points", 2);

            ctx.Log($"Line: origin={origin}, direction={direction}, length={length}, points={points}");

            var geo = new PCGGeometry();
            // TODO: 生成线段顶点
            return SingleOutput("geometry", geo);
        }
    }
}
