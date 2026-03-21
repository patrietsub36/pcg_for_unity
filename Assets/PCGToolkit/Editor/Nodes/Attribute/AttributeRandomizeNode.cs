using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 为属性赋随机值（对标 Houdini Attribute Randomize SOP）
    /// 支持 Uniform 和 Gaussian 分布。
    /// </summary>
    public class AttributeRandomizeNode : PCGNodeBase
    {
        public override string Name => "AttributeRandomize";
        public override string DisplayName => "Attribute Randomize";
        public override string Description => "为属性赋随机值（Uniform/Gaussian）";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "属性名称", "Cd"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级（point/primitive）", "point"),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "值类型（float/vector3/color）", "float"),
            new PCGParamSchema("distribution", PCGPortDirection.Input, PCGPortType.String,
                "Distribution", "分布（uniform/gaussian）", "uniform"),
            new PCGParamSchema("min", PCGPortDirection.Input, PCGPortType.Float,
                "Min", "最小值", 0f),
            new PCGParamSchema("max", PCGPortDirection.Input, PCGPortType.Float,
                "Max", "最大值", 1f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组赋值", ""),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string attrName = GetParamString(parameters, "name", "Cd");
            string attrClass = GetParamString(parameters, "class", "point").ToLower();
            string valType = GetParamString(parameters, "type", "float").ToLower();
            string distribution = GetParamString(parameters, "distribution", "uniform").ToLower();
            float min = GetParamFloat(parameters, "min", 0f);
            float max = GetParamFloat(parameters, "max", 1f);
            int seed = GetParamInt(parameters, "seed", 0);
            string group = GetParamString(parameters, "group", "");

            var rng = new System.Random(seed);

            AttribType aType = valType switch
            {
                "vector3" => AttribType.Vector3,
                "color" => AttribType.Color,
                _ => AttribType.Float
            };

            AttributeStore store = attrClass == "primitive" ? geo.PrimAttribs : geo.PointAttribs;
            int count = attrClass == "primitive" ? geo.Primitives.Count : geo.Points.Count;

            var attr = store.GetAttribute(attrName);
            if (attr == null)
            {
                attr = store.CreateAttribute(attrName, aType, GetDefault(aType));
                for (int i = 0; i < count; i++)
                    attr.Values.Add(attr.DefaultValue);
            }
            while (attr.Values.Count < count)
                attr.Values.Add(attr.DefaultValue);

            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group))
            {
                var groups = attrClass == "primitive" ? geo.PrimGroups : geo.PointGroups;
                if (groups.TryGetValue(group, out var grp))
                    indices = grp;
            }

            for (int i = 0; i < count; i++)
            {
                if (indices != null && !indices.Contains(i)) continue;

                attr.Values[i] = GenerateRandomValue(rng, aType, distribution, min, max);
            }

            return SingleOutput("geometry", geo);
        }

        private object GenerateRandomValue(System.Random rng, AttribType type, string dist, float min, float max)
        {
            switch (type)
            {
                case AttribType.Vector3:
                    return new Vector3(
                        RandFloat(rng, dist, min, max),
                        RandFloat(rng, dist, min, max),
                        RandFloat(rng, dist, min, max));
                case AttribType.Color:
                    return new Color(
                        RandFloat(rng, dist, 0, 1),
                        RandFloat(rng, dist, 0, 1),
                        RandFloat(rng, dist, 0, 1),
                        1f);
                default:
                    return RandFloat(rng, dist, min, max);
            }
        }

        private float RandFloat(System.Random rng, string dist, float min, float max)
        {
            if (dist == "gaussian")
            {
                // Box-Muller transform
                double u1 = 1.0 - rng.NextDouble();
                double u2 = 1.0 - rng.NextDouble();
                double normal = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                                System.Math.Sin(2.0 * System.Math.PI * u2);
                float mean = (min + max) * 0.5f;
                float stddev = (max - min) / 6f; // ~99.7% within [min, max]
                return Mathf.Clamp(mean + (float)normal * stddev, min, max);
            }

            return min + (float)rng.NextDouble() * (max - min);
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
