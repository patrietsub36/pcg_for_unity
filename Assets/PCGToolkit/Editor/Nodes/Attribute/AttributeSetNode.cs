using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 设置/修改属性值（对标 Houdini AttribWrangle / AttribSet）
    /// </summary>
    public class AttributeSetNode : PCGNodeBase
    {
        public override string Name => "AttributeSet";
        public override string DisplayName => "Attribute Set";
        public override string Description => "设置或修改几何体上的属性值";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "属性名称", "Cd"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级（point/vertex/primitive/detail）", "point"),
            new PCGParamSchema("expression", PCGPortDirection.Input, PCGPortType.String,
                "Expression", "值表达式（如 @P.y, rand(@ptnum) 等）", ""),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组的元素进行设置", ""),
            new PCGParamSchema("valueFloat", PCGPortDirection.Input, PCGPortType.Float,
                "Value (Float)", "常量值（Float 类型，expression 为空时使用）", 0f),
            new PCGParamSchema("valueVector3", PCGPortDirection.Input, PCGPortType.Vector3,
                "Value (Vector3)", "常量值（Vector3 类型）", Vector3.zero),
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
            ctx.Log("AttributeSet: 设置属性 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string attrName = GetParamString(parameters, "name", "Cd");
            string attrClass = GetParamString(parameters, "class", "point");
            string expression = GetParamString(parameters, "expression", "");

            ctx.Log($"AttributeSet: name={attrName}, class={attrClass}, expression={expression}");

            // TODO: 解析表达式并设置属性值
            return SingleOutput("geometry", geo);
        }
    }
}
