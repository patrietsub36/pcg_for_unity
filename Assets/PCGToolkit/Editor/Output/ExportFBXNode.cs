using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 导出为 FBX 文件（使用 com.unity.formats.fbx）
    /// </summary>
    public class ExportFBXNode : PCGNodeBase
    {
        public override string Name => "ExportFBX";
        public override string DisplayName => "Export FBX";
        public override string Description => "将几何体导出为 FBX 文件";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("filePath", PCGPortDirection.Input, PCGPortType.String,
                "File Path", "FBX 文件保存路径", "Assets/PCGOutput/output.fbx"),
            new PCGParamSchema("exportFormat", PCGPortDirection.Input, PCGPortType.String,
                "Format", "导出格式（binary/ascii）", "binary"),
            new PCGParamSchema("includeAnimation", PCGPortDirection.Input, PCGPortType.Bool,
                "Include Animation", "是否包含动画数据", false),
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
            ctx.Log("ExportFBX: 导出 FBX (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            string filePath = GetParamString(parameters, "filePath", "Assets/PCGOutput/output.fbx");
            string exportFormat = GetParamString(parameters, "exportFormat", "binary");

            ctx.Log($"ExportFBX: path={filePath}, format={exportFormat}");

            // TODO: PCGGeometryToMesh.Convert → ModelExporter.ExportObject (com.unity.formats.fbx)
            return SingleOutput("geometry", geo);
        }
    }
}
