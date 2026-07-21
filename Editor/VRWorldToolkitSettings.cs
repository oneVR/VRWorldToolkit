using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace VRWorldToolkit.Editor
{
    public class VRWorldToolkitSettings : ScriptableObject
    {
        private static VRWorldToolkitSettings _instance;
        public static VRWorldToolkitSettings Instance => _instance != null ? _instance : _instance = GetOrCreateSettings();

        private const string DefaultSettingsPath = "Assets/VRWorldToolkit/";

        // Udon
        public enum AssignUdonBehaviourSyncMode { DoNotOverride, Continuous, Manual, None }
        [Tooltip("Specifies the Sync Mode to assign by default to newly created UdonBehaviours. Do Not Override will leave the UdonBehaviour as the VRChat SDK default.")]
        public AssignUdonBehaviourSyncMode defaultUdonBehaviourSyncMode = AssignUdonBehaviourSyncMode.DoNotOverride;

        // Build Reports
        public bool alwaysCaptureBuildReport = true;
        public bool archiveBuildReports = true;
        public bool rotateArchivedBuildReports = true;
        [Min(1)] public int maxArchivedBuildReports = 30;

        // World Debugger
        public bool alwaysDisplaySizesInKB = false;
        public List<int> ignoredWorldDebuggerMessages;
        
        private static VRWorldToolkitSettings CreateSettings(string path)
        {
            var settings = CreateInstance<VRWorldToolkitSettings>();
            CheckOrCreateDirectoryPath(DefaultSettingsPath);
            AssetDatabase.CreateAsset(settings, $"{path}VRWorldToolkitSettings.asset");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            return settings;
        }

        private static void CheckOrCreateDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            
            // Assuming '/' is separator, this may need to be changed for other file path systems.
            if (!path.Contains('/'))
                return;
            
            var directories = path.Split('/');
            var pathing = directories[0];
            for (var i = 1; i < directories.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(directories[i]))
                    continue;

                if (!AssetDatabase.IsValidFolder($"{pathing}/{directories[i]}"))
                {
                    Debug.Log($"{pathing}, {directories[i]}");
                    AssetDatabase.CreateFolder(pathing, directories[i]);
                }

                pathing += "/" + directories[i];
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        public static VRWorldToolkitSettings GetOrCreateSettings()
        {
            VRWorldToolkitSettings currentSettingsAsset = null;
            
            var foundAssetGUIDs = AssetDatabase.FindAssets("t:ScriptableObject VRWorldToolkitSettings");
            if (foundAssetGUIDs != null)
            {
                foreach (var foundAssetGUID in foundAssetGUIDs)
                {
                    currentSettingsAsset = AssetDatabase.LoadAssetAtPath<VRWorldToolkitSettings>(AssetDatabase.GUIDToAssetPath(foundAssetGUID));
                    if (currentSettingsAsset)
                        return currentSettingsAsset;
                }
            }
            
            if (!currentSettingsAsset)
                currentSettingsAsset = CreateSettings(DefaultSettingsPath);

            return currentSettingsAsset;
        }
    }
}