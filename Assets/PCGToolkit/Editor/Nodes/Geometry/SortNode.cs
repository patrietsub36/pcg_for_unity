using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 排序点/面顺序（对标 Houdini Sort SOP）
    /// </summary>
    public class SortNode : PCGNodeBase
    {
        public override string Name => "Sort";
        public override string DisplayName => "Sort";
        public override string Description => "按指定规则排序点或面的顺序";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("key", PCGPortDirection.Input, PCGPortType.String,
                "Key", "排序依据（x/y/z/random/attribute）", "y"),
            new PCGParamSchema("reverse", PCGPortDirection.Input, PCGPortType.Bool,
                "Reverse", "降序排列", false),
            new PCGParamSchema("pointSort", PCGPortDirection.Input, PCGPortType.Bool,
                "Sort Points", "是否排序点", true),
            new PCGParamSchema("primSort", PCGPortDirection.Input, PCGPortType.Bool,
                "Sort Primitives", "是否排序面", false),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子（当 key=random 时使用）", 0),
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
            ctx.Log("Sort: 排序元素 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string key = GetParamString(parameters, "key", "y");
            bool reverse = GetParamBool(parameters, "reverse", false);

            ctx.Log($"Sort: key={key}, reverse={reverse}");

            // TODO: 按指定规则对 Points/Primitives 重排序
            return SingleOutput("geometry", geo);
        }
    }
}
