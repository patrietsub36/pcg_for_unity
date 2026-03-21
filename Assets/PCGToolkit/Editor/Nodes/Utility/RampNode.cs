using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 曲线 Ramp 映射（对标 Houdini Ramp Parameter）
    /// 将输入值 [0,1] 通过插值曲线映射到输出值。
    /// 模式: linear / smooth / step
    /// </summary>
    public class RampNode : PCGNodeBase
    {
        public override string Name => "Ramp";
        public override string DisplayName => "Ramp";
        public override string Description => "曲线 Ramp 映射（线性/平滑/阶梯）";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.Float,
                "Value", "输入值（0~1）", 0f),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "插值模式（linear/smooth/step）", "smooth"),
            new PCGParamSchema("key0Pos", PCGPortDirection.Input, PCGPortType.Float,
                "Key 0 Position", "关键帧0位置", 0f),
            new PCGParamSchema("key0Val", PCGPortDirection.Input, PCGPortType.Float,
                "Key 0 Value", "关键帧0值", 0f),
            new PCGParamSchema("key1Pos", PCGPortDirection.Input, PCGPortType.Float,
                "Key 1 Position", "关键帧1位置", 0.5f),
            new PCGParamSchema("key1Val", PCGPortDirection.Input, PCGPortType.Float,
                "Key 1 Value", "关键帧1值", 1f),
            new PCGParamSchema("key2Pos", PCGPortDirection.Input, PCGPortType.Float,
                "Key 2 Position", "关键帧2位置", 1f),
            new PCGParamSchema("key2Val", PCGPortDirection.Input, PCGPortType.Float,
                "Key 2 Value", "关键帧2值", 0f),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Float,
                "Value", "映射后的值"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            float v = GetParamFloat(parameters, "value", 0f);
            string mode = GetParamString(parameters, "mode", "smooth").ToLower();

            // 收集关键帧并排序
            var keys = new List<(float pos, float val)>
            {
                (GetParamFloat(parameters, "key0Pos", 0f), GetParamFloat(parameters, "key0Val", 0f)),
                (GetParamFloat(parameters, "key1Pos", 0.5f), GetParamFloat(parameters, "key1Val", 1f)),
                (GetParamFloat(parameters, "key2Pos", 1f), GetParamFloat(parameters, "key2Val", 0f)),
            };
            keys.Sort((a, b) => a.pos.CompareTo(b.pos));

            float result = EvaluateRamp(v, keys, mode);

            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = result;
            return new Dictionary<string, PCGGeometry>();
        }

        private float EvaluateRamp(float t, List<(float pos, float val)> keys, string mode)
        {
            if (keys.Count == 0) return 0f;
            if (t <= keys[0].pos) return keys[0].val;
            if (t >= keys[keys.Count - 1].pos) return keys[keys.Count - 1].val;

            // 找到 t 所在的区间
            for (int i = 0; i < keys.Count - 1; i++)
            {
                if (t >= keys[i].pos && t <= keys[i + 1].pos)
                {
                    float range = keys[i + 1].pos - keys[i].pos;
                    if (range < 1e-8f) return keys[i].val;
                    float localT = (t - keys[i].pos) / range;

                    switch (mode)
                    {
                        case "step":
                            return keys[i].val;
                        case "smooth":
                            // Hermite smoothstep
                            localT = localT * localT * (3f - 2f * localT);
                            return Mathf.Lerp(keys[i].val, keys[i + 1].val, localT);
                        default: // linear
                            return Mathf.Lerp(keys[i].val, keys[i + 1].val, localT);
                    }
                }
            }

            return keys[keys.Count - 1].val;
        }
    }
}
