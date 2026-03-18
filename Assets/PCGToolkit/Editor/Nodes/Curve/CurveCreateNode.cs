using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 创建曲线（对标 Houdini Curve SOP）
    /// </summary>
    public class CurveCreateNode : PCGNodeBase
    {
        public override string Name => "CurveCreate";
        public override string DisplayName => "Curve Create";
        public override string Description => "创建贝塞尔/NURBS/多段线曲线";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("curveType", PCGPortDirection.Input, PCGPortType.String,
                "Curve Type", "曲线类型（bezier/nurbs/polyline）", "bezier"),
            new PCGParamSchema("order", PCGPortDirection.Input, PCGPortType.Int,
                "Order", "曲线阶数（对 Bezier/NURBS 有效）", 4),
            new PCGParamSchema("closed", PCGPortDirection.Input, PCGPortType.Bool,
                "Closed", "是否闭合曲线", false),
            new PCGParamSchema("pointCount", PCGPortDirection.Input, PCGPortType.Int,
                "Point Count", "控制点数量", 4),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "曲线几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("CurveCreate: 创建曲线 (TODO)");

            string curveType = GetParamString(parameters, "curveType", "bezier");
            int order = GetParamInt(parameters, "order", 4);
            bool closed = GetParamBool(parameters, "closed", false);
            int pointCount = GetParamInt(parameters, "pointCount", 4);

            ctx.Log($"CurveCreate: type={curveType}, order={order}, closed={closed}, points={pointCount}");

            // TODO: 生成曲线控制点，存储曲线类型信息到 Detail 属性
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
