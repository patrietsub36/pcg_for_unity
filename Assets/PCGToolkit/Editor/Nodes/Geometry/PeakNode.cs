using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 沿法线均匀偏移点（对标 Houdini Peak SOP）
    /// 不改变拓扑，仅移动顶点位置。
    /// </summary>
    public class PeakNode : PCGNodeBase
    {
        public override string Name => "Peak";
        public override string DisplayName => "Peak";
        public override string Description => "沿法线方向均匀偏移顶点位置（不改变拓扑）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("distance", PCGPortDirection.Input, PCGPortType.Float,
                "Distance", "沿法线偏移距离", 0.1f),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定点分组偏移（留空=全部）", ""),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            float distance = GetParamFloat(parameters, "distance", 0.1f);
            string group = GetParamString(parameters, "group", "");

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo);

            // 计算每个顶点的法线（面积加权平均）
            Vector3[] normals = ComputeVertexNormals(geo);

            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group) && geo.PointGroups.TryGetValue(group, out var grp))
                indices = grp;

            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (indices != null && !indices.Contains(i)) continue;
                geo.Points[i] += normals[i] * distance;
            }

            return SingleOutput("geometry", geo);
        }

        private Vector3[] ComputeVertexNormals(PCGGeometry geo)
        {
            var normals = new Vector3[geo.Points.Count];

            // 如果已有法线属性，直接使用
            var nAttr = geo.PointAttribs.GetAttribute("N");
            if (nAttr != null && nAttr.Values.Count == geo.Points.Count)
            {
                for (int i = 0; i < geo.Points.Count; i++)
                {
                    if (nAttr.Values[i] is Vector3 n)
                        normals[i] = n;
                    else
                        normals[i] = Vector3.up;
                }
                return normals;
            }

            // 否则从面计算面积加权顶点法线
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.zero;

            foreach (var prim in geo.Primitives)
            {
                if (prim.Length < 3) continue;
                Vector3 v0 = geo.Points[prim[0]];
                Vector3 v1 = geo.Points[prim[1]];
                Vector3 v2 = geo.Points[prim[2]];
                Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
                // faceNormal 的模 = 2 * 面积，自然做面积加权

                foreach (int idx in prim)
                    normals[idx] += faceNormal;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                if (normals[i].sqrMagnitude > 0.000001f)
                    normals[i] = normals[i].normalized;
                else
                    normals[i] = Vector3.up;
            }

            return normals;
        }
    }
}
