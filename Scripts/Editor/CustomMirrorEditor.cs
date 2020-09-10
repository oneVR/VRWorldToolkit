#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        bool showExplanations;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Quick set Reflect Layers:");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Show only players")) MirrorLayerChange(262656);

            if (GUILayout.Button("Show players/world")) MirrorLayerChange(262657);

            EditorGUILayout.EndHorizontal();

            if (Selection.gameObjects.Length == 1)
            {
                var currentMirror = (VRC_MirrorReflection)target;

                if ((LightmapSettings.lightProbes != null && LightmapSettings.lightProbes.positions.Length == 0 && currentMirror.m_DisablePixelLights) || (LightmapSettings.lightProbes is null && currentMirror.m_DisablePixelLights))
                    EditorGUILayout.HelpBox("No baked light probes found in lighting data. Dynamic objects objects such as players and pickups won't appear lit in mirrors without having baked light probes.", MessageType.Warning);

                if (currentMirror.m_ReflectLayers == -1025)
                    EditorGUILayout.HelpBox("Avoid using default layers on mirrors to save on frames, you should disable all layers that aren't needed in this mirror.", MessageType.Info);

                if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("UiMenu"), currentMirror.m_ReflectLayers))
                    EditorGUILayout.HelpBox("Having UiMenu enabled on mirrors causes VRChat UI elements to render twice which can cause noticeable performance drop in populated instances", MessageType.Warning);
            }

            showExplanations = EditorGUILayout.Foldout(showExplanations, "VRChat specific layer explanations");

            if (showExplanations)
            {
                GUILayout.Label("<b>Player:</b>\nThis layer is used for other players than yourself", Styles.RichTextWrap);
                GUILayout.Label("<b>PlayerLocal:</b>\nThis layer is only used for first-person view and should not be enabled in mirrors", Styles.RichTextWrap);
                GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used for your own mirror version", Styles.RichTextWrap);
            }
        }

        /// <summary>
        /// Change selected Reflect Layers on selected VRC_MirrorReflections to the supplied LayerMask value
        /// </summary>
        /// <param name="layerMask">New LayerMask value to set for Reflect Layers</param>
        private static void MirrorLayerChange(int layerMask)
        {
            for (var index = 0; index < Selection.objects.Length; index++)
            {
                var gameObject = Selection.objects[index] as GameObject;

                if (gameObject == null) continue;

                var mirror = gameObject.GetComponent<VRC_MirrorReflection>();

                mirror.m_ReflectLayers.value = layerMask;

                PrefabUtility.RecordPrefabInstancePropertyModifications(mirror);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif