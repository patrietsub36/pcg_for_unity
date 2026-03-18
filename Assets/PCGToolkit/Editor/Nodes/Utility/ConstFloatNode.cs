using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstFloatNode : PCGNodeBase  
    {  
        public override string Name => "ConstFloat";  
        public override string DisplayName => "Const Float";  
        public override string Description => "输出一个常量浮点数";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.Float,  
                "Value", "浮点数值", 0f),  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Float,  
                "Value", "输出值"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            float val = GetParamFloat(parameters, "value", 0f);  
            // 通过 GlobalVariables 传递非 Geometry 值  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstFloat: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}