using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 将 PCGGeometry 导出为 Unity Mesh 资产（对标 Houdini File SOP 的导出功能）
    /// </summary>
    public class ExportMeshNode : PCGNodeBase
    {
        public override string Name => "ExportMesh";
        public override string DisplayName => "Export Mesh";
        public override string Description => "将几何体导出为 Unity Mesh 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "保存路径（Assets/ 开头）", "Assets/PCGOutput/mesh.asset"),
            new PCGParamSchema("createRenderer", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Renderer", "是否在场景中创建 MeshRenderer 预览", true),
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
            ctx.Log("ExportMesh: 导出 Mesh 资产 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/mesh.asset");
            bool createRenderer = GetParamBool(parameters, "createRenderer", true);

            ctx.Log($"ExportMesh: path={assetPath}, createRenderer={createRenderer}");

            // TODO: 调用 PCGGeometryToMesh.Convert 转换为 Mesh
            // 然后使用 AssetDatabase.CreateAsset 保存
            return SingleOutput("geometry", geo);
        }
    }
}
