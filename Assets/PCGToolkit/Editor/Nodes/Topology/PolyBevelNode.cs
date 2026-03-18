using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 多边形倒角（对标 Houdini PolyBevel SOP）
    /// </summary>
    public class PolyBevelNode : PCGNodeBase
    {
        public override string Name => "PolyBevel";
        public override string DisplayName => "Poly Bevel";
        public override string Description => "对多边形的边或顶点进行倒角";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Float,
                "Offset", "倒角偏移距离", 0.1f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "倒角分段数", 1),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "倒角模式（edges/vertices）", "edges"),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组倒角", ""),
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
            ctx.Log("PolyBevel: 多边形倒角 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float offset = GetParamFloat(parameters, "offset", 0.1f);
            int divisions = GetParamInt(parameters, "divisions", 1);
            string mode = GetParamString(parameters, "mode", "edges");

            ctx.Log($"PolyBevel: offset={offset}, divisions={divisions}, mode={mode}");

            // TODO: 在选中的边/顶点处生成倒角几何
            return SingleOutput("geometry", geo);
        }
    }
}
