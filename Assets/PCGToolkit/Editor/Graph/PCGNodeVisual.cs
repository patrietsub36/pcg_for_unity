using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using PCGToolkit.Core;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// PCG 节点在 GraphView 中的可视化表示
    /// 负责端口绘制、参数面板、预览缩略图等
    /// </summary>
    public class PCGNodeVisual : Node
    {
        public string NodeId { get; private set; }
        public IPCGNode PCGNode { get; private set; }

        private Dictionary<string, Port> inputPorts = new Dictionary<string, Port>();
        private Dictionary<string, Port> outputPorts = new Dictionary<string, Port>();

        /// <summary>
        /// 初始化节点可视化
        /// </summary>
        public void Initialize(IPCGNode pcgNode, Vector2 position)
        {
            PCGNode = pcgNode;
            NodeId = System.Guid.NewGuid().ToString();
            title = pcgNode.DisplayName;
            tooltip = pcgNode.Description;

            SetPosition(new Rect(position, Vector2.zero));

            // TODO: 根据 Category 设置标题栏颜色
            SetCategoryColor(pcgNode.Category);

            // 创建输入端口
            CreateInputPorts();

            // 创建输出端口
            CreateOutputPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void SetCategoryColor(PCGNodeCategory category)
        {
            // TODO: 设置不同类别的标题栏颜色
            // Create=绿, Attribute=青, Transform=黄, Geometry=蓝
            // UV=紫, Distribute=橙, Curve=粉, Deform=红
            // Topology=灰蓝, Procedural=金, Output=白
            Debug.Log($"PCGNodeVisual: SetCategoryColor - {category} (TODO)");
        }

        private void CreateInputPorts()
        {
            if (PCGNode.Inputs == null) return;

            foreach (var schema in PCGNode.Inputs)
            {
                var portType = GetPortCapacity(schema);
                var port = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Input,
                    portType,
                    GetSystemType(schema.PortType));

                port.portName = schema.DisplayName;
                port.portColor = GetPortColor(schema.PortType);

                inputPorts[schema.Name] = port;
                inputContainer.Add(port);
            }
        }

        private void CreateOutputPorts()
        {
            if (PCGNode.Outputs == null) return;

            foreach (var schema in PCGNode.Outputs)
            {
                var port = InstantiatePort(
                    Orientation.Horizontal,
                    Direction.Output,
                    Port.Capacity.Multi,
                    GetSystemType(schema.PortType));

                port.portName = schema.DisplayName;
                port.portColor = GetPortColor(schema.PortType);

                outputPorts[schema.Name] = port;
                outputContainer.Add(port);
            }
        }

        private Port.Capacity GetPortCapacity(PCGParamSchema schema)
        {
            return schema.AllowMultiple ? Port.Capacity.Multi : Port.Capacity.Single;
        }

        private System.Type GetSystemType(PCGPortType portType)
        {
            // TODO: 映射 PCGPortType 到 System.Type（用于 GraphView 类型兼容性检查）
            switch (portType)
            {
                case PCGPortType.Geometry: return typeof(PCGGeometry);
                case PCGPortType.Float: return typeof(float);
                case PCGPortType.Int: return typeof(int);
                case PCGPortType.Vector3: return typeof(Vector3);
                case PCGPortType.String: return typeof(string);
                case PCGPortType.Bool: return typeof(bool);
                case PCGPortType.Color: return typeof(Color);
                default: return typeof(object);
            }
        }

        private Color GetPortColor(PCGPortType portType)
        {
            // TODO: 根据端口类型返回颜色（便于视觉区分）
            switch (portType)
            {
                case PCGPortType.Geometry: return new Color(0.2f, 0.8f, 0.4f);  // 绿色
                case PCGPortType.Float: return new Color(0.4f, 0.6f, 1.0f);     // 蓝色
                case PCGPortType.Int: return new Color(0.3f, 0.9f, 0.9f);       // 青色
                case PCGPortType.Vector3: return new Color(1.0f, 0.8f, 0.2f);   // 黄色
                case PCGPortType.String: return new Color(1.0f, 0.4f, 0.6f);    // 粉色
                case PCGPortType.Bool: return new Color(0.9f, 0.3f, 0.3f);      // 红色
                case PCGPortType.Color: return Color.white;
                default: return Color.gray;
            }
        }

        /// <summary>
        /// 获取指定名称的输入端口
        /// </summary>
        public Port GetInputPort(string portName)
        {
            inputPorts.TryGetValue(portName, out var port);
            return port;
        }

        /// <summary>
        /// 获取指定名称的输出端口
        /// </summary>
        public Port GetOutputPort(string portName)
        {
            outputPorts.TryGetValue(portName, out var port);
            return port;
        }
    }
}
