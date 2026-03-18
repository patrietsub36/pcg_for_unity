using UnityEditor;  
using UnityEngine;  
  
namespace PCGToolkit.Graph  
{  
    /// <summary>  
    /// 节点图序列化/反序列化  
    /// 支持 ScriptableObject 和 JSON 两种格式  
    /// </summary>  
    public static class PCGGraphSerializer  
    {  
        /// <summary>  
        /// 保存图为 ScriptableObject 资产  
        /// </summary>  
        public static void SaveAsAsset(PCGGraphData graphData, string assetPath)  
        {  
            var existing = AssetDatabase.LoadAssetAtPath<PCGGraphData>(assetPath);  
            if (existing != null)  
            {  
                EditorUtility.CopySerialized(graphData, existing);  
                AssetDatabase.SaveAssets();  
            }  
            else  
            {  
                AssetDatabase.CreateAsset(graphData, assetPath);  
                AssetDatabase.SaveAssets();  
            }  
            AssetDatabase.Refresh();  
        }  
  
        /// <summary>  
        /// 从 ScriptableObject 资产加载图  
        /// </summary>  
        public static PCGGraphData LoadFromAsset(string assetPath)  
        {  
            return AssetDatabase.LoadAssetAtPath<PCGGraphData>(assetPath);  
        }  
  
        /// <summary>  
        /// 序列化为 JSON 字符串  
        /// </summary>  
        public static string ToJson(PCGGraphData graphData)  
        {  
            return JsonUtility.ToJson(graphData, true);  
        }  
  
        /// <summary>  
        /// 从 JSON 字符串反序列化  
        /// </summary>  
        public static PCGGraphData FromJson(string json)  
        {  
            var data = ScriptableObject.CreateInstance<PCGGraphData>();  
            JsonUtility.FromJsonOverwrite(json, data);  
            return data;  
        }  
    }  
}