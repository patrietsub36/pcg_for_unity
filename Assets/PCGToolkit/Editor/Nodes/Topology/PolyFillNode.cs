using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 填充孔洞（对标 Houdini PolyFill / PolyCap SOP）
    /// </summary>
    public class PolyFillNode : PCGNodeBase
    {
        public override string Name => "PolyFill";
        public override string DisplayName => "Poly Fill";
        public override string Description => "填充几何体上的孔洞/开放边界";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "指定要填充的边界边分组（留空=自动检测所有孔洞）", ""),
            new PCGParamSchema("fillMode", PCGPortDirection.Input, PCGPortType.String,
                "Fill Mode", "填充模式（single/fan/grid）", "fan"),
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
            ctx.Log("PolyFill: 填充孔洞 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string fillMode = GetParamString(parameters, "fillMode", "fan");

            ctx.Log($"PolyFill: fillMode={fillMode}");

            // TODO: 检测边界边环，用三角形/扇形/网格填充
            return SingleOutput("geometry", geo);
        }
    }
}
