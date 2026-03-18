using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 实例化（对标 Houdini Instance SOP / 引用实例概念）
    /// </summary>
    public class InstanceNode : PCGNodeBase
    {
        public override string Name => "Instance";
        public override string DisplayName => "Instance";
        public override string Description => "将几何体标记为实例（共享几何数据，减少内存占用）";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("instancePath", PCGPortDirection.Input, PCGPortType.String,
                "Instance Path", "实例引用路径", ""),
            new PCGParamSchema("usePointInstancing", PCGPortDirection.Input, PCGPortType.Bool,
                "Point Instancing", "使用逐点实例化", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带实例标记）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Instance: 实例化 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string instancePath = GetParamString(parameters, "instancePath", "");

            ctx.Log($"Instance: path={instancePath}");

            // TODO: 在 Detail 属性中标记实例引用
            return SingleOutput("geometry", geo);
        }
    }
}
