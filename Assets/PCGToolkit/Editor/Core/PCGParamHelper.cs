using System.Globalization;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG 参数工具类，提供参数序列化/反序列化的共享方法。
    /// </summary>
    public static class PCGParamHelper
    {
        /// <summary>
        /// 将 PCGSerializedParameter 反序列化为对应类型的值
        /// </summary>
        public static object DeserializeParamValue(PCGSerializedParameter param)
        {
            try
            {
                switch (param.ValueType)
                {
                    case "float":
                        return float.Parse(param.ValueJson, CultureInfo.InvariantCulture);
                    case "int":
                        return int.Parse(param.ValueJson);
                    case "bool":
                        return bool.Parse(param.ValueJson);
                    case "string":
                        return param.ValueJson;
                    case "Vector3":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 3)
                        {
                            return new Vector3(
                                float.Parse(parts[0], CultureInfo.InvariantCulture),
                                float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture));
                        }
                        return Vector3.zero;
                    }
                    case "Color":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 4)
                        {
                            return new Color(
                                float.Parse(parts[0], CultureInfo.InvariantCulture),
                                float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture),
                                float.Parse(parts[3], CultureInfo.InvariantCulture));
                        }
                        return Color.white;
                    }
                    case "null":
                    case null:
                    case "":
                        return param.ValueJson;
                    default:
                        return param.ValueJson;
                }
            }
            catch
            {
                return param.ValueJson;
            }
        }

        /// <summary>
        /// 将值序列化为 JSON 字符串
        /// </summary>
        public static string SerializeParamValue(object value)
        {
            if (value == null) return "";

            switch (value)
            {
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString();
                case bool b:
                    return b.ToString().ToLower();
                case string s:
                    return s;
                case Vector3 v:
                    return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
                case Color c:
                    return $"{c.r.ToString(CultureInfo.InvariantCulture)},{c.g.ToString(CultureInfo.InvariantCulture)},{c.b.ToString(CultureInfo.InvariantCulture)},{c.a.ToString(CultureInfo.InvariantCulture)}";
                default:
                    return value.ToString();
            }
        }
    }
}