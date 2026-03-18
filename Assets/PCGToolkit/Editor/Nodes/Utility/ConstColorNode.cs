using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstColorNode : PCGNodeBase  
    {  
        public override string Name => "ConstColor";  
        public override string DisplayName => "Const Color";  
        public override string Description => "输出一个常量颜色";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("r", PCGPortDirection.Input, PCGPortType.Float, "R", "红色通道", 1f) { Min = 0f, Max = 1f },  
            new PCGParamSchema("g", PCGPortDirection.Input, PCGPortType.Float, "G", "绿色通道", 1f) { Min = 0f, Max = 1f },  
            new PCGParamSchema("b", PCGPortDirection.Input, PCGPortType.Float, "B", "蓝色通道", 1f) { Min = 0f, Max = 1f },  
            new PCGParamSchema("a", PCGPortDirection.Input, PCGPortType.Float, "A", "透明通道", 1f) { Min = 0f, Max = 1f },  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Color,  
                "Value", "输出颜色"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            float r = GetParamFloat(parameters, "r", 1f);  
            float g = GetParamFloat(parameters, "g", 1f);  
            float b = GetParamFloat(parameters, "b", 1f);  
            float a = GetParamFloat(parameters, "a", 1f);  
            var val = new Color(r, g, b, a);  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstColor: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}