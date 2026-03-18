using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Skill
{
    /// <summary>
    /// 将 IPCGNode 适配为 ISkill 的适配器
    /// 每个 PCG 节点自动成为一个可被 AI Agent 调用的 Skill
    /// </summary>
    public class PCGNodeSkillAdapter : ISkill
    {
        private readonly IPCGNode node;

        public PCGNodeSkillAdapter(IPCGNode node)
        {
            this.node = node;
        }

        public string Name => node.Name;
        public string DisplayName => node.DisplayName;
        public string Description => node.Description;

        public string GetJsonSchema()
        {
            // TODO: 根据节点的 Inputs 生成 JSON Schema
            // 格式符合 OpenAI Function Calling / Tool Use 规范
            Debug.Log($"PCGNodeSkillAdapter: GetJsonSchema - {node.Name} (TODO)");
            return "{}";
        }

        public string Execute(string parametersJson)
        {
            // TODO: 解析 JSON 参数 → 调用 node.Execute → 序列化结果为 JSON
            Debug.Log($"PCGNodeSkillAdapter: Execute - {node.Name} (TODO)");
            return "{ \"status\": \"TODO\" }";
        }

        public List<SkillParameter> GetParameters()
        {
            // TODO: 从节点的 Inputs 转换为 SkillParameter 列表
            var parameters = new List<SkillParameter>();

            if (node.Inputs != null)
            {
                foreach (var input in node.Inputs)
                {
                    // 跳过 Geometry 类型的端口（由图连线决定，非 Skill 参数）
                    if (input.PortType == PCGPortType.Geometry) continue;

                    parameters.Add(new SkillParameter
                    {
                        Name = input.Name,
                        Type = input.PortType.ToString().ToLower(),
                        Description = input.Description,
                        DefaultValue = input.DefaultValue,
                        Required = input.Required,
                    });
                }
            }

            return parameters;
        }
    }
}
