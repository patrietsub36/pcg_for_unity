using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 删除元素类型枚举
    /// </summary>
    public enum DeleteEntityType
    {
        Points,
        Primitives,
        Edges
    }

    /// <summary>
    /// 删除几何体中的元素（对标 Houdini Delete SOP）
    /// </summary>
    public class DeleteNode : PCGNodeBase
    {
        public override string Name => "Delete";
        public override string DisplayName => "Delete";
        public override string Description => "删除几何体中的点、面或边";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要删除的分组名（留空则用 filter）", ""),
            new PCGParamSchema("filter", PCGPortDirection.Input, PCGPortType.String,
                "Filter", "过滤表达式（如 @P.y > 0）", ""),
            new PCGParamSchema("deleteNonSelected", PCGPortDirection.Input, PCGPortType.Bool,
                "Delete Non-Selected", "反转选择（删除未选中的元素）", false),
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
            ctx.Log("Delete: 删除几何元素 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");
            string filter = GetParamString(parameters, "filter", "");
            bool deleteNonSelected = GetParamBool(parameters, "deleteNonSelected", false);

            ctx.Log($"Delete: group={group}, filter={filter}, deleteNonSelected={deleteNonSelected}");

            // TODO: 根据 group 或 filter 选择元素并删除
            return SingleOutput("geometry", geo);
        }
    }
}
