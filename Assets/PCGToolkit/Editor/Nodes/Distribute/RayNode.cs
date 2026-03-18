using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 射线投射/投影（对标 Houdini Ray SOP）
    /// </summary>
    public class RayNode : PCGNodeBase
    {
        public override string Name => "Ray";
        public override string DisplayName => "Ray";
        public override string Description => "将几何体的点沿射线方向投影到目标表面";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "要投影的几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target", "目标表面几何体", null, required: true),
            new PCGParamSchema("direction", PCGPortDirection.Input, PCGPortType.Vector3,
                "Direction", "射线方向（留空则使用点法线）", Vector3.down),
            new PCGParamSchema("maxDistance", PCGPortDirection.Input, PCGPortType.Float,
                "Max Distance", "最大投射距离", 100f),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅投影指定分组的点", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "投影后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Ray: 射线投影 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            var target = GetInputGeometry(inputGeometries, "target");
            Vector3 direction = GetParamVector3(parameters, "direction", Vector3.down);
            float maxDistance = GetParamFloat(parameters, "maxDistance", 100f);

            ctx.Log($"Ray: direction={direction}, maxDistance={maxDistance}");

            // TODO: 对每个点执行射线检测，投影到目标表面
            return SingleOutput("geometry", geo);
        }
    }
}
