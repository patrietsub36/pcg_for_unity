using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 将几何体复制到点上（对标 Houdini CopyToPoints SOP）
    /// </summary>
    public class CopyToPointsNode : PCGNodeBase
    {
        public override string Name => "CopyToPoints";
        public override string DisplayName => "Copy To Points";
        public override string Description => "将源几何体复制到目标点的每个位置上";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("source", PCGPortDirection.Input, PCGPortType.Geometry,
                "Source", "要复制的源几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target Points", "目标点集", null, required: true),
            new PCGParamSchema("usePointOrient", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Orient", "使用点的 orient 属性控制旋转", true),
            new PCGParamSchema("usePointScale", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Scale", "使用点的 pscale 属性控制缩放", true),
            new PCGParamSchema("pack", PCGPortDirection.Input, PCGPortType.Bool,
                "Pack", "是否将副本打包为实例", false),
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
            ctx.Log("CopyToPoints: 复制到点 (TODO)");

            var source = GetInputGeometry(inputGeometries, "source");
            var target = GetInputGeometry(inputGeometries, "target");
            bool usePointOrient = GetParamBool(parameters, "usePointOrient", true);

            ctx.Log($"CopyToPoints: source.points={source.Points.Count}, target.points={target.Points.Count}");

            // TODO: 对目标几何体的每个点，复制源几何体并应用该点的 TRS
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
