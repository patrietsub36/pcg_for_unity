using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// PCG 节点图的 GraphView 实现
    /// 负责节点的可视化、连线、缩放平移等交互
    /// </summary>
    public class PCGGraphView : GraphView
    {
        private PCGGraphData graphData;

        public PCGGraphView()
        {
            // TODO: 初始化 GraphView 基础设置
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // TODO: 加载 USS 样式表
            // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("...");
            // styleSheets.Add(styleSheet);

            // 添加网格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        /// <summary>
        /// 从 PCGGraphData 加载节点图
        /// </summary>
        public void LoadGraph(PCGGraphData data)
        {
            // TODO: 清空当前视图，根据 data 重建所有节点和连线
            graphData = data;
            Debug.Log($"PCGGraphView: LoadGraph - {data.GraphName} (TODO)");
        }

        /// <summary>
        /// 将当前视图状态保存回 PCGGraphData
        /// </summary>
        public void SaveGraph()
        {
            // TODO: 遍历所有节点和连线，更新 graphData
            Debug.Log("PCGGraphView: SaveGraph (TODO)");
        }

        /// <summary>
        /// 创建节点的可视化表示并添加到视图
        /// </summary>
        public PCGNodeVisual CreateNodeVisual(IPCGNode node, Vector2 position)
        {
            // TODO: 创建 PCGNodeVisual，设置端口，添加到视图
            Debug.Log($"PCGGraphView: CreateNodeVisual - {node.Name} (TODO)");
            return null;
        }

        /// <summary>
        /// 获取兼容的端口列表（用于连线时的类型检查）
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            // TODO: 根据 PCGPortType 过滤兼容端口
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port &&
                    startPort.node != port.node &&
                    startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        /// <summary>
        /// 右键菜单
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // TODO: 添加创建节点、复制、粘贴等菜单项
            base.BuildContextualMenu(evt);
        }
    }
}
