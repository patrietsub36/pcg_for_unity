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

            // 根据 Category 设置标题栏颜色  
            SetCategoryColor(pcgNode.Category);

            // 创建输入端口  
            CreateInputPorts();

            // 创建输出端口  
            CreateOutputPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>  
        /// 设置节点 ID（用于加载时恢复）  
        /// </summary>  
        public void SetNodeId(string id)
        {
            NodeId = id;
        }

        private void SetCategoryColor(PCGNodeCategory category)
        {
            Color color;
            switch (category)
            {
                case PCGNodeCategory.Create:
                    color = new Color(0.15f, 0.45f, 0.2f);
                    break;
                case PCGNodeCategory.Attribute:
                    color = new Color(0.15f, 0.45f, 0.45f);
                    break;
                case PCGNodeCategory.Transform:
                    color = new Color(0.55f, 0.5f, 0.1f);
                    break;
                case PCGNodeCategory.Utility:
                    color = new Color(0.35f, 0.35f, 0.35f);
                    break;
                case PCGNodeCategory.Geometry:
                    color = new Color(0.2f, 0.35f, 0.6f);
                    break;
                case PCGNodeCategory.UV:
                    color = new Color(0.4f, 0.2f, 0.55f);
                    break;
                case PCGNodeCategory.Distribute:
                    color = new Color(0.6f, 0.35f, 0.1f);
                    break;
                case PCGNodeCategory.Curve:
                    color = new Color(0.6f, 0.25f, 0.4f);
                    break;
                case PCGNodeCategory.Deform:
                    color = new Color(0.6f, 0.15f, 0.15f);
                    break;
                case PCGNodeCategory.Topology:
                    color = new Color(0.25f, 0.35f, 0.5f);
                    break;
                case PCGNodeCategory.Procedural:
                    color = new Color(0.5f, 0.45f, 0.1f);
                    break;
                case PCGNodeCategory.Output:
                    color = new Color(0.4f, 0.4f, 0.4f);
                    break;
                default:
                    color = new Color(0.3f, 0.3f, 0.3f);
                    break;
            }

            titleContainer.style.backgroundColor = new StyleColor(color);

            // 根据背景亮度自动选择文字颜色  
            // 使用相对亮度公式: L = 0.299*R + 0.587*G + 0.114*B  
            float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            var textColor = luminance > 0.5f ? Color.black : Color.white;

            // 设置标题文字颜色  
            var titleLabel = titleContainer.Q<Label>("title-label");
            if (titleLabel != null)
            {
                titleLabel.style.color = new StyleColor(textColor);
            }
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
            switch (portType)
            {
                case PCGPortType.Geometry: return new Color(0.2f, 0.8f, 0.4f);
                case PCGPortType.Float: return new Color(0.4f, 0.6f, 1.0f);
                case PCGPortType.Int: return new Color(0.3f, 0.9f, 0.9f);
                case PCGPortType.Vector3: return new Color(1.0f, 0.8f, 0.2f);
                case PCGPortType.String: return new Color(1.0f, 0.4f, 0.6f);
                case PCGPortType.Bool: return new Color(0.9f, 0.3f, 0.3f);
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