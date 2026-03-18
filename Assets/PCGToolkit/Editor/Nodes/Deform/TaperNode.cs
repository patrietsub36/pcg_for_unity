using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 锥化变形（对标 Houdini Taper SOP）
    /// </summary>
    public class TaperNode : PCGNodeBase
    {
        public override string Name => "Taper";
        public override string DisplayName => "Taper";
        public override string Description => "沿指定轴对几何体进行锥化（渐变缩放）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("scaleStart", PCGPortDirection.Input, PCGPortType.Float,
                "Scale Start", "起始端缩放", 1.0f),
            new PCGParamSchema("scaleEnd", PCGPortDirection.Input, PCGPortType.Float,
                "Scale End", "结束端缩放", 0.0f),
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.String,
                "Axis", "锥化轴（x/y/z）", "y"),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "锥化中心", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "锥化后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Taper: 锥化变形 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float scaleStart = GetParamFloat(parameters, "scaleStart", 1.0f);
            float scaleEnd = GetParamFloat(parameters, "scaleEnd", 0.0f);
            string axis = GetParamString(parameters, "axis", "y");

            ctx.Log($"Taper: scaleStart={scaleStart}, scaleEnd={scaleEnd}, axis={axis}");

            // TODO: 根据点在轴上的位置比例，插值缩放截面
            return SingleOutput("geometry", geo);
        }
    }
}
