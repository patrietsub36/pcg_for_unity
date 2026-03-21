using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using PCGToolkit.Graph;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 对每个 PrimGroup / PointGroup / Piece 执行子图（对标 Houdini ForEach SOP）
    /// 迭代模式:
    ///   byGroup   — 遍历所有 PrimGroup，每次提取该组的面和相关点
    ///   byPiece   — 按连通分量拆分，每个连通分量作为一次迭代
    ///   count     — 按固定次数迭代，每次传入完整几何体
    /// 每次迭代通过 ctx.GlobalVariables 注入 iteration / groupname
    /// 所有迭代结果合并（Merge）后输出
    /// </summary>
    public class ForEachNode : PCGNodeBase
    {
        public override string Name => "ForEach";
        public override string DisplayName => "For Each";
        public override string Description => "对每个 Group/Piece/迭代执行子图";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("subGraphPath", PCGPortDirection.Input, PCGPortType.String,
                "SubGraph Path", "子图资源路径（Assets/...）", ""),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "迭代模式（byGroup/byPiece/count）", "byGroup"),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "count 模式下的迭代次数", 1),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "所有迭代结果合并后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string subGraphPath = GetParamString(parameters, "subGraphPath", "");
            string mode = GetParamString(parameters, "mode", "byGroup");
            int iterations = GetParamInt(parameters, "iterations", 1);

            if (string.IsNullOrEmpty(subGraphPath))
            {
                ctx.LogWarning("ForEach: subGraphPath is empty");
                return SingleOutput("geometry", geo.Clone());
            }

            var subGraphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<PCGGraphData>(subGraphPath);
            if (subGraphAsset == null)
            {
                ctx.LogWarning($"ForEach: Failed to load subgraph at {subGraphPath}");
                return SingleOutput("geometry", geo.Clone());
            }

            var pieces = new List<PCGGeometry>();

            switch (mode.ToLower())
            {
                case "bygroup":
                    pieces = IterateByGroup(geo, subGraphAsset, ctx);
                    break;
                case "bypiece":
                    pieces = IterateByPiece(geo, subGraphAsset, ctx);
                    break;
                case "count":
                    pieces = IterateByCount(geo, subGraphAsset, iterations, ctx);
                    break;
                default:
                    ctx.LogWarning($"ForEach: Unknown mode '{mode}', using byGroup");
                    pieces = IterateByGroup(geo, subGraphAsset, ctx);
                    break;
            }

            var merged = MergeResults(pieces);
            ctx.Log($"ForEach: {pieces.Count} iterations, result {merged.Points.Count} points, {merged.Primitives.Count} prims");
            return SingleOutput("geometry", merged);
        }

        private List<PCGGeometry> IterateByGroup(PCGGeometry geo, PCGGraphData subGraph, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();

            if (geo.PrimGroups.Count == 0)
            {
                ctx.LogWarning("ForEach byGroup: no PrimGroups found, running once with full geometry");
                var result = ExecuteSubGraph(subGraph, geo, ctx, 0, "");
                if (result != null) results.Add(result);
                return results;
            }

            int iteration = 0;
            foreach (var kvp in geo.PrimGroups)
            {
                var piece = ExtractPrimGroup(geo, kvp.Key, kvp.Value);
                var result = ExecuteSubGraph(subGraph, piece, ctx, iteration, kvp.Key);
                if (result != null) results.Add(result);
                iteration++;
            }
            return results;
        }

        private List<PCGGeometry> IterateByPiece(PCGGeometry geo, PCGGraphData subGraph, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();
            var pieces = SplitByConnectivity(geo);

            for (int i = 0; i < pieces.Count; i++)
            {
                var result = ExecuteSubGraph(subGraph, pieces[i], ctx, i, $"piece_{i}");
                if (result != null) results.Add(result);
            }
            return results;
        }

        private List<PCGGeometry> IterateByCount(PCGGeometry geo, PCGGraphData subGraph, int count, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();
            var current = geo;

            for (int i = 0; i < count; i++)
            {
                var result = ExecuteSubGraph(subGraph, current, ctx, i, "");
                if (result != null)
                {
                    results.Add(result);
                    current = result;
                }
            }
            return results;
        }

        private PCGGeometry ExecuteSubGraph(PCGGraphData subGraph, PCGGeometry inputGeo, PCGContext ctx, int iteration, string groupName)
        {
            var subExecutor = new PCGGraphExecutor(subGraph);

            ctx.GlobalVariables["iteration"] = (float)iteration;
            ctx.GlobalVariables["groupname"] = groupName;
            ctx.SetExternalInput("geometry", inputGeo);

            try
            {
                subExecutor.Execute(ctx);
            }
            catch (System.Exception e)
            {
                ctx.LogError($"ForEach iteration {iteration}: {e.Message}");
                return null;
            }

            if (ctx.TryGetExternalOutput("geometry", out var output))
                return output;

            return null;
        }

        private PCGGeometry ExtractPrimGroup(PCGGeometry source, string groupName, HashSet<int> primIndices)
        {
            var result = new PCGGeometry();

            // 收集被引用的点索引
            var usedPoints = new HashSet<int>();
            foreach (int pi in primIndices)
            {
                if (pi < source.Primitives.Count)
                {
                    foreach (int vi in source.Primitives[pi])
                        usedPoints.Add(vi);
                }
            }

            // 建立旧索引 -> 新索引映射
            var indexMap = new Dictionary<int, int>();
            foreach (int oldIdx in usedPoints.OrderBy(x => x))
            {
                indexMap[oldIdx] = result.Points.Count;
                result.Points.Add(source.Points[oldIdx]);
            }

            // 复制面并重映射索引
            foreach (int pi in primIndices)
            {
                if (pi >= source.Primitives.Count) continue;
                var prim = source.Primitives[pi];
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = indexMap[prim[i]];
                result.Primitives.Add(newPrim);
            }

            // 复制相关点属性
            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in usedPoints.OrderBy(x => x))
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            return result;
        }

        private List<PCGGeometry> SplitByConnectivity(PCGGeometry geo)
        {
            int pointCount = geo.Points.Count;
            int[] componentId = new int[pointCount];
            for (int i = 0; i < pointCount; i++) componentId[i] = i;

            // Union-Find
            int Find(int x) {
                while (componentId[x] != x) {
                    componentId[x] = componentId[componentId[x]];
                    x = componentId[x];
                }
                return x;
            }
            void Union(int a, int b) {
                a = Find(a); b = Find(b);
                if (a != b) componentId[a] = b;
            }

            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 0) continue;
                for (int i = 1; i < prim.Length; i++)
                    Union(prim[0], prim[i]);
            }

            // 按连通分量分组
            var groups = new Dictionary<int, List<int>>();
            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length == 0) continue;
                int root = Find(prim[0]);
                if (!groups.ContainsKey(root))
                    groups[root] = new List<int>();
                groups[root].Add(pi);
            }

            var results = new List<PCGGeometry>();
            foreach (var kvp in groups)
            {
                var primSet = new HashSet<int>(kvp.Value);
                results.Add(ExtractPrimGroup(geo, "", primSet));
            }
            return results;
        }

        private PCGGeometry MergeResults(List<PCGGeometry> pieces)
        {
            var result = new PCGGeometry();
            int pointOffset = 0;

            foreach (var piece in pieces)
            {
                if (piece == null || piece.Points.Count == 0) continue;

                result.Points.AddRange(piece.Points);
                foreach (var prim in piece.Primitives)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        newPrim[i] = prim[i] + pointOffset;
                    result.Primitives.Add(newPrim);
                }

                // 合并点属性
                foreach (var attr in piece.PointAttribs.GetAllAttributes())
                {
                    var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                    if (destAttr == null)
                    {
                        destAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                        for (int j = 0; j < pointOffset; j++)
                            destAttr.Values.Add(destAttr.DefaultValue);
                    }
                    destAttr.Values.AddRange(attr.Values);
                }

                // 合并点分组
                foreach (var grp in piece.PointGroups)
                {
                    if (!result.PointGroups.ContainsKey(grp.Key))
                        result.PointGroups[grp.Key] = new HashSet<int>();
                    foreach (int idx in grp.Value)
                        result.PointGroups[grp.Key].Add(idx + pointOffset);
                }

                // 合并面分组
                int primOffset = result.Primitives.Count - piece.Primitives.Count;
                foreach (var grp in piece.PrimGroups)
                {
                    if (!result.PrimGroups.ContainsKey(grp.Key))
                        result.PrimGroups[grp.Key] = new HashSet<int>();
                    foreach (int idx in grp.Value)
                        result.PrimGroups[grp.Key].Add(idx + primOffset);
                }

                pointOffset += piece.Points.Count;
            }

            return result;
        }
    }
}
