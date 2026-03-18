using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 曲线倒角/圆角（对标 Houdini Fillet SOP）
    /// </summary>
    public class FilletNode : PCGNodeBase
    {
        public override string Name => "Fillet";
        public override string DisplayName => "Fillet";
        public override string Description => "对曲线或多段线的拐角进行倒角/圆角处理";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线/多段线", null, required: true),
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "倒角半径", 0.1f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "每个拐角的分段数", 4),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "倒角后的曲线"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Fillet: 曲线倒角 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float radius = GetParamFloat(parameters, "radius", 0.1f);
            int divisions = GetParamInt(parameters, "divisions", 4);

            ctx.Log($"Fillet: radius={radius}, divisions={divisions}");

            // TODO: 在每个拐角处生成圆弧段替代尖角
            return SingleOutput("geometry", geo);
        }
    }
}
