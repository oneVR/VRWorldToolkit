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

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class QuickFunctions : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", false, 4)]
        public static void CopyWorldID()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager)
                {
                    if (pipelineManager.blueprintId.Length > 0)
                    {
                        EditorGUIUtility.systemCopyBuffer = pipelineManager.blueprintId;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Blueprint ID", "The pipeline manager in the current scene has no Blueprint ID attached to it yet.", "Ok");
                    }
                }
            }
        }
    }
}
#endif