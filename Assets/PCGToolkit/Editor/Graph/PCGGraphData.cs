using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 节点图中单个节点的序列化数据
    /// </summary>
    [Serializable]
    public class PCGNodeData
    {
        public string NodeId;
        public string NodeType;
        public Vector2 Position;
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// 节点图中单条连线的序列化数据
    /// </summary>
    [Serializable]
    public class PCGEdgeData
    {
        public string OutputNodeId;
        public string OutputPortName;
        public string InputNodeId;
        public string InputPortName;
    }

    /// <summary>
    /// 节点图的完整序列化数据（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "NewPCGGraph", menuName = "PCG Toolkit/PCG Graph")]
    public class PCGGraphData : ScriptableObject
    {
        public string GraphName = "New Graph";
        public List<PCGNodeData> Nodes = new List<PCGNodeData>();
        public List<PCGEdgeData> Edges = new List<PCGEdgeData>();

        /// <summary>
        /// 添加节点数据
        /// </summary>
        public PCGNodeData AddNode(string nodeType, Vector2 position)
        {
            // TODO: 实现添加节点
            var data = new PCGNodeData
            {
                NodeId = Guid.NewGuid().ToString(),
                NodeType = nodeType,
                Position = position,
            };
            Nodes.Add(data);
            return data;
        }

        /// <summary>
        /// 移除节点数据及其关联的连线
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            // TODO: 移除节点及关联连线
            Nodes.RemoveAll(n => n.NodeId == nodeId);
            Edges.RemoveAll(e => e.OutputNodeId == nodeId || e.InputNodeId == nodeId);
        }

        /// <summary>
        /// 添加连线
        /// </summary>
        public PCGEdgeData AddEdge(string outputNodeId, string outputPortName,
            string inputNodeId, string inputPortName)
        {
            // TODO: 添加连线（含类型兼容性检查）
            var edge = new PCGEdgeData
            {
                OutputNodeId = outputNodeId,
                OutputPortName = outputPortName,
                InputNodeId = inputNodeId,
                InputPortName = inputPortName,
            };
            Edges.Add(edge);
            return edge;
        }

        /// <summary>
        /// 清空图数据
        /// </summary>
        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
        }
    }
}
