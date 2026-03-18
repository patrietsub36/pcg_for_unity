using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 布尔运算模式
    /// </summary>
    public enum BooleanOperation
    {
        Union,
        Intersect,
        Subtract
    }

    /// <summary>
    /// 布尔运算（对标 Houdini Boolean SOP）
    /// </summary>
    public class BooleanNode : PCGNodeBase
    {
        public override string Name => "Boolean";
        public override string DisplayName => "Boolean";
        public override string Description => "对两个几何体执行布尔运算（并集/交集/差集）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("inputA", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input A", "第一个几何体", null, required: true),
            new PCGParamSchema("inputB", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input B", "第二个几何体", null, required: true),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "布尔运算类型（union/intersect/subtract）", "union"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "运算结果几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Boolean: 布尔运算 (TODO)");

            var geoA = GetInputGeometry(inputGeometries, "inputA");
            var geoB = GetInputGeometry(inputGeometries, "inputB");
            string operation = GetParamString(parameters, "operation", "union");

            ctx.Log($"Boolean: operation={operation}, A.points={geoA.Points.Count}, B.points={geoB.Points.Count}");

            // TODO: 实现 3D 布尔运算（使用 geometry3Sharp 或自实现 CSG）
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
