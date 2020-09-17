using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
    public class VRWTAbout : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/About VRWorld Toolkit", false, 100)]
        public static void ShowWindow()
        {
            var window = (VRWTAbout)GetWindow(typeof(VRWTAbout), true, "VRWorld Toolkit");
            window.minSize = new Vector2(600, 380);
            window.maxSize = new Vector2(600, 380);
            window.Show();
        }

        private static GUIStyle Header, Text;

        private static Texture IconTwitter, IconDiscord, IconGithub, IconPatreon, IconKofi;

        public void OnEnable()
        {
            Header = new GUIStyle
            {
                normal =
                {
                    background = Resources.Load("SplashTextures/VRWTSplashLogo") as Texture2D,
                    textColor = Color.white,
                },
                fixedHeight = 140
            };

            Text = new GUIStyle
            {
                padding = new RectOffset(5, 5, 5, 5),
                wordWrap = true,
                richText = true
            };

            IconTwitter = Resources.Load("SplashTextures/IconTwitter") as Texture2D;
            IconDiscord = Resources.Load("SplashTextures/IconDiscord") as Texture2D;
            IconGithub = Resources.Load("SplashTextures/IconGithub") as Texture2D;
            IconPatreon = Resources.Load("SplashTextures/IconPatreon") as Texture2D;
            IconKofi = Resources.Load("SplashTextures/IconKoFi") as Texture2D;
        }

        private void OnGUI()
        {
            // Header Image
            GUILayout.Box("", Header);

            // Information Texts
            GUILayout.Label("Welcome to VRWorld Toolkit!", EditorStyles.boldLabel);

            GUILayout.Label("VRWorld Toolkit is an project aimed at helping people get into world building faster without having to spent time on combing different documentations for all the smaller mistakes you can make while making your first world. Even for experienced world builders it helps make tedious steps like setting up post processing faster and helps you not forget the dozen little things you need to remember while building worlds.", Text);

            GUILayout.Label("If you have suggestions, found problems with the included tools or just want to check my social channels you can click on the buttons below. Feedback is always welcome so I know what to improve!", Text);

            GUILayout.FlexibleSpace();

            // Social Buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(IconTwitter, GUIStyle.none)) Application.OpenURL("https://twitter.com/Sackboy_1");
            GUILayout.Space(20);
            if (GUILayout.Button(IconDiscord, GUIStyle.none)) Application.OpenURL("https://discord.gg/8w2Tc6C");
            GUILayout.Space(20);
            if (GUILayout.Button(IconGithub, GUIStyle.none)) Application.OpenURL("https://github.com/oneVR/VRWorldToolkit");
            GUILayout.Space(20);
            if (GUILayout.Button(IconPatreon, GUIStyle.none)) Application.OpenURL("https://www.patreon.com/onevr");
            GUILayout.Space(20);
            if (GUILayout.Button(IconKofi, GUIStyle.none)) Application.OpenURL("https://ko-fi.com/onevr");

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
        }
    }
}
