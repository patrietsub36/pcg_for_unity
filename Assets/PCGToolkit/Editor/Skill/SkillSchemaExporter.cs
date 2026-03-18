using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PCGToolkit.Skill
{
    /// <summary>
    /// Skill JSON Schema 导出器
    /// 将所有 Skill 导出为符合 OpenAI Function Calling 规范的 JSON Schema
    /// </summary>
    public static class SkillSchemaExporter
    {
        /// <summary>
        /// 导出所有 Skill 的 JSON Schema
        /// </summary>
        public static string ExportAll()
        {
            // TODO: 遍历 SkillRegistry，为每个 Skill 生成 JSON Schema
            Debug.Log("SkillSchemaExporter: ExportAll (TODO)");

            SkillRegistry.EnsureInitialized();
            var sb = new StringBuilder();
            sb.AppendLine("[");

            bool first = true;
            foreach (var skill in SkillRegistry.GetAllSkills())
            {
                if (!first) sb.AppendLine(",");
                first = false;
                sb.Append(skill.GetJsonSchema());
            }

            sb.AppendLine("]");
            return sb.ToString();
        }

        /// <summary>
        /// 导出单个 Skill 的 JSON Schema
        /// </summary>
        public static string ExportSingle(string skillName)
        {
            // TODO: 生成指定 Skill 的 JSON Schema
            var skill = SkillRegistry.GetSkill(skillName);
            if (skill == null)
            {
                Debug.LogWarning($"SkillSchemaExporter: Skill not found - {skillName}");
                return "{}";
            }
            return skill.GetJsonSchema();
        }

        /// <summary>
        /// 导出到文件
        /// </summary>
        public static void ExportToFile(string filePath)
        {
            // TODO: 将所有 Schema 写入文件
            Debug.Log($"SkillSchemaExporter: ExportToFile - {filePath} (TODO)");
        }
    }
}
