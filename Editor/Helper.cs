using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public static class Helper
    {
        public static float GetBrightness(Color color)
        {
            var num = color.r;
            var num2 = color.g;
            var num3 = color.b;
            var num4 = num;
            var num5 = num;
            if (num2 > num4)
                num4 = num2;
            if (num3 > num4)
                num4 = num3;
            if (num2 < num5)
                num5 = num2;
            if (num3 < num5)
                num5 = num3;
            return (num4 + num5) / 2;
        }

        public static int[] GetAllLayerNumbersFromMask(LayerMask layerMask)
        {
            List<int> layers = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    layers.Add(i);
                }
            }

            return layers.ToArray();
        }

        public static GameObject CreateMainCamera()
        {
            var camera = new GameObject("Main Camera");
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";

            return camera;
        }

        public static string GetAllLayersFromMask(LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            for (var i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    layers.Add(LayerMask.LayerToName(i));
                }
            }

            return String.Join(", ", layers.ToArray());
        }

        public static bool LayerIncludedInMask(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }

        public static string FormatTime(TimeSpan t)
        {
            var formattedTime = "";
            if (t.TotalDays > 1)
            {
                formattedTime = string.Concat(formattedTime, t.Days + " days ");
            }

            if (t.TotalHours > 1)
            {
                formattedTime = string.Concat(formattedTime, t.Hours + " days ");
            }

            if (t.TotalMinutes > 1)
            {
                formattedTime = string.Concat(formattedTime, t.Minutes + " minutes ");
            }
            else
            {
                formattedTime = string.Concat(formattedTime, t.Seconds + " seconds");
            }

            return formattedTime;
        }

        public static RuntimePlatform BuildPlatform()
        {
#if UNITY_ANDROID
            return RuntimePlatform.Android;
#elif UNITY_IOS
            return RuntimePlatform.IPhonePlayer;
#else
            return RuntimePlatform.WindowsPlayer;
#endif
        }

        public static Type GetTypeFromName(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static string GetSteamVrcExecutablePath()
        {
            var steamKey = Registry.LocalMachine.OpenSubKey("Software\\Valve\\Steam") ?? Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Valve\\Steam");

            if (steamKey != null)
            {
                const string commonPath = "\\SteamApps\\common";
                const string executablePath = "\\VRChat.exe";

                var steamPath = (string)steamKey.GetValue("InstallPath");

                var configFile = Path.Combine(steamPath, "config", "config.vdf");

                var folders = new List<string> { steamPath + commonPath };

                var configText = File.ReadAllText(configFile);

                folders.AddRange(Regex.Matches(configText, "(?<=BaseInstallFolder.*\".+?\").+?(?=\")").Cast<Match>().Select(x => x.Value + commonPath));

                foreach (var folder in folders)
                {
                    try
                    {
                        var matches = Directory.GetDirectories(folder, "VRChat");
                        if (matches.Length >= 1)
                        {
                            var finalPath = matches[0] + executablePath;

                            if (File.Exists(finalPath)) return finalPath;
                        }
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                }
            }

            return null;
        }

        public static void AddTag(string tag)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                var so = new SerializedObject(asset[0]);
                var tags = so.FindProperty("tags");

                for (var i = 0; i < tags.arraySize; ++i)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        return;
                    }
                }

                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                so.ApplyModifiedProperties();
                so.Update();
            }
        }

        public static bool TagExists(string tag)
        {
            UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if ((asset != null) && (asset.Length > 0))
            {
                var so = new SerializedObject(asset[0]);
                var tags = so.FindProperty("tags");

                for (var i = 0; i < tags.arraySize; ++i)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}