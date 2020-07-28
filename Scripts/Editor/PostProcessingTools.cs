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
                EditorUtility.DisplayDialog("Scene descriptor missing", "You haven't added a scene descriptor yet, you can add one by dragging in the VRCWorld prefab.", "OK");
            }
            else
            {
                SetupBasicPostProcessing(descriptors[0]);
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

        private static void SetupBasicPostProcessing(VRC_SceneDescriptor descriptor)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (!UpdateLayers.AreLayersSetup())
            {
                EditorUtility.DisplayDialog("Layers Missing", "Start by setting up your layers in the VRCSDK builder tab", "OK");
            }
            else
            {
                if (EditorUtility.DisplayDialog("Setup Post Processing?", "This will setup your scenes Reference Camera and make a new global volume using the included example Post Processing Profile", "OK", "Cancel"))
                {
                    //Check if reference camera exists
                    if (!descriptor.ReferenceCamera)
                    {
                        if (Camera.main == null)
                        {
                            GameObject camera = new GameObject("Main Camera");
                            camera.AddComponent<Camera>();
                            camera.AddComponent<AudioListener>();
                            camera.tag = "MainCamera";
                        }
                        descriptor.ReferenceCamera = Camera.main.gameObject;
                    }

                    //Use PostProcessing layer if it exists otherwise use Water
                    var layer = LayerMask.NameToLayer("PostProcessing") > -1 ? "PostProcessing" : "Water";

                    //Make sure the Post Process Layer exists and set it up
                    if (!descriptor.ReferenceCamera.gameObject.GetComponent<PostProcessLayer>())
                        descriptor.ReferenceCamera.gameObject.AddComponent(typeof(PostProcessLayer));
                    var postprocessLayer = descriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
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
            }
#endif
        }

        private static AddRequest _request;

        private static void ImportPostProcessing()
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