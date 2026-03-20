using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Curve
{
    /// <summary>
    /// 创建曲线（对标 Houdini Curve SOP）
    /// </summary>
    public class CurveCreateNode : PCGNodeBase
    {
        public override string Name => "CurveCreate";
        public override string DisplayName => "Curve Create";
        public override string Description => "创建贝塞尔/多段线曲线";
        public override PCGNodeCategory Category => PCGNodeCategory.Curve;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("curveType", PCGPortDirection.Input, PCGPortType.String,
                "Curve Type", "曲线类型（bezier/polyline）", "polyline"),
            new PCGParamSchema("closed", PCGPortDirection.Input, PCGPortType.Bool,
                "Closed", "是否闭合曲线", false),
            new PCGParamSchema("pointCount", PCGPortDirection.Input, PCGPortType.Int,
                "Point Count", "控制点数量", 4),
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "控制点分布半径", 1.0f),
            new PCGParamSchema("height", PCGPortDirection.Input, PCGPortType.Float,
                "Height", "Y轴高度", 0.0f),
            new PCGParamSchema("shape", PCGPortDirection.Input, PCGPortType.String,
                "Shape", "形状（circle/line/spiral/random）", "circle"),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子（shape=random时）", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "曲线几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string curveType = GetParamString(parameters, "curveType", "polyline").ToLower();
            bool closed = GetParamBool(parameters, "closed", false);
            int pointCount = Mathf.Max(2, GetParamInt(parameters, "pointCount", 4));
            float radius = GetParamFloat(parameters, "radius", 1.0f);
            float height = GetParamFloat(parameters, "height", 0.0f);
            string shape = GetParamString(parameters, "shape", "circle").ToLower();
            int seed = GetParamInt(parameters, "seed", 0);

            var geo = new PCGGeometry();
            var points = new List<Vector3>();

            // 根据形状生成控制点
            switch (shape)
            {
                case "line":
                    // 直线
                    for (int i = 0; i < pointCount; i++)
                    {
                        float t = pointCount > 1 ? (float)i / (pointCount - 1) : 0f;
                        points.Add(new Vector3(t * radius * 2f - radius, height, 0f));
                    }
                    break;

                case "spiral":
                    // 螺旋线
                    for (int i = 0; i < pointCount; i++)
                    {
                        float t = pointCount > 1 ? (float)i / (pointCount - 1) : 0f;
                        float angle = t * Mathf.PI * 2f * 2f; // 两圈
                        float r = t * radius;
                        float h = t * height * 2f;
                        points.Add(new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r));
                    }
                    break;

                case "random":
                    // 随机点
                    var rng = new System.Random(seed);
                    for (int i = 0; i < pointCount; i++)
                    {
                        float t = (float)i / pointCount;
                        float x = (float)(rng.NextDouble() * 2 - 1) * radius;
                        float z = (float)(rng.NextDouble() * 2 - 1) * radius;
                        points.Add(new Vector3(x, height, z));
                    }
                    break;

                default: // circle
                    // 圆形
                    for (int i = 0; i < pointCount; i++)
                    {
                        float angle = (float)i / pointCount * Mathf.PI * 2f;
                        points.Add(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
                    }
                    break;
            }

            // 闭合曲线时添加属性标记
            if (closed && points.Count > 0)
            {
                // 闭合：最后一个点连回第一个点
                geo.DetailAttribs.SetAttribute("closed", true);
            }

            geo.Points = points;

            // 存储曲线类型到 Detail 属性
            geo.DetailAttribs.SetAttribute("curveType", curveType);

            ctx.Log($"CurveCreate: shape={shape}, points={points.Count}, closed={closed}, type={curveType}");
            return SingleOutput("geometry", geo);
        }
    }
}