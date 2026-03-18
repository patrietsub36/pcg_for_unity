using System;
using UnityEngine;

namespace PCGToolkit.Communication
{
    /// <summary>
    /// AI Agent 通信协议定义
    /// 定义请求/响应格式
    /// </summary>
    public static class AgentProtocol
    {
        /// <summary>
        /// Agent 请求数据结构
        /// </summary>
        [Serializable]
        public class AgentRequest
        {
            /// <summary>
            /// 请求动作（execute_skill / list_skills / get_schema / get_all_schemas）
            /// </summary>
            public string Action;

            /// <summary>
            /// Skill 名称（execute_skill / get_schema 时使用）
            /// </summary>
            public string SkillName;

            /// <summary>
            /// JSON 格式的参数
            /// </summary>
            public string Parameters;

            /// <summary>
            /// 请求 ID（用于异步响应匹配）
            /// </summary>
            public string RequestId;
        }

        /// <summary>
        /// Agent 响应数据结构
        /// </summary>
        [Serializable]
        public class AgentResponse
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success;

            /// <summary>
            /// 请求 ID（与请求匹配）
            /// </summary>
            public string RequestId;

            /// <summary>
            /// 响应数据（JSON 字符串）
            /// </summary>
            public string Data;

            /// <summary>
            /// 错误信息
            /// </summary>
            public string Error;
        }

        /// <summary>
        /// 解析请求 JSON
        /// </summary>
        public static AgentRequest ParseRequest(string json)
        {
            // TODO: 使用 JsonUtility 或更强大的 JSON 库解析
            Debug.Log("AgentProtocol: ParseRequest (TODO)");
            return JsonUtility.FromJson<AgentRequest>(json);
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static string CreateSuccessResponse(string data, string requestId = "")
        {
            var response = new AgentResponse
            {
                Success = true,
                RequestId = requestId,
                Data = data,
                Error = "",
            };
            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        public static string CreateErrorResponse(string error, string requestId = "")
        {
            var response = new AgentResponse
            {
                Success = false,
                RequestId = requestId,
                Data = "",
                Error = error,
            };
            return JsonUtility.ToJson(response);
        }
    }
}
