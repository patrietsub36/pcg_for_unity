using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Skill
{
    /// <summary>
    /// Skill 注册表 — 管理所有可用 Skill
    /// 从 PCGNodeRegistry 自动生成 Skill 列表
    /// </summary>
    public static class SkillRegistry
    {
        private static Dictionary<string, ISkill> skills = new Dictionary<string, ISkill>();
        private static bool initialized = false;

        /// <summary>
        /// 确保注册表已初始化
        /// </summary>
        public static void EnsureInitialized()
        {
            if (initialized) return;

            // TODO: 从 PCGNodeRegistry 获取所有节点，为每个节点创建对应的 Skill
            PCGNodeRegistry.EnsureInitialized();
            var nodes = PCGNodeRegistry.GetAllNodes();

            foreach (var node in nodes)
            {
                var skill = new PCGNodeSkillAdapter(node);
                skills[skill.Name] = skill;
            }

            initialized = true;
            Debug.Log($"SkillRegistry: 已注册 {skills.Count} 个 Skill");
        }

        /// <summary>
        /// 获取指定 Skill
        /// </summary>
        public static ISkill GetSkill(string name)
        {
            EnsureInitialized();
            skills.TryGetValue(name, out var skill);
            return skill;
        }

        /// <summary>
        /// 获取所有 Skill
        /// </summary>
        public static IEnumerable<ISkill> GetAllSkills()
        {
            EnsureInitialized();
            return skills.Values;
        }

        /// <summary>
        /// 获取所有 Skill 名称
        /// </summary>
        public static IEnumerable<string> GetSkillNames()
        {
            EnsureInitialized();
            return skills.Keys;
        }

        /// <summary>
        /// 刷新注册表
        /// </summary>
        public static void Refresh()
        {
            initialized = false;
            skills.Clear();
            EnsureInitialized();
        }
    }
}
