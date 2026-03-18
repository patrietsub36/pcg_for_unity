using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 测量几何属性（对标 Houdini Measure SOP）
    /// </summary>
    public class MeasureNode : PCGNodeBase
    {
        public override string Name => "Measure";
        public override string DisplayName => "Measure";
        public override string Description => "测量面积、周长、曲率等几何属性并写入属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "测量类型（area/perimeter/curvature/volume）", "area"),
            new PCGParamSchema("attribName", PCGPortDirection.Input, PCGPortType.String,
                "Attribute Name", "存储结果的属性名", "area"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带测量属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Measure: 测量几何属性 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string type = GetParamString(parameters, "type", "area");
            string attribName = GetParamString(parameters, "attribName", "area");

            ctx.Log($"Measure: type={type}, attribName={attribName}");

            // TODO: 计算指定测量值并写入属性
            return SingleOutput("geometry", geo);
        }
    }
}
