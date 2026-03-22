using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 通用噪声变形（对标 Houdini Mountain SOP 增强版）
    /// 支持 Perlin / Simplex / Worley / Curl 噪声类型，
    /// 比 MountainNode 更灵活：可选变形方向（法线/自定义轴/各轴独立）。
    /// </summary>
    public class NoiseNode : PCGNodeBase
    {
        public override string Name => "Noise";
        public override string DisplayName => "Noise";
        public override string Description => "通用噪声变形（Perlin/Worley/Curl）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("noiseType", PCGPortDirection.Input, PCGPortType.String,
                "Noise Type", "噪声类型（perlin/worley/curl）", "perlin")
            {
                EnumOptions = new[] { "perlin", "worley", "curl" }
            },
            new PCGParamSchema("amplitude", PCGPortDirection.Input, PCGPortType.Float,
                "Amplitude", "噪声振幅", 0.5f),
            new PCGParamSchema("frequency", PCGPortDirection.Input, PCGPortType.Float,
                "Frequency", "噪声频率", 1f),
            new PCGParamSchema("octaves", PCGPortDirection.Input, PCGPortType.Int,
                "Octaves", "叠加层数", 3),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Vector3,
                "Offset", "噪声空间偏移", Vector3.zero),
            new PCGParamSchema("direction", PCGPortDirection.Input, PCGPortType.String,
                "Direction", "变形方向（normal/axis/3d）", "normal")
            {
                EnumOptions = new[] { "normal", "axis", "3d" }
            },
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.Vector3,
                "Axis", "axis 模式下的变形轴向", Vector3.up),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定点分组变形", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string noiseType = GetParamString(parameters, "noiseType", "perlin").ToLower();
            float amplitude = GetParamFloat(parameters, "amplitude", 0.5f);
            float frequency = GetParamFloat(parameters, "frequency", 1f);
            int octaves = Mathf.Clamp(GetParamInt(parameters, "octaves", 3), 1, 8);
            Vector3 offset = GetParamVector3(parameters, "offset", Vector3.zero);
            string direction = GetParamString(parameters, "direction", "normal").ToLower();
            Vector3 axis = GetParamVector3(parameters, "axis", Vector3.up).normalized;
            string group = GetParamString(parameters, "group", "");

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 计算顶点法线（用于 normal 模式）
            Vector3[] normals = null;
            if (direction == "normal")
                normals = ComputeVertexNormals(geo);

            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group) && geo.PointGroups.TryGetValue(group, out var grp))
                indices = grp;

            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (indices != null && !indices.Contains(i)) continue;

                Vector3 p = geo.Points[i];
                Vector3 samplePos = (p + offset) * frequency;

                Vector3 displacement;
                switch (noiseType)
                {
                    case "worley":
                        float wn = WorleyNoise(samplePos, octaves) * amplitude;
                        displacement = GetDirection(direction, normals, axis, i) * wn;
                        break;
                    case "curl":
                        displacement = CurlNoise(samplePos, octaves) * amplitude;
                        break;
                    default: // perlin
                        if (direction == "3d")
                        {
                            displacement = new Vector3(
                                FBM(samplePos.x, samplePos.y, octaves),
                                FBM(samplePos.y, samplePos.z, octaves),
                                FBM(samplePos.z, samplePos.x, octaves)
                            ) * amplitude;
                        }
                        else
                        {
                            float n = FBM(samplePos.x, samplePos.z, octaves) * amplitude;
                            displacement = GetDirection(direction, normals, axis, i) * n;
                        }
                        break;
                }

                geo.Points[i] = p + displacement;
            }

            return SingleOutput("geometry", geo);
        }

        private Vector3 GetDirection(string dir, Vector3[] normals, Vector3 axis, int idx)
        {
            if (dir == "normal" && normals != null && idx < normals.Length)
                return normals[idx];
            return axis;
        }

        private Vector3[] ComputeVertexNormals(PCGGeometry geo)
        {
            var n = new Vector3[geo.Points.Count];
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length < 3) continue;
                Vector3 fn = Vector3.Cross(
                    geo.Points[prim[1]] - geo.Points[prim[0]],
                    geo.Points[prim[2]] - geo.Points[prim[0]]);
                foreach (int idx in prim) n[idx] += fn;
            }
            for (int i = 0; i < n.Length; i++)
                n[i] = n[i].sqrMagnitude > 1e-6f ? n[i].normalized : Vector3.up;
            return n;
        }

        private static float FBM(float x, float y, int octaves)
        {
            float val = 0, amp = 1, freq = 1, max = 0;
            for (int i = 0; i < octaves; i++)
            {
                val += (Mathf.PerlinNoise(x * freq + 100, y * freq + 100) * 2f - 1f) * amp;
                max += amp; amp *= 0.5f; freq *= 2f;
            }
            return val / max;
        }

        private static float WorleyNoise(Vector3 p, int octaves)
        {
            float val = 0, amp = 1, freq = 1, max = 0;
            for (int i = 0; i < octaves; i++)
            {
                val += WorleySingle(p * freq) * amp;
                max += amp; amp *= 0.5f; freq *= 2f;
            }
            return val / max;
        }

        private static float WorleySingle(Vector3 p)
        {
            Vector3 cell = new Vector3(Mathf.Floor(p.x), Mathf.Floor(p.y), Mathf.Floor(p.z));
            float minDist = 10f;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        Vector3 neighbor = cell + new Vector3(dx, dy, dz);
                        Vector3 point = neighbor + Hash3(neighbor);
                        float dist = (p - point).sqrMagnitude;
                        if (dist < minDist) minDist = dist;
                    }
            return Mathf.Sqrt(minDist);
        }

        private static Vector3 CurlNoise(Vector3 p, int octaves)
        {
            float e = 0.01f;
            float nx = FBM(p.y + e, p.z, octaves) - FBM(p.y - e, p.z, octaves);
            float ny = FBM(p.z + e, p.x, octaves) - FBM(p.z - e, p.x, octaves);
            float nz = FBM(p.x + e, p.y, octaves) - FBM(p.x - e, p.y, octaves);
            return new Vector3(nx, ny, nz) / (2f * e);
        }

        private static Vector3 Hash3(Vector3 p)
        {
            float x = Mathf.Sin(Vector3.Dot(p, new Vector3(127.1f, 311.7f, 74.7f))) * 43758.5453f;
            float y = Mathf.Sin(Vector3.Dot(p, new Vector3(269.5f, 183.3f, 246.1f))) * 43758.5453f;
            float z = Mathf.Sin(Vector3.Dot(p, new Vector3(113.5f, 271.9f, 124.6f))) * 43758.5453f;
            return new Vector3(x - Mathf.Floor(x), y - Mathf.Floor(y), z - Mathf.Floor(z));
        }
    }
}
