using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 比较两个值，输出 Bool（可驱动 Switch 节点）
    /// </summary>
    public class CompareNode : PCGNodeBase
    {
        public override string Name => "Compare";
        public override string DisplayName => "Compare";
        public override string Description => "比较两个值，输出 Bool 结果";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("a", PCGPortDirection.Input, PCGPortType.Float,
                "A", "第一个值", 0f),
            new PCGParamSchema("b", PCGPortDirection.Input, PCGPortType.Float,
                "B", "第二个值", 0f),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "比较运算（equal/notEqual/greater/less/greaterEqual/lessEqual）", "equal"),
            new PCGParamSchema("tolerance", PCGPortDirection.Input, PCGPortType.Float,
                "Tolerance", "equal/notEqual 的容差", 0.0001f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("result", PCGPortDirection.Output, PCGPortType.Bool,
                "Result", "比较结果"),
            new PCGParamSchema("index", PCGPortDirection.Output, PCGPortType.Int,
                "Index", "结果为 true 时输出 1，否则 0（可驱动 Switch）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            float a = GetParamFloat(parameters, "a", 0f);
            float b = GetParamFloat(parameters, "b", 0f);
            string op = GetParamString(parameters, "operation", "equal").ToLower();
            float tol = GetParamFloat(parameters, "tolerance", 0.0001f);

            bool result = op switch
            {
                "equal" => Mathf.Abs(a - b) <= tol,
                "notequal" => Mathf.Abs(a - b) > tol,
                "greater" => a > b,
                "less" => a < b,
                "greaterequal" => a >= b,
                "lessequal" => a <= b,
                _ => Mathf.Abs(a - b) <= tol
            };

            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.result"] = result;
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.index"] = result ? 1 : 0;
            return new Dictionary<string, PCGGeometry>();
        }
    }
}
