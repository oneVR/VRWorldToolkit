#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.Core;
#endif
using UnityEditor;
using UnityEngine;

namespace VRWorldToolkit
{
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
    public class QuickFunctions : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", false, 15)]
        public static void CopyWorldID()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager)
                {
                    EditorGUIUtility.systemCopyBuffer = pipelineManager.blueprintId;
                }
            }
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", true)]
        private static bool CopyWorldIDValidate()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager)
                    return pipelineManager.blueprintId.Length > 0;
            }

            return false;
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", false, 16)]
        public static void SetupLayersCollisionMatrix()
        {
            if (!UpdateLayers.AreLayersSetup())
            {
                UpdateLayers.SetupEditorLayers();
            }

            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                UpdateLayers.SetupCollisionLayerMatrix();
            }
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", true)]
        private static bool SetupLayersCollisionMatrixValidate()
        {
            if (UpdateLayers.AreLayersSetup() && UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                return false;
            }

            return true;
        }
#endif
        [MenuItem("VRWorld Toolkit/Quick Functions/Remove Missing Scripts From Scene", false, 17)]
        private static void FindAndRemoveMissingScripts()
        {
            if (EditorUtility.DisplayDialog("Remove Missing Scripts", "Running this will go through all GameObjects in the open scene and remove any components with missing scripts. This action can't be reversed!\n\nAre you sure you want to continue?", "Continue", "Cancel"))
            {
                var overallRemovedCount = 0;
                var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                var allGameObjectsLength = allGameObjects.Length;
                for (var i = 0; i < allGameObjectsLength; i++)
                {
                    var gameObject = allGameObjects[i] as GameObject;

                    if (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject)) continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Checking For Missing Scripts", gameObject.name, (float) i / allGameObjectsLength))
                    {
                        break;
                    }

                    var serializedObject = new SerializedObject(gameObject);
                    var property = serializedObject.FindProperty("m_Component");
                    var components = gameObject.GetComponents<Component>();
                    var removedCount = 0;
                    for (var j = 0; j < components.Length; j++)
                    {
                        if (components[j] is null)
                        {
                            property.DeleteArrayElementAtIndex(j - removedCount);
                            removedCount++;
                        }
                    }

                    overallRemovedCount += removedCount;
                    if (removedCount > 0) serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.ClearProgressBar();
                var message = overallRemovedCount > 0 ? $"Removed total of {overallRemovedCount} components with missing scripts." : "No components with missing scripts were found.";
                EditorUtility.DisplayDialog("Remove Missing Scripts", message, "Ok");
            }
        }
    }
}