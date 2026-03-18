using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 创建并保存材质资产
    /// </summary>
    public class SaveMaterialNode : PCGNodeBase
    {
        public override string Name => "SaveMaterial";
        public override string DisplayName => "Save Material";
        public override string Description => "根据几何体属性创建并保存 Unity Material 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（可带 Cd 颜色属性）", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "材质保存路径", "Assets/PCGOutput/material.mat"),
            new PCGParamSchema("shader", PCGPortDirection.Input, PCGPortType.String,
                "Shader", "Shader 名称", "Standard"),
            new PCGParamSchema("color", PCGPortDirection.Input, PCGPortType.Color,
                "Color", "基础颜色", null),
            new PCGParamSchema("useVertexColor", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Vertex Color", "是否使用顶点颜色", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("SaveMaterial: 保存材质 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/material.mat");
            string shader = GetParamString(parameters, "shader", "Standard");

            ctx.Log($"SaveMaterial: path={assetPath}, shader={shader}");

            // TODO: new Material(Shader.Find(shader)) → 设置属性 → AssetDatabase.CreateAsset
            return SingleOutput("geometry", geo);
        }
    }
}
