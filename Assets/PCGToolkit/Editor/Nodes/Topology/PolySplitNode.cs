using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 用平面切割面，拆分为子面（对标 Houdini Clip / PolySplit）
    /// 平面由 origin + normal 定义，面被平面穿过时拆分为两部分。
    /// </summary>
    public class PolySplitNode : PCGNodeBase
    {
        public override string Name => "PolySplit";
        public override string DisplayName => "Poly Split";
        public override string Description => "用平面切割面，拆分为子面";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "切割平面原点", Vector3.zero),
            new PCGParamSchema("normal", PCGPortDirection.Input, PCGPortType.Vector3,
                "Normal", "切割平面法线", Vector3.up),
            new PCGParamSchema("keepBoth", PCGPortDirection.Input, PCGPortType.Bool,
                "Keep Both", "保留两侧（false 则仅保留法线正侧）", true),
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
            var geo = GetInputGeometry(inputGeometries, "input");
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 normal = GetParamVector3(parameters, "normal", Vector3.up).normalized;
            bool keepBoth = GetParamBool(parameters, "keepBoth", true);

            if (geo.Primitives.Count == 0 || normal.sqrMagnitude < 0.0001f)
                return SingleOutput("geometry", geo.Clone());

            var result = new PCGGeometry();
            result.Points.AddRange(geo.Points);

            // 计算每个点到平面的有符号距离
            float[] dists = new float[geo.Points.Count];
            for (int i = 0; i < geo.Points.Count; i++)
                dists[i] = Vector3.Dot(geo.Points[i] - origin, normal);

            // 边中点缓存
            var edgeSplitPoints = new Dictionary<(int, int), int>();

            int GetSplitPoint(int a, int b)
            {
                var key = a < b ? (a, b) : (b, a);
                if (edgeSplitPoints.TryGetValue(key, out int idx))
                    return idx;

                float da = dists[a], db = dists[b];
                float t = da / (da - db);
                Vector3 p = Vector3.Lerp(geo.Points[a], geo.Points[b], t);
                idx = result.Points.Count;
                result.Points.Add(p);
                edgeSplitPoints[key] = idx;
                return idx;
            }

            foreach (var prim in geo.Primitives)
            {
                // 分类顶点
                var positiveSide = new List<int>(); // normal 正侧
                var negativeSide = new List<int>(); // normal 负侧

                for (int i = 0; i < prim.Length; i++)
                {
                    if (dists[prim[i]] >= 0)
                        positiveSide.Add(i);
                    else
                        negativeSide.Add(i);
                }

                if (negativeSide.Count == 0)
                {
                    // 全在正侧
                    result.Primitives.Add((int[])prim.Clone());
                }
                else if (positiveSide.Count == 0)
                {
                    // 全在负侧
                    if (keepBoth)
                        result.Primitives.Add((int[])prim.Clone());
                }
                else
                {
                    // 面被平面穿过 -> 拆分
                    var posFace = new List<int>();
                    var negFace = new List<int>();

                    for (int i = 0; i < prim.Length; i++)
                    {
                        int cur = prim[i];
                        int next = prim[(i + 1) % prim.Length];
                        float dCur = dists[cur];
                        float dNext = dists[next];

                        if (dCur >= 0)
                            posFace.Add(cur);
                        else
                            negFace.Add(cur);

                        // 如果当前边穿过平面，添加交点
                        if ((dCur >= 0) != (dNext >= 0))
                        {
                            int sp = GetSplitPoint(cur, next);
                            posFace.Add(sp);
                            negFace.Add(sp);
                        }
                    }

                    if (posFace.Count >= 3)
                        result.Primitives.Add(posFace.ToArray());
                    if (keepBoth && negFace.Count >= 3)
                        result.Primitives.Add(negFace.ToArray());
                }
            }

            ctx.Log($"PolySplit: {geo.Primitives.Count} faces -> {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }
    }
}
