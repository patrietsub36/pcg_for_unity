using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 减面/简化（对标 Houdini PolyReduce SOP）
    /// </summary>
    public class DecimateNode : PCGNodeBase
    {
        public override string Name => "Decimate";
        public override string DisplayName => "Decimate";
        public override string Description => "减少几何体的面数（简化网格）";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("targetPercentage", PCGPortDirection.Input, PCGPortType.Float,
                "Target %", "目标面数百分比（0~1）", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("maxError", PCGPortDirection.Input, PCGPortType.Float,
                "Max Error", "最大允许误差", 0.01f),
            new PCGParamSchema("preserveBoundary", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Boundary", "保持边界", true),
            new PCGParamSchema("preserveHardEdges", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Hard Edges", "保持硬边", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "简化后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Decimate: 减面 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float targetPercentage = GetParamFloat(parameters, "targetPercentage", 0.5f);
            float maxError = GetParamFloat(parameters, "maxError", 0.01f);

            ctx.Log($"Decimate: target={targetPercentage * 100}%, maxError={maxError}");

            // TODO: 使用 geometry3Sharp 的 MeshSimplification 或 QEM 算法
            return SingleOutput("geometry", geo);
        }
    }
}
