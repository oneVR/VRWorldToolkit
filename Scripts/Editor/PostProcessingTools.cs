using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
using System.Reflection;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class PostProcessingTools : MonoBehaviour
    {
        [MenuItem("VRWorld Toolkit/Post Processing/Setup Post Processing", false, -102)]
        private static void PostProcessingSetup()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            if (descriptors.Length == 0)
            {
                if (EditorUtility.DisplayDialog("Scene descriptor missing!", "No scene descriptor was found. A scene descriptor must exist and contain a reference camera for post-processing to appear in-game.\r\n\r\nYou can add a scene descriptor by adding a VRCWorld prefab included with the SDK.\r\n\r\nSelect Cancel to return and add a scene descriptor so the setup can set the reference camera for you, or select Continue to ignore this warning.", "Continue", "Cancel"))
                {
                    SetupBasicPostProcessing(descriptors);
                }
            }
            else if (descriptors.Length > 1)
            {
                EditorUtility.DisplayDialog("Multiple scene descriptors!", "Multiple scene descriptors found, remove any you aren't using and run the setup again.", "OK");
            }
            else
            {
                SetupBasicPostProcessing(descriptors);
            }
#endif
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Setup Post Processing", true)]
        private static bool PostProcessingSetupValidation()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            return true;
#else
            return false;
#endif
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Import Post Processing", false, -101)]
        private static void PostProcessingInstall()
        {
            VRC_SceneDescriptor[] descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            ImportPostProcessing();
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Import Post Processing", true)]
        private static bool PostProcessingInstallValidation()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            return false;
#else
            return true;
#endif
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Post Processing Guide", false, -100)]
        private static void PostProcessingGuide()
        {
            Application.OpenURL("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Post-Processing");
        }

        private static void SetupBasicPostProcessing(VRC_SceneDescriptor[] descriptors)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (descriptors.Length > 0 && !UpdateLayers.AreLayersSetup())
            {
                if (EditorUtility.DisplayDialog("Layers Missing!", "You haven't setup the project layers from the VRCSDK Builder tab.\r\n\r\nSelect Continue to set them up now or Cancel if you want to set them up yourself from the Builder tab.", "Continue", "Cancel"))
                {
                    UpdateLayers.SetupEditorLayers();
                }
            }

            if (UpdateLayers.AreLayersSetup() && EditorUtility.DisplayDialog("Setup Post Processing?", "This will setup your scenes Reference Camera and make a new global volume using the included example Post Processing Profile.", "OK", "Cancel"))
            {
                bool referenceCamera = descriptors.Length > 0 && descriptors[0].ReferenceCamera;
                bool referenceCameraNeeded = descriptors.Length > 0 && !descriptors[0].ReferenceCamera;

                GameObject camera;

                if (!referenceCamera && Camera.main is null)
                {
                    camera = new GameObject("Main Camera");
                    camera.AddComponent<Camera>();
                    camera.AddComponent<AudioListener>();
                    camera.tag = "MainCamera";

                    if (referenceCameraNeeded)
                    {
                        descriptors[0].ReferenceCamera = camera;
                    }
                }
                else
                {
                    if (referenceCamera)
                    {
                        camera = descriptors[0].ReferenceCamera;
                    }
                    else
                    {
                        camera = Camera.main.gameObject;
                    }
                }

                //Use PostProcessing layer if it exists otherwise use Water
                var layer = LayerMask.NameToLayer("PostProcessing") > -1 ? "PostProcessing" : "Water";

                //Make sure the Post Process Layer exists and set it up
                if (!camera.GetComponent<PostProcessLayer>())
                    camera.AddComponent(typeof(PostProcessLayer));
                var postprocessLayer = camera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                postprocessLayer.volumeLayer = LayerMask.GetMask(layer);

                //Copy the example profile to the Post Processing folder
                if (!Directory.Exists("Assets/Post Processing"))
                    AssetDatabase.CreateFolder("Assets", "Post Processing");
                if (AssetDatabase.LoadAssetAtPath("Assets/Post Processing/SilentProfile.asset", typeof(PostProcessProfile)) == null)
                {
                    var path = AssetDatabase.GUIDToAssetPath("eaac6f7291834264f97854154e89bf76");
                    if (path != null)
                    {
                        AssetDatabase.CopyAsset(path, "Assets/Post Processing/SilentProfile.asset");
                    }
                }

                //Set up the post process volume
                var volume = GameObject.Instantiate(PostProcessManager.instance.QuickVolume(16, 100f));
                if (File.Exists("Assets/Post Processing/SilentProfile.asset"))
                    volume.sharedProfile = (PostProcessProfile)AssetDatabase.LoadAssetAtPath("Assets/Post Processing/SilentProfile.asset", typeof(PostProcessProfile));
                volume.gameObject.name = "Post Processing Volume";
                volume.gameObject.layer = LayerMask.NameToLayer(layer);

                //Mark the scene as dirty for saving
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
#endif
        }

        private static AddRequest _request;

        public static void ImportPostProcessing()
        {
            _request = Client.Add("com.unity.postprocessing");
            EditorApplication.update += PPImportProgress;
        }

        private static void PPImportProgress()
        {
            if (_request.IsCompleted)
            {
                if (_request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + _request.Result.packageId);
                else if (_request.Status >= StatusCode.Failure)
                    Debug.Log(_request.Error.message);

                EditorApplication.update -= PPImportProgress;
            }
        }
    }
}
#endif