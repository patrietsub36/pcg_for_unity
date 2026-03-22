using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 对两个 Group 做集合运算（Union/Intersect/Subtract）
    /// 结果写入新的 Group。
    /// </summary>
    public class GroupCombineNode : PCGNodeBase
    {
        public override string Name => "GroupCombine";
        public override string DisplayName => "Group Combine";
        public override string Description => "对两个 Group 做集合运算（Union/Intersect/Subtract）";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupA", PCGPortDirection.Input, PCGPortType.String,
                "Group A", "第一个分组名", ""),
            new PCGParamSchema("groupB", PCGPortDirection.Input, PCGPortType.String,
                "Group B", "第二个分组名", ""),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "集合运算（union/intersect/subtract）", "union")
            {
                EnumOptions = new[] { "union", "intersect", "subtract" }
            },
            new PCGParamSchema("resultGroup", PCGPortDirection.Input, PCGPortType.String,
                "Result Group", "结果分组名", "combined"),
            new PCGParamSchema("groupType", PCGPortDirection.Input, PCGPortType.String,
                "Group Type", "分组类型（point/prim）", "prim")
            {
                EnumOptions = new[] { "point", "prim" }
            },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（含新分组）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupA = GetParamString(parameters, "groupA", "");
            string groupB = GetParamString(parameters, "groupB", "");
            string operation = GetParamString(parameters, "operation", "union").ToLower();
            string resultGroup = GetParamString(parameters, "resultGroup", "combined");
            string groupType = GetParamString(parameters, "groupType", "prim").ToLower();

            var groups = groupType == "point" ? geo.PointGroups : geo.PrimGroups;

            HashSet<int> setA = new HashSet<int>();
            HashSet<int> setB = new HashSet<int>();
            if (!string.IsNullOrEmpty(groupA) && groups.TryGetValue(groupA, out var a)) setA = a;
            if (!string.IsNullOrEmpty(groupB) && groups.TryGetValue(groupB, out var b)) setB = b;

            HashSet<int> result;
            switch (operation)
            {
                case "intersect":
                    result = new HashSet<int>(setA);
                    result.IntersectWith(setB);
                    break;
                case "subtract":
                    result = new HashSet<int>(setA);
                    result.ExceptWith(setB);
                    break;
                default: // union
                    result = new HashSet<int>(setA);
                    result.UnionWith(setB);
                    break;
            }

            groups[resultGroup] = result;

            ctx.Log($"GroupCombine: {groupA}({setA.Count}) {operation} {groupB}({setB.Count}) -> {resultGroup}({result.Count})");
            return SingleOutput("geometry", geo);
        }
    }
}
