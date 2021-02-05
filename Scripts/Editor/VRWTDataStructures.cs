using UnityEngine;

namespace VRWorldToolkit.DataStructures
{
    public static class Styles
    {
        public static GUIStyle HelpBoxRichText { get; internal set; }
        public static GUIStyle HelpBoxPadded { get; internal set; }
        public static GUIStyle LabelRichText { get; internal set; }
        public static GUIStyle RichText { get; internal set; }
        public static GUIStyle RichTextWrap { get; internal set; }
        public static GUIStyle BoldWrap { get; internal set; }
        public static GUIStyle RedLabel { get; internal set; }
        public static GUIStyle TreeViewLabel { get; internal set; }
        public static GUIStyle TreeViewLabelSelected { get; internal set; }
        public static GUIStyle CenteredLabel { get; internal set; }
        public static GUIStyle Center { get; internal set; }

        static Styles()
        {
            Reload();
        }

        static void Reload()
        {
            HelpBoxRichText = new GUIStyle("HelpBox")
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            HelpBoxPadded = new GUIStyle("HelpBox")
            {
                margin = new RectOffset(18, 4, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            LabelRichText = new GUIStyle("Label")
            {
                richText = true,
                margin = new RectOffset(5, 5, 0, 0),
            };

            RichText = new GUIStyle
            {
                richText = true
            };

            RichTextWrap = new GUIStyle("Label")
            {
                richText = true,
                wordWrap = true
            };

            BoldWrap = new GUIStyle("boldLabel")
            {
                wordWrap = true
            };

            RedLabel = new GUIStyle("Label")
            {
                normal =
                {
                    textColor = Color.red,
                },
            };

            TreeViewLabel = new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
            };

            TreeViewLabelSelected = new GUIStyle("WhiteLabel")
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
            };

            CenteredLabel = new GUIStyle("Label")
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 17,
                fontStyle = FontStyle.BoldAndItalic,
                normal =
                {
                    textColor = new Color(0.33f, 0.33f, 0.33f),
                }
            };

            Center = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }

    public static class Validation
    {
        public static readonly string[] WorldShaderWhiteList = new string[]
        {
            "VRChat/Mobile/Standard Lite",
            "VRChat/Mobile/Diffuse",
            "VRChat/Mobile/Bumped Diffuse",
            "VRChat/Mobile/Bumped Mapped Specular",
            "VRChat/Mobile/Toon Lit",
            "VRChat/Mobile/MatCap Lit",
            "VRChat/Mobile/Lightmapped",
            "VRChat/Mobile/Skybox",
            "VRChat/Mobile/Particles/Additive",
            "VRChat/Mobile/Particles/Multiply",
            "FX/MirrorReflection",
            "UI/Default",
        };
    }
}