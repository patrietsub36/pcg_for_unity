using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 简易表达式解析/求值器，对标 Houdini VEX 的核心子集。
    /// 支持:
    ///   变量: @P, @N, @Cd, @ptnum, @primnum, @numpt, @numprim, 以及自定义属性 @attribName
    ///   分量访问: @P.x, @P.y, @P.z
    ///   算术: +, -, *, /, % 以及括号
    ///   一元负号: -expr
    ///   函数: sin, cos, tan, abs, sqrt, pow, min, max, floor, ceil, round, rand, fit, lerp, length, normalize
    ///   赋值语句: @P.y = expr;  @Cd = {expr, expr, expr};
    ///   多条语句用分号分隔
    /// </summary>
    public class ExpressionParser
    {
        public class EvalContext
        {
            public PCGGeometry Geometry;
            public int PointIndex;
            public int PrimIndex;
            public int TotalPoints;
            public int TotalPrims;
            public Dictionary<string, object> Variables = new Dictionary<string, object>();

            // 由 AttribWrangleNode 在执行后回写
            public Dictionary<string, object> Outputs = new Dictionary<string, object>();
        }

        private string source;
        private int pos;
        private EvalContext ctx;

        public void Execute(string code, EvalContext evalCtx)
        {
            ctx = evalCtx;
            PopulateBuiltinVariables();

            var statements = code.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var stmt in statements)
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                ExecuteStatement(trimmed);
            }
        }

        public float EvaluateFloat(string expression, EvalContext evalCtx)
        {
            ctx = evalCtx;
            PopulateBuiltinVariables();
            source = expression.Trim();
            pos = 0;
            return ToFloat(ParseExpression());
        }

        public Vector3 EvaluateVector3(string expression, EvalContext evalCtx)
        {
            ctx = evalCtx;
            PopulateBuiltinVariables();
            source = expression.Trim();
            pos = 0;
            var result = ParseExpression();
            if (result is Vector3 v3) return v3;
            float f = ToFloat(result);
            return new Vector3(f, f, f);
        }

        private void PopulateBuiltinVariables()
        {
            var g = ctx.Geometry;
            int pi = ctx.PointIndex;

            ctx.Variables["@ptnum"] = (float)pi;
            ctx.Variables["@primnum"] = (float)ctx.PrimIndex;
            ctx.Variables["@numpt"] = (float)ctx.TotalPoints;
            ctx.Variables["@numprim"] = (float)ctx.TotalPrims;

            if (pi >= 0 && pi < g.Points.Count)
                ctx.Variables["@P"] = g.Points[pi];
            else
                ctx.Variables["@P"] = Vector3.zero;

            // 加载 Point 属性
            foreach (var name in g.PointAttribs.GetAttributeNames())
            {
                var attr = g.PointAttribs.GetAttribute(name);
                if (pi >= 0 && pi < attr.Values.Count)
                    ctx.Variables[$"@{name}"] = attr.Values[pi];
            }
        }

        private void ExecuteStatement(string stmt)
        {
            // 赋值: @X = expr  或 @X.c = expr  或 @X = {a, b, c}
            int eqIdx = stmt.IndexOf('=');
            if (eqIdx > 0 && stmt[0] == '@')
            {
                string lhs = stmt.Substring(0, eqIdx).Trim();
                string rhs = stmt.Substring(eqIdx + 1).Trim();

                object value;
                if (rhs.StartsWith("{") && rhs.EndsWith("}"))
                {
                    // Vector literal {x, y, z}
                    var inner = rhs.Substring(1, rhs.Length - 2);
                    var parts = inner.Split(',');
                    float x = 0, y = 0, z = 0;
                    if (parts.Length >= 1) x = EvalSubExpr(parts[0].Trim());
                    if (parts.Length >= 2) y = EvalSubExpr(parts[1].Trim());
                    if (parts.Length >= 3) z = EvalSubExpr(parts[2].Trim());
                    value = new Vector3(x, y, z);
                }
                else
                {
                    source = rhs;
                    pos = 0;
                    value = ParseExpression();
                }

                // 分量赋值: @P.x
                if (lhs.Length > 2 && lhs[lhs.Length - 2] == '.')
                {
                    string varName = lhs.Substring(0, lhs.Length - 2);
                    char comp = lhs[lhs.Length - 1];
                    Vector3 vec = Vector3.zero;
                    if (ctx.Variables.TryGetValue(varName, out var existing) && existing is Vector3 ev)
                        vec = ev;

                    float fv = ToFloat(value);
                    switch (comp)
                    {
                        case 'x': vec.x = fv; break;
                        case 'y': vec.y = fv; break;
                        case 'z': vec.z = fv; break;
                    }
                    ctx.Variables[varName] = vec;
                    ctx.Outputs[varName] = vec;
                }
                else
                {
                    ctx.Variables[lhs] = value;
                    ctx.Outputs[lhs] = value;
                }
            }
        }

        private float EvalSubExpr(string expr)
        {
            source = expr;
            pos = 0;
            return ToFloat(ParseExpression());
        }

        // ---- Recursive Descent Parser ----

        private object ParseExpression()
        {
            return ParseAddSub();
        }

        private object ParseAddSub()
        {
            var left = ParseMulDiv();
            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos >= source.Length) break;
                char c = source[pos];
                if (c == '+' || c == '-')
                {
                    pos++;
                    var right = ParseMulDiv();
                    if (left is Vector3 lv && right is Vector3 rv)
                        left = c == '+' ? lv + rv : lv - rv;
                    else if (left is Vector3 lv2)
                        left = c == '+' ? lv2 + Vector3.one * ToFloat(right) : lv2 - Vector3.one * ToFloat(right);
                    else if (right is Vector3 rv2)
                        left = c == '+' ? Vector3.one * ToFloat(left) + rv2 : Vector3.one * ToFloat(left) - rv2;
                    else
                        left = c == '+' ? ToFloat(left) + ToFloat(right) : ToFloat(left) - ToFloat(right);
                }
                else break;
            }
            return left;
        }

        private object ParseMulDiv()
        {
            var left = ParseUnary();
            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos >= source.Length) break;
                char c = source[pos];
                if (c == '*' || c == '/' || c == '%')
                {
                    pos++;
                    var right = ParseUnary();
                    float rf = ToFloat(right);
                    if (left is Vector3 lv)
                    {
                        if (c == '*') left = lv * rf;
                        else if (c == '/') left = rf != 0 ? lv / rf : Vector3.zero;
                        else left = ToFloat(left) % rf;
                    }
                    else
                    {
                        float lf = ToFloat(left);
                        if (c == '*') left = lf * rf;
                        else if (c == '/') left = rf != 0 ? lf / rf : 0f;
                        else left = rf != 0 ? lf % rf : 0f;
                    }
                }
                else break;
            }
            return left;
        }

        private object ParseUnary()
        {
            SkipWhitespace();
            if (pos < source.Length && source[pos] == '-')
            {
                pos++;
                var val = ParsePrimary();
                if (val is Vector3 v) return -v;
                return -ToFloat(val);
            }
            return ParsePrimary();
        }

        private object ParsePrimary()
        {
            SkipWhitespace();
            if (pos >= source.Length) return 0f;

            // Parentheses
            if (source[pos] == '(')
            {
                pos++;
                var val = ParseExpression();
                SkipWhitespace();
                if (pos < source.Length && source[pos] == ')') pos++;
                return val;
            }

            // Variable: @name or @name.component
            if (source[pos] == '@')
            {
                int start = pos;
                pos++; // skip @
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;

                string varName = source.Substring(start, pos - start);

                // component access
                if (pos < source.Length && source[pos] == '.')
                {
                    pos++;
                    if (pos < source.Length && (source[pos] == 'x' || source[pos] == 'y' || source[pos] == 'z'))
                    {
                        char comp = source[pos];
                        pos++;
                        if (ctx.Variables.TryGetValue(varName, out var vec) && vec is Vector3 v3)
                        {
                            return comp == 'x' ? v3.x : comp == 'y' ? v3.y : v3.z;
                        }
                        return 0f;
                    }
                }

                if (ctx.Variables.TryGetValue(varName, out var val))
                    return val;
                return 0f;
            }

            // Function call: name(args)
            if (char.IsLetter(source[pos]))
            {
                int start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                string funcName = source.Substring(start, pos - start);
                SkipWhitespace();

                if (pos < source.Length && source[pos] == '(')
                {
                    pos++;
                    var args = ParseArgList();
                    SkipWhitespace();
                    if (pos < source.Length && source[pos] == ')') pos++;
                    return CallFunction(funcName, args);
                }

                // Constants
                if (funcName == "PI") return Mathf.PI;
                return 0f;
            }

            // Number literal
            if (char.IsDigit(source[pos]) || source[pos] == '.')
            {
                int start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                    pos++;
                if (float.TryParse(source.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out float num))
                    return num;
                return 0f;
            }

            pos++;
            return 0f;
        }

        private List<object> ParseArgList()
        {
            var args = new List<object>();
            SkipWhitespace();
            if (pos < source.Length && source[pos] == ')') return args;

            args.Add(ParseExpression());
            while (pos < source.Length && source[pos] == ',')
            {
                pos++;
                args.Add(ParseExpression());
            }
            return args;
        }

        private object CallFunction(string name, List<object> args)
        {
            switch (name)
            {
                case "sin": return Mathf.Sin(ArgFloat(args, 0));
                case "cos": return Mathf.Cos(ArgFloat(args, 0));
                case "tan": return Mathf.Tan(ArgFloat(args, 0));
                case "abs": return Mathf.Abs(ArgFloat(args, 0));
                case "sqrt": return Mathf.Sqrt(ArgFloat(args, 0));
                case "pow": return Mathf.Pow(ArgFloat(args, 0), ArgFloat(args, 1));
                case "min": return Mathf.Min(ArgFloat(args, 0), ArgFloat(args, 1));
                case "max": return Mathf.Max(ArgFloat(args, 0), ArgFloat(args, 1));
                case "floor": return Mathf.Floor(ArgFloat(args, 0));
                case "ceil": return Mathf.Ceil(ArgFloat(args, 0));
                case "round": return Mathf.Round(ArgFloat(args, 0));
                case "clamp": return Mathf.Clamp(ArgFloat(args, 0), ArgFloat(args, 1), ArgFloat(args, 2));
                case "rand":
                {
                    float seed = ArgFloat(args, 0);
                    float hash = seed * 0.618033988749895f + 0.3183098861837907f;
                    hash = hash - Mathf.Floor(hash);
                    hash = hash * (hash + 33.33f);
                    hash = hash - Mathf.Floor(hash);
                    return hash;
                }
                case "fit":
                {
                    // fit(value, oldMin, oldMax, newMin, newMax)
                    float v = ArgFloat(args, 0);
                    float oMin = ArgFloat(args, 1);
                    float oMax = ArgFloat(args, 2);
                    float nMin = ArgFloat(args, 3);
                    float nMax = ArgFloat(args, 4);
                    float range = oMax - oMin;
                    if (Mathf.Abs(range) < 1e-8f) return nMin;
                    float t = (v - oMin) / range;
                    return Mathf.Lerp(nMin, nMax, t);
                }
                case "lerp": return Mathf.Lerp(ArgFloat(args, 0), ArgFloat(args, 1), ArgFloat(args, 2));
                case "length":
                {
                    if (args.Count > 0 && args[0] is Vector3 v3)
                        return v3.magnitude;
                    return Mathf.Abs(ArgFloat(args, 0));
                }
                case "normalize":
                {
                    if (args.Count > 0 && args[0] is Vector3 v3)
                        return v3.normalized;
                    return ArgFloat(args, 0) >= 0 ? 1f : -1f;
                }
            }
            return 0f;
        }

        private float ArgFloat(List<object> args, int index)
        {
            if (index < args.Count) return ToFloat(args[index]);
            return 0f;
        }

        private static float ToFloat(object val)
        {
            if (val is float f) return f;
            if (val is double d) return (float)d;
            if (val is int i) return i;
            if (val is Vector3 v) return v.magnitude;
            return 0f;
        }

        private void SkipWhitespace()
        {
            while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                pos++;
        }
    }
}
