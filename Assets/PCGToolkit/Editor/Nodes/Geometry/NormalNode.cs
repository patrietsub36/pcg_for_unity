using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 计算/设置法线（对标 Houdini Normal SOP）
    /// </summary>
    public class NormalNode : PCGNodeBase
    {
        public override string Name => "Normal";
        public override string DisplayName => "Normal";
        public override string Description => "重新计算几何体的法线";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "法线计算类型（point/vertex/primitive）", "point"),
            new PCGParamSchema("cuspAngle", PCGPortDirection.Input, PCGPortType.Float,
                "Cusp Angle", "锐角阈值（超过此角度的边将产生硬边法线）", 60f),
            new PCGParamSchema("weightByArea", PCGPortDirection.Input, PCGPortType.Bool,
                "Weight by Area", "是否按面积加权", true),
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
            ctx.Log("Normal: 计算法线 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string type = GetParamString(parameters, "type", "point");
            float cuspAngle = GetParamFloat(parameters, "cuspAngle", 60f);

            ctx.Log($"Normal: type={type}, cuspAngle={cuspAngle}");

            // TODO: 计算面法线，然后根据 cuspAngle 决定顶点法线是平滑还是硬边
            return SingleOutput("geometry", geo);
        }
    }
}
