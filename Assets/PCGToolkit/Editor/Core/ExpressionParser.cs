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
    ///   比较运算符: <, >, <=, >=, ==, != → 返回 0f 或 1f
    ///   逻辑运算符: &&, ||, ! → 短路求值
    ///   三元表达式: condition ? trueExpr : falseExpr
    ///   if/else 语句: if (expr) { stmts } else { stmts }
    ///   局部变量: float h = expr; int i = expr;
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
            source = code;
            pos = 0;

            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos >= source.Length) break;
                ParseStatement();
            }
        }

        private void ParseStatement()
        {
            SkipWhitespace();
            if (pos >= source.Length) return;

            // 检查是否是 if 语句
            if (MatchKeyword("if"))
            {
                ParseIfStatement();
            }
            // 检查是否是局部变量声明
            else if (MatchKeyword("float"))
            {
                ParseLocalVariableDeclaration(typeof(float));
            }
            else if (MatchKeyword("int"))
            {
                ParseLocalVariableDeclaration(typeof(int));
            }
            else
            {
                // 普通语句（赋值或表达式）
                ParseAssignmentOrExpression();
                SkipWhitespace();
                if (pos < source.Length && source[pos] == ';')
                    pos++; // skip ';'
            }
        }

        private void ParseLocalVariableDeclaration(System.Type varType)
        {
            SkipWhitespace();

            // 解析变量名
            int start = pos;
            while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                pos++;

            if (pos == start) return; // 没有变量名

            string varName = source.Substring(start, pos - start);
            SkipWhitespace();

            // 检查是否有初始化表达式
            object value = varType == typeof(int) ? 0 : 0f;

            if (pos < source.Length && source[pos] == '=')
            {
                pos++; // skip '='
                SkipWhitespace();
                value = ParseExpression();
                if (varType == typeof(int))
                    value = Mathf.FloorToInt(ToFloat(value));
            }

            // 存储到局部变量（无 @ 前缀）
            ctx.Variables[varName] = value;

            SkipWhitespace();
            if (pos < source.Length && source[pos] == ';')
                pos++; // skip ';'
        }

        private bool MatchKeyword(string keyword)
        {
            SkipWhitespace();
            int savedPos = pos;
            if (pos + keyword.Length <= source.Length)
            {
                string word = source.Substring(pos, keyword.Length);
                if (word == keyword)
                {
                    // 确保是完整的关键词（后面不是字母、数字或下划线）
                    int afterPos = pos + keyword.Length;
                    if (afterPos >= source.Length || (!char.IsLetterOrDigit(source[afterPos]) && source[afterPos] != '_'))
                    {
                        pos = afterPos;
                        return true;
                    }
                }
            }
            pos = savedPos;
            return false;
        }

        private void ParseIfStatement()
        {
            SkipWhitespace();

            // 解析条件 (expr)
            if (pos >= source.Length || source[pos] != '(')
                return; // 语法错误
            pos++; // skip '('

            float condition = ToFloat(ParseExpression());

            SkipWhitespace();
            if (pos < source.Length && source[pos] == ')')
                pos++; // skip ')'

            // 执行 true 分支或 false 分支
            if (condition != 0f)
            {
                // 执行 true 分支
                ParseBlock();
                SkipWhitespace();

                // 检查 else
                if (MatchKeyword("else"))
                {
                    // 跳过 else 分支
                    SkipBlock();
                }
            }
            else
            {
                // 跳过 true 分支
                SkipBlock();
                SkipWhitespace();

                // 检查 else
                if (MatchKeyword("else"))
                {
                    // 执行 else 分支
                    ParseBlock();
                }
            }
        }

        private void ParseBlock()
        {
            SkipWhitespace();
            if (pos >= source.Length) return;

            if (source[pos] == '{')
            {
                pos++; // skip '{'
                while (pos < source.Length)
                {
                    SkipWhitespace();
                    if (pos < source.Length && source[pos] == '}') { pos++; break; }
                    ParseStatement();
                }
            }
            else
            {
                // 单条语句
                ParseStatement();
            }
        }

        private void SkipBlock()
        {
            SkipWhitespace();
            if (pos >= source.Length) return;

            if (source[pos] == '{')
            {
                int braceCount = 1;
                pos++; // skip '{'
                while (pos < source.Length && braceCount > 0)
                {
                    if (source[pos] == '{') braceCount++;
                    else if (source[pos] == '}') braceCount--;
                    pos++;
                }
            }
            else
            {
                // 跳过单条语句
                while (pos < source.Length && source[pos] != ';' && source[pos] != '}')
                    pos++;
                if (pos < source.Length && source[pos] == ';')
                    pos++;
            }
        }

        private void ParseAssignmentOrExpression()
        {
            SkipWhitespace();
            if (pos >= source.Length) return;

            // 检查是否是赋值语句 (@X = expr 或 localVar = expr)
            int savedPos = pos;

            // 检查是否以 @ 开头（属性变量）

            // 预扫描找到 '='
            int eqPos = -1;
            int scanPos = pos;
            while (scanPos < source.Length && source[scanPos] != ';' && source[scanPos] != '}')
            {
                if (source[scanPos] == '=' && scanPos > pos)
                {
                    // 排除 == 比较运算符
                    if (scanPos + 1 < source.Length && source[scanPos + 1] == '=')
                    { scanPos++; continue; }
                    // 排除 !=, <=, >=
                    char prev = source[scanPos - 1];
                    if (prev == '!' || prev == '<' || prev == '>')
                    { scanPos++; continue; }
                    eqPos = scanPos;
                    break;
                }
                scanPos++;
            }

            if (eqPos > 0)
            {
                // 是赋值语句
                string lhs = source.Substring(savedPos, eqPos - savedPos).Trim();

                // 检查左侧是否有效（@变量名 或 局部变量名）
                bool isValidLhs = false;
                if (lhs.StartsWith("@"))
                {
                    isValidLhs = true; // 属性变量
                }
                else if (char.IsLetter(lhs[0]) || lhs[0] == '_')
                {
                    // 可能是局部变量
                    isValidLhs = true;
                }

                if (isValidLhs)
                {
                    pos = eqPos + 1; // skip '='
                    SkipWhitespace();

                    object value;
                    if (pos < source.Length && source[pos] == '{')
                    {
                        // Vector literal {x, y, z}
                        value = ParseVectorLiteral();
                    }
                    else
                    {
                        value = ParseExpression();
                    }

                    ExecuteAssignment(lhs, value);
                    return;
                }
            }

            // 普通表达式
            ParseExpression();
        }

        private object ParseVectorLiteral()
        {
            if (pos < source.Length && source[pos] == '{')
                pos++; // skip '{'

            float x = 0, y = 0, z = 0;
            SkipWhitespace();

            if (pos < source.Length && source[pos] != '}')
                x = ToFloat(ParseExpression());

            SkipWhitespace();
            if (pos < source.Length && source[pos] == ',')
            {
                pos++; // skip ','
                SkipWhitespace();
                y = ToFloat(ParseExpression());
            }

            SkipWhitespace();
            if (pos < source.Length && source[pos] == ',')
            {
                pos++; // skip ','
                SkipWhitespace();
                z = ToFloat(ParseExpression());
            }

            SkipWhitespace();
            if (pos < source.Length && source[pos] == '}')
                pos++; // skip '}'

            return new Vector3(x, y, z);
        }

        private void ExecuteAssignment(string lhs, object value)
        {
            // 分量赋值: @P.x 或 var.x
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

                // 只有属性变量（@前缀）才写入 Outputs
                if (varName.StartsWith("@"))
                    ctx.Outputs[varName] = vec;
            }
            else
            {
                ctx.Variables[lhs] = value;

                // 只有属性变量（@前缀）才写入 Outputs
                if (lhs.StartsWith("@"))
                    ctx.Outputs[lhs] = value;
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

        // ---- Recursive Descent Parser ----
        // 解析优先级（从低到高）：
        // ParseExpression → ParseTernary → ParseLogicOr → ParseLogicAnd → ParseComparison → ParseAddSub → ParseMulDiv → ParseUnary → ParsePrimary

        private object ParseExpression()
        {
            return ParseTernary();
        }

        // 三元表达式: condition ? trueExpr : falseExpr
        private object ParseTernary()
        {
            var condition = ParseLogicOr();
            SkipWhitespace();

            if (pos < source.Length && source[pos] == '?')
            {
                pos++; // skip '?'
                var trueExpr = ParseTernary(); // 右结合，递归调用
                SkipWhitespace();

                if (pos < source.Length && source[pos] == ':')
                {
                    pos++; // skip ':'
                    var falseExpr = ParseTernary();
                    // 条件为真（非零）返回 trueExpr，否则返回 falseExpr
                    return ToFloat(condition) != 0f ? trueExpr : falseExpr;
                }
            }
            return condition;
        }

        // 逻辑或: ||
        private object ParseLogicOr()
        {
            var left = ParseLogicAnd();
            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos + 1 < source.Length && source[pos] == '|' && source[pos + 1] == '|')
                {
                    pos += 2;
                    // 短路求值：如果左边为真，不再计算右边
                    if (ToFloat(left) != 0f)
                    {
                        // 跳过右边的表达式
                        ParseLogicAnd();
                        left = 1f;
                    }
                    else
                    {
                        var right = ParseLogicAnd();
                        left = ToFloat(right) != 0f ? 1f : 0f;
                    }
                }
                else break;
            }
            return left;
        }

        // 逻辑与: &&
        private object ParseLogicAnd()
        {
            var left = ParseComparison();
            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos + 1 < source.Length && source[pos] == '&' && source[pos + 1] == '&')
                {
                    pos += 2;
                    // 短路求值：如果左边为假，不再计算右边
                    if (ToFloat(left) == 0f)
                    {
                        // 跳过右边的表达式
                        ParseComparison();
                        left = 0f;
                    }
                    else
                    {
                        var right = ParseComparison();
                        left = ToFloat(right) != 0f ? 1f : 0f;
                    }
                }
                else break;
            }
            return left;
        }

        // 比较运算符: <, >, <=, >=, ==, !=
        private object ParseComparison()
        {
            var left = ParseAddSub();
            while (pos < source.Length)
            {
                SkipWhitespace();
                if (pos >= source.Length) break;

                char c = source[pos];
                bool isTwoChar = pos + 1 < source.Length;

                if (c == '<' && isTwoChar && source[pos + 1] == '=')
                {
                    pos += 2;
                    var right = ParseAddSub();
                    left = ToFloat(left) <= ToFloat(right) ? 1f : 0f;
                }
                else if (c == '>' && isTwoChar && source[pos + 1] == '=')
                {
                    pos += 2;
                    var right = ParseAddSub();
                    left = ToFloat(left) >= ToFloat(right) ? 1f : 0f;
                }
                else if (c == '=' && isTwoChar && source[pos + 1] == '=')
                {
                    pos += 2;
                    var right = ParseAddSub();
                    left = ToFloat(left) == ToFloat(right) ? 1f : 0f;
                }
                else if (c == '!' && isTwoChar && source[pos + 1] == '=')
                {
                    pos += 2;
                    var right = ParseAddSub();
                    left = ToFloat(left) != ToFloat(right) ? 1f : 0f;
                }
                else if (c == '<')
                {
                    pos++;
                    var right = ParseAddSub();
                    left = ToFloat(left) < ToFloat(right) ? 1f : 0f;
                }
                else if (c == '>')
                {
                    pos++;
                    var right = ParseAddSub();
                    left = ToFloat(left) > ToFloat(right) ? 1f : 0f;
                }
                else break;
            }
            return left;
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
                var val = ParseUnary();
                if (val is Vector3 v) return -v;
                return -ToFloat(val);
            }
            // 逻辑非: !
            if (pos < source.Length && source[pos] == '!')
            {
                pos++;
                var val = ParseUnary();
                return ToFloat(val) == 0f ? 1f : 0f;
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

            // Function call: name(args) or local variable
            if (char.IsLetter(source[pos]))
            {
                int start = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                string name = source.Substring(start, pos - start);
                SkipWhitespace();

                // Check if it's a function call
                if (pos < source.Length && source[pos] == '(')
                {
                    pos++;
                    var args = ParseArgList();
                    SkipWhitespace();
                    if (pos < source.Length && source[pos] == ')') pos++;
                    return CallFunction(name, args);
                }

                // Check if it's a local variable (without @ prefix)
                if (ctx.Variables.TryGetValue(name, out var localVar))
                {
                    return localVar;
                }

                // Constants
                if (name == "PI") return Mathf.PI;
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
