using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 节点图 DAG 执行引擎
    /// 支持拓扑排序、脏标记增量执行、缓存
    /// </summary>
    public class PCGGraphExecutor
    {
        private PCGGraphData graphData;
        private PCGContext context;
        private HashSet<string> dirtyNodes = new HashSet<string>();

        public PCGGraphExecutor(PCGGraphData data)
        {
            graphData = data;
            context = new PCGContext();
        }

        /// <summary>
        /// 执行整个节点图
        /// </summary>
        public void Execute()
        {
            // TODO: 拓扑排序 → 按顺序执行每个节点
            Debug.Log("PCGGraphExecutor: Execute (TODO)");

            var sortedNodes = TopologicalSort();
            foreach (var nodeData in sortedNodes)
            {
                ExecuteNode(nodeData);
            }
        }

        /// <summary>
        /// 增量执行（仅执行脏节点及其下游）
        /// </summary>
        public void ExecuteIncremental()
        {
            // TODO: 从脏节点开始，仅执行受影响的子图
            Debug.Log("PCGGraphExecutor: ExecuteIncremental (TODO)");
        }

        /// <summary>
        /// 标记节点为脏（参数变更时调用）
        /// </summary>
        public void MarkDirty(string nodeId)
        {
            dirtyNodes.Add(nodeId);
            // TODO: 递归标记所有下游节点
            Debug.Log($"PCGGraphExecutor: MarkDirty - {nodeId} (TODO)");
        }

        /// <summary>
        /// 拓扑排序
        /// </summary>
        private List<PCGNodeData> TopologicalSort()
        {
            // TODO: 基于 graphData.Edges 构建 DAG 并进行拓扑排序
            Debug.Log("PCGGraphExecutor: TopologicalSort (TODO)");
            return new List<PCGNodeData>(graphData.Nodes);
        }

        /// <summary>
        /// 执行单个节点
        /// </summary>
        private void ExecuteNode(PCGNodeData nodeData)
        {
            // TODO: 从 PCGNodeRegistry 获取节点实例
            // 收集输入几何体（从缓存或上游节点输出）
            // 调用 node.Execute()
            // 缓存输出
            context.CurrentNodeId = nodeData.NodeId;
            Debug.Log($"PCGGraphExecutor: ExecuteNode - {nodeData.NodeType} (TODO)");
        }

        /// <summary>
        /// 获取节点的执行结果
        /// </summary>
        public PCGGeometry GetNodeOutput(string nodeId, string portName = "geometry")
        {
            return context.GetCachedOutput($"{nodeId}.{portName}");
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            context.ClearCache();
            dirtyNodes.Clear();
        }
    }
}
