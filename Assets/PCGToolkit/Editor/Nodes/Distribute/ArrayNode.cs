using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 阵列复制（对标 Houdini Copy + Transform / Radial Copy）
    /// linear 模式：沿偏移方向累加复制
    /// radial 模式：绕轴旋转 + 半径偏移复制
    /// </summary>
    public class ArrayNode : PCGNodeBase
    {
        public override string Name => "Array";
        public override string DisplayName => "Array";
        public override string Description => "线性阵列或径向阵列复制几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "阵列模式（linear/radial）", "linear")
            {
                EnumOptions = new[] { "linear", "radial" }
            },
            new PCGParamSchema("count", PCGPortDirection.Input, PCGPortType.Int,
                "Count", "复制数量（含原始）", 5),
            new PCGParamSchema("offset", PCGPortDirection.Input, PCGPortType.Vector3,
                "Offset", "linear 模式：每次复制的偏移量", new Vector3(1f, 0f, 0f)),
            new PCGParamSchema("axis", PCGPortDirection.Input, PCGPortType.Vector3,
                "Axis", "radial 模式：旋转轴", Vector3.up),
            new PCGParamSchema("center", PCGPortDirection.Input, PCGPortType.Vector3,
                "Center", "radial 模式：旋转中心", Vector3.zero),
            new PCGParamSchema("fullAngle", PCGPortDirection.Input, PCGPortType.Float,
                "Full Angle", "radial 模式：总旋转角度（度）", 360f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "阵列后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string mode = GetParamString(parameters, "mode", "linear").ToLower();
            int count = Mathf.Max(1, GetParamInt(parameters, "count", 5));

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            var result = new PCGGeometry();

            if (mode == "radial")
            {
                Vector3 axis = GetParamVector3(parameters, "axis", Vector3.up).normalized;
                Vector3 center = GetParamVector3(parameters, "center", Vector3.zero);
                float fullAngle = GetParamFloat(parameters, "fullAngle", 360f);

                for (int i = 0; i < count; i++)
                {
                    float angle = (count > 1) ? fullAngle * i / count : 0f;
                    Quaternion rot = Quaternion.AngleAxis(angle, axis);
                    AppendTransformed(result, geo, Vector3.zero, rot, center, i);
                }
            }
            else
            {
                Vector3 offset = GetParamVector3(parameters, "offset", new Vector3(1f, 0f, 0f));
                for (int i = 0; i < count; i++)
                {
                    Vector3 translation = offset * i;
                    AppendTransformed(result, geo, translation, Quaternion.identity, Vector3.zero, i);
                }
            }

            ctx.Log($"Array: {mode} x{count}, {result.Points.Count} pts, {result.Primitives.Count} faces");
            return SingleOutput("geometry", result);
        }

        private void AppendTransformed(PCGGeometry result, PCGGeometry src, Vector3 translation, Quaternion rotation, Vector3 rotCenter, int copyIndex)
        {
            int pointOffset = result.Points.Count;

            for (int i = 0; i < src.Points.Count; i++)
            {
                Vector3 p = src.Points[i];
                p = rotation * (p - rotCenter) + rotCenter + translation;
                result.Points.Add(p);
            }

            foreach (var prim in src.Primitives)
            {
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = prim[i] + pointOffset;
                result.Primitives.Add(newPrim);
            }

            // 合并点属性
            foreach (var attr in src.PointAttribs.GetAllAttributes())
            {
                var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    for (int j = 0; j < pointOffset; j++)
                        destAttr.Values.Add(destAttr.DefaultValue);
                }

                if (attr.Name == "N")
                {
                    foreach (var val in attr.Values)
                    {
                        if (val is Vector3 n)
                            destAttr.Values.Add(rotation * n);
                        else
                            destAttr.Values.Add(val);
                    }
                }
                else
                {
                    destAttr.Values.AddRange(attr.Values);
                }
            }

            // 注入 @copynum 属性（每个副本的点都有相同的 copynum 值）
            var copynumAttr = result.PointAttribs.GetAttribute("copynum");
            if (copynumAttr == null)
            {
                copynumAttr = result.PointAttribs.CreateAttribute("copynum", typeof(float), 0f);
                for (int j = 0; j < pointOffset; j++)
                    copynumAttr.Values.Add(0f);
            }
            for (int i = 0; i < src.Points.Count; i++)
                copynumAttr.Values.Add((float)copyIndex);
        }
    }
}
