using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.UV
{
    /// <summary>
    /// UV 布局/排列（对标 Houdini UVLayout SOP）
    /// 识别 UV 岛，用矩形装箱算法重新排布到 [0,1] 空间内。
    /// </summary>
    public class UVLayoutNode : PCGNodeBase
    {
        public override string Name => "UVLayout";
        public override string DisplayName => "UV Layout";
        public override string Description => "重新排列 UV 岛以优化空间利用率";
        public override PCGNodeCategory Category => PCGNodeCategory.UV;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("padding", PCGPortDirection.Input, PCGPortType.Float,
                "Padding", "UV 岛之间的间距", 0.01f),
            new PCGParamSchema("resolution", PCGPortDirection.Input, PCGPortType.Int,
                "Resolution", "布局分辨率", 1024),
            new PCGParamSchema("rotateIslands", PCGPortDirection.Input, PCGPortType.Bool,
                "Rotate Islands", "是否允许旋转 UV 岛以优化排列", true),
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
            float padding = GetParamFloat(parameters, "padding", 0.01f);
            bool rotateIslands = GetParamBool(parameters, "rotateIslands", true);

            var uvAttr = geo.PointAttribs.GetAttribute("uv");
            if (uvAttr == null)
            {
                ctx.LogWarning("UVLayout: 几何体没有 UV 属性，请先使用 UVProject 或 UVUnwrap");
                return SingleOutput("geometry", geo);
            }

            // 1. 识别 UV 岛（通过面连通性）
            var islands = FindUVIslands(geo);
            if (islands.Count == 0)
                return SingleOutput("geometry", geo);

            // 2. 计算每个岛的 UV 包围盒
            var islandBounds = new List<(Vector2 min, Vector2 max, List<int> pointIndices)>();
            foreach (var island in islands)
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                var pointSet = new HashSet<int>();

                foreach (int pi in island)
                {
                    var prim = geo.Primitives[pi];
                    foreach (int vi in prim)
                    {
                        pointSet.Add(vi);
                        if (vi < uvAttr.Values.Count)
                        {
                            Vector2 uv = ToVector2(uvAttr.Values[vi]);
                            min = Vector2.Min(min, uv);
                            max = Vector2.Max(max, uv);
                        }
                    }
                }

                islandBounds.Add((min, max, pointSet.ToList()));
            }

            // 3. 矩形装箱（按面积降序排列后贪心放置）
            var boxes = new List<(int idx, float w, float h)>();
            for (int i = 0; i < islandBounds.Count; i++)
            {
                var b = islandBounds[i];
                float w = b.max.x - b.min.x + padding;
                float h = b.max.y - b.min.y + padding;

                if (rotateIslands && h > w)
                    boxes.Add((i, h, w)); // 旋转使宽 >= 高
                else
                    boxes.Add((i, w, h));
            }

            boxes.Sort((a, b) => (b.w * b.h).CompareTo(a.w * a.h));

            // Shelf 装箱算法
            float shelfY = padding;
            float shelfH = 0;
            float curX = padding;
            float totalScale = 1f;

            var placements = new Vector2[islandBounds.Count]; // offset for each island
            var rotated = new bool[islandBounds.Count];

            // 先尝试放置，找到需要的总空间
            float maxX = 0, maxY = 0;
            foreach (var box in boxes)
            {
                float w = box.w;
                float h = box.h;
                bool isRotated = false;

                var origBounds = islandBounds[box.idx];
                float origW = origBounds.max.x - origBounds.min.x + padding;
                float origH = origBounds.max.y - origBounds.min.y + padding;
                if (rotateIslands && origH > origW)
                    isRotated = true;

                if (curX + w > 1f - padding)
                {
                    curX = padding;
                    shelfY += shelfH + padding;
                    shelfH = 0;
                }

                placements[box.idx] = new Vector2(curX, shelfY);
                rotated[box.idx] = isRotated;
                curX += w + padding;
                shelfH = Mathf.Max(shelfH, h);
                maxX = Mathf.Max(maxX, curX);
                maxY = Mathf.Max(maxY, shelfY + shelfH);
            }

            // 如果超出 [0,1]，缩放
            float requiredSize = Mathf.Max(maxX, maxY + padding);
            if (requiredSize > 1f)
                totalScale = (1f - padding * 2) / requiredSize;

            // 4. 应用新位置
            for (int i = 0; i < islandBounds.Count; i++)
            {
                var b = islandBounds[i];
                Vector2 offset = placements[i] * totalScale;
                Vector2 oldMin = b.min;

                foreach (int vi in b.pointIndices)
                {
                    if (vi >= uvAttr.Values.Count) continue;
                    Vector2 uv = ToVector2(uvAttr.Values[vi]);
                    Vector2 local = uv - oldMin;

                    if (rotated[i])
                        local = new Vector2(local.y, local.x);

                    local *= totalScale;
                    Vector2 newUV = offset + local;
                    uvAttr.Values[vi] = new Vector3(newUV.x, newUV.y, 0f);
                }
            }

            ctx.Log($"UVLayout: {islands.Count} UV islands packed, scale={totalScale:F3}");
            return SingleOutput("geometry", geo);
        }

        private List<HashSet<int>> FindUVIslands(PCGGeometry geo)
        {
            int primCount = geo.Primitives.Count;
            int[] parent = new int[primCount];
            for (int i = 0; i < primCount; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                a = Find(a); b = Find(b);
                if (a != b) parent[a] = b;
            }

            // 通过共享顶点连接面
            var vertToPrim = new Dictionary<int, List<int>>();
            for (int fi = 0; fi < primCount; fi++)
            {
                foreach (int vi in geo.Primitives[fi])
                {
                    if (!vertToPrim.ContainsKey(vi))
                        vertToPrim[vi] = new List<int>();
                    vertToPrim[vi].Add(fi);
                }
            }

            foreach (var kvp in vertToPrim)
            {
                var prims = kvp.Value;
                for (int i = 1; i < prims.Count; i++)
                    Union(prims[0], prims[i]);
            }

            var groups = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < primCount; i++)
            {
                int root = Find(i);
                if (!groups.ContainsKey(root))
                    groups[root] = new HashSet<int>();
                groups[root].Add(i);
            }

            return groups.Values.ToList();
        }

        private static Vector2 ToVector2(object val)
        {
            if (val is Vector3 v3) return new Vector2(v3.x, v3.y);
            if (val is Vector2 v2) return v2;
            return Vector2.zero;
        }
    }
}