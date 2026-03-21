using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 表达式驱动的属性操作（对标 Houdini AttribWrangle / Point Wrangle）
    /// 使用 ExpressionParser 对每个点/面执行用户编写的表达式
    /// 示例: @P.y = sin(@ptnum * 0.1); @Cd = {rand(@ptnum), 0, 1};
    /// </summary>
    public class AttribWrangleNode : PCGNodeBase
    {
        public override string Name => "AttribWrangle";
        public override string DisplayName => "Attribute Wrangle";
        public override string Description => "对每个点/面执行表达式，修改属性值";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("code", PCGPortDirection.Input, PCGPortType.String,
                "Code", "表达式代码（分号分隔多条语句）", ""),
            new PCGParamSchema("runOver", PCGPortDirection.Input, PCGPortType.String,
                "Run Over", "遍历模式（points/primitives/detail）", "points"),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组执行（留空=全部）", ""),
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
            string code = GetParamString(parameters, "code", "");
            string runOver = GetParamString(parameters, "runOver", "points");
            string group = GetParamString(parameters, "group", "");

            if (string.IsNullOrEmpty(code))
            {
                ctx.LogWarning("AttribWrangle: code 为空");
                return SingleOutput("geometry", geo);
            }

            var parser = new ExpressionParser();

            try
            {
                switch (runOver.ToLower())
                {
                    case "points":
                        RunOverPoints(geo, code, group, parser, ctx);
                        break;
                    case "primitives":
                        RunOverPrimitives(geo, code, group, parser, ctx);
                        break;
                    case "detail":
                        RunOverDetail(geo, code, parser, ctx);
                        break;
                    default:
                        RunOverPoints(geo, code, group, parser, ctx);
                        break;
                }
            }
            catch (System.Exception e)
            {
                ctx.LogError($"AttribWrangle: 执行异常 — {e.Message}");
            }

            return SingleOutput("geometry", geo);
        }

        private void RunOverPoints(PCGGeometry geo, string code, string group, ExpressionParser parser, PCGContext ctx)
        {
            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group) && geo.PointGroups.TryGetValue(group, out var grp))
                indices = grp;

            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (indices != null && !indices.Contains(i)) continue;

                var evalCtx = new ExpressionParser.EvalContext
                {
                    Geometry = geo,
                    PointIndex = i,
                    PrimIndex = -1,
                    TotalPoints = geo.Points.Count,
                    TotalPrims = geo.Primitives.Count
                };

                parser.Execute(code, evalCtx);
                ApplyOutputs(geo, evalCtx, i, "point");
            }
        }

        private void RunOverPrimitives(PCGGeometry geo, string code, string group, ExpressionParser parser, PCGContext ctx)
        {
            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var grp))
                indices = grp;

            for (int i = 0; i < geo.Primitives.Count; i++)
            {
                if (indices != null && !indices.Contains(i)) continue;

                var evalCtx = new ExpressionParser.EvalContext
                {
                    Geometry = geo,
                    PointIndex = -1,
                    PrimIndex = i,
                    TotalPoints = geo.Points.Count,
                    TotalPrims = geo.Primitives.Count
                };

                parser.Execute(code, evalCtx);
                ApplyOutputs(geo, evalCtx, i, "primitive");
            }
        }

        private void RunOverDetail(PCGGeometry geo, string code, ExpressionParser parser, PCGContext ctx)
        {
            var evalCtx = new ExpressionParser.EvalContext
            {
                Geometry = geo,
                PointIndex = 0,
                PrimIndex = 0,
                TotalPoints = geo.Points.Count,
                TotalPrims = geo.Primitives.Count
            };

            parser.Execute(code, evalCtx);

            foreach (var kvp in evalCtx.Outputs)
            {
                string varName = kvp.Key;
                if (!varName.StartsWith("@")) continue;
                string attrName = varName.Substring(1);
                if (attrName == "P" || attrName == "ptnum" || attrName == "primnum" ||
                    attrName == "numpt" || attrName == "numprim") continue;

                geo.DetailAttribs.SetAttribute(attrName, kvp.Value);
            }
        }

        private void ApplyOutputs(PCGGeometry geo, ExpressionParser.EvalContext evalCtx, int index, string runOver)
        {
            foreach (var kvp in evalCtx.Outputs)
            {
                string varName = kvp.Key;
                if (!varName.StartsWith("@")) continue;
                string attrName = varName.Substring(1);

                if (attrName == "P" && runOver == "point" && index < geo.Points.Count)
                {
                    if (kvp.Value is Vector3 v3)
                        geo.Points[index] = v3;
                    continue;
                }

                // 跳过内置只读变量
                if (attrName == "ptnum" || attrName == "primnum" ||
                    attrName == "numpt" || attrName == "numprim") continue;

                AttributeStore store = runOver == "point" ? geo.PointAttribs : geo.PrimAttribs;
                var attr = store.GetAttribute(attrName);
                if (attr == null)
                {
                    AttribType type = kvp.Value is Vector3 ? AttribType.Vector3 :
                                     kvp.Value is Color ? AttribType.Color : AttribType.Float;
                    attr = store.CreateAttribute(attrName, type, GetDefault(type));

                    int count = runOver == "point" ? geo.Points.Count : geo.Primitives.Count;
                    for (int j = 0; j < count; j++)
                        attr.Values.Add(attr.DefaultValue);
                }

                if (index < attr.Values.Count)
                    attr.Values[index] = kvp.Value;
            }
        }

        private static object GetDefault(AttribType type)
        {
            switch (type)
            {
                case AttribType.Vector3: return Vector3.zero;
                case AttribType.Color: return Color.white;
                default: return 0f;
            }
        }
    }
}
