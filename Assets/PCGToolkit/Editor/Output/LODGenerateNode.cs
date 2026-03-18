using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 生成 LOD（Level of Detail）层级
    /// </summary>
    public class LODGenerateNode : PCGNodeBase
    {
        public override string Name => "LODGenerate";
        public override string DisplayName => "LOD Generate";
        public override string Description => "自动生成多级 LOD 并配置 LODGroup";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（最高精度 LOD0）", null, required: true),
            new PCGParamSchema("lodCount", PCGPortDirection.Input, PCGPortType.Int,
                "LOD Count", "LOD 层级数", 3),
            new PCGParamSchema("reductionRatios", PCGPortDirection.Input, PCGPortType.String,
                "Reduction Ratios", "各级减面比例（逗号分隔，如 1.0,0.5,0.25）", "1.0,0.5,0.25"),
            new PCGParamSchema("screenRelativeHeights", PCGPortDirection.Input, PCGPortType.String,
                "Screen Heights", "各级屏幕占比阈值（逗号分隔，如 0.6,0.3,0.1）", "0.6,0.3,0.1"),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "保存路径", "Assets/PCGOutput/lod_output.prefab"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体（LOD0）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("LODGenerate: 生成 LOD (TODO)");

            var geo = GetInputGeometry(inputGeometries, "input");
            int lodCount = GetParamInt(parameters, "lodCount", 3);
            string reductionRatios = GetParamString(parameters, "reductionRatios", "1.0,0.5,0.25");
            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/lod_output.prefab");

            ctx.Log($"LODGenerate: lodCount={lodCount}, ratios={reductionRatios}, path={assetPath}");

            // TODO: 对输入几何体按比例 Decimate → 创建 LODGroup → 保存 Prefab
            return SingleOutput("geometry", geo);
        }
    }
}
