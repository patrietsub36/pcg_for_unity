using System.Collections.Generic;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 节点类别，用于搜索菜单分组和节点标题栏着色
    /// </summary>
    public enum PCGNodeCategory
    {
        Create,         // Tier 0: 基础几何体生成
        Attribute,      // Tier 0: 属性操作
        Transform,      // Tier 0: 变换操作
        Utility,        // Tier 0: Merge, Delete, Group 等
        Geometry,       // Tier 1: 核心几何操作
        UV,             // Tier 2: UV 操作
        Distribute,     // Tier 3: 分布与实例化
        Curve,          // Tier 4: 曲线与路径
        Deform,         // Tier 5: 噪声与变形
        Topology,       // Tier 6: 高级拓扑
        Procedural,     // Tier 7: 程序化规则
        Output          // Tier 8: 资产输出
    }

    /// <summary>
    /// PCG 节点统一接口。
    /// 同一个 IPCGNode 实现同时服务于 GraphView 可视化编辑和 AI Agent Skill 调用。
    /// </summary>
    public interface IPCGNode
    {
        /// <summary>节点唯一类型名称</summary>
        string Name { get; }

        /// <summary>节点显示名称</summary>
        string DisplayName { get; }

        /// <summary>节点描述</summary>
        string Description { get; }

        /// <summary>节点所属类别</summary>
        PCGNodeCategory Category { get; }

        /// <summary>输入端口 Schema 列表</summary>
        PCGParamSchema[] Inputs { get; }

        /// <summary>输出端口 Schema 列表</summary>
        PCGParamSchema[] Outputs { get; }

        /// <summary>
        /// 执行节点逻辑
        /// </summary>
        /// <param name="ctx">执行上下文</param>
        /// <param name="inputGeometries">输入几何体（按输入端口名索引）</param>
        /// <param name="parameters">参数字典（端口名 → 值）</param>
        /// <returns>输出几何体（按输出端口名索引）</returns>
        Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters);
    }
}
