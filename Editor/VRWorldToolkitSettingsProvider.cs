using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class VRWorldToolkitSettingsProvider : SettingsProvider
    {
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
            
            SerializedObject serializedObject = new SerializedObject(settingsInstance);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(settingsInstance.defaultUdonBehaviourSyncMode)));
        }
    }
}