using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 按 Group 拆分几何体为 matched/unmatched 两路输出。
    /// 唯一需要双 Geometry 输出的节点。
    /// </summary>
    public class SplitNode : PCGNodeBase
    {
        public override string Name => "Split";
        public override string DisplayName => "Split";
        public override string Description => "按 Group 拆分几何体为 matched 和 unmatched 两路";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "用于拆分的 PrimGroup 名称", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("matched", PCGPortDirection.Output, PCGPortType.Geometry,
                "Matched", "属于指定 Group 的面"),
            new PCGParamSchema("unmatched", PCGPortDirection.Output, PCGPortType.Geometry,
                "Unmatched", "不属于指定 Group 的面"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string group = GetParamString(parameters, "group", "");

            if (string.IsNullOrEmpty(group) || !geo.PrimGroups.TryGetValue(group, out var groupSet))
            {
                ctx.LogWarning($"Split: Group '{group}' 不存在，全部归入 unmatched");
                return new Dictionary<string, PCGGeometry>
                {
                    { "matched", new PCGGeometry() },
                    { "unmatched", geo.Clone() }
                };
            }

            var matchedPrimIndices = new HashSet<int>(groupSet);

            var matched = ExtractPrims(geo, matchedPrimIndices);
            var unmatchedIndices = new HashSet<int>();
            for (int i = 0; i < geo.Primitives.Count; i++)
                if (!matchedPrimIndices.Contains(i))
                    unmatchedIndices.Add(i);
            var unmatched = ExtractPrims(geo, unmatchedIndices);

            ctx.Log($"Split: group='{group}', matched={matched.Primitives.Count}, unmatched={unmatched.Primitives.Count}");
            return new Dictionary<string, PCGGeometry>
            {
                { "matched", matched },
                { "unmatched", unmatched }
            };
        }

        private PCGGeometry ExtractPrims(PCGGeometry source, HashSet<int> primIndices)
        {
            var result = new PCGGeometry();
            if (primIndices.Count == 0) return result;

            // 收集引用的点
            var usedPoints = new HashSet<int>();
            foreach (int pi in primIndices)
            {
                if (pi < source.Primitives.Count)
                    foreach (int vi in source.Primitives[pi])
                        usedPoints.Add(vi);
            }

            // 建立旧 -> 新索引映射
            var indexMap = new Dictionary<int, int>();
            foreach (int oldIdx in usedPoints.OrderBy(x => x))
            {
                indexMap[oldIdx] = result.Points.Count;
                result.Points.Add(source.Points[oldIdx]);
            }

            // 复制面
            foreach (int pi in primIndices)
            {
                if (pi >= source.Primitives.Count) continue;
                var prim = source.Primitives[pi];
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = indexMap[prim[i]];
                result.Primitives.Add(newPrim);
            }

            // 复制点属性
            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in usedPoints.OrderBy(x => x))
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            return result;
        }
    }
}
