using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 沿曲线扫掠截面生成几何体（对标 Houdini Sweep SOP）
    /// </summary>
    public class SweepNode : PCGNodeBase
    {
        public override string Name => "Sweep";
        public override string DisplayName => "Sweep";
        public override string Description => "沿路径曲线扫掠截面形状生成几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("backbone", PCGPortDirection.Input, PCGPortType.Geometry,
                "Backbone", "路径曲线（骨架线）", null, required: true),
            new PCGParamSchema("crossSection", PCGPortDirection.Input, PCGPortType.Geometry,
                "Cross Section", "截面形状（可选，默认使用圆形）", null),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Float,
                "Scale", "截面缩放", 1.0f),
            new PCGParamSchema("twist", PCGPortDirection.Input, PCGPortType.Float,
                "Twist", "沿路径的扭转角度", 0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "截面分段数（无截面输入时使用）", 8),
            new PCGParamSchema("capEnds", PCGPortDirection.Input, PCGPortType.Bool,
                "Cap Ends", "封口两端", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "扫掠生成的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Sweep: 沿曲线扫掠 (TODO)");

            var backbone = GetInputGeometry(inputGeometries, "backbone");
            float scale = GetParamFloat(parameters, "scale", 1.0f);
            float twist = GetParamFloat(parameters, "twist", 0f);
            int divisions = GetParamInt(parameters, "divisions", 8);

            ctx.Log($"Sweep: backbone.points={backbone.Points.Count}, scale={scale}, twist={twist}");

            // TODO: 沿骨架线的每个点放置截面，生成面连接相邻截面
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
