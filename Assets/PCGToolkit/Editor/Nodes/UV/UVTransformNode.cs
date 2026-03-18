using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 变换（对标 Houdini UVTransform SOP）
    /// </summary>
    public class UVTransformNode : PCGNodeBase
    {
        public override string Name => "UVTransform";
        public override string DisplayName => "UV Transform";
        public override string Description => "对 UV 坐标进行平移、旋转、缩放";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("translate", PCGPortDirection.Input, PCGPortType.Vector3,
                "Translate", "UV 平移 (仅 xy 有效)", Vector3.zero),
            new PCGParamSchema("rotate", PCGPortDirection.Input, PCGPortType.Float,
                "Rotate", "UV 旋转角度", 0f),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "UV 缩放 (仅 xy 有效)", Vector3.one),
            new PCGParamSchema("pivot", PCGPortDirection.Input, PCGPortType.Vector3,
                "Pivot", "变换枢轴 (仅 xy 有效)", new Vector3(0.5f, 0.5f, 0f)),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅变换指定分组的 UV（留空=全部）", ""),
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
            ctx.Log("UVTransform: UV 变换 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float rotate = GetParamFloat(parameters, "rotate", 0f);

            ctx.Log($"UVTransform: rotate={rotate}");

            // TODO: 对 UV 属性执行 2D 变换
            return SingleOutput("geometry", geo);
        }
    }
}
