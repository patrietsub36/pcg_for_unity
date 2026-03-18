using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstIntNode : PCGNodeBase  
    {  
        public override string Name => "ConstInt";  
        public override string DisplayName => "Const Int";  
        public override string Description => "输出一个常量整数";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.Int,  
                "Value", "整数值", 0),  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Int,  
                "Value", "输出值"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            int val = GetParamInt(parameters, "value", 0);  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstInt: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}