using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 导入 Unity Mesh 资产为 PCGGeometry（对标 Houdini File SOP 的导入功能）
    /// </summary>
    public class ImportMeshNode : PCGNodeBase
    {
        public override string Name => "ImportMesh";
        public override string DisplayName => "Import Mesh";
        public override string Description => "从 Unity Mesh 资产导入几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "Mesh 资产路径（Assets/ 开头）", ""),
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
            ctx.Log("ImportMesh: 导入 Mesh 资产 (TODO)");

            string assetPath = GetParamString(parameters, "assetPath", "");
            ctx.Log($"ImportMesh: path={assetPath}");

            // TODO: 使用 AssetDatabase.LoadAssetAtPath 加载 Mesh
            // 然后调用 PCGGeometryToMesh.FromMesh 转换
            var geo = new PCGGeometry();
            return SingleOutput("geometry", geo);
        }
    }
}
