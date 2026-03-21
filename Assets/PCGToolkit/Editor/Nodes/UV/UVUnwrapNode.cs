using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 展开（对标 Houdini UVUnwrap / UVFlatten SOP）
    /// 使用 xatlas 库进行真实参数化展开
    /// </summary>
    public class UVUnwrapNode : PCGNodeBase
    {
        public override string Name => "UVUnwrap";
        public override string DisplayName => "UV Unwrap";
        public override string Description => "自动展开几何体的 UV（使用 xatlas）";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组展开（留空=全部）", ""),
            new PCGParamSchema("maxStretch", PCGPortDirection.Input, PCGPortType.Float,
                "Max Stretch", "最大拉伸阈值", 0.5f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "图集分辨率", 1024),
            new PCGParamSchema("padding", PCGPortDirection.Input, PCGPortType.Int,
                "Padding", "UV 岛间距（像素）", 2),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带展开的 UV）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            int resolution = GetParamInt(parameters, "resolution", 1024);
            int padding = GetParamInt(parameters, "padding", 2);

            if (geo.Points.Count == 0 || geo.Primitives.Count == 0)
            {
                ctx.LogWarning("UVUnwrap: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            // 转换为 Unity Mesh 以调用 xatlas
            var mesh = PCGGeometryToMesh.Convert(geo);
            if (mesh.vertexCount == 0 || mesh.triangles.Length == 0)
            {
                ctx.LogWarning("UVUnwrap: Mesh 转换结果为空");
                return SingleOutput("geometry", geo);
            }

            // 确保有法线
            if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                mesh.RecalculateNormals();

            try
            {
                xatlas.xatlas.Unwrap(mesh, padding);

                // xatlas 展开后可能改变了顶点数（UV 接缝处会拆分顶点）
                // 用新的 Mesh 数据重建 PCGGeometry
                var result = new PCGGeometry();
                var verts = mesh.vertices;
                var uvs = mesh.uv2; // xatlas 写入 uv2
                var tris = mesh.triangles;

                for (int i = 0; i < verts.Length; i++)
                    result.Points.Add(verts[i]);

                // 写入 UV 属性
                var uvAttr = result.PointAttribs.CreateAttribute("uv", AttribType.Vector3, Vector3.zero);
                if (uvs != null && uvs.Length == verts.Length)
                {
                    for (int i = 0; i < uvs.Length; i++)
                        uvAttr.Values.Add(new Vector3(uvs[i].x, uvs[i].y, 0f));
                }
                else
                {
                    for (int i = 0; i < verts.Length; i++)
                        uvAttr.Values.Add(Vector3.zero);
                }

                // 写入法线
                var normals = mesh.normals;
                if (normals != null && normals.Length == verts.Length)
                {
                    var nAttr = result.PointAttribs.CreateAttribute("N", AttribType.Vector3, Vector3.up);
                    for (int i = 0; i < normals.Length; i++)
                        nAttr.Values.Add(normals[i]);
                }

                // 重建三角面
                for (int i = 0; i < tris.Length; i += 3)
                    result.Primitives.Add(new int[] { tris[i], tris[i + 1], tris[i + 2] });

                ctx.Log($"UVUnwrap: xatlas 展开完成, {result.Points.Count} pts, {result.Primitives.Count} faces");
                return SingleOutput("geometry", result);
            }
            catch (System.Exception e)
            {
                ctx.LogWarning($"UVUnwrap: xatlas 调用失败 ({e.Message})，回退到平面投影");
                return SingleOutput("geometry", FallbackProjection(geo));
            }
        }

        private PCGGeometry FallbackProjection(PCGGeometry geo)
        {
            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
                uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector3, Vector3.zero);
            uvAttr.Values.Clear();

            Vector3 min = geo.Points[0], max = geo.Points[0];
            foreach (var p in geo.Points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 size = max - min;

            foreach (var p in geo.Points)
            {
                uvAttr.Values.Add(new Vector3(
                    size.x > 0 ? (p.x - min.x) / size.x : 0f,
                    size.z > 0 ? (p.z - min.z) / size.z : 0f,
                    0f));
            }

            return geo;
        }
    }
}