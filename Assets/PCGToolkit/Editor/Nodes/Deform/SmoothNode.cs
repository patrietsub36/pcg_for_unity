using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 平滑几何体（对标 Houdini Smooth SOP）
    /// </summary>
    public class SmoothNode : PCGNodeBase
    {
        public override string Name => "Smooth";
        public override string DisplayName => "Smooth";
        public override string Description => "对几何体进行拉普拉斯平滑";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "平滑迭代次数", 10),
            new PCGParamSchema("strength", PCGPortDirection.Input, PCGPortType.Float,
                "Strength", "平滑强度（0~1）", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅平滑指定分组的点（留空=全部）", ""),
            new PCGParamSchema("preserveVolume", PCGPortDirection.Input, PCGPortType.Bool,
                "Preserve Volume", "保持体积（HC Laplacian）", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "平滑后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Smooth: 平滑几何体 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            int iterations = GetParamInt(parameters, "iterations", 10);
            float strength = GetParamFloat(parameters, "strength", 0.5f);

            ctx.Log($"Smooth: iterations={iterations}, strength={strength}");

            // TODO: 拉普拉斯平滑：每个点移向其邻居点的重心
            return SingleOutput("geometry", geo);
        }
    }
}
