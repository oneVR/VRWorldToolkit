using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public static class Styles
    {
        public static GUIStyle HelpBoxRichText { get; internal set; }
        public static GUIStyle HelpBoxPadded { get; internal set; }
        public static GUIStyle LabelRichText { get; internal set; }
        public static GUIStyle RichTextWrap { get; internal set; }
        public static GUIStyle BoldWrap { get; internal set; }
        public static GUIStyle RedLabel { get; internal set; }
        public static GUIStyle TreeViewLabel { get; internal set; }
        public static GUIStyle TreeViewLabelPositive { get; internal set; }
        public static GUIStyle TreeViewLabelNegative { get; internal set; }
        public static GUIStyle TreeViewLabelDimmed { get; internal set; }
        public static GUIStyle TreeViewLabelSelected { get; internal set; }
        public static GUIStyle TreeViewLabelRight { get; internal set; }
        public static GUIStyle TreeViewLabelRightDimmed { get; internal set; }
        public static GUIStyle TreeViewLabelSelectedRight { get; internal set; }
        public static GUIStyle TreeViewLabelPositiveRight { get; internal set; }
        public static GUIStyle TreeViewLabelNegativeRight { get; internal set; }
        public static GUIStyle TreeViewLabelCenter { get; internal set; }
        public static GUIStyle TreeViewLabelPositiveCenter { get; internal set; }
        public static GUIStyle TreeViewLabelNegativeCenter { get; internal set; }
        public static GUIStyle CenteredNoticeLabel { get; internal set; }
        public static GUIStyle CenteredNoticeTitle { get; internal set; }
        public static GUIStyle BuildReportStatsLabel { get; internal set; }
        public static GUIStyle PlatformSelector { get; internal set; }

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

            TreeViewLabelPositive = new GUIStyle(TreeViewLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green },
                hover = { textColor = Color.green }
            };

            TreeViewLabelNegative = new GUIStyle(TreeViewLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red },
                hover = { textColor = Color.red }
            };

            TreeViewLabelDimmed = new GUIStyle(TreeViewLabel)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) },
                hover = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) }
            };

            TreeViewLabelSelected = new GUIStyle("WhiteLabel")
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
            };

            TreeViewLabelRight = new GUIStyle(TreeViewLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            TreeViewLabelRightDimmed = new GUIStyle(TreeViewLabelRight)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) },
                hover = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) }
            };

            TreeViewLabelSelectedRight = new GUIStyle(TreeViewLabelSelected)
            {
                alignment = TextAnchor.MiddleRight,
            };

            TreeViewLabelPositiveRight = new GUIStyle(TreeViewLabelRight)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green },
                hover = { textColor = Color.green }
            };

            TreeViewLabelNegativeRight = new GUIStyle(TreeViewLabelRight)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red },
                hover = { textColor = Color.red }
            };

            TreeViewLabelCenter = new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleCenter
            };

            TreeViewLabelNegativeCenter = new GUIStyle(TreeViewLabelCenter)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red },
                hover = { textColor = Color.red }
            };

            TreeViewLabelPositiveCenter = new GUIStyle(TreeViewLabelCenter)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.green },
                hover = { textColor = Color.green }
            };

            CenteredNoticeLabel = new GUIStyle("Label")
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 17,
                fontStyle = FontStyle.BoldAndItalic,
                normal = { textColor = new Color(0.33f, 0.33f, 0.33f) }
            };

            CenteredNoticeTitle = new GUIStyle("Label")
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 17,
                fontStyle = FontStyle.BoldAndItalic,
            };
            
            BuildReportStatsLabel = new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleRight,
            };

            PlatformSelector = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 25,
                stretchWidth = true,
            };
        }
    }

    public static class Selectors
    {
        public static readonly string[] maxTextureNames = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192", "16384" };
        public static readonly int[] maxTextureSizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };

        public static int MaxSizeIntPopup(int value)
        {
            return EditorGUILayout.IntPopup("Max Size", value, maxTextureNames, maxTextureSizes);
        }

        public static readonly string[] WindowsFormatOptions =
        {
            new("RGB(A) Compressed BC7"),
            new("RGBA Compressed DXT5|BC3"),
            new("RGBA Crunched DXT5|BC3"),
            new("RGBA 64 bit"),
            new("RGBA 32 bit"),
            new("ARGB 16 bit"),
            new("RG Compressed BC5"),
            new("RGBA Float"),
            new("RGBA Half"),
        };

        public static readonly int[] WindowsFormatValues =
        {
            (int)TextureImporterFormat.BC7,
            (int)TextureImporterFormat.DXT5,
            (int)TextureImporterFormat.DXT5Crunched,
            (int)TextureImporterFormat.RGBA64,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.RGBAFloat,
            (int)TextureImporterFormat.RGBAHalf
        };

        public static int WindowsFormatIntPopup(int value)
        {
            return EditorGUILayout.IntPopup("Format", value, WindowsFormatOptions, WindowsFormatValues);
        }

        public static readonly string[] MobileFormatOptions =
        {
            "RGB(A) Compressed ASTC 4x4 block",
            "RGB(A) Compressed ASTC 5x5 block",
            "RGB(A) Compressed ASTC 6x6 block",
            "RGB(A) Compressed ASTC 8x8 block",
            "RGB(A) Compressed ASTC 10x10 block",
            "RGB(A) Compressed ASTC 12x12 block",
            "RGBA Compressed ETC2 8 bits",
            "RGB + 1-bit Alpha Compressed ETC2 4 bits",
            "RGB Compressed ETC2 4 bits",
            "RGBA Crunched ETC2",
            "RGB Crunched ETC",
            "RGBA 32 bit",
            "RGBA 16 bit",
            "RGB 24 bit",
            "RGBA Half",
            "RGBA Float",
        };

        public static readonly int[] MobileFormatValues =
        {
            (int)TextureImporterFormat.ASTC_4x4,
            (int)TextureImporterFormat.ASTC_5x5,
            (int)TextureImporterFormat.ASTC_6x6,
            (int)TextureImporterFormat.ASTC_8x8,
            (int)TextureImporterFormat.ASTC_10x10,
            (int)TextureImporterFormat.ASTC_12x12,
            (int)TextureImporterFormat.ETC2_RGBA8,
            (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            (int)TextureImporterFormat.ETC2_RGB4,
            (int)TextureImporterFormat.ETC2_RGBA8Crunched,
            (int)TextureImporterFormat.ETC_RGB4Crunched,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.RGBAHalf,
            (int)TextureImporterFormat.RGBAFloat,
        };

        public static int MobileFormatIntPopup(int value)
        {
            return EditorGUILayout.IntPopup("Format", value, MobileFormatOptions, MobileFormatValues);
        }
    }

    public static class Validation
    {
        /// <summary>
        /// Sourced from the whitelist included in the VRCSDK
        /// https://creators.vrchat.com/platforms/android/quest-content-limitations/
        /// </summary>
        public static readonly string[] WorldShaderWhiteList =
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
            "UI/Default"
        };

        /// <summary>
        /// Sourced from Unity documentation at:
        /// https://docs.unity3d.com/2022.3/Documentation/Manual/class-TextureImporterOverride.html
        /// </summary>
        public static readonly TextureImporterFormat[] UnsupportedCompressionFormatsAndroid =
        {
            TextureImporterFormat.DXT1,
            TextureImporterFormat.DXT5,
            TextureImporterFormat.DXT1Crunched,
            TextureImporterFormat.DXT5Crunched,
            TextureImporterFormat.BC6H,
            TextureImporterFormat.BC7,
            TextureImporterFormat.PVRTC_RGB2,
            TextureImporterFormat.PVRTC_RGB4,
            TextureImporterFormat.PVRTC_RGBA2,
            TextureImporterFormat.PVRTC_RGBA4,
        };
    }
}