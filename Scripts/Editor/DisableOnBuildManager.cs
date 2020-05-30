using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDKBase;

namespace VRWorldToolkit
{
    public class DisableOnBuildManager : Editor
    {
        const string dummyName = "_DisableOnBuild";
        static GameObject _disableOnBuild;

        public static GameObject DestroyCube
        {
            get
            {
                if (!_disableOnBuild)
                    _disableOnBuild = GameObject.Find("/" + dummyName);

                return _disableOnBuild;
            }

            private set
            {
                _disableOnBuild = value;
            }
        }

        static GameObject GetDestroyCube(bool createIfMissing)
        {
            if (!_disableOnBuild && createIfMissing)
            {
                _disableOnBuild = new GameObject(dummyName);

                //Prevent it from showing in hierarchy
                _disableOnBuild.hideFlags = HideFlags.HideInHierarchy;

                _disableOnBuild.tag = "EditorOnly";
                var comp = _disableOnBuild.GetComponent<DisableOnBuild>();
                if (!comp)
                    _disableOnBuild.AddComponent<DisableOnBuild>();

                //Mark the scene as dirty for saving
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            return _disableOnBuild;
        }

        private static void CreateCube()
        {
            DestroyCube = GetDestroyCube(true);
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Setup", false, -101)]
        private static void DisableOnUploadSetup()
        {
            //Create the tag if it doesn't exist yet
            TagHelper.AddTag("DisableOnBuild");

            //Spawn the cube
            CreateCube();
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Setup", true)]
        private static bool DisableOnUploadSetupValidate()
        {
            return (DestroyCube == null);
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Remove", false, -100)]
        private static void DisableOnUploadRemove()
        {
            //Delete the cube
            DestroyImmediate(DestroyCube);

            //Mark the scene dirty for saving
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Remove", true)]
        private static bool DisableOnUploadRemoveValidate()
        {
            return (DestroyCube != null);
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Disable Objects", false, -10)]
        private static void DisableObjectsLoop()
        {
            //Loop trough the objects with the tag disabling them
            GameObject[] toDisableOnBuild = GameObject.FindGameObjectsWithTag("DisableOnBuild");
            foreach (GameObject disableThis in toDisableOnBuild)
            {
                disableThis.SetActive(false);
            }
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Disable Objects", true)]
        private static bool DisableObjectsValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Enable Objects", false, -11)]
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
        }

        [MenuItem("VRWorld Toolkit/Disable On Build/Enable Objects", true)]
        private static bool EnableObjectsLoopValidate()
        {
            return TagHelper.TagExists("DisableOnBuild");
        }
    }
}