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
    /// 支持按名称/类别搜索，模糊匹配  
    /// </summary>  
    public class PCGNodeSearchWindow : ScriptableObject, ISearchWindowProvider  
    {  
        private PCGGraphView graphView;  
        private PCGGraphEditorWindow editorWindow;  
  
        public void Initialize(PCGGraphView view, PCGGraphEditorWindow window)  
        {  
            graphView = view;  
            editorWindow = window;  
        }  
  
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)  
        {  
            var tree = new List<SearchTreeEntry>  
            {  
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),  
            };  
  
            // 按类别分组  
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
  
                tree.Add(new SearchTreeGroupEntry(new GUIContent(category.ToString()), 1));  
  
                foreach (var node in nodeList)  
                {  
                    tree.Add(new SearchTreeEntry(new GUIContent(node.DisplayName))  
                    {  
                        userData = node,  
                        level = 2,  
                    });  
                }  
            }  
  
            return tree;  
        }  
  
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)  
        {  
            if (entry.userData is IPCGNode selectedNode)  
            {  
                // 创建新实例（不复用模板实例）  
                var newNode = (IPCGNode)Activator.CreateInstance(selectedNode.GetType());  
  
                // 将屏幕坐标转换为 GraphView 本地坐标  
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