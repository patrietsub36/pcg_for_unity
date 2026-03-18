using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 扭转变形（对标 Houdini Twist SOP）
    /// </summary>
    public class TwistNode : PCGNodeBase
    {
        public override string Name => "Twist";
        public override string DisplayName => "Twist";
        public override string Description => "沿指定轴扭转几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "总扭转角度", 180f),
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.String,
                "Axis", "扭转轴（x/y/z）", "y"),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "扭转中心", Vector3.zero),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "扭转后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Twist: 扭转变形 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float angle = GetParamFloat(parameters, "angle", 180f);
            string axis = GetParamString(parameters, "axis", "y");

            ctx.Log($"Twist: angle={angle}, axis={axis}");

            // TODO: 根据点在轴上的位置比例，绕轴旋转对应角度
            return SingleOutput("geometry", geo);
        }
    }
}
