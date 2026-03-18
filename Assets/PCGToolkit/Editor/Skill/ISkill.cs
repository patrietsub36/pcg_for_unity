using System.Collections.Generic;

namespace PCGToolkit.Skill
{
    /// <summary>
    /// Skill 接口 — 每个 PCG 节点自动导出为一个 Skill
    /// AI Agent 通过 Skill 调用节点功能
    /// </summary>
    public interface ISkill
    {
        /// <summary>
        /// Skill 唯一名称（与节点名称对应）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Skill 显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Skill 描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 获取 JSON Schema（用于 AI Agent 调用）
        /// </summary>
        string GetJsonSchema();

        /// <summary>
        /// 执行 Skill
        /// </summary>
        /// <param name="parametersJson">JSON 格式的参数</param>
        /// <returns>JSON 格式的执行结果</returns>
        string Execute(string parametersJson);

        /// <summary>
        /// 获取 Skill 的输入参数定义
        /// </summary>
        List<SkillParameter> GetParameters();
    }

    /// <summary>
    /// Skill 参数定义
    /// </summary>
    public class SkillParameter
    {
        public string Name;
        public string Type;
        public string Description;
        public object DefaultValue;
        public bool Required;
    }
}
