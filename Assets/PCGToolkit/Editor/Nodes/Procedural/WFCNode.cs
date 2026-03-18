using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// 波函数坍缩（Wave Function Collapse）程序化生成
    /// </summary>
    public class WFCNode : PCGNodeBase
    {
        public override string Name => "WFC";
        public override string DisplayName => "WFC (Wave Function Collapse)";
        public override string Description => "使用波函数坍缩算法进行程序化内容生成";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("tileSet", PCGPortDirection.Input, PCGPortType.Geometry,
                "Tile Set", "瓦片集几何体（每个 Primitive Group 为一种瓦片）", null, required: true),
            new PCGParamSchema("gridSizeX", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size X", "网格 X 方向大小", 10),
            new PCGParamSchema("gridSizeY", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size Y", "网格 Y 方向大小", 10),
            new PCGParamSchema("gridSizeZ", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size Z", "网格 Z 方向大小（2D 时为 1）", 1),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("maxAttempts", PCGPortDirection.Input, PCGPortType.Int,
                "Max Attempts", "最大尝试次数（回溯失败后重试）", 10),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "生成的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("WFC: 波函数坍缩 (TODO)");

            int gridSizeX = GetParamInt(parameters, "gridSizeX", 10);
            int gridSizeY = GetParamInt(parameters, "gridSizeY", 10);
            int gridSizeZ = GetParamInt(parameters, "gridSizeZ", 1);
            int seed = GetParamInt(parameters, "seed", 0);

            ctx.Log($"WFC: grid=({gridSizeX}, {gridSizeY}, {gridSizeZ}), seed={seed}");

            // TODO: 实现 WFC 算法 —— 初始化约束、传播、坍缩、回溯
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
