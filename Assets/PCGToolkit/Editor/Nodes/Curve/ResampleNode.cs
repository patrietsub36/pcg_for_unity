using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 重采样曲线（对标 Houdini Resample SOP）
    /// </summary>
    public class ResampleNode : PCGNodeBase
    {
        public override string Name => "Resample";
        public override string DisplayName => "Resample";
        public override string Description => "按指定间距或数量重采样曲线/多段线";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入曲线/多段线", null, required: true),
            new PCGParamSchema("method", PCGPortDirection.Input, PCGPortType.String,
                "Method", "采样方式（length/count）", "length"),
            new PCGParamSchema("length", PCGPortDirection.Input, PCGPortType.Float,
                "Length", "每段长度（method=length 时）", 0.1f),
            new PCGParamSchema("segments", PCGPortDirection.Input, PCGPortType.Int,
                "Segments", "总段数（method=count 时）", 10),
            new PCGParamSchema("treatAsSubdivision", PCGPortDirection.Input, PCGPortType.Bool,
                "Treat as Subdivision", "是否在现有点之间细分", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "重采样后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Resample: 重采样曲线 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string method = GetParamString(parameters, "method", "length");
            float length = GetParamFloat(parameters, "length", 0.1f);
            int segments = GetParamInt(parameters, "segments", 10);

            ctx.Log($"Resample: method={method}, length={length}, segments={segments}");

            // TODO: 按等距或等数重新采样曲线上的点
            return SingleOutput("geometry", geo);
        }
    }
}
