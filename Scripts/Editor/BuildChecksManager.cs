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
    public class BuildChecksManager : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Scene)
            {
                if (Object.FindObjectsOfType(typeof(VRC_SceneDescriptor)) is VRC_SceneDescriptor[] descriptors && descriptors.Length > 0)
                {
                    var spawnProblems = false;
                    var descriptor = descriptors[0];

                    if (descriptor.spawns != null)
                    {
                        var spawns = descriptor.spawns.Where(s => s != null).ToArray();
                        var spawnsLength = descriptor.spawns.Length;

                        if (spawnsLength != spawns.Length || spawnsLength == 0)
                        {
                            spawnProblems = true;
                        }
                    }
                    else
                    {
                        spawnProblems = true;
                    }

                    if (spawnProblems)
                    {
                        var selection = EditorUtility.DisplayDialogComplex("VRWorld Toolkit: Problem spawn points!", "Null or empty spawn points set in Scene Descriptor.\r\n\r\nSpawning into a null or empty spawn point will cause you get thrown back into your home world.\r\n\r\nSelect Cancel Build if you want to fix the problem yourself or press Bypass to ignore the problem and continue.",
                            "Fix And Continue", "Cancel Build", "Bypass");

                        switch (selection)
                        {
                            case 0:
                                WorldDebugger.FixSpawns(descriptor).Invoke();
                                break;
                            case 1:
                                return false;
                        }
                    }

                    if (Object.FindObjectsOfType(typeof(PipelineManager)) is PipelineManager[] pipelines && pipelines.Length > 1)
                    {
                        var selection = EditorUtility.DisplayDialogComplex("VRWorld Toolkit: Multiple Pipeline managers!", "Multiple Pipeline Manager components found in scene.\r\n\r\nThis can break the upload process and cause you to not be able to load into the world.\r\n\r\nSelect Cancel Build if you want to fix the problem yourself or press Bypass to ignore the problem and continue.",
                            "Fix And Continue", "Cancel Build", "Bypass");

                        switch (selection)
                        {
                            case 0:
                                WorldDebugger.RemoveBadPipelineManagers(pipelines).Invoke();
                                break;
                            case 1:
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
#endif