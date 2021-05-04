#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRWorldToolkit
{
    public class DisableOnBuildCallback : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 1;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            DisableOnBuildManager.ToggleObjectsUsingTag("DisableOnBuild", false, false);
            DisableOnBuildManager.ToggleObjectsUsingTag("EnableOnBuild", true, false);

            return true;
        }
    }

    public class DisableOnBuildManager : Editor
    {
        // Disable On Build
        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Setup", false, 2)]
        private static void DisableOnBuildSetup()
        {
            if (EditorUtility.DisplayDialog("Setup Disable On Build", "This setup will add a new tag DisableOnBuild. Assigning this tag to a GameObject will disable it before a build happens.", "Setup", "Cancel"))
            {
                TagHelper.AddTag("DisableOnBuild");
            }
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Setup", true)]
        private static bool DisableOnBuildSetupValidate()
        {
            return !TagHelper.TagExists("DisableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Disable Objects", false, 13)]
        private static void DisableDisableObjectsLoop()
        {
            ToggleObjectsUsingTag("DisableOnBuild", false, true);
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Disable Objects", true)]
        private static bool DisableDisableObjectsValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Enable Objects", false, 14)]
        private static void EnableDisableObjectsLoop()
        {
            ToggleObjectsUsingTag("DisableOnBuild", true, true);
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Disable On Build/Enable Objects", true)]
        private static bool EnableObjectsLoopValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }

        // Enable On Build
        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Setup", false, 13)]
        private static void EnableOnBuildSetup()
        {
            if (EditorUtility.DisplayDialog("Setup Enable On Build", "This setup will add a new tag EnableOnBuild. Assigning this tag to a GameObject will enable it before a build happens.", "Setup", "Cancel"))
            {
                TagHelper.AddTag("EnableOnBuild");
            }
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Setup", true)]
        private static bool EnableOnBuildSetupValidate()
        {
            return !TagHelper.TagExists("EnableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Disable Objects", false, 24)]
        private static void DisableEnableObjectsLoop()
        {
            ToggleObjectsUsingTag("EnableOnBuild", false, true);
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Disable Objects", true)]
        private static bool DisableEnableObjectsValidate()
        {
            return TagHelper.TagExists("EnableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Enable Objects", false, 25)]
        private static void EnableEnableObjectsLoop()
        {
            ToggleObjectsUsingTag("EnableOnBuild", true, true);
        }

        [MenuItem("VRWorld Toolkit/On Build Functions/Enable On Build/Enable Objects", true)]
        private static bool EnableEnableObjectsLoopValidate()
        {
            return TagHelper.TagExists("EnableOnBuild");
        }

        public static void ToggleObjectsUsingTag(string tag, bool active, bool markSceneDirty)
        {
            if (!TagHelper.TagExists(tag)) return;

            var toggledGameObjectCount = 0;
            var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            var allGameObjectsLength = allGameObjects.Length;
            for (var i = 0; i < allGameObjectsLength; i++)
            {
                var gameObject = allGameObjects[i] as GameObject;

                if (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject)) continue;

                if (gameObject.CompareTag(tag))
                {
                    gameObject.SetActive(active);
                    toggledGameObjectCount++;
                }
            }

            var state = active ? "active" : "inactive";
            var plural = toggledGameObjectCount > 1 ? "s" : "";
            Debug.Log($"Set {toggledGameObjectCount} GameObject{plural} in Scene with tag {tag} to be {state}");
            if (markSceneDirty) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif