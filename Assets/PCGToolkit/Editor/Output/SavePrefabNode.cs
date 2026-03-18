using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 保存为 Prefab 资产
    /// </summary>
    public class SavePrefabNode : PCGNodeBase
    {
        public override string Name => "SavePrefab";
        public override string DisplayName => "Save Prefab";
        public override string Description => "将几何体转换为 Mesh 并保存为 Prefab 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "保存路径（Assets/ 开头，.prefab 后缀）", "Assets/PCGOutput/output.prefab"),
            new PCGParamSchema("material", PCGPortDirection.Input, PCGPortType.String,
                "Material", "材质路径（留空使用默认材质）", ""),
            new PCGParamSchema("generateCollider", PCGPortDirection.Input, PCGPortType.Bool,
                "Generate Collider", "是否添加 MeshCollider", false),
            new PCGParamSchema("isStatic", PCGPortDirection.Input, PCGPortType.Bool,
                "Is Static", "标记为 Static（用于烘焙光照等）", true),
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
            ctx.Log("SavePrefab: 保存 Prefab (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/output.prefab");
            bool generateCollider = GetParamBool(parameters, "generateCollider", false);

            ctx.Log($"SavePrefab: path={assetPath}, collider={generateCollider}");

            // TODO: PCGGeometryToMesh.Convert → 创建 GameObject → PrefabUtility.SaveAsPrefabAsset
            return SingleOutput("geometry", geo);
        }
    }
}
