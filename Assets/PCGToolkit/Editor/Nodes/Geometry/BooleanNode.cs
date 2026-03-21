using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;
using g3;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 布尔运算模式
    /// </summary>
    public enum BooleanOperation
    {
        Union,
        Intersect,
        Subtract
    }

    /// <summary>
    /// 布尔运算（对标 Houdini Boolean SOP）
    /// 使用 geometry3Sharp 的 MeshBoolean 实现真实 CSG 运算
    /// 通过 GeometryBridge 保留法线和 UV 属性
    /// </summary>
    public class BooleanNode : PCGNodeBase
    {
        public override string Name => "Boolean";
        public override string DisplayName => "Boolean";
        public override string Description => "对两个几何体执行布尔运算（并集/交集/差集）";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("inputA", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input A", "第一个几何体", null, required: true),
            new PCGParamSchema("inputB", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input B", "第二个几何体", null, required: true),
            new PCGParamSchema("operation", PCGPortDirection.Input, PCGPortType.String,
                "Operation", "布尔运算类型（union/intersect/subtract）", "union"),
            new PCGParamSchema("vertexSnapTol", PCGPortDirection.Input, PCGPortType.Float,
                "Vertex Snap Tolerance", "顶点合并容差", 0.00001f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "运算结果几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geoA = GetInputGeometry(inputGeometries, "inputA");
            var geoB = GetInputGeometry(inputGeometries, "inputB");
            string operation = GetParamString(parameters, "operation", "union");
            double snapTol = GetParamFloat(parameters, "vertexSnapTol", 0.00001f);

            if (geoA.Points.Count == 0)
            {
                ctx.LogWarning("Boolean: Input A 为空");
                return SingleOutput("geometry", geoB.Clone());
            }
            if (geoB.Points.Count == 0)
            {
                ctx.LogWarning("Boolean: Input B 为空");
                return SingleOutput("geometry", operation == "subtract" ? geoA.Clone() : new PCGGeometry());
            }

            // 使用 GeometryBridge 转换，保留法线和 UV
            var meshA = GeometryBridge.ToDMesh3(geoA);
            var meshB = GeometryBridge.ToDMesh3(geoB);

            DMesh3 resultMesh = null;

            try
            {
                switch (operation)
                {
                    case "union":
                        resultMesh = ComputeUnion(meshA, meshB, snapTol, ctx);
                        break;
                    case "subtract":
                        resultMesh = ComputeSubtract(meshA, meshB, snapTol, ctx);
                        break;
                    case "intersect":
                        resultMesh = ComputeIntersect(meshA, meshB, snapTol, ctx);
                        break;
                    default:
                        ctx.LogWarning($"Boolean: 未知操作 '{operation}'，使用 union");
                        resultMesh = ComputeUnion(meshA, meshB, snapTol, ctx);
                        break;
                }
            }
            catch (System.Exception e)
            {
                ctx.LogError($"Boolean: CSG 运算异常 — {e.Message}");
                return SingleOutput("geometry", geoA.Clone());
            }

            if (resultMesh == null)
            {
                ctx.LogWarning("Boolean: CSG 运算返回空结果");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 使用 GeometryBridge 转换回来，保留法线和 UV
            var result = GeometryBridge.FromDMesh3(resultMesh);
            ctx.Log($"Boolean: {operation} 完成, {result.Points.Count} 点, {result.Primitives.Count} 面");
            return SingleOutput("geometry", result);
        }

        private DMesh3 ComputeUnion(DMesh3 a, DMesh3 b, double snapTol, PCGContext ctx)
        {
            var boolean = new MeshBoolean
            {
                Target = a,
                Tool = b,
                VertexSnapTol = snapTol
            };
            boolean.Compute();
            return boolean.Result;
        }

        private DMesh3 ComputeSubtract(DMesh3 a, DMesh3 b, double snapTol, PCGContext ctx)
        {
            var flippedB = new DMesh3(b);
            flippedB.ReverseOrientation();

            var boolean = new MeshBoolean
            {
                Target = a,
                Tool = flippedB,
                VertexSnapTol = snapTol
            };
            boolean.Compute();
            return boolean.Result;
        }

        private DMesh3 ComputeIntersect(DMesh3 a, DMesh3 b, double snapTol, PCGContext ctx)
        {
            var flippedA = new DMesh3(a);
            flippedA.ReverseOrientation();
            var flippedB = new DMesh3(b);
            flippedB.ReverseOrientation();

            var boolean = new MeshBoolean
            {
                Target = flippedA,
                Tool = flippedB,
                VertexSnapTol = snapTol
            };
            boolean.Compute();

            if (boolean.Result != null)
                boolean.Result.ReverseOrientation();

            return boolean.Result;
        }
    }
}