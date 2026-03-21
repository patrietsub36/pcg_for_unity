using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 在包围盒内按体素网格生成点（对标 Houdini Points from Volume）
    /// </summary>
    public class PointsFromVolumeNode : PCGNodeBase
    {
        public override string Name => "PointsFromVolume";
        public override string DisplayName => "Points From Volume";
        public override string Description => "在包围盒内按体素网格生成点";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（用于确定包围盒）", null, required: true),
            new PCGParamSchema("spacing", PCGPortDirection.Input, PCGPortType.Float,
                "Spacing", "体素间距", 0.5f),
            new PCGParamSchema("padding", PCGPortDirection.Input, PCGPortType.Float,
                "Padding", "包围盒外扩距离", 0f),
            new PCGParamSchema("jitter", PCGPortDirection.Input, PCGPortType.Float,
                "Jitter", "随机抖动量（0=无抖动）", 0f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "生成的点几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            float spacing = Mathf.Max(0.01f, GetParamFloat(parameters, "spacing", 0.5f));
            float padding = GetParamFloat(parameters, "padding", 0f);
            float jitter = GetParamFloat(parameters, "jitter", 0f);
            int seed = GetParamInt(parameters, "seed", 0);

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", new PCGGeometry());

            // 计算包围盒
            Vector3 min = geo.Points[0];
            Vector3 max = geo.Points[0];
            foreach (var p in geo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            min -= Vector3.one * padding;
            max += Vector3.one * padding;

            var result = new PCGGeometry();
            var rng = new System.Random(seed);

            int nx = Mathf.Max(1, Mathf.FloorToInt((max.x - min.x) / spacing) + 1);
            int ny = Mathf.Max(1, Mathf.FloorToInt((max.y - min.y) / spacing) + 1);
            int nz = Mathf.Max(1, Mathf.FloorToInt((max.z - min.z) / spacing) + 1);

            // 安全上限
            if ((long)nx * ny * nz > 1000000)
            {
                ctx.LogWarning("PointsFromVolume: 体素数量超过 1M 上限，请增大 spacing");
                return SingleOutput("geometry", result);
            }

            for (int ix = 0; ix < nx; ix++)
            {
                for (int iy = 0; iy < ny; iy++)
                {
                    for (int iz = 0; iz < nz; iz++)
                    {
                        Vector3 p = new Vector3(
                            min.x + ix * spacing,
                            min.y + iy * spacing,
                            min.z + iz * spacing
                        );

                        if (jitter > 0f)
                        {
                            p.x += (float)(rng.NextDouble() * 2 - 1) * jitter * spacing;
                            p.y += (float)(rng.NextDouble() * 2 - 1) * jitter * spacing;
                            p.z += (float)(rng.NextDouble() * 2 - 1) * jitter * spacing;
                        }

                        result.Points.Add(p);
                    }
                }
            }

            ctx.Log($"PointsFromVolume: {nx}x{ny}x{nz} grid, spacing={spacing}, {result.Points.Count} points");
            return SingleOutput("geometry", result);
        }
    }
}
