using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 值域重映射 fit(v, oldMin, oldMax, newMin, newMax)
    /// </summary>
    public class FitRangeNode : PCGNodeBase
    {
        public override string Name => "FitRange";
        public override string DisplayName => "Fit Range";
        public override string Description => "将值从旧范围重映射到新范围";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.Float,
                "Value", "输入值", 0f),
            new PCGParamSchema("oldMin", PCGPortDirection.Input, PCGPortType.Float,
                "Old Min", "旧范围最小值", 0f),
            new PCGParamSchema("oldMax", PCGPortDirection.Input, PCGPortType.Float,
                "Old Max", "旧范围最大值", 1f),
            new PCGParamSchema("newMin", PCGPortDirection.Input, PCGPortType.Float,
                "New Min", "新范围最小值", 0f),
            new PCGParamSchema("newMax", PCGPortDirection.Input, PCGPortType.Float,
                "New Max", "新范围最大值", 1f),
            new PCGParamSchema("clamp", PCGPortDirection.Input, PCGPortType.Bool,
                "Clamp", "是否将结果限制在新范围内", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Float,
                "Value", "重映射后的值"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            float v = GetParamFloat(parameters, "value", 0f);
            float oMin = GetParamFloat(parameters, "oldMin", 0f);
            float oMax = GetParamFloat(parameters, "oldMax", 1f);
            float nMin = GetParamFloat(parameters, "newMin", 0f);
            float nMax = GetParamFloat(parameters, "newMax", 1f);
            bool clamp = GetParamBool(parameters, "clamp", false);

            float range = oMax - oMin;
            float result;
            if (Mathf.Abs(range) < 1e-8f)
                result = nMin;
            else
            {
                float t = (v - oMin) / range;
                result = Mathf.Lerp(nMin, nMax, t);
            }

            if (clamp)
                result = Mathf.Clamp(result, Mathf.Min(nMin, nMax), Mathf.Max(nMin, nMax));

            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = result;
            return new Dictionary<string, PCGGeometry>();
        }
    }
}
