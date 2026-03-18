using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstBoolNode : PCGNodeBase  
    {  
        public override string Name => "ConstBool";  
        public override string DisplayName => "Const Bool";  
        public override string Description => "输出一个常量布尔值";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.Bool,  
                "Value", "布尔值", false),  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Bool,  
                "Value", "输出值"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            bool val = GetParamBool(parameters, "value", false);  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstBool: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}