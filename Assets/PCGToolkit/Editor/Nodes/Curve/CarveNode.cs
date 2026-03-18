using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 裁切曲线（对标 Houdini Carve SOP）
    /// </summary>
    public class CarveNode : PCGNodeBase
    {
        public override string Name => "Carve";
        public override string DisplayName => "Carve";
        public override string Description => "按参数范围裁切曲线（保留指定比例范围内的段）";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线", null, required: true),
            new PCGParamSchema("firstU", PCGPortDirection.Input, PCGPortType.Float,
                "First U", "起始参数（0~1）", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("secondU", PCGPortDirection.Input, PCGPortType.Float,
                "Second U", "结束参数（0~1）", 1f) { Min = 0f, Max = 1f },
            new PCGParamSchema("cutAtFirstU", PCGPortDirection.Input, PCGPortType.Bool,
                "Cut at First U", "在起始参数处切断", true),
            new PCGParamSchema("cutAtSecondU", PCGPortDirection.Input, PCGPortType.Bool,
                "Cut at Second U", "在结束参数处切断", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "裁切后的曲线"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Carve: 裁切曲线 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float firstU = GetParamFloat(parameters, "firstU", 0f);
            float secondU = GetParamFloat(parameters, "secondU", 1f);

            ctx.Log($"Carve: firstU={firstU}, secondU={secondU}");

            // TODO: 按 U 参数范围裁切曲线
            return SingleOutput("geometry", geo);
        }
    }
}
