using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 在几何体表面散布点（对标 Houdini Scatter SOP）
    /// </summary>
    public class ScatterNode : PCGNodeBase
    {
        public override string Name => "Scatter";
        public override string DisplayName => "Scatter";
        public override string Description => "在几何体表面随机散布点";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（散布表面）", null, required: true),
            new PCGParamSchema("count", PCGPortDirection.Input, PCGPortType.Int,
                "Count", "散布点数量", 100),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("densityAttrib", PCGPortDirection.Input, PCGPortType.String,
                "Density Attribute", "密度属性名（控制分布密度）", ""),
            new PCGParamSchema("relaxIterations", PCGPortDirection.Input, PCGPortType.Int,
                "Relax Iterations", "松弛迭代次数（使分布更均匀）", 0),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅在指定面分组上散布", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "散布点几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Scatter: 散布点 (TODO)");

            var inputGeo = GetInputGeometry(inputGeometries, "input");
            int count = GetParamInt(parameters, "count", 100);
            int seed = GetParamInt(parameters, "seed", 0);

            ctx.Log($"Scatter: count={count}, seed={seed}");

            // TODO: 在输入几何体表面随机采样点
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
