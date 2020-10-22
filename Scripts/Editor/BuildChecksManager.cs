#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;
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
using System.Linq;

namespace VRWorldToolkit
{
    public class BuildChecksManager : MonoBehaviour
    {
        public class OnBuildChecksCallback : IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 0;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                var pipelines = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];

                if (pipelines.Length > 1)
                {
                    int selection = EditorUtility.DisplayDialogComplex("VRWorld Toolkit: Multiple Pipeline managers!", "Multiple Pipeline Manager components found in scene.\r\n\r\nThis can break the upload process and cause you to not be able to load into the world.\r\n\r\nSelect Cancel Build if you want to fix the problem yourself or press Bypass to ignore the problem and continue.", "Fix And Continue", "Cancel Build", "Bypass");

                    switch (selection)
                    {
                        case 0:
                            WorldDebugger.RemoveBadPipelineManagers(pipelines).Invoke();
                            break;
                        case 1:
                            return false;
                        default:
                            break;
                    }
                }

                var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

                if (descriptors.Length > 0)
                {
                    var spawns = descriptors[0].spawns.Where(s => s != null).ToArray();
                    var spawnsLength = descriptors[0].spawns.Length;

                    if (spawnsLength != spawns.Length)
                    {
                        int selection = EditorUtility.DisplayDialogComplex("VRWorld Toolkit: Null spawn points!", "Null spawn points set in Scene Descriptor.\r\n\r\nSpawning into a null spawn point will cause you get thrown back into your home world.\r\n\r\nSelect Cancel Build if you want to fix the problem yourself or press Bypass to ignore the problem and continue.", "Fix And Continue", "Cancel Build", "Bypass");

                        switch (selection)
                        {
                            case 0:
                                WorldDebugger.FixSpawns(descriptors[0]).Invoke();
                                break;
                            case 1:
                                return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
#endif