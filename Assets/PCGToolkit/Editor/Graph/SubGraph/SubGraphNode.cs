using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// SubGraph 节点 — 封装一个子图为单个节点
    /// 用于控制节点图复杂度（单图上限 20~30 节点）
    /// </summary>
    public class SubGraphNode : PCGNodeBase
    {
        private PCGGraphData subGraphData;

        public override string Name => "SubGraph";
        public override string DisplayName => subGraphData != null ? subGraphData.GraphName : "SubGraph";
        public override string Description => "封装的子节点图";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => GetSubGraphInputs();
        public override PCGParamSchema[] Outputs => GetSubGraphOutputs();

        /// <summary>
        /// 设置子图数据
        /// </summary>
        public void SetSubGraphData(PCGGraphData data)
        {
            subGraphData = data;
        }

        /// <summary>
        /// 获取子图数据
        /// </summary>
        public PCGGraphData GetSubGraphData()
        {
            return subGraphData;
        }

        private PCGParamSchema[] GetSubGraphInputs()
        {
            // TODO: 从子图的入口节点（SubGraphInput）提取端口定义
            return new[]
            {
                new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                    "Input", "子图输入", null),
            };
        }

        private PCGParamSchema[] GetSubGraphOutputs()
        {
            // TODO: 从子图的出口节点（SubGraphOutput）提取端口定义
            return new[]
            {
                new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                    "Geometry", "子图输出"),
            };
        }

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            ctx.Log("SubGraph: 执行子图 (TODO)");

            // TODO: 创建子执行器，传入输入，执行子图，返回输出
            var geo = GetInputGeometry(inputGeometries, "input");
            if (geo != null)
            {
                return SingleOutput("geometry", geo.Clone());
            }

            return SingleOutput("geometry", new PCGGeometry());
        }
    }
}
