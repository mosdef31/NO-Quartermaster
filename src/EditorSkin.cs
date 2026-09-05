using System.Collections.Generic;
using UnityEngine;

namespace Quartermaster
{
    internal static class EditorSkin
    {

        private static readonly Color Window = new Color(0.09f, 0.10f, 0.11f, 0.97f);
        private static readonly Color Panel = new Color(0.13f, 0.14f, 0.16f, 1f);
        private static readonly Color Row = new Color(0.17f, 0.18f, 0.21f, 1f);

        private static readonly Color RowAlt = new Color(0.185f, 0.195f, 0.225f, 1f);

        private static readonly Color Trough = new Color(0.07f, 0.08f, 0.09f, 1f);
        private static readonly Color Edge = new Color(0.32f, 0.35f, 0.40f, 1f);

        private static readonly Color Ink = new Color(0.88f, 0.90f, 0.93f, 1f);
        private static readonly Color Dim = new Color(0.62f, 0.65f, 0.70f, 1f);

        internal static readonly Color Good = new Color(0.45f, 0.82f, 0.50f, 1f);
        internal static readonly Color Warn = new Color(0.95f, 0.76f, 0.34f, 1f);
        internal static readonly Color Bad = new Color(0.94f, 0.45f, 0.42f, 1f);
        internal static readonly Color Accent = new Color(0.45f, 0.70f, 0.95f, 1f);

        internal static float Scale { get; private set; } = 1f;

        internal static float S(float baseSize)
        {
            return Mathf.Round(baseSize * Scale);
        }

        internal static int F(float baseSize)
        {
            return Mathf.Max(8, Mathf.RoundToInt(baseSize * Scale));
        }

        internal static GUIStyle WindowStyle = null!;
        internal static GUIStyle PanelStyle = null!;
        internal static GUIStyle RowStyle = null!;
        internal static GUIStyle RowAltStyle = null!;
        internal static GUIStyle StripStyle = null!;

        internal static GUIStyle HeadingStyle = null!;
        internal static GUIStyle LabelStyle = null!;
        internal static GUIStyle DimStyle = null!;

        internal static GUIStyle PriceStyle = null!;
        internal static GUIStyle WrapStyle = null!;

        internal static GUIStyle CardStyle = null!;

        internal static GUIStyle CountStyle = null!;

        internal static GUIStyle ButtonStyle = null!;
        internal static GUIStyle SmallButtonStyle = null!;

        internal static GUIStyle ListButtonStyle = null!;

        internal static GUIStyle ListButtonSelectedStyle = null!;

        internal static GUIStyle ListButtonOffStyle = null!;

        internal static GUIStyle SectionStyle = null!;

        internal static GUIStyle FieldStyle = null!;

        internal static GUIStyle ToggleStyle = null!;

        internal static GUIStyle ToggleOnStyle = null!;

        internal static GUISkin Skin = null!;

        private static Texture2D? _window;
        private static Texture2D? _panel;
        private static Texture2D? _row;
        private static Texture2D? _rowAlt;
        private static Texture2D? _button;
        private static Texture2D? _buttonHover;
        private static Texture2D? _buttonDown;
        private static Texture2D? _field;
        private static Texture2D? _selected;
        private static Texture2D? _trough;
        private static Texture2D? _thumb;
        private static Texture2D? _thumbHover;

        private static int _edgePixels = 1;

        private static RectOffset Slice()
        {
            return new RectOffset(_edgePixels, _edgePixels, _edgePixels, _edgePixels);
        }

        private static RectOffset NoSlice()
        {
            return new RectOffset(0, 0, 0, 0);
        }

        private static float _builtAtScale = -1f;

        internal static void Ensure(float configuredScale)
        {
            float wanted = configuredScale > 0f
                ? Mathf.Clamp(configuredScale, 0.6f, 3f)
                : Mathf.Clamp(Screen.height / 1080f, 1f, 2.5f);

            if (_window != null && Mathf.Approximately(wanted, _builtAtScale)) return;

            Scale = wanted;
            _builtAtScale = wanted;
            Build();
        }

        private static void Build()
        {

            _edgePixels = Mathf.Clamp(Mathf.RoundToInt(Scale), 1, 3);

            _window = Solid(Window);
            _panel = Bordered(Panel, Edge);
            _row = Solid(Row);
            _rowAlt = Solid(RowAlt);
            _button = Bordered(Row, Edge);
            _buttonHover = Bordered(RowAlt, Accent);
            _buttonDown = Bordered(Edge, Accent);
            _field = Bordered(new Color(0.06f, 0.07f, 0.08f, 1f), Edge);
            _selected = Bordered(new Color(0.20f, 0.30f, 0.42f, 1f), Accent);
            _trough = Solid(Trough);
            _thumb = Bordered(RowAlt, Edge);
            _thumbHover = Bordered(new Color(0.26f, 0.29f, 0.34f, 1f), Accent);

            int body = F(13f);
            var pad = new RectOffset((int)S(6f), (int)S(6f), (int)S(4f), (int)S(4f));

            WindowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _window, textColor = Ink },
                onNormal = { background = _window, textColor = Ink },
                focused = { background = _window, textColor = Ink },
                onFocused = { background = _window, textColor = Ink },
                border = NoSlice(),
                padding = new RectOffset((int)S(8f), (int)S(8f), (int)S(24f), (int)S(8f)),
                fontSize = F(14f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
            };

            PanelStyle = new GUIStyle
            {
                normal = { background = _panel },
                border = Slice(),
                padding = new RectOffset((int)S(6f), (int)S(6f), (int)S(6f), (int)S(6f)),
                margin = new RectOffset((int)S(3f), (int)S(3f), (int)S(3f), (int)S(3f)),
            };

            StripStyle = new GUIStyle
            {
                normal = { background = _rowAlt },
                padding = new RectOffset((int)S(8f), (int)S(8f), (int)S(5f), (int)S(5f)),
                margin = new RectOffset(0, 0, (int)S(2f), (int)S(2f)),
            };

            RowStyle = new GUIStyle
            {
                normal = { background = _row },
                padding = pad,
                margin = new RectOffset(0, 0, (int)S(1f), (int)S(1f)),
            };

            RowAltStyle = new GUIStyle(RowStyle) { normal = { background = _rowAlt } };

            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Ink },
                fontSize = body,
                padding = new RectOffset((int)S(2f), (int)S(2f), (int)S(3f), (int)S(3f)),
                wordWrap = false,
                clipping = TextClipping.Clip,

                alignment = TextAnchor.MiddleLeft,
            };

            DimStyle = new GUIStyle(LabelStyle) { normal = { textColor = Dim }, fontSize = F(11f) };

            PriceStyle = new GUIStyle(DimStyle) { alignment = TextAnchor.MiddleRight };

            WrapStyle = new GUIStyle(LabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Overflow,
                fontSize = F(11f),
                normal = { textColor = Dim },
            };

            CardStyle = new GUIStyle(LabelStyle)
            {
                wordWrap = true,
                clipping = TextClipping.Overflow,
                fontSize = F(10f),
                alignment = TextAnchor.UpperLeft,
                normal = { background = _field, textColor = Ink },
                border = Slice(),
                padding = new RectOffset((int)S(7f), (int)S(7f), (int)S(5f), (int)S(5f)),
            };

            HeadingStyle = new GUIStyle(LabelStyle)
            {
                fontSize = F(14f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Accent },
                margin = new RectOffset(0, 0, (int)S(9f), (int)S(3f)),
                alignment = TextAnchor.MiddleLeft,
            };

            CountStyle = new GUIStyle(LabelStyle)
            {
                fontSize = F(16f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Ink, background = _field },
                border = Slice(),
                padding = new RectOffset((int)S(2f), (int)S(2f), (int)S(2f), (int)S(2f)),
                clipping = TextClipping.Overflow,
            };

            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _button, textColor = Ink },
                hover = { background = _buttonHover, textColor = Ink },
                active = { background = _buttonDown, textColor = Ink },

                focused = { background = _button, textColor = Ink },
                onFocused = { background = _selected, textColor = Ink },
                onNormal = { background = _selected, textColor = Ink },
                onHover = { background = _buttonHover, textColor = Ink },
                onActive = { background = _buttonDown, textColor = Ink },
                border = Slice(),
                padding = new RectOffset((int)S(8f), (int)S(8f), (int)S(5f), (int)S(5f)),
                margin = new RectOffset((int)S(2f), (int)S(2f), (int)S(2f), (int)S(2f)),
                fontSize = body,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };

            SmallButtonStyle = new GUIStyle(ButtonStyle)
            {
                padding = new RectOffset((int)S(2f), (int)S(2f), (int)S(3f), (int)S(3f)),
                fontSize = F(12f),
                fontStyle = FontStyle.Bold,
            };

            ListButtonStyle = new GUIStyle(ButtonStyle) { alignment = TextAnchor.MiddleLeft };

            ListButtonSelectedStyle = new GUIStyle(ListButtonStyle)
            {
                normal = { background = _selected, textColor = Ink },
                hover = { background = _selected, textColor = Ink },
                focused = { background = _selected, textColor = Ink },
            };

            ListButtonOffStyle = new GUIStyle(ListButtonStyle)
            {
                normal = { background = _button, textColor = Dim },
                hover = { background = _buttonHover, textColor = Dim },
                focused = { background = _button, textColor = Dim },
            };

            SectionStyle = new GUIStyle(LabelStyle)
            {
                fontSize = F(11f),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Accent },
                margin = new RectOffset((int)S(2f), (int)S(2f), (int)S(6f), (int)S(2f)),
            };

            FieldStyle = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = _field, textColor = Ink },
                focused = { background = _field, textColor = Ink },
                hover = { background = _field, textColor = Ink },
                border = Slice(),
                padding = new RectOffset((int)S(6f), (int)S(6f), (int)S(4f), (int)S(4f)),
                margin = new RectOffset((int)S(2f), (int)S(2f), (int)S(2f), (int)S(2f)),
                fontSize = body,
            };

            ToggleStyle = new GUIStyle(ButtonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset((int)S(4f), (int)S(4f), (int)S(4f), (int)S(4f)),
                margin = new RectOffset((int)S(2f), (int)S(2f), (int)S(2f), (int)S(2f)),
                clipping = TextClipping.Overflow,
            };

            ToggleOnStyle = new GUIStyle(ToggleStyle)
            {
                normal = { background = _selected, textColor = Ink },
                hover = { background = _selected, textColor = Ink },
                focused = { background = _selected, textColor = Ink },
                active = { background = _buttonDown, textColor = Ink },
            };

            BuildSkin();
        }

        private static void BuildSkin()
        {
            Skin = Object.Instantiate(GUI.skin);
            Skin.hideFlags = HideFlags.HideAndDontSave;

            float bar = S(11f);

            var trough = new GUIStyle(Skin.verticalScrollbar)
            {
                normal = { background = _trough },
                border = NoSlice(),
                margin = new RectOffset((int)S(2f), 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = bar,
            };

            var thumb = new GUIStyle(Skin.verticalScrollbarThumb)
            {
                normal = { background = _thumb },
                hover = { background = _thumbHover },
                active = { background = _thumbHover },
                focused = { background = _thumb },
                border = Slice(),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = bar,
            };

            Skin.verticalScrollbar = trough;
            Skin.verticalScrollbarThumb = thumb;
            Skin.verticalScrollbarUpButton = GUIStyle.none;
            Skin.verticalScrollbarDownButton = GUIStyle.none;

            Skin.horizontalScrollbar = new GUIStyle(trough)
            {
                fixedWidth = 0f,
                fixedHeight = bar,
                margin = new RectOffset(0, 0, (int)S(2f), 0),
            };

            Skin.horizontalScrollbarThumb = new GUIStyle(thumb)
            {
                fixedWidth = 0f,
                fixedHeight = bar,
            };

            Skin.horizontalScrollbarLeftButton = GUIStyle.none;
            Skin.horizontalScrollbarRightButton = GUIStyle.none;

            Skin.scrollView = GUIStyle.none;
        }

        internal static float WidestOf(GUIStyle style, IEnumerable<string> labels)
        {
            float widest = 0f;

            foreach (string label in labels)
                widest = Mathf.Max(widest, style.CalcSize(new GUIContent(label)).x);

            return Mathf.Ceil(widest);
        }

        private static Texture2D Solid(Color colour)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, colour);
            texture.Apply();
            return texture;
        }

        private static Texture2D Bordered(Color fill, Color edge)
        {
            int t = _edgePixels;
            int size = 2 * t + 1;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    bool middle = x >= t && x < size - t && y >= t && y < size - t;
                    texture.SetPixel(x, y, middle ? fill : edge);
                }

            texture.Apply();
            return texture;
        }

        internal static void Coloured(Color colour, string text, GUIStyle style,
                                      params GUILayoutOption[] options)
        {
            Color was = GUI.color;
            GUI.color = colour;
            GUILayout.Label(text, style, options);
            GUI.color = was;
        }
    }
}
