using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 生成球体几何体（对标 Houdini Sphere SOP）
    /// </summary>
    public class SphereNode : PCGNodeBase
    {
        public override string Name => "Sphere";
        public override string DisplayName => "Sphere";
        public override string Description => "生成一个球体几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "球体半径", 0.5f),
            new PCGParamSchema("rows", PCGPortDirection.Input, PCGPortType.Int,
                "Rows", "纬度方向的分段数", 16),
            new PCGParamSchema("columns", PCGPortDirection.Input, PCGPortType.Int,
                "Columns", "经度方向的分段数", 32),
            new PCGParamSchema("center", PCGPortDirection.Input, PCGPortType.Vector3,
                "Center", "中心位置", Vector3.zero),
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
            ctx.Log("Sphere: 生成球体 (TODO)");

            float radius = GetParamFloat(parameters, "radius", 0.5f);
            int rows = GetParamInt(parameters, "rows", 16);
            int columns = GetParamInt(parameters, "columns", 32);
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            ctx.Log($"Sphere: radius={radius}, rows={rows}, columns={columns}, center={center}");

            var geo = new PCGGeometry();
            // TODO: 生成球体顶点和面
            return SingleOutput("geometry", geo);
        }
    }
}
