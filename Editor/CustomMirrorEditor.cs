#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
using UnityEditor;
using UnityEngine;
using VRWorldToolkit.DataStructures;

#if (VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3) && !VRWT_DISABLE_EDITORS
namespace VRWorldToolkit
{
    /// <summary>
    /// Custom editor for VRC_MirrorReflection with added quick actions
    /// </summary>
    [CustomEditor(typeof(VRC_MirrorReflection), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class CustomMirrorEditor : Editor
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

            if (GUILayout.Button("Show only players")) MirrorLayerChange(262656);

            if (GUILayout.Button("Show players/world")) MirrorLayerChange(262657);

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

            showExplanations = EditorGUILayout.Foldout(showExplanations, "VRChat specific layer explanations");

            if (showExplanations)
            {
                GUILayout.Label("<b>Player:</b>\nThis layer is used for other players than yourself", Styles.RichTextWrap);
                GUILayout.Label("<b>PlayerLocal:</b>\nThis layer is only used for first-person view and should not be enabled in mirrors", Styles.RichTextWrap);
                GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used for your own mirror version", Styles.RichTextWrap);
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