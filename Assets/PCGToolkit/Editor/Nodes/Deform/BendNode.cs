using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 弯曲变形（对标 Houdini Bend SOP）
    /// </summary>
    public class BendNode : PCGNodeBase
    {
        public override string Name => "Bend";
        public override string DisplayName => "Bend";
        public override string Description => "沿指定轴弯曲几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "弯曲角度", 90f),
            new PCGParamSchema("upAxis", PCGPortDirection.Input, PCGPortType.String,
                "Up Axis", "弯曲轴向（x/y/z）", "y"),
            new PCGParamSchema("captureOrigin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Capture Origin", "弯曲起始点", Vector3.zero),
            new PCGParamSchema("captureLength", PCGPortDirection.Input, PCGPortType.Float,
                "Capture Length", "受影响的长度范围", 1.0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "弯曲后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Bend: 弯曲变形 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float angle = GetParamFloat(parameters, "angle", 90f);
            string upAxis = GetParamString(parameters, "upAxis", "y");

            ctx.Log($"Bend: angle={angle}, upAxis={upAxis}");

            // TODO: 根据点在 upAxis 方向上的位置比例，应用圆弧变换
            return SingleOutput("geometry", geo);
        }
    }
}
