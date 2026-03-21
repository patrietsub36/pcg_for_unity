using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Topology
{
    /// <summary>
    /// 在边上等距插入新点（对标 Houdini Subdivide 的边分割模式）
    /// 对几何体的每条边插入 divisions 个等距点，将面细分。
    /// </summary>
    public class EdgeDivideNode : PCGNodeBase
    {
        public override string Name => "EdgeDivide";
        public override string DisplayName => "Edge Divide";
        public override string Description => "在边上等距插入新点";
        public override PCGNodeCategory Category => PCGNodeCategory.Topology;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("divisions", PCGPortDirection.Input, PCGPortType.Int,
                "Divisions", "每条边插入的点数", 1),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定 PrimGroup 的边操作（留空=所有）", ""),
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
            int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
            string group = GetParamString(parameters, "group", "");

            if (geo.Primitives.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            HashSet<int> groupPrims = null;
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var grp))
                groupPrims = grp;

            var result = new PCGGeometry();
            result.Points.AddRange(geo.Points);

            // 边 -> 插入的新点索引列表 (从 v0 到 v1 方向)
            var edgeNewPoints = new Dictionary<(int, int), List<int>>();

            List<int> GetOrCreateEdgePoints(int a, int b)
            {
                var key = a < b ? (a, b) : (b, a);
                if (edgeNewPoints.TryGetValue(key, out var existing))
                    return a < b ? existing : ReverseList(existing);

                var pts = new List<int>();
                Vector3 pA = geo.Points[a < b ? a : b];
                Vector3 pB = geo.Points[a < b ? b : a];

                for (int d = 1; d <= divisions; d++)
                {
                    float t = (float)d / (divisions + 1);
                    int idx = result.Points.Count;
                    result.Points.Add(Vector3.Lerp(pA, pB, t));
                    pts.Add(idx);
                }

                edgeNewPoints[key] = pts;
                return a < b ? pts : ReverseList(pts);
            }

            for (int fi = 0; fi < geo.Primitives.Count; fi++)
            {
                var prim = geo.Primitives[fi];

                if (groupPrims != null && !groupPrims.Contains(fi))
                {
                    result.Primitives.Add((int[])prim.Clone());
                    continue;
                }

                // 构建每条边的扩展顶点序列
                var expanded = new List<int>();
                for (int i = 0; i < prim.Length; i++)
                {
                    int v = prim[i];
                    int vNext = prim[(i + 1) % prim.Length];

                    expanded.Add(v);
                    var midPts = GetOrCreateEdgePoints(v, vNext);
                    expanded.AddRange(midPts);
                }

                // 对于三角形：扇形三角化扩展后的多边形
                // 对于四边形及以上：直接作为一个多边形面
                if (expanded.Count >= 3)
                    result.Primitives.Add(expanded.ToArray());
            }

            ctx.Log($"EdgeDivide: {divisions} divisions per edge, {geo.Points.Count} -> {result.Points.Count} pts, {geo.Primitives.Count} -> {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }

        private static List<int> ReverseList(List<int> list)
        {
            var rev = new List<int>(list);
            rev.Reverse();
            return rev;
        }
    }
}
