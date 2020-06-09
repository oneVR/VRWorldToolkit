using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class CustomEditors : MonoBehaviour
    {
        //Custom editor for VRCMirror for quickly setting layers correctly
        [CustomEditor(typeof(VRC_MirrorReflection), true, isFallback = false)]
        [CanEditMultipleObjects]
        public class CustomMirrorInspector : Editor
        {
            private bool _showExplanations = false;

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Quick set Reflect Layers:");

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Show only players"))
                {
                    foreach (var o in Selection.objects)
                    {
                        var mirror = (GameObject) o;
                        mirror.GetComponent<VRC_MirrorReflection>().m_ReflectLayers.value = 262656;
                    }
                    
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                if (GUILayout.Button("Show players/world"))
                {
                    foreach (var o in Selection.objects)
                    {
                        var mirror = (GameObject) o;
                        mirror.GetComponent<VRC_MirrorReflection>().m_ReflectLayers.value = 262657;
                    }
                    
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                EditorGUILayout.EndHorizontal();

                if (Selection.gameObjects.Length == 1)
                {
                    VRC_MirrorReflection currentMirror = (VRC_MirrorReflection)target;

                    if (currentMirror.m_ReflectLayers == -1025)
                    {
                        EditorGUILayout.HelpBox("Avoid using default layers on mirrors to save on frames, you should disable all layers that aren't needed in this mirror.", MessageType.Info);
                    }

                    if (Helper.LayerIncludedInMask(LayerMask.NameToLayer("UiMenu"), currentMirror.m_ReflectLayers))
                    {
                        EditorGUILayout.HelpBox("Having UiMenu enabled on mirrors causes VRChat UI elements to render twice which can cause noticable performance drop in populated instances", MessageType.Warning);
                    }
                }

                _showExplanations = EditorGUILayout.Foldout(_showExplanations, "VRChat specific layer explanations");

                if (_showExplanations)
                {
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true
                    };

                    GUILayout.Label("<b>Player:</b>\nThis layer is used for other players than yourself", style);
                    GUILayout.Label("<b>PlayerLocal:</b>\nThis layer is only used for first-person view and should not be enabled in mirrors", style);
                    GUILayout.Label("<b>MirrorReflection:</b>\nThis layer is used for your own mirror version", style);
                }
            }
        }
    }
}
#endif