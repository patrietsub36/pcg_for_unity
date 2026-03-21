using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 输出随机 Float/Int/Vector3，支持 seed 控制
    /// </summary>
    public class RandomNode : PCGNodeBase
    {
        public override string Name => "Random";
        public override string DisplayName => "Random";
        public override string Description => "输出随机 Float/Int/Vector3 值";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("min", PCGPortDirection.Input, PCGPortType.Float,
                "Min", "最小值", 0f),
            new PCGParamSchema("max", PCGPortDirection.Input, PCGPortType.Float,
                "Max", "最大值", 1f),
            new PCGParamSchema("outputType", PCGPortDirection.Input, PCGPortType.String,
                "Output Type", "输出类型（float/int/vector3）", "float"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Float,
                "Value", "随机值 (Float)"),
            new PCGParamSchema("valueInt", PCGPortDirection.Output, PCGPortType.Int,
                "Value Int", "随机值 (Int)"),
            new PCGParamSchema("valueVec3", PCGPortDirection.Output, PCGPortType.Vector3,
                "Value Vector3", "随机值 (Vector3)"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            int seed = GetParamInt(parameters, "seed", 0);
            float min = GetParamFloat(parameters, "min", 0f);
            float max = GetParamFloat(parameters, "max", 1f);

            var rng = new System.Random(seed);
            float f = min + (float)rng.NextDouble() * (max - min);
            int i = Mathf.RoundToInt(f);
            Vector3 v = new Vector3(
                min + (float)rng.NextDouble() * (max - min),
                min + (float)rng.NextDouble() * (max - min),
                min + (float)rng.NextDouble() * (max - min)
            );

            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = f;
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.valueInt"] = i;
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.valueVec3"] = v;
            return new Dictionary<string, PCGGeometry>();
        }
    }
}
