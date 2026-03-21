using System.Collections.Generic;
using UnityEngine;
using g3;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCGGeometry 与 geometry3Sharp DMesh3 之间的双向转换桥接
    /// </summary>
    public static class GeometryBridge
    {
        /// <summary>
        /// PCGGeometry → DMesh3
        /// 仅处理三角形面；非三角形面先三角化再添加。
        /// </summary>
        public static DMesh3 ToDMesh3(PCGGeometry geo)
        {
            if (geo == null || geo.Points.Count == 0)
                return new DMesh3();

            bool hasNormals = geo.PointAttribs.HasAttribute("N");
            bool hasUVs = geo.PointAttribs.HasAttribute("uv");

            var mesh = new DMesh3(hasNormals, false, hasUVs, true);

            PCGAttribute normalAttr = hasNormals ? geo.PointAttribs.GetAttribute("N") : null;
            PCGAttribute uvAttr = hasUVs ? geo.PointAttribs.GetAttribute("uv") : null;

            // 添加顶点
            for (int i = 0; i < geo.Points.Count; i++)
            {
                var p = geo.Points[i];
                var info = new NewVertexInfo(new Vector3d(p.x, p.y, p.z));

                if (hasNormals && i < normalAttr.Values.Count)
                {
                    var n = (Vector3)normalAttr.Values[i];
                    info.bHaveN = true;
                    info.n = new Vector3f(n.x, n.y, n.z);
                }

                if (hasUVs && i < uvAttr.Values.Count)
                {
                    object uvVal = uvAttr.Values[i];
                    if (uvVal is Vector2 uv2)
                    {
                        info.bHaveUV = true;
                        info.uv = new Vector2f(uv2.x, uv2.y);
                    }
                    else if (uvVal is Vector3 uv3)
                    {
                        info.bHaveUV = true;
                        info.uv = new Vector2f(uv3.x, uv3.y);
                    }
                }

                mesh.AppendVertex(info);
            }

            // 添加面（三角化非三角形面）
            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length < 3) continue;

                // 确定面组 ID（从 PrimGroups 中查找）
                int groupId = 0;
                foreach (var kvp in geo.PrimGroups)
                {
                    if (kvp.Value.Contains(pi))
                    {
                        groupId = kvp.Key.GetHashCode() & 0x7FFFFFFF;
                        break;
                    }
                }

                if (prim.Length == 3)
                {
                    mesh.AppendTriangle(prim[0], prim[1], prim[2], groupId);
                }
                else
                {
                    // 扇形三角化
                    for (int j = 1; j < prim.Length - 1; j++)
                    {
                        mesh.AppendTriangle(prim[0], prim[j], prim[j + 1], groupId);
                    }
                }
            }

            return mesh;
        }

        /// <summary>
        /// DMesh3 → PCGGeometry
        /// 紧凑化后转换，每个三角形作为独立面。
        /// </summary>
        public static PCGGeometry FromDMesh3(DMesh3 mesh)
        {
            var geo = new PCGGeometry();
            if (mesh == null || mesh.VertexCount == 0)
                return geo;

            // 紧凑化索引映射
            var compactMesh = new DMesh3(mesh, true);

            // 转换顶点
            for (int vid = 0; vid < compactMesh.VertexCount; vid++)
            {
                var v = compactMesh.GetVertex(vid);
                geo.Points.Add(new Vector3((float)v.x, (float)v.y, (float)v.z));
            }

            // 转换法线
            if (compactMesh.HasVertexNormals)
            {
                var normalAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3, Vector3.up);
                for (int vid = 0; vid < compactMesh.VertexCount; vid++)
                {
                    var n = compactMesh.GetVertexNormal(vid);
                    normalAttr.Values.Add(new Vector3(n.x, n.y, n.z));
                }
            }

            // 转换 UV
            if (compactMesh.HasVertexUVs)
            {
                var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2, Vector2.zero);
                for (int vid = 0; vid < compactMesh.VertexCount; vid++)
                {
                    var uv = compactMesh.GetVertexUV(vid);
                    uvAttr.Values.Add(new Vector2(uv.x, uv.y));
                }
            }

            // 转换三角形面
            var groupMap = new Dictionary<int, string>();
            foreach (int tid in compactMesh.TriangleIndices())
            {
                var tri = compactMesh.GetTriangle(tid);
                geo.Primitives.Add(new int[] { tri.a, tri.b, tri.c });

                // 处理面组
                if (compactMesh.HasTriangleGroups)
                {
                    int gid = compactMesh.GetTriangleGroup(tid);
                    if (gid != 0)
                    {
                        if (!groupMap.ContainsKey(gid))
                            groupMap[gid] = $"group_{gid}";

                        string groupName = groupMap[gid];
                        if (!geo.PrimGroups.ContainsKey(groupName))
                            geo.PrimGroups[groupName] = new HashSet<int>();
                        geo.PrimGroups[groupName].Add(geo.Primitives.Count - 1);
                    }
                }
            }

            return geo;
        }
    }
}
