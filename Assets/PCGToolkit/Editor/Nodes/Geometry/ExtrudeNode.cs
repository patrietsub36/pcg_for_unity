using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 挤出面（对标 Houdini PolyExtrude SOP）
    /// </summary>
    public class ExtrudeNode : PCGNodeBase
    {
        public override string Name => "Extrude";
        public override string DisplayName => "Extrude";
        public override string Description => "沿法线方向挤出几何体的面";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要挤出的面分组（留空=全部面）", ""),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "挤出距离", 0.5f),
            new PCGParamSchema("inset", PCGPortDirection.Input, PCGPortType.Float,
                "Inset", "内缩距离", 0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "挤出方向的分段数", 1),
            new PCGParamSchema("outputFront", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Front", "是否输出顶面", true),
            new PCGParamSchema("outputSide", PCGPortDirection.Input, PCGPortType.Bool,
                "Output Side", "是否输出侧面", true),
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
            ctx.Log("Extrude: 挤出面 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float distance = GetParamFloat(parameters, "distance", 0.5f);
            float inset = GetParamFloat(parameters, "inset", 0f);
            int divisions = GetParamInt(parameters, "divisions", 1);

            ctx.Log($"Extrude: distance={distance}, inset={inset}, divisions={divisions}");

            // TODO: 实现面挤出（沿法线方向偏移顶点，生成侧面）
            return SingleOutput("geometry", geo);
        }
    }
}
