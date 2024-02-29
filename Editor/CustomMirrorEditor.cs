#if VRC_SDK_VRCSDK3
#if !VRWT_DISABLE_EDITORS
using VRC.SDKBase;
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    /// <summary>
    /// Custom editor for VRC_MirrorReflection with added quick actions
    /// </summary>
    [CustomEditor(typeof(VRC_MirrorReflection), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class CustomMirrorEditor : UnityEditor.Editor
    {
        private bool showExplanations;
        private SerializedProperty mirrorMask;

        private void OnEnable()
        {
            mirrorMask = serializedObject.FindProperty("m_ReflectLayers");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Quick set Reflect Layers:");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Show only Players")) MirrorLayerChange(262656);

            if (GUILayout.Button("Show Players/World")) MirrorLayerChange(264705);

            EditorGUILayout.EndHorizontal();

            if (Selection.gameObjects.Length == 1)
            {
                var currentMirror = (VRC_MirrorReflection) target;

                if ((LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.positions.Length == 0 && currentMirror.m_DisablePixelLights) || (LightmapSettings.lightProbes is null && currentMirror.m_DisablePixelLights))
                    EditorGUILayout.HelpBox("No baked light probes were found in lighting data. Dynamic objects such as players and pickups will not appear lit in mirrors without baked light probes.", MessageType.Warning);

                if (mirrorMask.intValue == -1025)
                    EditorGUILayout.HelpBox("This mirror has default layers set. Unnecessary layers should be disabled to save on performance.", MessageType.Info);

                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("UiMenu"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Having UiMenu enabled on mirrors causes VRChat UI elements to be rendered twice, causing a noticeable performance drop in populated instances.", MessageType.Warning);

                if (!Helper.LayerIncludedInMask(LayerMask.NameToLayer("MirrorReflection"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Having the MirrorReflection layer disabled will stop the player from seeing themselves in the mirror.", MessageType.Warning);

                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("PlayerLocal"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("PlayerLocal is only meant to be seen in first-person view and should not be enabled on mirrors.", MessageType.Error);
            }

            showExplanations = EditorGUILayout.Foldout(showExplanations, "VRChat Specific Layer Descriptions");

            if (showExplanations)
            {
                GUILayout.Label("<b>Player:</b>\nThis layer is used to show players other than yourself.", Styles.RichTextWrap);
                GUILayout.Label("<b>PlayerLocal:</b>\nThis layer is only used for first-person view and should not be enabled in mirrors.", Styles.RichTextWrap);
                GUILayout.Label("<b>Environment:</b>\nThis layer is used for static meshes and objects in the world. Shares the same properties as the Default layer.", Styles.RichTextWrap);
                GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used to fully show your own self in the mirror.", Styles.RichTextWrap);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Change selected Reflect Layers on selected VRC_MirrorReflections to the supplied LayerMask value
        /// </summary>
        /// <param name="layerMask">New LayerMask value to set for Reflect Layers</param>
        private void MirrorLayerChange(int layerMask)
        {
            mirrorMask.intValue = layerMask;
        }
    }
}
#endif
#endif