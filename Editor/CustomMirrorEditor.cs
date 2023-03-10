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

            EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.largeLabel);

            EditorGUILayout.LabelField("Quick-Set Reflect Layers:");
            
            EditorGUILayout.LabelField("This function will set the necessary Reflect Layers on this Mirror for optimal performance. If you are new to creating worlds, we highly recommend choosing one of these options.", Styles.HelpBoxRichText);
            
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Show only Players")) MirrorLayerChange(262656);

            if (GUILayout.Button("Show Players/World")) MirrorLayerChange(264705);
            
            EditorGUILayout.EndHorizontal();

            if (Selection.gameObjects.Length == 1)
            {
                var currentMirror = (VRC_MirrorReflection) target;

                if ((LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.positions.Length == 0 && currentMirror.m_DisablePixelLights) || (LightmapSettings.lightProbes is null && currentMirror.m_DisablePixelLights))
                    EditorGUILayout.HelpBox("No baked light probes were found in lighting data. Dynamic objects such as players and pickups will not appear lit in mirrors without baked light probes.", MessageType.Warning);

                if (mirrorMask.intValue == ~0)
                    EditorGUILayout.HelpBox("This mirror has it's Reflect Layers set to Everything. This can lead to degraded performance in populated instances!\nYou should consider disabling unnecessary Reflect Layers to save on performance. Perhaps you can use the Quick-Set Reflect Layer Utility above?", MessageType.Error);

                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("UiMenu"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Having UiMenu enabled on mirrors causes Nameplates to be rendered twice. It should never be enabled on mirrors.", MessageType.Warning);
                
                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("reserved2"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Having reserved2 enabled on mirrors causes the VRChat HUD, Main Menu and Tooltips to be rendered twice, causing a noticeable performance drop in populated instances.", MessageType.Warning);

                if (!Helper.LayerIncludedInMask(LayerMask.NameToLayer("MirrorReflection"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Having the MirrorReflection layer disabled will stop the player from seeing themselves in the mirror.", MessageType.Warning);

                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("PlayerLocal"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("PlayerLocal is only meant to be seen in first-person view and will not render in mirrors. Use MirrorReflection instead.", MessageType.Error);
                
                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("Water"), mirrorMask.intValue))
                    EditorGUILayout.HelpBox("Objects that are in the Water layer are never rendered in mirrors!", MessageType.Error);
            }

            showExplanations = EditorGUILayout.Foldout(showExplanations, "VRChat Specific Layer Descriptions");

            if (showExplanations)
            {
                EditorGUILayout.HelpBox("These VRChat-specific Layers used in the mirror are described below. Only the Default and Environment layer will be used in addition if you choose to Show Players/World.\nDo not use other Layers in the mirror unless you know what you are doing!", MessageType.Info);
                GUILayout.Label("<b>Player:</b>\nThis layer is used to show players other than yourself.", Styles.RichTextWrap);
                GUILayout.Label("<b>Environment:</b>\nThis layer is used for static meshes and objects in the world. Shares the same properties as the Default layer.", Styles.RichTextWrap);
                GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used to show render your own self in the mirror.", Styles.RichTextWrap);
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
