using System;  
using System.Collections.Generic;  
using UnityEditor;  
using UnityEditor.Experimental.GraphView;  
using UnityEngine;  
using PCGToolkit.Core;
using UnityEngine.UIElements;

namespace PCGToolkit.Graph  
{  
    /// <summary>  
    /// 节点搜索窗口（Tab 键或从端口拖拽时弹出）  
    /// 迭代二：支持端口类型过滤
    /// 迭代四：多字段搜索（DisplayName + Description + Name），描述副标题
    /// </summary>  
    public class PCGNodeSearchWindow : ScriptableObject, ISearchWindowProvider  
    {  
        private PCGGraphView graphView;  
        private PCGGraphEditorWindow editorWindow;
        
        // 迭代二：端口过滤
        private PCGPortType? _filterPortType;
        private Direction? _filterDirection;
  
        public void Initialize(PCGGraphView view, PCGGraphEditorWindow window)  
        {  
            graphView = view;  
            editorWindow = window;  
        }
        
        public void SetPortFilter(PCGPortType? portType, Direction? direction)
        {
            _filterPortType = portType;
            _filterDirection = direction;
        }
  
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)  
        {  
            var L = PCGLocalization.Get;
            var tree = new List<SearchTreeEntry>  
            {  
                new SearchTreeGroupEntry(new GUIContent(L("search.title")), 0),  
            };  
  
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
                var nodes = PCGNodeRegistry.GetNodesByCategory(category);  
                var nodeList = new List<IPCGNode>(nodes);  
                if (nodeList.Count == 0) continue;
                
                var filteredNodes = FilterNodes(nodeList);
                if (filteredNodes.Count == 0) continue;

                string catLabel = L($"cat.{category}");
                tree.Add(new SearchTreeGroupEntry(new GUIContent(catLabel), 1));  
  
                foreach (var node in filteredNodes)
                {
                    // 迭代四：在节点名称后附加简短描述作为副标题
                    string label = node.DisplayName;
                    if (!string.IsNullOrEmpty(node.Description))
                    {
                        // 截取描述前 20 个字符避免过长
                        string desc = node.Description.Length > 20
                            ? node.Description.Substring(0, 20) + "…"
                            : node.Description;
                        label = $"{node.DisplayName}  —  {desc}";
                    }
                    tree.Add(new SearchTreeEntry(new GUIContent(label))  
                    {  
                        userData = node,  
                        level = 2,  
                    });  
                }  
            }  
  
            return tree;  
        }
        
        private List<IPCGNode> FilterNodes(List<IPCGNode> nodes)
        {
            // 端口类型过滤（来自拖拽）
            List<IPCGNode> portFiltered = nodes;
            if (_filterPortType.HasValue && _filterDirection.HasValue)
            {
                portFiltered = new List<IPCGNode>();
                foreach (var node in nodes)
                {
                    var targetDirection = _filterDirection.Value == Direction.Input
                        ? PCGPortDirection.Output
                        : PCGPortDirection.Input;
                    var portList = targetDirection == PCGPortDirection.Output
                        ? node.Outputs
                        : node.Inputs;
                    if (portList == null) continue;
                    foreach (var schema in portList)
                    {
                        if (IsPortTypeCompatible(schema.PortType, _filterPortType.Value))
                        {
                            portFiltered.Add(node);
                            break;
                        }
                    }
                }
            }
            return portFiltered;
        }
        
        private bool IsPortTypeCompatible(PCGPortType a, PCGPortType b)
        {
            if (a == PCGPortType.Any || b == PCGPortType.Any)
                return true;
            return a == b;
        }
  
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)  
        {  
            if (entry.userData is IPCGNode selectedNode)  
            {  
                var newNode = (IPCGNode)Activator.CreateInstance(selectedNode.GetType());  
  
                var windowRoot = editorWindow.rootVisualElement;  
                var windowMousePosition = windowRoot.ChangeCoordinatesTo(  
                    windowRoot.parent,  
                    context.screenMousePosition - editorWindow.position.position);  
                var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);  
  
                graphView.CreateNodeVisual(newNode, graphMousePosition);  
                return true;  
            }  
            return false;  
        }  
    }  
}