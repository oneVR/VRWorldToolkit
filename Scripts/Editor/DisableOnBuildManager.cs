#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRWorldToolkit
{
    public class DisableOnBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (TagHelper.TagExists("DisableOnBuild"))
            {
                var toDisableOnBuild = GameObject.FindGameObjectsWithTag("DisableOnBuild");

                foreach (var disableThis in toDisableOnBuild)
                    disableThis.SetActive(false);
            }

            return true;
        }
    }

    [InitializeOnLoad]
    public class DestroyRedundantOnBuildMethod : Editor
    {
        static DestroyRedundantOnBuildMethod()
        {
            var cube = GameObject.Find("/_DisableOnBuild");

            if (cube && cube.hideFlags == HideFlags.HideInHierarchy)
            {
                DestroyImmediate(cube);

                Debug.Log("VRWorld Toolkit: Removed old Disable On Build object");
            }
        }
    }

    public class DisableOnBuildManager : Editor
    {
        [MenuItem("VRWorld Toolkit/Disable On Build/Setup", false, 2)]
        private static void DisableOnUploadSetup()
        {
            if (EditorUtility.DisplayDialog("Setup Disable On Build", "This setup will add a new tag DisableOnBuild. Assigning this tag to a GameObject will disable it before a build happens.", "Setup", "Cancel"))
            {
                TagHelper.AddTag("DisableOnBuild");
            }
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Setup", true)]
        private static bool DisableOnUploadSetupValidate()
        {
            return !TagHelper.TagExists("DisableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Disable Objects", false, 13)]
        private static void DisableObjectsLoop()
        {
            //Loop trough the objects with the tag disabling them
            GameObject[] toDisableOnBuild = GameObject.FindGameObjectsWithTag("DisableOnBuild");
            foreach (GameObject disableThis in toDisableOnBuild)
            {
                disableThis.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Disable Objects", true)]
        private static bool DisableObjectsValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Enable Objects", false, 14)]
        private static void EnableObjectsLoop()
        {
            //Loop trough every game object in the scene since you can't find them with tag when disabled
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                if (obj && obj.tag == "DisableOnBuild")
                {
                    obj.SetActive(true);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Enable Objects", true)]
        private static bool EnableObjectsLoopValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }
    }
}
#endif