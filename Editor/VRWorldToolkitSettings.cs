using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class VRWorldToolkitSettings : ScriptableObject
    {
        private const string defaultSettingsPath = "Assets/VRWorldToolkit/";

        public enum AssignUdonBehaviourSyncMode { Ignore, Continuous, Manual, None }
        [Tooltip("Specifies the Sync Mode to assign by default to newly created UdonBehaviours. Ignore will leave the UdonBehaviour as the VRChat SDK default.")]
        public AssignUdonBehaviourSyncMode defaultUdonBehaviourSyncMode = AssignUdonBehaviourSyncMode.None;
        
        private static VRWorldToolkitSettings CreateSettings(string path)
        {
            VRWorldToolkitSettings settings = CreateInstance<VRWorldToolkitSettings>();
            CheckOrCreateDirectoryPath(defaultSettingsPath);
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
            
            string[] directories = path.Split('/');
            string pathing = directories[0];
            for (int i = 1; i < directories.Length; i++)
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
            
            string[] foundAssetGUIDs = AssetDatabase.FindAssets("t:ScriptableObject VRWorld Toolkit Settings");
            if (foundAssetGUIDs != null)
            {
                foreach (string foundAssetGUID in foundAssetGUIDs)
                {
                    currentSettingsAsset = AssetDatabase.LoadAssetAtPath<VRWorldToolkitSettings>(AssetDatabase.GUIDToAssetPath(foundAssetGUID));
                    if (currentSettingsAsset)
                        return currentSettingsAsset;
                }
            }
            
            if (!currentSettingsAsset)
                currentSettingsAsset = CreateSettings(defaultSettingsPath);

            return currentSettingsAsset;
        }
    }
}