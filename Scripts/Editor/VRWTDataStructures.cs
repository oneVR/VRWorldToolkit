using UnityEngine;

namespace VRWorldToolkit.DataStructures
{
    public static class Styles
    {
        public static GUIStyle HelpBoxRichText { get; internal set; }
        public static GUIStyle HelpBoxPadded { get; internal set; }
        public static GUIStyle RichText { get; internal set; }
        public static GUIStyle RichTextWrap { get; internal set; }
        public static GUIStyle RedText { get; internal set; }

        static Styles()
        {
            Reload();
        }

        static void Reload()
        {
            HelpBoxRichText = new GUIStyle("HelpBox")
            {
                richText = true
            };

            HelpBoxPadded = new GUIStyle("HelpBox")
            {
                margin = new RectOffset(18, 4, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            RichText = new GUIStyle
            {
                richText = true
            };

            RichTextWrap = new GUIStyle
            {
                richText = true,
                wordWrap = true
            };

            RedText = new GUIStyle { };
            RedText.normal.textColor = Color.red;
        }
    }
}
