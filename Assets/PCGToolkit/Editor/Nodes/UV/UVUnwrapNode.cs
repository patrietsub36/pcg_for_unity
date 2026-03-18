using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 展开（对标 Houdini UVUnwrap / UVFlatten SOP）
    /// </summary>
    public class UVUnwrapNode : PCGNodeBase
    {
        public override string Name => "UVUnwrap";
        public override string DisplayName => "UV Unwrap";
        public override string Description => "自动展开几何体的 UV（使用 xatlas）";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组展开（留空=全部）", ""),
            new PCGParamSchema("maxStretch", PCGPortDirection.Input, PCGPortType.Float,
                "Max Stretch", "最大拉伸阈值", 0.5f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "图集分辨率", 1024),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带展开的 UV）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("UVUnwrap: UV 展开 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float maxStretch = GetParamFloat(parameters, "maxStretch", 0.5f);
            int resolution = GetParamInt(parameters, "resolution", 1024);

            ctx.Log($"UVUnwrap: maxStretch={maxStretch}, resolution={resolution}");

            // TODO: 调用 xatlas DLL 进行 UV 展开
            return SingleOutput("geometry", geo);
        }
    }
}
