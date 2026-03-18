using System.Collections.Generic;  
using PCGToolkit.Core;  
using UnityEngine;  
  
namespace PCGToolkit.Nodes.Utility  
{  
    public class ConstVector3Node : PCGNodeBase  
    {  
        public override string Name => "ConstVector3";  
        public override string DisplayName => "Const Vector3";  
        public override string Description => "输出一个常量三维向量";  
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;  
  
        public override PCGParamSchema[] Inputs => new[]  
        {  
            new PCGParamSchema("x", PCGPortDirection.Input, PCGPortType.Float, "X", "X 分量", 0f),  
            new PCGParamSchema("y", PCGPortDirection.Input, PCGPortType.Float, "Y", "Y 分量", 0f),  
            new PCGParamSchema("z", PCGPortDirection.Input, PCGPortType.Float, "Z", "Z 分量", 0f),  
        };  
  
        public override PCGParamSchema[] Outputs => new[]  
        {  
            new PCGParamSchema("value", PCGPortDirection.Output, PCGPortType.Vector3,  
                "Value", "输出向量"),  
        };  
  
        public override Dictionary<string, PCGGeometry> Execute(  
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,  
            Dictionary<string, object> parameters)  
        {  
            float x = GetParamFloat(parameters, "x", 0f);  
            float y = GetParamFloat(parameters, "y", 0f);  
            float z = GetParamFloat(parameters, "z", 0f);  
            var val = new Vector3(x, y, z);  
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.value"] = val;  
            ctx.Log($"ConstVector3: {val}");  
            return new Dictionary<string, PCGGeometry>();  
        }  
    }  
}