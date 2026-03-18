using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 合并重叠顶点（对标 Houdini Fuse SOP）
    /// </summary>
    public class FuseNode : PCGNodeBase
    {
        public override string Name => "Fuse";
        public override string DisplayName => "Fuse";
        public override string Description => "合并距离阈值内的重叠顶点";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "合并距离阈值", 0.001f),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅处理指定分组（留空=全部）", ""),
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
            ctx.Log("Fuse: 合并重叠顶点 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float distance = GetParamFloat(parameters, "distance", 0.001f);

            ctx.Log($"Fuse: distance={distance}");

            // TODO: 找出距离 < threshold 的顶点对，合并为一个，更新 Primitives 索引
            return SingleOutput("geometry", geo);
        }
    }
}
