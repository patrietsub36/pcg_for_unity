using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 晶格变形（对标 Houdini Lattice SOP）
    /// 使用 FFD（Free-Form Deformation）算法
    /// </summary>
    public class LatticeNode : PCGNodeBase
    {
        public override string Name => "Lattice";
        public override string DisplayName => "Lattice";
        public override string Description => "使用晶格控制点对几何体进行自由变形（FFD）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("lattice", PCGPortDirection.Input, PCGPortType.Geometry,
                "Lattice", "变形后的晶格控制点", null),
            new PCGParamSchema("divisionsX", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions X", "X 方向晶格分段", 2),
            new PCGParamSchema("divisionsY", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions Y", "Y 方向晶格分段", 2),
            new PCGParamSchema("divisionsZ", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions Z", "Z 方向晶格分段", 2),
            new PCGParamSchema("deformX", PCGPortDirection.Input, PCGPortType.Float,
                "Deform X", "X 方向变形强度", 0.5f),
            new PCGParamSchema("deformY", PCGPortDirection.Input, PCGPortType.Float,
                "Deform Y", "Y 方向变形强度", 0.5f),
            new PCGParamSchema("deformZ", PCGPortDirection.Input, PCGPortType.Float,
                "Deform Z", "Z 方向变形强度", 0.5f),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            var latticeGeo = GetInputGeometry(inputGeometries, "lattice");

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Lattice: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            int divX = Mathf.Max(2, GetParamInt(parameters, "divisionsX", 2));
            int divY = Mathf.Max(2, GetParamInt(parameters, "divisionsY", 2));
            int divZ = Mathf.Max(2, GetParamInt(parameters, "divisionsZ", 2));
            float deformX = GetParamFloat(parameters, "deformX", 0.5f);
            float deformY = GetParamFloat(parameters, "deformY", 0.5f);
            float deformZ = GetParamFloat(parameters, "deformZ", 0.5f);

            // 计算输入几何体的包围盒
            Vector3 min = geo.Points[0];
            Vector3 max = geo.Points[0];
            foreach (var p in geo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 size = max - min;
            Vector3 center = (min + max) * 0.5f;

            // 确保有有效的包围盒
            if (size.x < 0.001f) size.x = 1f;
            if (size.y < 0.001f) size.y = 1f;
            if (size.z < 0.001f) size.z = 1f;

            // 生成变形晶格控制点（如果没有提供）
            Vector3[,,] latticePoints;
            Vector3[,,] restLatticePoints;

            int latticePointCount = (divX + 1) * (divY + 1) * (divZ + 1);

            if (latticeGeo != null && latticeGeo.Points.Count >= latticePointCount)
            {
                // 使用提供的晶格点
                latticePoints = new Vector3[divX + 1, divY + 1, divZ + 1];
                int idx = 0;
                for (int x = 0; x <= divX; x++)
                    for (int y = 0; y <= divY; y++)
                        for (int z = 0; z <= divZ; z++)
                            latticePoints[x, y, z] = latticeGeo.Points[idx++];

                // Rest lattice 是单位晶格
                restLatticePoints = GenerateRestLattice(divX, divY, divZ, min, max);
            }
            else
            {
                // 自动生成变形晶格（应用正弦波变形作为示例）
                restLatticePoints = GenerateRestLattice(divX, divY, divZ, min, max);
                latticePoints = new Vector3[divX + 1, divY + 1, divZ + 1];

                for (int x = 0; x <= divX; x++)
                {
                    for (int y = 0; y <= divY; y++)
                    {
                        for (int z = 0; z <= divZ; z++)
                        {
                            Vector3 restP = restLatticePoints[x, y, z];
                            Vector3 localPos = (restP - min);

                            // 应用变形
                            float nx = localPos.x / size.x; // 0~1
                            float ny = localPos.y / size.y;
                            float nz = localPos.z / size.z;

                            // 正弦波变形
                            float deformAmount = Mathf.Sin(nx * Mathf.PI) * deformX * size.x * 0.2f;
                            deformAmount += Mathf.Sin(ny * Mathf.PI) * deformY * size.y * 0.2f;
                            deformAmount += Mathf.Sin(nz * Mathf.PI) * deformZ * size.z * 0.2f;

                            latticePoints[x, y, z] = restP + Vector3.up * deformAmount;
                        }
                    }
                }
            }

            // FFD: 对每个点计算其在晶格中的参数坐标，然后插值
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i];

                // 计算参数坐标 (s, t, u) 在 [0, 1] 范围
                float s = Mathf.Clamp01((p.x - min.x) / size.x);
                float t = Mathf.Clamp01((p.y - min.y) / size.y);
                float u = Mathf.Clamp01((p.z - min.z) / size.z);

                // 三线性插值
                Vector3 deformedPoint = TrilinearInterpolate(latticePoints, divX, divY, divZ, s, t, u);

                geo.Points[i] = deformedPoint;
            }

            ctx.Log($"Lattice: divisions=({divX}, {divY}, {divZ}), input={geo.Points.Count}pts");
            return SingleOutput("geometry", geo);
        }

        private Vector3[,,] GenerateRestLattice(int divX, int divY, int divZ, Vector3 min, Vector3 max)
        {
            var lattice = new Vector3[divX + 1, divY + 1, divZ + 1];
            Vector3 size = max - min;

            for (int x = 0; x <= divX; x++)
            {
                for (int y = 0; y <= divY; y++)
                {
                    for (int z = 0; z <= divZ; z++)
                    {
                        float sx = (float)x / divX;
                        float sy = (float)y / divY;
                        float sz = (float)z / divZ;

                        lattice[x, y, z] = min + new Vector3(sx * size.x, sy * size.y, sz * size.z);
                    }
                }
            }

            return lattice;
        }

        private Vector3 TrilinearInterpolate(Vector3[,,] lattice, int divX, int divY, int divZ, float s, float t, float u)
        {
            // 计算晶格索引
            float fx = s * divX;
            float fy = t * divY;
            float fz = u * divZ;

            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            int z0 = Mathf.FloorToInt(fz);

            int x1 = Mathf.Min(x0 + 1, divX);
            int y1 = Mathf.Min(y0 + 1, divY);
            int z1 = Mathf.Min(z0 + 1, divZ);

            x0 = Mathf.Clamp(x0, 0, divX);
            y0 = Mathf.Clamp(y0, 0, divY);
            z0 = Mathf.Clamp(z0, 0, divZ);

            float dx = fx - x0;
            float dy = fy - y0;
            float dz = fz - z0;

            // 三线性插值
            Vector3 c000 = lattice[x0, y0, z0];
            Vector3 c100 = lattice[x1, y0, z0];
            Vector3 c010 = lattice[x0, y1, z0];
            Vector3 c110 = lattice[x1, y1, z0];
            Vector3 c001 = lattice[x0, y0, z1];
            Vector3 c101 = lattice[x1, y0, z1];
            Vector3 c011 = lattice[x0, y1, z1];
            Vector3 c111 = lattice[x1, y1, z1];

            Vector3 c00 = Vector3.Lerp(c000, c100, dx);
            Vector3 c01 = Vector3.Lerp(c001, c101, dx);
            Vector3 c10 = Vector3.Lerp(c010, c110, dx);
            Vector3 c11 = Vector3.Lerp(c011, c111, dx);

            Vector3 c0 = Vector3.Lerp(c00, c10, dy);
            Vector3 c1 = Vector3.Lerp(c01, c11, dy);

            return Vector3.Lerp(c0, c1, dz);
        }
    }
}