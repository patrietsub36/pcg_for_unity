using UnityEngine;

namespace PCGToolkit.Skill
{
    /// <summary>
    /// Skill 执行器 — 处理 AI Agent 的 Skill 调用请求
    /// </summary>
    public class SkillExecutor
    {
        /// <summary>
        /// 根据 Skill 名称和 JSON 参数执行 Skill
        /// </summary>
        public string ExecuteSkill(string skillName, string parametersJson)
        {
            // TODO: 从 SkillRegistry 查找 Skill 并执行
            Debug.Log($"SkillExecutor: ExecuteSkill - {skillName} (TODO)");

            var skill = SkillRegistry.GetSkill(skillName);
            if (skill == null)
            {
                return $"{{ \"error\": \"Skill not found: {skillName}\" }}";
            }

            return skill.Execute(parametersJson);
        }

        /// <summary>
        /// 执行多个 Skill（管线/链式调用）
        /// </summary>
        public string ExecutePipeline(string[] skillNames, string[] parametersJsonArray)
        {
            // TODO: 按顺序执行多个 Skill，上一个的输出作为下一个的输入
            Debug.Log($"SkillExecutor: ExecutePipeline - {skillNames.Length} skills (TODO)");
            return "{ \"status\": \"TODO\" }";
        }

        /// <summary>
        /// 列出所有可用的 Skill
        /// </summary>
        public string ListSkills()
        {
            // TODO: 返回所有 Skill 的名称和描述
            Debug.Log("SkillExecutor: ListSkills (TODO)");
            return SkillSchemaExporter.ExportAll();
        }
    }
}
