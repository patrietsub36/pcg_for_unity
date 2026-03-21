using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// MaterialAssign 节点：为指定的面分组分配材质。
    /// 通过设置 PrimAttribs 的 @material 属性实现多材质输出。
    /// </summary>
    public class MaterialAssignNode : PCGNodeBase
    {
        public override string Name => "MaterialAssign";
        public override string DisplayName => "Material Assign";
        public override string Description => "为指定的面分组分配材质";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "面分组名称（留空=所有面）", ""),
            new PCGParamSchema("materialPath", PCGPortDirection.Input, PCGPortType.String,
                "Material Path", "材质路径（如 Assets/Materials/MyMat.mat）", ""),
            new PCGParamSchema("materialId", PCGPortDirection.Input, PCGPortType.Int,
                "Material ID", "材质 ID（可选，用于整数索引）", 0),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带材质属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string group = GetParamString(parameters, "group", "");
            string materialPath = GetParamString(parameters, "materialPath", "");
            int materialId = GetParamInt(parameters, "materialId", 0);

            if (geo.Primitives.Count == 0)
            {
                ctx.LogWarning("MaterialAssign: 输入几何体没有面");
                return SingleOutput("geometry", geo);
            }

            // 确定要分配材质的面
            HashSet<int> targetPrims;
            if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var groupPrims))
            {
                targetPrims = groupPrims;
            }
            else if (!string.IsNullOrEmpty(group))
            {
                ctx.LogWarning($"MaterialAssign: 分组 '{group}' 不存在，跳过分配");
                return SingleOutput("geometry", geo);
            }
            else
            {
                // 所有面
                targetPrims = new HashSet<int>();
                for (int i = 0; i < geo.Primitives.Count; i++)
                    targetPrims.Add(i);
            }

            // 创建或获取 material 属性
            PCGAttribute materialAttr = geo.PrimAttribs.GetAttribute("material");
            if (materialAttr == null)
            {
                materialAttr = geo.PrimAttribs.CreateAttribute("material", typeof(string), "");
                // 初始化所有面为空字符串
                for (int i = 0; i < geo.Primitives.Count; i++)
                    materialAttr.Values.Add("");
            }

            // 同时设置 materialId 属性（可选）
            PCGAttribute materialIdAttr = geo.PrimAttribs.GetAttribute("materialId");
            if (materialIdAttr == null)
            {
                materialIdAttr = geo.PrimAttribs.CreateAttribute("materialId", typeof(float), 0f);
                for (int i = 0; i < geo.Primitives.Count; i++)
                    materialIdAttr.Values.Add(0f);
            }

            // 分配材质
            int assignedCount = 0;
            foreach (int primIdx in targetPrims)
            {
                if (primIdx < materialAttr.Values.Count)
                {
                    materialAttr.Values[primIdx] = materialPath;
                    materialIdAttr.Values[primIdx] = (float)materialId;
                    assignedCount++;
                }
            }

            ctx.Log($"MaterialAssign: 为 {assignedCount} 个面分配了材质 '{materialPath}' (ID: {materialId})");
            return SingleOutput("geometry", geo);
        }
    }
}