using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 将生成的内容保存到 Unity Scene
    /// </summary>
    public class SaveSceneNode : PCGNodeBase
    {
        public override string Name => "SaveScene";
        public override string DisplayName => "Save Scene";
        public override string Description => "将生成的几何体放置到场景中并保存";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("parentPath", PCGPortDirection.Input, PCGPortType.String,
                "Parent Path", "父物体路径（Hierarchy 中的路径）", ""),
            new PCGParamSchema("objectName", PCGPortDirection.Input, PCGPortType.String,
                "Object Name", "创建的 GameObject 名称", "PCG_Output"),
            new PCGParamSchema("position", PCGPortDirection.Input, PCGPortType.Vector3,
                "Position", "放置位置", Vector3.zero),
            new PCGParamSchema("replaceExisting", PCGPortDirection.Input, PCGPortType.Bool,
                "Replace Existing", "替换同名已存在的物体", true),
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
            ctx.Log("SaveScene: 保存到场景 (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            string objectName = GetParamString(parameters, "objectName", "PCG_Output");
            Vector3 position = GetParamVector3(parameters, "position", Vector3.zero);

            ctx.Log($"SaveScene: name={objectName}, position={position}");

            // TODO: PCGGeometryToMesh.Convert → 创建/替换 GameObject → 添加 MeshFilter + MeshRenderer
            return SingleOutput("geometry", geo);
        }
    }
}
