using UnityEngine;

namespace GKFR.ModLoader
{
    static class GarfieldTheme
    {
        public static readonly Color Cream = new Color(1f, 0.96f, 0.82f, 1f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.12f, 1f);
        public static readonly Color DarkOrange = new Color(0.85f, 0.32f, 0.05f, 1f);
        public static readonly Color Brown = new Color(0.28f, 0.13f, 0.05f, 0.94f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.28f, 1f);
        public static readonly Color Shadow = new Color(0.12f, 0.05f, 0.02f, 0.92f);

        public static GUISkin Skin { get; private set; }

        public static void Ensure()
        {
            if (Skin != null)
            {
                return;
            }

            Skin = ScriptableObject.CreateInstance<GUISkin>();
            Object.DontDestroyOnLoad(Skin);

            Skin.box = MakeBox();
            Skin.window = MakeWindow();
            Skin.label = MakeLabel(18, FontStyle.Bold, Cream);
            Skin.button = MakeButton();
            Skin.toggle = MakeToggle();
            Skin.horizontalSlider = MakeSlider();
            Skin.horizontalSliderThumb = MakeSliderThumb();
            Skin.textField = MakeTextField();
            Skin.verticalScrollbar = MakeScrollbar(false);
            Skin.verticalScrollbarThumb = MakeScrollbar(true);
            Skin.scrollView = MakeBox();
            Skin.font = GUI.skin.font;
        }

        static GUIStyle MakeWindow()
        {
            var style = new GUIStyle(GUI.skin.window);
            style.normal.background = Tex(Brown);
            style.onNormal.background = Tex(Brown);
            style.active.background = Tex(Brown);
            style.focused.background = Tex(Brown);
            style.normal.textColor = Gold;
            style.onNormal.textColor = Gold;
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperCenter;
            style.padding = new RectOffset(16, 16, 32, 14);
            style.border = new RectOffset(8, 8, 8, 8);
            return style;
        }

        static GUIStyle MakeBox()
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = Tex(Shadow);
            style.normal.textColor = Cream;
            style.padding = new RectOffset(8, 8, 8, 8);
            return style;
        }

        static GUIStyle MakeLabel(int size, FontStyle fontStyle, Color color)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            style.fontSize = size;
            style.fontStyle = fontStyle;
            style.wordWrap = true;
            style.richText = true;
            return style;
        }

        static GUIStyle MakeButton()
        {
            var style = new GUIStyle(GUI.skin.button);
            Texture2D up = Tex(DarkOrange);
            Texture2D hover = Tex(Orange);
            style.normal.background = up;
            style.hover.background = hover;
            style.active.background = Tex(Gold);
            style.normal.textColor = Cream;
            style.hover.textColor = Color.white;
            style.active.textColor = Shadow;
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(10, 10, 6, 6);
            return style;
        }

        static GUIStyle MakeToggle()
        {
            var style = new GUIStyle(GUI.skin.toggle);
            style.normal.textColor = Cream;
            style.onNormal.textColor = Gold;
            style.hover.textColor = Color.white;
            style.onHover.textColor = Gold;
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.padding = new RectOffset(24, 4, 4, 4);
            return style;
        }

        static GUIStyle MakeSlider()
        {
            var style = new GUIStyle(GUI.skin.horizontalSlider);
            style.normal.background = Tex(new Color(0.45f, 0.22f, 0.08f, 1f));
            style.fixedHeight = 10f;
            return style;
        }

        static GUIStyle MakeSliderThumb()
        {
            var style = new GUIStyle(GUI.skin.horizontalSliderThumb);
            style.normal.background = Tex(Orange);
            style.hover.background = Tex(Gold);
            style.fixedWidth = 18f;
            style.fixedHeight = 18f;
            return style;
        }

        static GUIStyle MakeTextField()
        {
            var style = new GUIStyle(GUI.skin.textField);
            style.normal.background = Tex(new Color(0.18f, 0.08f, 0.03f, 1f));
            style.normal.textColor = Cream;
            style.focused.textColor = Gold;
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(8, 8, 4, 4);
            return style;
        }

        static GUIStyle MakeScrollbar(bool thumb)
        {
            var style = new GUIStyle(thumb ? GUI.skin.verticalScrollbarThumb : GUI.skin.verticalScrollbar);
            style.normal.background = Tex(thumb ? Orange : Shadow);
            return style;
        }

        static Texture2D Tex(Color color)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
