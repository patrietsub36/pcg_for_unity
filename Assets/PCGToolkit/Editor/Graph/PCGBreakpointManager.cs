using System;  
using System.Collections.Generic;  
  
namespace PCGToolkit.Graph  
{  
    /// <summary>  
    /// 断点管理器，管理图节点上的断点状态  
    /// </summary>  
    public static class PCGBreakpointManager  
    {  
        private static readonly HashSet<string> _breakpoints = new HashSet<string>();  
  
        /// <summary>  
        /// 断点切换时触发，参数为 nodeId  
        /// </summary>  
        public static event Action<string> OnBreakpointToggled;  
  
        /// <summary>  
        /// 切换指定节点的断点状态  
        /// </summary>  
        public static void ToggleBreakpoint(string nodeId)  
        {  
            if (string.IsNullOrEmpty(nodeId)) return;  
  
            if (_breakpoints.Contains(nodeId))  
                _breakpoints.Remove(nodeId);  
            else  
                _breakpoints.Add(nodeId);  
  
            OnBreakpointToggled?.Invoke(nodeId);  
        }  
  
        /// <summary>  
        /// 检查指定节点是否有断点  
        /// </summary>  
        public static bool HasBreakpoint(string nodeId)  
        {  
            return _breakpoints.Contains(nodeId);  
        }  
  
        /// <summary>  
        /// 获取所有断点的节点ID列表  
        /// </summary>  
        public static List<string> GetAllBreakpoints()  
        {  
            return new List<string>(_breakpoints);  
        }  
  
        /// <summary>  
        /// 清除所有断点  
        /// </summary>  
        public static void ClearAll()  
        {  
            var ids = new List<string>(_breakpoints);  
            _breakpoints.Clear();  
            // Notify for each cleared breakpoint so UI can update  
            foreach (var id in ids)  
                OnBreakpointToggled?.Invoke(id);  
              
            // If there were no breakpoints, still notify with empty to update count  
            if (ids.Count == 0)  
                OnBreakpointToggled?.Invoke(string.Empty);  
        }  
    }  
}