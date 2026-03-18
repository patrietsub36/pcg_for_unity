using System;
using UnityEngine;
using PCGToolkit.Skill;

namespace PCGToolkit.Communication
{
    /// <summary>
    /// AI Agent 通信服务器
    /// 支持 HTTP / WebSocket / stdin-stdout 三种通信方式
    /// </summary>
    public class AgentServer
    {
        /// <summary>
        /// 通信协议类型
        /// </summary>
        public enum ProtocolType
        {
            Http,
            WebSocket,
            StdInOut
        }

        private ProtocolType protocol;
        private int port;
        private bool isRunning;
        private SkillExecutor skillExecutor;

        public bool IsRunning => isRunning;

        public AgentServer(ProtocolType protocol = ProtocolType.Http, int port = 8765)
        {
            this.protocol = protocol;
            this.port = port;
            this.skillExecutor = new SkillExecutor();
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public void Start()
        {
            // TODO: 根据 protocol 类型启动对应的服务器
            // HTTP: 使用 HttpListener
            // WebSocket: 使用 WebSocket 库
            // StdInOut: 监听标准输入
            isRunning = true;
            Debug.Log($"AgentServer: Start - protocol={protocol}, port={port} (TODO)");
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public void Stop()
        {
            // TODO: 停止服务器，释放资源
            isRunning = false;
            Debug.Log("AgentServer: Stop (TODO)");
        }

        /// <summary>
        /// 处理收到的请求
        /// </summary>
        public string HandleRequest(string requestJson)
        {
            // TODO: 解析请求 JSON → 路由到对应的处理函数
            Debug.Log($"AgentServer: HandleRequest (TODO)");

            try
            {
                var request = AgentProtocol.ParseRequest(requestJson);
                return ProcessRequest(request);
            }
            catch (Exception e)
            {
                return AgentProtocol.CreateErrorResponse($"Request handling failed: {e.Message}");
            }
        }

        private string ProcessRequest(AgentProtocol.AgentRequest request)
        {
            // TODO: 根据请求类型分发处理
            switch (request.Action)
            {
                case "execute_skill":
                    return skillExecutor.ExecuteSkill(request.SkillName, request.Parameters);

                case "list_skills":
                    return skillExecutor.ListSkills();

                case "get_schema":
                    return SkillSchemaExporter.ExportSingle(request.SkillName);

                case "get_all_schemas":
                    return SkillSchemaExporter.ExportAll();

                default:
                    return AgentProtocol.CreateErrorResponse($"Unknown action: {request.Action}");
            }
        }
    }
}
