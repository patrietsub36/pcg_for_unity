using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 正多面体生成（对标 Houdini Platonic Solids SOP）
    /// 支持: tetrahedron(4), octahedron(8), icosahedron(20), dodecahedron(12)
    /// </summary>
    public class PlatonicSolidsNode : PCGNodeBase
    {
        public override string Name => "PlatonicSolids";
        public override string DisplayName => "Platonic Solids";
        public override string Description => "生成正多面体（正四/八/十二/二十面体）";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "多面体类型（tetrahedron/octahedron/icosahedron/dodecahedron）", "icosahedron"),
            new PCGParamSchema("radius", PCGPortDirection.Input, PCGPortType.Float,
                "Radius", "外接球半径", 1f),
            new PCGParamSchema("center", PCGPortDirection.Input, PCGPortType.Vector3,
                "Center", "中心位置", Vector3.zero),
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
            string type = GetParamString(parameters, "type", "icosahedron").ToLower();
            float radius = GetParamFloat(parameters, "radius", 1f);
            Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);

            PCGGeometry geo;
            switch (type)
            {
                case "tetrahedron": geo = BuildTetrahedron(); break;
                case "octahedron": geo = BuildOctahedron(); break;
                case "dodecahedron": geo = BuildDodecahedron(); break;
                default: geo = BuildIcosahedron(); break;
            }

            // 缩放和平移
            for (int i = 0; i < geo.Points.Count; i++)
                geo.Points[i] = geo.Points[i] * radius + center;

            return SingleOutput("geometry", geo);
        }

        private PCGGeometry BuildTetrahedron()
        {
            var geo = new PCGGeometry();
            float a = 1f / Mathf.Sqrt(3f);
            geo.Points.Add(new Vector3(a, a, a));
            geo.Points.Add(new Vector3(a, -a, -a));
            geo.Points.Add(new Vector3(-a, a, -a));
            geo.Points.Add(new Vector3(-a, -a, a));
            geo.Primitives.Add(new[] { 0, 1, 2 });
            geo.Primitives.Add(new[] { 0, 3, 1 });
            geo.Primitives.Add(new[] { 0, 2, 3 });
            geo.Primitives.Add(new[] { 1, 3, 2 });
            return geo;
        }

        private PCGGeometry BuildOctahedron()
        {
            var geo = new PCGGeometry();
            geo.Points.Add(new Vector3(0, 1, 0));
            geo.Points.Add(new Vector3(1, 0, 0));
            geo.Points.Add(new Vector3(0, 0, 1));
            geo.Points.Add(new Vector3(-1, 0, 0));
            geo.Points.Add(new Vector3(0, 0, -1));
            geo.Points.Add(new Vector3(0, -1, 0));
            geo.Primitives.Add(new[] { 0, 1, 2 });
            geo.Primitives.Add(new[] { 0, 2, 3 });
            geo.Primitives.Add(new[] { 0, 3, 4 });
            geo.Primitives.Add(new[] { 0, 4, 1 });
            geo.Primitives.Add(new[] { 5, 2, 1 });
            geo.Primitives.Add(new[] { 5, 3, 2 });
            geo.Primitives.Add(new[] { 5, 4, 3 });
            geo.Primitives.Add(new[] { 5, 1, 4 });
            return geo;
        }

        private PCGGeometry BuildIcosahedron()
        {
            var geo = new PCGGeometry();
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            float s = 1f / Mathf.Sqrt(1f + t * t); // normalize
            float ts = t * s;

            geo.Points.Add(new Vector3(-s, ts, 0));
            geo.Points.Add(new Vector3(s, ts, 0));
            geo.Points.Add(new Vector3(-s, -ts, 0));
            geo.Points.Add(new Vector3(s, -ts, 0));
            geo.Points.Add(new Vector3(0, -s, ts));
            geo.Points.Add(new Vector3(0, s, ts));
            geo.Points.Add(new Vector3(0, -s, -ts));
            geo.Points.Add(new Vector3(0, s, -ts));
            geo.Points.Add(new Vector3(ts, 0, -s));
            geo.Points.Add(new Vector3(ts, 0, s));
            geo.Points.Add(new Vector3(-ts, 0, -s));
            geo.Points.Add(new Vector3(-ts, 0, s));

            int[][] faces = {
                new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11},
                new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
                new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9},
                new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1}
            };
            foreach (var f in faces) geo.Primitives.Add(f);
            return geo;
        }

        private PCGGeometry BuildDodecahedron()
        {
            var geo = new PCGGeometry();
            float phi = (1f + Mathf.Sqrt(5f)) / 2f;
            float invPhi = 1f / phi;
            float r = 1f / Mathf.Sqrt(3f); // normalize to unit sphere

            // 20 vertices of dodecahedron
            Vector3[] v = {
                new Vector3(1,1,1)*r, new Vector3(1,1,-1)*r, new Vector3(1,-1,1)*r, new Vector3(1,-1,-1)*r,
                new Vector3(-1,1,1)*r, new Vector3(-1,1,-1)*r, new Vector3(-1,-1,1)*r, new Vector3(-1,-1,-1)*r,
                new Vector3(0,invPhi,phi)*r, new Vector3(0,invPhi,-phi)*r, new Vector3(0,-invPhi,phi)*r, new Vector3(0,-invPhi,-phi)*r,
                new Vector3(invPhi,phi,0)*r, new Vector3(invPhi,-phi,0)*r, new Vector3(-invPhi,phi,0)*r, new Vector3(-invPhi,-phi,0)*r,
                new Vector3(phi,0,invPhi)*r, new Vector3(phi,0,-invPhi)*r, new Vector3(-phi,0,invPhi)*r, new Vector3(-phi,0,-invPhi)*r,
            };
            foreach (var p in v) geo.Points.Add(p);

            // 12 pentagonal faces
            int[][] faces = {
                new[]{0,8,10,2,16}, new[]{0,16,17,1,12}, new[]{0,12,14,4,8},
                new[]{1,17,3,11,9}, new[]{1,9,5,14,12}, new[]{2,10,6,15,13},
                new[]{2,13,3,17,16}, new[]{3,13,15,7,11}, new[]{4,14,5,19,18},
                new[]{4,18,6,10,8}, new[]{5,9,11,7,19}, new[]{6,18,19,7,15}
            };
            foreach (var f in faces) geo.Primitives.Add(f);
            return geo;
        }
    }
}
