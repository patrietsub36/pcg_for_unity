using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 重新网格化（对标 Houdini Remesh SOP）
    /// </summary>
    public class RemeshNode : PCGNodeBase
    {
        public override string Name => "Remesh";
        public override string DisplayName => "Remesh";
        public override string Description => "重新生成均匀的三角形网格";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("targetEdgeLength", PCGPortDirection.Input, PCGPortType.Float,
                "Target Edge Length", "目标边长", 0.1f),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "迭代次数", 10),
            new PCGParamSchema("smoothing", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothing", "平滑系数", 0.5f),
            new PCGParamSchema("preserveBoundary", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Boundary", "保持边界不变", true),
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
            ctx.Log("Remesh: 重新网格化 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float targetEdgeLength = GetParamFloat(parameters, "targetEdgeLength", 0.1f);
            int iterations = GetParamInt(parameters, "iterations", 10);

            ctx.Log($"Remesh: targetEdgeLength={targetEdgeLength}, iterations={iterations}");

            // TODO: 使用 geometry3Sharp 的 Remesher 实现等距三角化
            return SingleOutput("geometry", geo);
        }
    }
}
