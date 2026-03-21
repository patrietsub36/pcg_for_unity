using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 高度场网格生成（对标 Houdini HeightField SOP）
    /// 生成 XZ 平面网格并用噪声初始化 height 属性和 Y 坐标。
    /// </summary>
    public class HeightfieldNode : PCGNodeBase
    {
        public override string Name => "Heightfield";
        public override string DisplayName => "Heightfield";
        public override string Description => "生成带 height 属性的噪声网格";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("sizeX", PCGPortDirection.Input, PCGPortType.Float,
                "Size X", "网格 X 方向大小", 10f),
            new PCGParamSchema("sizeZ", PCGPortDirection.Input, PCGPortType.Float,
                "Size Z", "网格 Z 方向大小", 10f),
            new PCGParamSchema("resX", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution X", "X 方向分段数", 32),
            new PCGParamSchema("resZ", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution Z", "Z 方向分段数", 32),
            new PCGParamSchema("amplitude", PCGPortDirection.Input, PCGPortType.Float,
                "Amplitude", "噪声振幅", 1f),
            new PCGParamSchema("frequency", PCGPortDirection.Input, PCGPortType.Float,
                "Frequency", "噪声频率", 0.5f),
            new PCGParamSchema("octaves", PCGPortDirection.Input, PCGPortType.Int,
                "Octaves", "噪声叠加层数", 4),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子偏移", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "高度场网格"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            float sizeX = GetParamFloat(parameters, "sizeX", 10f);
            float sizeZ = GetParamFloat(parameters, "sizeZ", 10f);
            int resX = Mathf.Max(2, GetParamInt(parameters, "resX", 32));
            int resZ = Mathf.Max(2, GetParamInt(parameters, "resZ", 32));
            float amplitude = GetParamFloat(parameters, "amplitude", 1f);
            float frequency = GetParamFloat(parameters, "frequency", 0.5f);
            int octaves = Mathf.Clamp(GetParamInt(parameters, "octaves", 4), 1, 8);
            int seed = GetParamInt(parameters, "seed", 0);

            var geo = new PCGGeometry();
            var heightAttr = geo.PointAttribs.CreateAttribute("height", AttribType.Float, 0f);

            float halfX = sizeX * 0.5f;
            float halfZ = sizeZ * 0.5f;

            // 生成顶点
            for (int z = 0; z <= resZ; z++)
            {
                for (int x = 0; x <= resX; x++)
                {
                    float px = -halfX + (float)x / resX * sizeX;
                    float pz = -halfZ + (float)z / resZ * sizeZ;

                    float h = FBMNoise(px * frequency + seed, pz * frequency + seed, octaves) * amplitude;

                    geo.Points.Add(new Vector3(px, h, pz));
                    heightAttr.Values.Add(h);
                }
            }

            // 生成四边形面
            for (int z = 0; z < resZ; z++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int i00 = z * (resX + 1) + x;
                    int i10 = i00 + 1;
                    int i01 = i00 + (resX + 1);
                    int i11 = i01 + 1;
                    geo.Primitives.Add(new[] { i00, i10, i11, i01 });
                }
            }

            ctx.Log($"Heightfield: {resX}x{resZ}, amp={amplitude}, freq={frequency}, {geo.Points.Count} pts");
            return SingleOutput("geometry", geo);
        }

        private static float FBMNoise(float x, float z, int octaves)
        {
            float value = 0f;
            float amp = 1f;
            float freq = 1f;
            float maxVal = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += PerlinNoise(x * freq, z * freq) * amp;
                maxVal += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return value / maxVal;
        }

        private static float PerlinNoise(float x, float z)
        {
            return Mathf.PerlinNoise(x + 100f, z + 100f) * 2f - 1f;
        }
    }
}
