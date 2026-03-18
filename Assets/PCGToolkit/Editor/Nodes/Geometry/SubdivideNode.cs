using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 细分几何体（对标 Houdini Subdivide SOP）
    /// </summary>
    public class SubdivideNode : PCGNodeBase
    {
        public override string Name => "Subdivide";
        public override string DisplayName => "Subdivide";
        public override string Description => "对几何体进行细分（Catmull-Clark 或 Linear）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "细分迭代次数", 1),
            new PCGParamSchema("algorithm", PCGPortDirection.Input, PCGPortType.String,
                "Algorithm", "细分算法（catmull-clark / linear）", "catmull-clark"),
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
            ctx.Log("Subdivide: 细分几何体 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            int iterations = GetParamInt(parameters, "iterations", 1);
            string algorithm = GetParamString(parameters, "algorithm", "catmull-clark");

            ctx.Log($"Subdivide: iterations={iterations}, algorithm={algorithm}");

            // TODO: 实现 Catmull-Clark / Linear 细分
            return SingleOutput("geometry", geo);
        }
    }
}
