using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 创建新属性（对标 Houdini AttribCreate SOP）
    /// </summary>
    public class AttributeCreateNode : PCGNodeBase
    {
        public override string Name => "AttributeCreate";
        public override string DisplayName => "Attribute Create";
        public override string Description => "在几何体上创建新属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "属性名称", "Cd"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级（point/vertex/primitive/detail）", "point"),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "数据类型（float/int/vector3/vector4/color/string）", "float"),
            new PCGParamSchema("defaultFloat", PCGPortDirection.Input, PCGPortType.Float,
                "Default (Float)", "默认值（Float 类型）", 0f),
            new PCGParamSchema("defaultVector3", PCGPortDirection.Input, PCGPortType.Vector3,
                "Default (Vector3)", "默认值（Vector3 类型）", Vector3.zero),
            new PCGParamSchema("defaultString", PCGPortDirection.Input, PCGPortType.String,
                "Default (String)", "默认值（String 类型）", ""),
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
            ctx.Log("AttributeCreate: 创建属性 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string attrName = GetParamString(parameters, "name", "Cd");
            string attrClass = GetParamString(parameters, "class", "point");
            string attrType = GetParamString(parameters, "type", "float");

            ctx.Log($"AttributeCreate: name={attrName}, class={attrClass}, type={attrType}");

            // TODO: 在指定层级创建属性，并为所有元素填入默认值
            return SingleOutput("geometry", geo);
        }
    }
}
