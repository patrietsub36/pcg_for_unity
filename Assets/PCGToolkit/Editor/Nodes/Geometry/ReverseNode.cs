using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 翻转面法线（对标 Houdini Reverse SOP）
    /// </summary>
    public class ReverseNode : PCGNodeBase
    {
        public override string Name => "Reverse";
        public override string DisplayName => "Reverse";
        public override string Description => "翻转几何体的面法线方向（反转顶点顺序）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅翻转指定分组的面（留空=全部）", ""),
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
            ctx.Log("Reverse: 翻转面法线 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");

            ctx.Log($"Reverse: group={group}");

            // TODO: 反转 Primitives 中的顶点索引顺序
            return SingleOutput("geometry", geo);
        }
    }
}
