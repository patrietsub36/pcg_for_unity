using System;  
using System.Collections.Generic;  
using System.Linq;  
using UnityEditor;  
using UnityEditor.Experimental.GraphView;  
using UnityEngine;  
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
        private PCGNodeSearchWindow _searchWindow;  
        private PCGGraphEditorWindow _editorWindow;  
  
        public PCGGraphView()  
        {  
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);  
  
            this.AddManipulator(new ContentDragger());  
            this.AddManipulator(new SelectionDragger());  
            this.AddManipulator(new RectangleSelector());  
  
            // 添加网格背景  
            var grid = new GridBackground();  
            Insert(0, grid);  
            grid.StretchToParentSize();  
  
            // 注册 graphViewChanged 回调处理连线和删除  
            graphViewChanged += OnGraphViewChanged;  
        }  
  
        /// <summary>  
        /// 初始化搜索窗口和 nodeCreationRequest  
        /// </summary>  
        public void Initialize(PCGGraphEditorWindow editorWindow)  
        {  
            _editorWindow = editorWindow;  
  
            _searchWindow = ScriptableObject.CreateInstance<PCGNodeSearchWindow>();  
            _searchWindow.Initialize(this, editorWindow);  
  
            nodeCreationRequest = ctx =>  
            {  
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);  
            };  
        }  
  
        /// <summary>  
        /// 从 PCGGraphData 加载节点图  
        /// </summary>  
        public void LoadGraph(PCGGraphData data)  
        {  
            graphData = data;  
  
            // 清空当前视图  
            DeleteElements(graphElements.ToList());  
  
            if (data == null) return;  
  
            // 重建节点  
            var nodeVisualMap = new Dictionary<string, PCGNodeVisual>();  
            foreach (var nodeData in data.Nodes)  
            {  
                var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);  
                if (nodeTemplate == null)  
                {  
                    Debug.LogWarning($"PCGGraphView: Node type not found: {nodeData.NodeType}");  
                    continue;  
                }  
  
                var newNode = (IPCGNode)Activator.CreateInstance(nodeTemplate.GetType());  
                var visual = CreateNodeVisual(newNode, nodeData.Position);  
                visual.SetNodeId(nodeData.NodeId);  
                nodeVisualMap[nodeData.NodeId] = visual;  
            }  
  
            // 重建连线  
            foreach (var edgeData in data.Edges)  
            {  
                if (!nodeVisualMap.TryGetValue(edgeData.OutputNodeId, out var outputVisual)) continue;  
                if (!nodeVisualMap.TryGetValue(edgeData.InputNodeId, out var inputVisual)) continue;  
  
                var outputPort = outputVisual.GetOutputPort(edgeData.OutputPortName);  
                var inputPort = inputVisual.GetInputPort(edgeData.InputPortName);  
                if (outputPort == null || inputPort == null) continue;  
  
                var edge = outputPort.ConnectTo(inputPort);  
                AddElement(edge);  
            }  
        }  
  
        /// <summary>  
        /// 将当前视图状态保存为 PCGGraphData  
        /// </summary>  
        public PCGGraphData SaveToGraphData()  
        {  
            var data = ScriptableObject.CreateInstance<PCGGraphData>();  
  
            // 遍历所有节点  
            nodes.ForEach(node =>  
            {  
                if (node is PCGNodeVisual visual)  
                {  
                    var nodeData = new PCGNodeData  
                    {  
                        NodeId = visual.NodeId,  
                        NodeType = visual.PCGNode.Name,  
                        Position = visual.GetPosition().position  
                    };  
                    data.Nodes.Add(nodeData);  
                }  
            });  
  
            // 遍历所有边  
            edges.ForEach(edge =>  
            {  
                if (edge.output?.node is PCGNodeVisual outputVisual &&  
                    edge.input?.node is PCGNodeVisual inputVisual)  
                {  
                    var edgeData = new PCGEdgeData  
                    {  
                        OutputNodeId = outputVisual.NodeId,  
                        OutputPortName = edge.output.portName,  
                        InputNodeId = inputVisual.NodeId,  
                        InputPortName = edge.input.portName  
                    };  
                    data.Edges.Add(edgeData);  
                }  
            });  
  
            return data;  
        }  
  
        /// <summary>  
        /// 创建节点的可视化表示并添加到视图  
        /// </summary>  
        public PCGNodeVisual CreateNodeVisual(IPCGNode node, Vector2 position)  
        {  
            var visual = new PCGNodeVisual();  
            visual.Initialize(node, position);  
            AddElement(visual);  
            return visual;  
        }  
  
        /// <summary>  
        /// 获取兼容的端口列表（用于连线时的类型检查）  
        /// </summary>  
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)  
        {  
            var compatiblePorts = new List<Port>();  
            ports.ForEach(port =>  
            {  
                if (startPort != port &&  
                    startPort.node != port.node &&  
                    startPort.direction != port.direction &&  
                    (startPort.portType == port.portType ||  
                     startPort.portType == typeof(object) ||  
                     port.portType == typeof(object)))  
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
            // 按类别添加所有已注册节点的创建菜单项  
            var categories = new[]  
            {  
                PCGNodeCategory.Create,  
                PCGNodeCategory.Attribute,  
                PCGNodeCategory.Transform,  
                PCGNodeCategory.Utility,  
                PCGNodeCategory.Geometry,  
                PCGNodeCategory.UV,  
                PCGNodeCategory.Distribute,  
                PCGNodeCategory.Curve,  
                PCGNodeCategory.Deform,  
                PCGNodeCategory.Topology,  
                PCGNodeCategory.Procedural,  
                PCGNodeCategory.Output,  
            };  
  
            foreach (var category in categories)  
            {  
                var nodesInCategory = PCGNodeRegistry.GetNodesByCategory(category).ToList();  
                foreach (var node in nodesInCategory)  
                {  
                    evt.menu.AppendAction(  
                        $"Create Node/{category}/{node.DisplayName}",  
                        action =>  
                        {  
                            var newNode = (IPCGNode)Activator.CreateInstance(node.GetType());  
                            var localMousePos = contentViewContainer.WorldToLocal(  
                                action.eventInfo.localMousePosition);  
                            CreateNodeVisual(newNode, localMousePos);  
                        });  
                }  
            }  
  
            base.BuildContextualMenu(evt);  
        }  
  
        /// <summary>  
        /// 处理 GraphView 变更（连线创建、元素删除等）  
        /// </summary>  
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)  
        {  
            if (change.edgesToCreate != null)  
            {  
                foreach (var edge in change.edgesToCreate)  
                {  
                    // 连线创建时可在此做额外验证或数据同步  
                }  
            }  
  
            if (change.elementsToRemove != null)  
            {  
                foreach (var element in change.elementsToRemove)  
                {  
                    // 处理节点删除和边删除的数据同步  
                }  
            }  
  
            return change;  
        }  
    }  
}