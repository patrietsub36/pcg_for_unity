using UnityEditor;  
using UnityEditor.UIElements;  
using UnityEngine;  
using UnityEngine.UIElements;  
  
namespace PCGToolkit.Graph  
{  
    /// <summary>  
    /// PCG 节点编辑器主窗口  
    /// </summary>  
    public class PCGGraphEditorWindow : EditorWindow  
    {  
        private PCGGraphView graphView;  
        private PCGGraphData currentGraph;  
  
        [MenuItem("PCG Toolkit/Node Editor")]  
        public static void OpenWindow()  
        {  
            var window = GetWindow<PCGGraphEditorWindow>();  
            window.titleContent = new GUIContent("PCG Node Editor");  
            window.minSize = new Vector2(800, 600);  
        }  
  
        private void OnEnable()  
        {  
            ConstructGraphView();  
            GenerateToolbar();  
        }  
  
        private void OnDisable()  
        {  
            if (graphView != null)  
            {  
                rootVisualElement.Remove(graphView);  
            }  
        }  
  
        private void ConstructGraphView()  
        {  
            graphView = new PCGGraphView();  
            graphView.StretchToParentSize();  
            graphView.Initialize(this);  
            rootVisualElement.Add(graphView);  
        }  
  
        private void GenerateToolbar()  
        {  
            var toolbar = new Toolbar();  
  
            var newButton = new Button(() => NewGraph()) { text = "New" };  
            toolbar.Add(newButton);  
  
            var saveButton = new Button(() => SaveGraph()) { text = "Save" };  
            toolbar.Add(saveButton);  
  
            var loadButton = new Button(() => LoadGraph()) { text = "Load" };  
            toolbar.Add(loadButton);  
  
            var executeButton = new Button(() => ExecuteGraph()) { text = "Execute" };  
            toolbar.Add(executeButton);  
  
            rootVisualElement.Add(toolbar);  
        }  
  
        private void NewGraph()  
        {  
            currentGraph = ScriptableObject.CreateInstance<PCGGraphData>();  
            currentGraph.GraphName = "New Graph";  
            graphView.LoadGraph(currentGraph);  
        }  
  
        private void SaveGraph()  
        {  
            var path = EditorUtility.SaveFilePanelInProject(  
                "Save PCG Graph", "NewPCGGraph", "asset", "Save PCG Graph");  
            if (string.IsNullOrEmpty(path)) return;  
  
            var data = graphView.SaveToGraphData();  
            data.GraphName = System.IO.Path.GetFileNameWithoutExtension(path);  
  
            // 检查是否已有同路径资产  
            var existing = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);  
            if (existing != null)  
            {  
                EditorUtility.CopySerialized(data, existing);  
                AssetDatabase.SaveAssets();  
            }  
            else  
            {  
                AssetDatabase.CreateAsset(data, path);  
                AssetDatabase.SaveAssets();  
            }  
  
            AssetDatabase.Refresh();  
            currentGraph = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);  
            Debug.Log($"Graph saved to {path}");  
        }  
  
        private void LoadGraph()  
        {  
            var path = EditorUtility.OpenFilePanel("Load PCG Graph", "Assets", "asset");  
            if (string.IsNullOrEmpty(path)) return;  
  
            // 转换为相对路径  
            if (path.StartsWith(Application.dataPath))  
                path = "Assets" + path.Substring(Application.dataPath.Length);  
  
            var data = AssetDatabase.LoadAssetAtPath<PCGGraphData>(path);  
            if (data == null)  
            {  
                Debug.LogError($"Failed to load graph from {path}");  
                return;  
            }  
  
            currentGraph = data;  
            graphView.LoadGraph(data);  
            Debug.Log($"Graph loaded from {path}");  
        }  
  
        private void ExecuteGraph()  
        {  
            // 如果没有保存过，先从当前视图生成数据  
            if (currentGraph == null)  
            {  
                currentGraph = graphView.SaveToGraphData();  
            }  
            else  
            {  
                // 用当前视图最新状态更新  
                var latestData = graphView.SaveToGraphData();  
                currentGraph.Nodes = latestData.Nodes;  
                currentGraph.Edges = latestData.Edges;  
            }  
  
            var executor = new PCGGraphExecutor(currentGraph);  
            executor.Execute();  
            Debug.Log("Graph execution completed.");  
        }  
    }  
}