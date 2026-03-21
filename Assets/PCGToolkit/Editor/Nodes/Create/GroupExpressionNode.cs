using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// GroupExpression 节点：使用表达式创建分组。
    /// 对每个点/面执行表达式，结果 > 0 则加入组。
    /// 对标 Houdini Group Expression SOP。
    /// </summary>
    public class GroupExpressionNode : PCGNodeBase
    {
        public override string Name => "GroupExpression";
        public override string DisplayName => "Group Expression";
        public override string Description => "使用表达式创建点或面分组";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("expression", PCGPortDirection.Input, PCGPortType.String,
                "Expression", "分组表达式（如 @P.y > 5）", ""),
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                "Group Name", "分组名称", "newGroup"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "分组类型（point/primitive）", "point"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带新分组）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string expression = GetParamString(parameters, "expression", "");
            string groupName = GetParamString(parameters, "groupName", "newGroup");
            string groupClass = GetParamString(parameters, "class", "point").ToLower();

            if (string.IsNullOrEmpty(expression))
            {
                ctx.LogWarning("GroupExpression: expression is empty");
                return SingleOutput("geometry", geo.Clone());
            }

            if (string.IsNullOrEmpty(groupName))
            {
                ctx.LogWarning("GroupExpression: groupName is empty");
                return SingleOutput("geometry", geo.Clone());
            }

            var result = geo.Clone();
            var parser = new ExpressionParser();
            var evalCtx = new ExpressionParser.EvalContext
            {
                Geometry = result,
                TotalPoints = result.Points.Count,
                TotalPrims = result.Primitives.Count
            };

            if (groupClass == "primitive" || groupClass == "prim" || groupClass == "face")
            {
                // 按 Primitive 分组
                var groupIndices = new HashSet<int>();

                for (int i = 0; i < result.Primitives.Count; i++)
                {
                    evalCtx.PrimIndex = i;
                    evalCtx.PointIndex = -1;

                    // 将 prim 的中心点作为 @P
                    var prim = result.Primitives[i];
                    if (prim.Length > 0)
                    {
                        Vector3 center = Vector3.zero;
                        foreach (int vi in prim)
                        {
                            if (vi >= 0 && vi < result.Points.Count)
                                center += result.Points[vi];
                        }
                        center /= prim.Length;
                        evalCtx.Variables["@P"] = center;
                    }

                    // 加载 Primitive 属性
                    foreach (var attrName in result.PrimAttribs.GetAttributeNames())
                    {
                        var attr = result.PrimAttribs.GetAttribute(attrName);
                        if (i < attr.Values.Count)
                            evalCtx.Variables[$"@{attrName}"] = attr.Values[i];
                    }

                    try
                    {
                        float value = parser.EvaluateFloat(expression, evalCtx);
                        if (value > 0)
                            groupIndices.Add(i);
                    }
                    catch (System.Exception e)
                    {
                        ctx.LogWarning($"GroupExpression: Error evaluating expression at prim {i}: {e.Message}");
                    }
                }

                result.PrimGroups[groupName] = groupIndices;
                ctx.Log($"GroupExpression: Created primitive group '{groupName}' with {groupIndices.Count} prims");
            }
            else
            {
                // 按 Point 分组
                var groupIndices = new HashSet<int>();

                for (int i = 0; i < result.Points.Count; i++)
                {
                    evalCtx.PointIndex = i;
                    evalCtx.PrimIndex = -1;

                    try
                    {
                        float value = parser.EvaluateFloat(expression, evalCtx);
                        if (value > 0)
                            groupIndices.Add(i);
                    }
                    catch (System.Exception e)
                    {
                        ctx.LogWarning($"GroupExpression: Error evaluating expression at point {i}: {e.Message}");
                    }
                }

                result.PointGroups[groupName] = groupIndices;
                ctx.Log($"GroupExpression: Created point group '{groupName}' with {groupIndices.Count} points");
            }

            return SingleOutput("geometry", result);
        }
    }
}