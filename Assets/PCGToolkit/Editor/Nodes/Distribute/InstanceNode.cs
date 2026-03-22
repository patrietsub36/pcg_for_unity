using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 多几何体实例化（对标 Houdini Instance SOP）
    /// 根据每个点的 @instance 属性值选择对应的几何体进行实例化
    /// </summary>
    public class InstanceNode : PCGNodeBase
    {
        public override string Name => "Instance";
        public override string DisplayName => "Instance";
        public override string Description => "按属性选择不同几何体实例化到点上";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        private const int MaxInstances = 8;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target Points", "目标点集", null, required: true),
            new PCGParamSchema("instance0", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 0", "实例几何体 0（index=0）", null, required: false),
            new PCGParamSchema("instance1", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 1", "实例几何体 1（index=1）", null, required: false),
            new PCGParamSchema("instance2", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 2", "实例几何体 2（index=2）", null, required: false),
            new PCGParamSchema("instance3", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 3", "实例几何体 3（index=3）", null, required: false),
            new PCGParamSchema("instance4", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 4", "实例几何体 4（index=4）", null, required: false),
            new PCGParamSchema("instance5", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 5", "实例几何体 5（index=5）", null, required: false),
            new PCGParamSchema("instance6", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 6", "实例几何体 6（index=6）", null, required: false),
            new PCGParamSchema("instance7", PCGPortDirection.Input, PCGPortType.Geometry,
                "Instance 7", "实例几何体 7（index=7）", null, required: false),
            new PCGParamSchema("instanceAttrib", PCGPortDirection.Input, PCGPortType.String,
                "Instance Attribute", "选择实例的属性名", "instance"),
            new PCGParamSchema("usePointOrient", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Orient", "使用点的 orient 属性控制旋转", true),
            new PCGParamSchema("usePointScale", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Scale", "使用点的 pscale 属性控制缩放", true),
            new PCGParamSchema("pack", PCGPortDirection.Input, PCGPortType.Bool,
                "Pack", "是否输出打包的实例（而非展开的几何体）", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var target = GetInputGeometry(inputGeometries, "target");
            string instanceAttrib = GetParamString(parameters, "instanceAttrib", "instance");
            bool usePointOrient = GetParamBool(parameters, "usePointOrient", true);
            bool usePointScale = GetParamBool(parameters, "usePointScale", true);
            bool pack = GetParamBool(parameters, "pack", false);

            if (target.Points.Count == 0)
            {
                ctx.LogWarning("Instance: 目标点集为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 收集可用的实例几何体
            var instances = new List<PCGGeometry>();
            for (int i = 0; i < MaxInstances; i++)
            {
                string portName = $"instance{i}";
                if (inputGeometries.TryGetValue(portName, out var geo) && geo != null && geo.Points.Count > 0)
                {
                    instances.Add(geo);
                }
                else
                {
                    instances.Add(null);
                }
            }

            // 检查是否有任何实例几何体
            bool hasAnyInstance = false;
            foreach (var inst in instances)
            {
                if (inst != null)
                {
                    hasAnyInstance = true;
                    break;
                }
            }

            if (!hasAnyInstance)
            {
                ctx.LogWarning("Instance: 没有连接任何实例几何体");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 获取属性
            var instanceIndexAttr = target.PointAttribs.GetAttribute(instanceAttrib);
            var orientAttr = usePointOrient ? target.PointAttribs.GetAttribute("orient") : null;
            var scaleAttr = usePointScale ? target.PointAttribs.GetAttribute("pscale") : null;

            // Pack 模式：只输出点，带实例属性
            if (pack)
            {
                return PackMode(target, instanceIndexAttr, orientAttr, scaleAttr, instanceAttrib, ctx);
            }

            // 展开模式：实际复制几何体
            return ExpandMode(target, instances, instanceIndexAttr, orientAttr, scaleAttr, ctx);
        }

        private Dictionary<string, PCGGeometry> PackMode(
            PCGGeometry target,
            PCGAttribute instanceIndexAttr,
            PCGAttribute orientAttr,
            PCGAttribute scaleAttr,
            string instanceAttrib,
            PCGContext ctx)
        {
            var result = new PCGGeometry();
            result.Points = new List<Vector3>(target.Points);

            // 复制 instance 属性
            if (instanceIndexAttr != null)
            {
                var resultInstanceAttr = result.PointAttribs.CreateAttribute(instanceAttrib, instanceIndexAttr.Type);
                resultInstanceAttr.Values = new List<object>(instanceIndexAttr.Values);
            }

            // 复制 orient 属性
            if (orientAttr != null)
            {
                var resultOrientAttr = result.PointAttribs.CreateAttribute("orient", orientAttr.Type);
                resultOrientAttr.Values = new List<object>(orientAttr.Values);
            }

            // 复制 pscale 属性
            if (scaleAttr != null)
            {
                var resultScaleAttr = result.PointAttribs.CreateAttribute("pscale", scaleAttr.Type);
                resultScaleAttr.Values = new List<object>(scaleAttr.Values);
            }

            ctx.Log($"Instance (pack): {target.Points.Count} 个实例点");
            return SingleOutput("geometry", result);
        }

        private Dictionary<string, PCGGeometry> ExpandMode(
            PCGGeometry target,
            List<PCGGeometry> instances,
            PCGAttribute instanceIndexAttr,
            PCGAttribute orientAttr,
            PCGAttribute scaleAttr,
            PCGContext ctx)
        {
            var result = new PCGGeometry();
            int instancedCount = 0;

            for (int pointIdx = 0; pointIdx < target.Points.Count; pointIdx++)
            {
                Vector3 position = target.Points[pointIdx];
                Quaternion rotation = Quaternion.identity;
                float scale = 1f;

                // 读取实例索引
                int instanceIndex = 0;
                if (instanceIndexAttr != null && pointIdx < instanceIndexAttr.Values.Count)
                {
                    var val = instanceIndexAttr.Values[pointIdx];
                    try { instanceIndex = Mathf.Clamp(System.Convert.ToInt32(val), 0, MaxInstances - 1); }
                    catch { instanceIndex = 0; }
                }

                // 获取对应的实例几何体
                var instanceGeo = instanceIndex < instances.Count ? instances[instanceIndex] : null;
                if (instanceGeo == null)
                {
                    // 尝试 fallback 到第一个可用的实例
                    for (int i = 0; i < instances.Count; i++)
                    {
                        if (instances[i] != null)
                        {
                            instanceGeo = instances[i];
                            break;
                        }
                    }
                }

                if (instanceGeo == null || instanceGeo.Points.Count == 0)
                    continue;

                // 读取旋转
                if (orientAttr != null && pointIdx < orientAttr.Values.Count)
                {
                    var orientVal = orientAttr.Values[pointIdx];
                    if (orientVal is Vector3 euler)
                        rotation = Quaternion.Euler(euler);
                    else if (orientVal is Vector4 quat)
                        rotation = new Quaternion(quat.x, quat.y, quat.z, quat.w);
                    else if (orientVal is Quaternion q)
                        rotation = q;
                }

                // 读取缩放
                if (scaleAttr != null && pointIdx < scaleAttr.Values.Count)
                {
                    try { scale = System.Convert.ToSingle(scaleAttr.Values[pointIdx]); }
                    catch { scale = 1f; }
                }

                // 复制变换后的实例几何体
                int vertexOffset = result.Points.Count;
                foreach (var srcPoint in instanceGeo.Points)
                {
                    Vector3 transformed = rotation * (srcPoint * scale) + position;
                    result.Points.Add(transformed);
                }

                foreach (var srcPrim in instanceGeo.Primitives)
                {
                    var newPrim = new int[srcPrim.Length];
                    for (int i = 0; i < srcPrim.Length; i++)
                        newPrim[i] = srcPrim[i] + vertexOffset;
                    result.Primitives.Add(newPrim);
                }

                // 注入 @copynum 属性
                var copynumAttr = result.PointAttribs.GetAttribute("copynum");
                if (copynumAttr == null)
                {
                    copynumAttr = result.PointAttribs.CreateAttribute("copynum", typeof(float), 0f);
                    for (int j = 0; j < vertexOffset; j++)
                        copynumAttr.Values.Add(0f);
                }
                for (int i = 0; i < instanceGeo.Points.Count; i++)
                    copynumAttr.Values.Add((float)pointIdx);

                // 注入 @instanceindex 属性
                var instanceIdxAttr = result.PointAttribs.GetAttribute("instanceindex");
                if (instanceIdxAttr == null)
                {
                    instanceIdxAttr = result.PointAttribs.CreateAttribute("instanceindex", typeof(float), 0f);
                    for (int j = 0; j < vertexOffset; j++)
                        instanceIdxAttr.Values.Add(0f);
                }
                for (int i = 0; i < instanceGeo.Points.Count; i++)
                    instanceIdxAttr.Values.Add((float)instanceIndex);

                instancedCount++;
            }

            ctx.Log($"Instance (expand): 实例化了 {instancedCount} 个点，共 {result.Points.Count} 顶点");
            return SingleOutput("geometry", result);
        }
    }
}