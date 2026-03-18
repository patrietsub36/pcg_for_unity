using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstStringNode : PCGNodeBase  
    {  
        public override string Name => "ConstString";  
        public override string DisplayName => "Const String";  
        public override string Description => "输出一个常量字符串";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Input, PCGPortType.String,  
                "Value", "字符串值", ""),  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.String,  
                "Value", "输出值"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            string val = GetParamString(parameters, "value", "");  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstString: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}