using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 用平面裁剪几何体（对标 Houdini Clip SOP）
    /// </summary>
    public class ClipNode : PCGNodeBase
    {
        public override string Name => "Clip";
        public override string DisplayName => "Clip";
        public override string Description => "用一个平面裁剪几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "裁剪平面原点", Vector3.zero),
            new PCGParamSchema("normal", PCGPortDirection.Input, PCGPortType.Vector3,
                "Normal", "裁剪平面法线", Vector3.up),
            new PCGParamSchema("keepAbove", PCGPortDirection.Input, PCGPortType.Bool,
                "Keep Above", "保留法线方向侧", true),
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
            ctx.Log("Clip: 平面裁剪 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 normal = GetParamVector3(parameters, "normal", Vector3.up);
            bool keepAbove = GetParamBool(parameters, "keepAbove", true);

            ctx.Log($"Clip: origin={origin}, normal={normal}, keepAbove={keepAbove}");

            // TODO: 用平面方程 dot(P - origin, normal) 判断每个顶点的侧面
            // 裁剪穿过平面的面并生成新的切面边
            return SingleOutput("geometry", geo);
        }
    }
}
