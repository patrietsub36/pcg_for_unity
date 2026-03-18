using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using PCGToolkit.Core;

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
            // TODO: 从 PCGNodeRegistry 获取所有节点，按类别分组构建搜索树
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
                tree.Add(new SearchTreeGroupEntry(new GUIContent(category.ToString()), 1));

                // TODO: 获取该类别下的所有节点并添加为 SearchTreeEntry
                var nodes = PCGNodeRegistry.GetNodesByCategory(category);
                foreach (var node in nodes)
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
            // TODO: 在鼠标位置创建选中的节点
            if (entry.userData is IPCGNode selectedNode)
            {
                Debug.Log($"PCGNodeSearchWindow: Selected {selectedNode.Name} (TODO)");
                return true;
            }
            return false;
        }
    }
}
