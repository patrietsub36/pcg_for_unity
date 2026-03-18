using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 删除指定元素（对标 Houdini Blast SOP）
    /// </summary>
    public class BlastNode : PCGNodeBase
    {
        public override string Name => "Blast";
        public override string DisplayName => "Blast";
        public override string Description => "按分组或编号删除点/面";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要删除的分组名或编号列表", ""),
            new PCGParamSchema("groupType", PCGPortDirection.Input, PCGPortType.String,
                "Group Type", "分组类型（point/primitive）", "primitive"),
            new PCGParamSchema("deleteNonSelected", PCGPortDirection.Input, PCGPortType.Bool,
                "Delete Non-Selected", "反转选择", false),
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
            ctx.Log("Blast: 删除元素 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");
            string groupType = GetParamString(parameters, "groupType", "primitive");
            bool deleteNonSelected = GetParamBool(parameters, "deleteNonSelected", false);

            ctx.Log($"Blast: group={group}, groupType={groupType}, deleteNonSelected={deleteNonSelected}");

            // TODO: 解析 group 表达式，删除对应元素
            return SingleOutput("geometry", geo);
        }
    }
}
