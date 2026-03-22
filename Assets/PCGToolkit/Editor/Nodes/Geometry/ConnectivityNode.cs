using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// Connectivity 节点：为每个连通分量写入 @class 属性。
    /// 对标 Houdini Connectivity SOP。
    /// 配合 ForEach byPiece 使用。
    /// </summary>
    public class ConnectivityNode : PCGNodeBase
    {
        public override string Name => "Connectivity";
        public override string DisplayName => "Connectivity";
        public override string Description => "为每个连通分量分配唯一的 class 属性值";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("attribName", PCGPortDirection.Input, PCGPortType.String,
                "Attribute Name", "输出属性名", "class"),
            new PCGParamSchema("connectType", PCGPortDirection.Input, PCGPortType.String,
                "Connect Type", "连通类型（point/prim）", "point")
            {
                EnumOptions = new[] { "point", "prim" }
            },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带 class 属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string attribName = GetParamString(parameters, "attribName", "class");
            string connectType = GetParamString(parameters, "connectType", "point").ToLower();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Connectivity: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            if (connectType == "prim" || connectType == "primitive")
            {
                ConnectivityByPrim(geo, attribName);
            }
            else
            {
                ConnectivityByPoint(geo, attribName);
            }

            return SingleOutput("geometry", geo);
        }

        private void ConnectivityByPoint(PCGGeometry geo, string attribName)
        {
            int pointCount = geo.Points.Count;
            int[] componentId = new int[pointCount];

            // 初始化 Union-Find
            for (int i = 0; i < pointCount; i++)
                componentId[i] = i;

            // Union-Find 函数
            int Find(int x)
            {
                while (componentId[x] != x)
                {
                    componentId[x] = componentId[componentId[x]];
                    x = componentId[x];
                }
                return x;
            }

            void Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);
                if (a != b) componentId[a] = b;
            }

            // 根据面的连接关系建立 Union
            foreach (var prim in geo.Primitives)
            {
                if (prim.Length < 2) continue;
                for (int i = 1; i < prim.Length; i++)
                {
                    Union(prim[0], prim[i]);
                }
            }

            // 为每个点找到其连通分量根
            var rootToClass = new Dictionary<int, int>();
            int classCounter = 0;

            var classAttr = geo.PointAttribs.CreateAttribute(attribName, typeof(float), 0f);
            classAttr.Values.Clear();

            for (int i = 0; i < pointCount; i++)
            {
                int root = Find(i);
                if (!rootToClass.TryGetValue(root, out int classValue))
                {
                    classValue = classCounter++;
                    rootToClass[root] = classValue;
                }
                classAttr.Values.Add((float)classValue);
            }

            // 同时为 Prim 创建属性
            var primClassAttr = geo.PrimAttribs.CreateAttribute(attribName, typeof(float), 0f);
            primClassAttr.Values.Clear();

            foreach (var prim in geo.Primitives)
            {
                if (prim.Length > 0 && prim[0] < pointCount)
                {
                    int root = Find(prim[0]);
                    int classValue = rootToClass.TryGetValue(root, out int cv) ? cv : 0;
                    primClassAttr.Values.Add((float)classValue);
                }
                else
                {
                    primClassAttr.Values.Add(0f);
                }
            }
        }

        private void ConnectivityByPrim(PCGGeometry geo, string attribName)
        {
            int primCount = geo.Primitives.Count;
            if (primCount == 0) return;

            // 构建边相邻关系
            var edgeToPrims = new Dictionary<long, List<int>>();

            for (int pi = 0; pi < primCount; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length < 2) continue;

                for (int i = 0; i < prim.Length; i++)
                {
                    int a = prim[i];
                    int b = prim[(i + 1) % prim.Length];
                    long edgeKey = a < b ? ((long)a << 32) | b : ((long)b << 32) | a;

                    if (!edgeToPrims.TryGetValue(edgeKey, out var primList))
                    {
                        primList = new List<int>();
                        edgeToPrims[edgeKey] = primList;
                    }
                    primList.Add(pi);
                }
            }

            // 初始化 Union-Find
            int[] componentId = new int[primCount];
            for (int i = 0; i < primCount; i++)
                componentId[i] = i;

            int Find(int x)
            {
                while (componentId[x] != x)
                {
                    componentId[x] = componentId[componentId[x]];
                    x = componentId[x];
                }
                return x;
            }

            void Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);
                if (a != b) componentId[a] = b;
            }

            // 通过共享边建立 Union
            foreach (var kvp in edgeToPrims)
            {
                var primList = kvp.Value;
                if (primList.Count >= 2)
                {
                    for (int i = 1; i < primList.Count; i++)
                    {
                        Union(primList[0], primList[i]);
                    }
                }
            }

            // 为每个 Prim 分配 class
            var rootToClass = new Dictionary<int, int>();
            int classCounter = 0;

            var primClassAttr = geo.PrimAttribs.CreateAttribute(attribName, typeof(float), 0f);
            primClassAttr.Values.Clear();

            for (int i = 0; i < primCount; i++)
            {
                int root = Find(i);
                if (!rootToClass.TryGetValue(root, out int classValue))
                {
                    classValue = classCounter++;
                    rootToClass[root] = classValue;
                }
                primClassAttr.Values.Add((float)classValue);
            }

            // 为点也分配 class（使用所属第一个 Prim 的 class）
            var pointClassAttr = geo.PointAttribs.CreateAttribute(attribName, typeof(float), 0f);
            var pointClass = new int[geo.Points.Count];
            for (int i = 0; i < pointClass.Length; i++) pointClass[i] = -1;

            for (int pi = 0; pi < primCount; pi++)
            {
                int classValue = (int)primClassAttr.Values[pi];
                foreach (int vi in geo.Primitives[pi])
                {
                    if (vi >= 0 && vi < pointClass.Length && pointClass[vi] == -1)
                    {
                        pointClass[vi] = classValue;
                    }
                }
            }

            pointClassAttr.Values.Clear();
            for (int i = 0; i < pointClass.Length; i++)
            {
                pointClassAttr.Values.Add((float)(pointClass[i] >= 0 ? pointClass[i] : 0));
            }
        }
    }
}