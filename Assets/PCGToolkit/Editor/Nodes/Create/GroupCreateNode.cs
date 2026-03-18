using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 创建分组（对标 Houdini GroupCreate SOP）
    /// </summary>
    public class GroupCreateNode : PCGNodeBase
    {
        public override string Name => "GroupCreate";
        public override string DisplayName => "Group Create";
        public override string Description => "创建或修改点/面分组";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                "Group Name", "分组名称", "group1"),
            new PCGParamSchema("groupType", PCGPortDirection.Input, PCGPortType.String,
                "Group Type", "分组类型（point/primitive）", "point"),
            new PCGParamSchema("filter", PCGPortDirection.Input, PCGPortType.String,
                "Filter", "过滤表达式（如 @P.y > 0）", ""),
            new PCGParamSchema("baseGroup", PCGPortDirection.Input, PCGPortType.String,
                "Base Group", "基于哪个已有分组进行过滤", ""),
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
            ctx.Log("GroupCreate: 创建分组 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupName = GetParamString(parameters, "groupName", "group1");
            string groupType = GetParamString(parameters, "groupType", "point");
            string filter = GetParamString(parameters, "filter", "");

            ctx.Log($"GroupCreate: name={groupName}, type={groupType}, filter={filter}");

            // TODO: 根据 filter 创建分组
            return SingleOutput("geometry", geo);
        }
    }
}
