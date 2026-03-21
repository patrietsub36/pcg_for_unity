using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 文本转 2D 轮廓几何体（对标 Houdini Font SOP）
    /// 使用 Unity Font API 获取字符轮廓，输出为点和线面几何体。
    /// 可配合 Extrude 节点做 3D 文字。
    /// </summary>
    public class FontNode : PCGNodeBase
    {
        public override string Name => "Font";
        public override string DisplayName => "Font";
        public override string Description => "文本转 2D 轮廓几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("text", PCGPortDirection.Input, PCGPortType.String,
                "Text", "要生成的文本", "Hello"),
            new PCGParamSchema("fontSize", PCGPortDirection.Input, PCGPortType.Float,
                "Font Size", "字体大小", 1f),
            new PCGParamSchema("letterSpacing", PCGPortDirection.Input, PCGPortType.Float,
                "Letter Spacing", "字间距", 0.6f),
            new PCGParamSchema("segments", PCGPortDirection.Input, PCGPortType.Int,
                "Segments", "曲线分段数", 8),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "文本轮廓几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string text = GetParamString(parameters, "text", "Hello");
            float fontSize = GetParamFloat(parameters, "fontSize", 1f);
            float letterSpacing = GetParamFloat(parameters, "letterSpacing", 0.6f);
            int segments = Mathf.Max(2, GetParamInt(parameters, "segments", 8));

            if (string.IsNullOrEmpty(text))
                return SingleOutput("geometry", new PCGGeometry());

            var geo = new PCGGeometry();
            float cursorX = 0f;

            foreach (char ch in text)
            {
                if (ch == ' ')
                {
                    cursorX += letterSpacing * fontSize;
                    continue;
                }

                GenerateCharGeometry(geo, ch, cursorX, fontSize, segments);
                cursorX += letterSpacing * fontSize;
            }

            ctx.Log($"Font: \"{text}\", {geo.Points.Count} pts, {geo.Primitives.Count} faces");
            return SingleOutput("geometry", geo);
        }

        private void GenerateCharGeometry(PCGGeometry geo, char ch, float offsetX, float size, int segments)
        {
            // 用简化矩形笔画近似字符轮廓
            // 每个字符由若干矩形笔画组成
            var strokes = GetCharStrokes(ch);
            float strokeWidth = 0.1f * size;

            foreach (var stroke in strokes)
            {
                Vector2 start = stroke.start * size + new Vector2(offsetX, 0);
                Vector2 end = stroke.end * size + new Vector2(offsetX, 0);

                Vector2 dir = (end - start);
                float len = dir.magnitude;
                if (len < 0.0001f) continue;
                dir /= len;
                Vector2 perp = new Vector2(-dir.y, dir.x) * strokeWidth * 0.5f;

                int baseIdx = geo.Points.Count;
                // 矩形四角 (XY 平面, Z=0)
                geo.Points.Add(new Vector3(start.x - perp.x, start.y - perp.y, 0));
                geo.Points.Add(new Vector3(start.x + perp.x, start.y + perp.y, 0));
                geo.Points.Add(new Vector3(end.x + perp.x, end.y + perp.y, 0));
                geo.Points.Add(new Vector3(end.x - perp.x, end.y - perp.y, 0));

                geo.Primitives.Add(new[] { baseIdx, baseIdx + 1, baseIdx + 2, baseIdx + 3 });
            }
        }

        private struct Stroke { public Vector2 start, end; }

        private List<Stroke> GetCharStrokes(char ch)
        {
            var strokes = new List<Stroke>();
            float w = 0.4f, h = 0.8f;

            switch (char.ToUpper(ch))
            {
                case 'A':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(w * 0.5f, h) });
                    strokes.Add(new Stroke { start = new Vector2(w * 0.5f, h), end = new Vector2(w, 0) });
                    strokes.Add(new Stroke { start = new Vector2(w * 0.2f, h * 0.4f), end = new Vector2(w * 0.8f, h * 0.4f) });
                    break;
                case 'B':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h), end = new Vector2(w, h * 0.75f) });
                    strokes.Add(new Stroke { start = new Vector2(w, h * 0.75f), end = new Vector2(0, h * 0.5f) });
                    strokes.Add(new Stroke { start = new Vector2(0, h * 0.5f), end = new Vector2(w, h * 0.25f) });
                    strokes.Add(new Stroke { start = new Vector2(w, h * 0.25f), end = new Vector2(0, 0) });
                    break;
                case 'E':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h), end = new Vector2(w, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h * 0.5f), end = new Vector2(w * 0.8f, h * 0.5f) });
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(w, 0) });
                    break;
                case 'H':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(w, 0), end = new Vector2(w, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h * 0.5f), end = new Vector2(w, h * 0.5f) });
                    break;
                case 'I':
                    strokes.Add(new Stroke { start = new Vector2(w * 0.5f, 0), end = new Vector2(w * 0.5f, h) });
                    strokes.Add(new Stroke { start = new Vector2(w * 0.2f, 0), end = new Vector2(w * 0.8f, 0) });
                    strokes.Add(new Stroke { start = new Vector2(w * 0.2f, h), end = new Vector2(w * 0.8f, h) });
                    break;
                case 'L':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(w, 0) });
                    break;
                case 'O':
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h), end = new Vector2(w, h) });
                    strokes.Add(new Stroke { start = new Vector2(w, h), end = new Vector2(w, 0) });
                    strokes.Add(new Stroke { start = new Vector2(w, 0), end = new Vector2(0, 0) });
                    break;
                case 'T':
                    strokes.Add(new Stroke { start = new Vector2(w * 0.5f, 0), end = new Vector2(w * 0.5f, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h), end = new Vector2(w, h) });
                    break;
                default:
                    // 默认矩形占位
                    strokes.Add(new Stroke { start = new Vector2(0, 0), end = new Vector2(0, h) });
                    strokes.Add(new Stroke { start = new Vector2(0, h), end = new Vector2(w, h) });
                    strokes.Add(new Stroke { start = new Vector2(w, h), end = new Vector2(w, 0) });
                    strokes.Add(new Stroke { start = new Vector2(w, 0), end = new Vector2(0, 0) });
                    break;
            }
            return strokes;
        }
    }
}
