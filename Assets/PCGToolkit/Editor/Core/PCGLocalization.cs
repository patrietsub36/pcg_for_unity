using System.Collections.Generic;
using UnityEditor;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG Toolkit 中英文本地化系统。
    /// 使用静态字典存储所有 UI 文本，支持运行时切换语言。
    /// 语言偏好通过 EditorPrefs 持久化。
    /// </summary>
    public static class PCGLocalization
    {
        private const string PREFS_KEY = "PCGToolkit.Language";

        public static string CurrentLanguage { get; private set; }

        public static System.Action OnLanguageChanged;

        private static readonly Dictionary<string, Dictionary<string, string>> _strings =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    // Toolbar
                    ["btn.new"]             = "New",
                    ["btn.save"]            = "Save",
                    ["btn.saveas"]          = "Save As",
                    ["btn.load"]            = "Load",
                    ["btn.execute"]         = "Execute",
                    ["btn.runToSelected"]   = "Run To Selected",
                    ["btn.stop"]            = "Stop",
                    ["btn.inspector"]       = "Inspector",
                    ["btn.lang"]            = "中文",
                    ["state.idle"]          = "Idle",
                    ["state.running"]       = "Running...",
                    ["state.paused"]        = "Paused",
                    ["state.stopped"]       = "Stopped",
                    ["state.completed"]     = "Completed",
                    ["total.label"]         = "Total: --",
                    // Search window
                    ["search.title"]        = "Create Node",
                    // Inspector
                    ["inspector.title"]     = "Node Inspector",
                    ["inspector.noNode"]    = "No node selected",
                    ["inspector.execTime"]  = "Exec Time",
                    ["inspector.points"]    = "Points",
                    ["inspector.prims"]     = "Primitives",
                    // Node categories
                    ["cat.Create"]          = "Create",
                    ["cat.Attribute"]       = "Attribute",
                    ["cat.Transform"]       = "Transform",
                    ["cat.Utility"]         = "Utility",
                    ["cat.Geometry"]        = "Geometry",
                    ["cat.UV"]              = "UV",
                    ["cat.Distribute"]      = "Distribute",
                    ["cat.Curve"]           = "Curve",
                    ["cat.Deform"]          = "Deform",
                    ["cat.Topology"]        = "Topology",
                    ["cat.Procedural"]      = "Procedural",
                    ["cat.Output"]          = "Output",
                },
                ["zh"] = new Dictionary<string, string>
                {
                    // Toolbar
                    ["btn.new"]             = "新建",
                    ["btn.save"]            = "保存",
                    ["btn.saveas"]          = "另存为",
                    ["btn.load"]            = "加载",
                    ["btn.execute"]         = "执行",
                    ["btn.runToSelected"]   = "运行到选中",
                    ["btn.stop"]            = "停止",
                    ["btn.inspector"]       = "属性面板",
                    ["btn.lang"]            = "EN",
                    ["state.idle"]          = "空闲",
                    ["state.running"]       = "运行中...",
                    ["state.paused"]        = "已暂停",
                    ["state.stopped"]       = "已停止",
                    ["state.completed"]     = "完成",
                    ["total.label"]         = "总计: --",
                    // Search window
                    ["search.title"]        = "创建节点",
                    // Inspector
                    ["inspector.title"]     = "节点属性",
                    ["inspector.noNode"]    = "未选中节点",
                    ["inspector.execTime"]  = "执行时间",
                    ["inspector.points"]    = "点数",
                    ["inspector.prims"]     = "面数",
                    // Node categories
                    ["cat.Create"]          = "创建",
                    ["cat.Attribute"]       = "属性",
                    ["cat.Transform"]       = "变换",
                    ["cat.Utility"]         = "工具",
                    ["cat.Geometry"]        = "几何",
                    ["cat.UV"]              = "UV",
                    ["cat.Distribute"]      = "分布",
                    ["cat.Curve"]           = "曲线",
                    ["cat.Deform"]          = "变形",
                    ["cat.Topology"]        = "拓扑",
                    ["cat.Procedural"]      = "程序化",
                    ["cat.Output"]          = "输出",
                },
            };

        static PCGLocalization()
        {
            CurrentLanguage = EditorPrefs.GetString(PREFS_KEY, "en");
        }

        /// <summary>
        /// 获取本地化字符串。找不到 key 时返回 key 本身。
        /// </summary>
        public static string Get(string key)
        {
            if (_strings.TryGetValue(CurrentLanguage, out var dict) &&
                dict.TryGetValue(key, out var value))
                return value;
            // Fallback to English
            if (_strings.TryGetValue("en", out var enDict) &&
                enDict.TryGetValue(key, out var enValue))
                return enValue;
            return key;
        }

        /// <summary>
        /// 切换语言（en/zh），并持久化到 EditorPrefs。
        /// </summary>
        public static void SetLanguage(string lang)
        {
            if (CurrentLanguage == lang) return;
            CurrentLanguage = lang;
            EditorPrefs.SetString(PREFS_KEY, lang);
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// 在当前语言和英文之间切换。
        /// </summary>
        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == "en" ? "zh" : "en");
        }

        public static bool IsZh => CurrentLanguage == "zh";
    }
}
