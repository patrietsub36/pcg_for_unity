using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 多边形桥接（对标 Houdini PolyBridge SOP）
    /// </summary>
    public class PolyBridgeNode : PCGNodeBase
    {
        public override string Name => "PolyBridge";
        public override string DisplayName => "Poly Bridge";
        public override string Description => "在两组边界边之间创建桥接面";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupA", PCGPortDirection.Input, PCGPortType.String,
                "Group A", "第一组边界边分组", ""),
            new PCGParamSchema("groupB", PCGPortDirection.Input, PCGPortType.String,
                "Group B", "第二组边界边分组", ""),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "桥接方向的分段数", 1),
            new PCGParamSchema("magnitude", PCGPortDirection.Input, PCGPortType.Float,
                "Magnitude", "桥接曲率幅度", 0f),
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
            ctx.Log("PolyBridge: 多边形桥接 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupA = GetParamString(parameters, "groupA", "");
            string groupB = GetParamString(parameters, "groupB", "");
            int divisions = GetParamInt(parameters, "divisions", 1);

            ctx.Log($"PolyBridge: groupA={groupA}, groupB={groupB}, divisions={divisions}");

            // TODO: 找到两组边界环，创建连接面
            return SingleOutput("geometry", geo);
        }
    }
}
