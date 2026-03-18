using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 合并多个几何体为一个（对标 Houdini Merge SOP）
    /// </summary>
    public class MergeNode : PCGNodeBase
    {
        public override string Name => "Merge";
        public override string DisplayName => "Merge";
        public override string Description => "合并多个几何体为一个";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（支持多输入）", null, required: true, allowMultiple: true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "合并后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Merge: 合并几何体 (TODO)");

            // TODO: 合并所有输入几何体的 Points、Primitives、属性和分组
            // 需要处理索引偏移
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
