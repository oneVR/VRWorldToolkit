using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class VRWorldToolkitSettingsProvider : SettingsProvider
    {
        [MenuItem("VRWorld Toolkit/Settings")]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/VRWorld Toolkit");
        }

        private VRWorldToolkitSettings settingsInstance;
        
        public VRWorldToolkitSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) {}

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VRWorldToolkitSettingsProvider("Project/VRWorld Toolkit", SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            if (!settingsInstance)
                settingsInstance = VRWorldToolkitSettings.GetOrCreateSettings();

            EditorGUIUtility.labelWidth = 215f;   
            SerializedObject serializedObject = new SerializedObject(settingsInstance);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(settingsInstance.defaultUdonBehaviourSyncMode)));
            serializedObject.ApplyModifiedProperties();
        }
    }
}