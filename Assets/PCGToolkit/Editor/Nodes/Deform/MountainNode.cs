using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 噪声变形（对标 Houdini Mountain SOP）
    /// </summary>
    public class MountainNode : PCGNodeBase
    {
        public override string Name => "Mountain";
        public override string DisplayName => "Mountain";
        public override string Description => "用噪声函数对几何体进行变形（产生山脉/地形效果）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("height", PCGPortDirection.Input, PCGPortType.Float,
                "Height", "噪声高度/振幅", 1.0f),
            new PCGParamSchema("frequency", PCGPortDirection.Input, PCGPortType.Float,
                "Frequency", "噪声频率", 1.0f),
            new PCGParamSchema("octaves", PCGPortDirection.Input, PCGPortType.Int,
                "Octaves", "分形叠加层数", 4),
            new PCGParamSchema("lacunarity", PCGPortDirection.Input, PCGPortType.Float,
                "Lacunarity", "频率递增倍数", 2.0f),
            new PCGParamSchema("persistence", PCGPortDirection.Input, PCGPortType.Float,
                "Persistence", "振幅递减比例", 0.5f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("noiseType", PCGPortDirection.Input, PCGPortType.String,
                "Noise Type", "噪声类型（perlin/simplex/value）", "perlin"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "变形后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("Mountain: 噪声变形 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float height = GetParamFloat(parameters, "height", 1.0f);
            float frequency = GetParamFloat(parameters, "frequency", 1.0f);
            int octaves = GetParamInt(parameters, "octaves", 4);

            ctx.Log($"Mountain: height={height}, frequency={frequency}, octaves={octaves}");

            // TODO: 对每个点采样噪声值，沿法线方向偏移
            return SingleOutput("geometry", geo);
        }
    }
}
