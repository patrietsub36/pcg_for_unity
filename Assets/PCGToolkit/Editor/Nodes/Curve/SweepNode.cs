using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 沿曲线扫掠截面生成几何体（对标 Houdini Sweep SOP）
    /// </summary>
    public class SweepNode : PCGNodeBase
    {
        public override string Name => "Sweep";
        public override string DisplayName => "Sweep";
        public override string Description => "沿路径曲线扫掠截面形状生成几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("backbone", PCGPortDirection.Input, PCGPortType.Geometry,
                "Backbone", "路径曲线（骨架线）", null, required: true),
            new PCGParamSchema("crossSection", PCGPortDirection.Input, PCGPortType.Geometry,
                "Cross Section", "截面形状（可选，默认使用圆形）", null),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Float,
                "Scale", "截面缩放", 1.0f),
            new PCGParamSchema("twist", PCGPortDirection.Input, PCGPortType.Float,
                "Twist", "沿路径的扭转角度", 0f),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "截面分段数（无截面输入时使用）", 8),
            new PCGParamSchema("capEnds", PCGPortDirection.Input, PCGPortType.Bool,
                "Cap Ends", "封口两端", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "扫掠生成的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var backbone = GetInputGeometry(inputGeometries, "backbone");
            var crossSection = GetInputGeometry(inputGeometries, "crossSection");

            if (backbone.Points.Count < 2)
            {
                ctx.LogWarning("Sweep: 骨架线点数不足");
                return SingleOutput("geometry", new PCGGeometry());
            }

            float scale = GetParamFloat(parameters, "scale", 1.0f);
            float twist = GetParamFloat(parameters, "twist", 0f);
            int divisions = GetParamInt(parameters, "divisions", 8);
            bool capEnds = GetParamBool(parameters, "capEnds", true);

            var geo = new PCGGeometry();
            var points = new List<Vector3>();
            var primitives = new List<int[]>();

            // 生成默认圆形截面（如果没有提供截面）
            var sectionPoints = new List<Vector3>();
            if (crossSection != null && crossSection.Points.Count >= 3)
            {
                // 使用提供的截面（假设在 XZ 平面）
                foreach (var p in crossSection.Points)
                {
                    sectionPoints.Add(new Vector3(p.x, 0, p.z));
                }
            }
            else
            {
                // 生成圆形截面
                for (int i = 0; i < divisions; i++)
                {
                    float angle = (float)i / divisions * Mathf.PI * 2f;
                    sectionPoints.Add(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.5f);
                }
            }

            int sectionPointCount = sectionPoints.Count;

            // 沿骨架线的每个点放置截面
            for (int i = 0; i < backbone.Points.Count; i++)
            {
                Vector3 pos = backbone.Points[i];

                // 计算骨架线在该点的切向（方向）
                Vector3 tangent;
                if (i == 0)
                    tangent = (backbone.Points[1] - backbone.Points[0]).normalized;
                else if (i == backbone.Points.Count - 1)
                    tangent = (backbone.Points[i] - backbone.Points[i - 1]).normalized;
                else
                    tangent = (backbone.Points[i + 1] - backbone.Points[i - 1]).normalized;

                // 构建局部坐标系
                Vector3 up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(tangent, up)) > 0.99f)
                    up = Vector3.forward;

                Vector3 binormal = Vector3.Cross(tangent, up).normalized;
                Vector3 normal = Vector3.Cross(binormal, tangent).normalized;

                // 计算扭转角度
                float twistAngle = twist * ((float)i / (backbone.Points.Count - 1));
                Quaternion twistRotation = Quaternion.AngleAxis(twistAngle, tangent);

                // 计算缩放（可选：沿路径渐变）
                float s = scale;

                // 变换截面点
                for (int j = 0; j < sectionPointCount; j++)
                {
                    Vector3 localPoint = sectionPoints[j] * s;
                    localPoint = twistRotation * localPoint;

                    // 从局部坐标转换到世界坐标
                    Vector3 worldPoint = pos + binormal * localPoint.x + tangent * localPoint.y + normal * localPoint.z;
                    points.Add(worldPoint);
                }
            }

            // 连接相邻截面生成面
            for (int i = 0; i < backbone.Points.Count - 1; i++)
            {
                int baseIdx0 = i * sectionPointCount;
                int baseIdx1 = (i + 1) * sectionPointCount;

                for (int j = 0; j < sectionPointCount; j++)
                {
                    int next = (j + 1) % sectionPointCount;
                    // 四边形面（两个三角形）
                    primitives.Add(new int[] {
                        baseIdx0 + j, baseIdx0 + next, baseIdx1 + next
                    });
                    primitives.Add(new int[] {
                        baseIdx0 + j, baseIdx1 + next, baseIdx1 + j
                    });
                }
            }

            // 封口
            if (capEnds)
            {
                // 底部封口
                var bottomCap = new List<int>();
                for (int j = sectionPointCount - 1; j >= 0; j--)
                    bottomCap.Add(j);
                primitives.Add(bottomCap.ToArray());

                // 顶部封口
                int topBase = (backbone.Points.Count - 1) * sectionPointCount;
                var topCap = new List<int>();
                for (int j = 0; j < sectionPointCount; j++)
                    topCap.Add(topBase + j);
                primitives.Add(topCap.ToArray());
            }

            geo.Points = points;
            geo.Primitives = primitives;

            ctx.Log($"Sweep: backbone={backbone.Points.Count}, section={sectionPointCount}, output={points.Count}pts, {primitives.Count}faces");
            return SingleOutput("geometry", geo);
        }
    }
}