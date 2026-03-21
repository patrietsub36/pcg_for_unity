using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 基于空间距离从源几何体传递属性到目标（对标 Houdini AttribTransfer SOP）
    /// 对目标几何体的每个点，找到源几何体中最近的点，复制其属性值。
    /// </summary>
    public class AttributeTransferNode : PCGNodeBase
    {
        public override string Name => "AttributeTransfer";
        public override string DisplayName => "Attribute Transfer";
        public override string Description => "基于空间距离从源几何体传递属性到目标";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target", "目标几何体（接收属性）", null, required: true),
            new PCGParamSchema("source", PCGPortDirection.Input, PCGPortType.Geometry,
                "Source", "源几何体（提供属性）", null, required: true),
            new PCGParamSchema("attributes", PCGPortDirection.Input, PCGPortType.String,
                "Attributes", "要传递的属性名（逗号分隔，* 表示全部）", "*"),
            new PCGParamSchema("maxDistance", PCGPortDirection.Input, PCGPortType.Float,
                "Max Distance", "最大搜索距离（0=无限）", 0f),
            new PCGParamSchema("blendWidth", PCGPortDirection.Input, PCGPortType.Float,
                "Blend Width", "混合宽度（距离衰减）", 0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "带传递属性的目标几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var target = GetInputGeometry(inputGeometries, "target").Clone();
            var source = GetInputGeometry(inputGeometries, "source");
            string attribStr = GetParamString(parameters, "attributes", "*");
            float maxDist = GetParamFloat(parameters, "maxDistance", 0f);
            float blendWidth = GetParamFloat(parameters, "blendWidth", 0f);

            if (target.Points.Count == 0 || source.Points.Count == 0)
                return SingleOutput("geometry", target);

            // 确定要传递的属性列表
            var attrNames = new List<string>();
            if (attribStr == "*")
            {
                foreach (var name in source.PointAttribs.GetAttributeNames())
                    attrNames.Add(name);
            }
            else
            {
                foreach (var name in attribStr.Split(','))
                {
                    string trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        attrNames.Add(trimmed);
                }
            }

            // 对每个目标点找最近的源点
            int[] nearestIdx = new int[target.Points.Count];
            float[] nearestDist = new float[target.Points.Count];

            for (int i = 0; i < target.Points.Count; i++)
            {
                Vector3 tp = target.Points[i];
                float bestDist = float.MaxValue;
                int bestIdx = 0;

                for (int j = 0; j < source.Points.Count; j++)
                {
                    float d = (tp - source.Points[j]).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = j;
                    }
                }

                nearestIdx[i] = bestIdx;
                nearestDist[i] = Mathf.Sqrt(bestDist);
            }

            // 传递属性
            foreach (string attrName in attrNames)
            {
                var srcAttr = source.PointAttribs.GetAttribute(attrName);
                if (srcAttr == null || srcAttr.Values.Count == 0) continue;

                var dstAttr = target.PointAttribs.GetAttribute(attrName);
                if (dstAttr == null)
                    dstAttr = target.PointAttribs.CreateAttribute(attrName, srcAttr.Type, srcAttr.DefaultValue);

                // 确保目标属性有足够的值
                while (dstAttr.Values.Count < target.Points.Count)
                    dstAttr.Values.Add(dstAttr.DefaultValue);

                for (int i = 0; i < target.Points.Count; i++)
                {
                    float dist = nearestDist[i];
                    if (maxDist > 0 && dist > maxDist) continue;

                    int srcIdx = nearestIdx[i];
                    if (srcIdx >= srcAttr.Values.Count) continue;

                    object srcVal = srcAttr.Values[srcIdx];

                    if (blendWidth > 0 && dist > 0)
                    {
                        float blend = Mathf.Clamp01(1f - dist / blendWidth);
                        dstAttr.Values[i] = BlendValues(dstAttr.Values[i], srcVal, blend, srcAttr.Type);
                    }
                    else
                    {
                        dstAttr.Values[i] = srcVal;
                    }
                }
            }

            ctx.Log($"AttributeTransfer: {attrNames.Count} attrs from {source.Points.Count} to {target.Points.Count} pts");
            return SingleOutput("geometry", target);
        }

        private object BlendValues(object a, object b, float t, AttribType type)
        {
            switch (type)
            {
                case AttribType.Float:
                    float fa = a is float af ? af : 0f;
                    float fb = b is float bf ? bf : 0f;
                    return Mathf.Lerp(fa, fb, t);
                case AttribType.Vector3:
                    Vector3 va = a is Vector3 av ? av : Vector3.zero;
                    Vector3 vb = b is Vector3 bv ? bv : Vector3.zero;
                    return Vector3.Lerp(va, vb, t);
                case AttribType.Color:
                    Color ca = a is Color ac ? ac : Color.black;
                    Color cb = b is Color bc ? bc : Color.black;
                    return Color.Lerp(ca, cb, t);
                default:
                    return t >= 0.5f ? b : a;
            }
        }
    }
}
