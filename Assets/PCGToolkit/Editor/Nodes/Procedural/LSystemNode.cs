using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// L-System 程序化生成（对标 Houdini L-System SOP）
    /// </summary>
    public class LSystemNode : PCGNodeBase
    {
        public override string Name => "LSystem";
        public override string DisplayName => "L-System";
        public override string Description => "使用 Lindenmayer 系统生成分形/植物几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("axiom", PCGPortDirection.Input, PCGPortType.String,
                "Axiom", "初始公理字符串", "F"),
            new PCGParamSchema("rules", PCGPortDirection.Input, PCGPortType.String,
                "Rules", "产生式规则（格式: F=FF+[+F-F-F]-[-F+F+F]，多条规则用分号分隔）",
                "F=FF+[+F-F-F]-[-F+F+F]"),
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "迭代次数", 3),
            new PCGParamSchema("angle", PCGPortDirection.Input, PCGPortType.Float,
                "Angle", "转向角度", 25.7f),
            new PCGParamSchema("stepLength", PCGPortDirection.Input, PCGPortType.Float,
                "Step Length", "每步前进长度", 1.0f),
            new PCGParamSchema("stepLengthScale", PCGPortDirection.Input, PCGPortType.Float,
                "Step Length Scale", "每次迭代的长度缩放", 0.5f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子（用于随机规则）", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "生成的几何体（线段/管道）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("LSystem: L-System 生成 (TODO)");

            string axiom = GetParamString(parameters, "axiom", "F");
            string rules = GetParamString(parameters, "rules", "F=FF+[+F-F-F]-[-F+F+F]");
            int iterations = GetParamInt(parameters, "iterations", 3);
            float angle = GetParamFloat(parameters, "angle", 25.7f);
            float stepLength = GetParamFloat(parameters, "stepLength", 1.0f);

            ctx.Log($"LSystem: axiom={axiom}, iterations={iterations}, angle={angle}, step={stepLength}");

            // TODO: 字符串重写 → 龟壳解释器 → 生成线段/管道几何体
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
