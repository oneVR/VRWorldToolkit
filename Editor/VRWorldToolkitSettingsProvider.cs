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
        
        public VRWorldToolkitSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) {}

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VRWorldToolkitSettingsProvider("Project/VRWorld Toolkit", SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 215f;
            var serializedObject = new SerializedObject(VRWorldToolkitSettings.Instance);
            
            GUILayout.Label("VRChat", Styles.LabelTitle);
            EditorGUILayout.HelpBox("The following settings only apply when used with the VRChat SDK.", MessageType.Info, true);
            
            GUILayout.Label("Udon", Styles.SubTitle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VRWorldToolkitSettings.Instance.defaultUdonBehaviourSyncMode)));

            GUILayout.Label("Build Reports", Styles.SubTitle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VRWorldToolkitSettings.Instance.alwaysCaptureBuildReport)));
            EditorGUILayout.HelpBox("When enabled everytime you build from the VRChat SDK the build report will be captured and archived depending on the settings below.", MessageType.Info, true);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VRWorldToolkitSettings.Instance.archiveBuildReports)));
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!VRWorldToolkitSettings.Instance.archiveBuildReports))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VRWorldToolkitSettings.Instance.rotateArchivedBuildReports)));
                    using (new EditorGUI.DisabledScope(!VRWorldToolkitSettings.Instance.rotateArchivedBuildReports))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(VRWorldToolkitSettings.Instance.maxArchivedBuildReports)));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}