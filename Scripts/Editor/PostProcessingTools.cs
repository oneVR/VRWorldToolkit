using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class PostProcessingTools : MonoBehaviour
    {
        [MenuItem("VRWorld Toolkit/Post Processing/Import Post Processing", false, 1)]
        private static void PostProcessingInstall()
        {
            Helper.ImportPackage("com.unity.postprocessing@3.0.3");
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

        [MenuItem("VRWorld Toolkit/Post Processing/Setup Post Processing", false, 12)]
        private static void PostProcessingSetup()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            var sceneDescriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            var avatarDescriptors = FindObjectsOfType(typeof(VRC_AvatarDescriptor)) as VRC_AvatarDescriptor[];

            if (UpdateLayers.AreLayersSetup() || EditorUtility.DisplayDialog("Layers Missing!", "You haven't setup the project layers from the VRCSDK Builder tab.\r\n\r\nSelect Continue to set them up now.", "Continue", "Cancel"))
            {
                UpdateLayers.SetupEditorLayers();

                if (sceneDescriptors.Length == 0)
                {
                    if (avatarDescriptors.Length > 0)
                    {
                        SetupBasicPostProcessing();
                    }
                    else if (EditorUtility.DisplayDialog("Scene descriptor missing!",
                        "No scene descriptor or avatar descriptors was found. A scene descriptor must exist and contain a reference camera for post-processing to appear in-game.\r\n\r\nYou can add a scene descriptor by adding a VRCWorld prefab included with the SDK.\r\n\r\nSelect Cancel to return and add a scene descriptor so the setup can set the reference camera for you, or select Continue to ignore this warning.",
                        "Continue",
                        "Cancel"))
                    {
                        SetupBasicPostProcessing();
                    }
                }
                else if (sceneDescriptors.Length > 1)
                {
                    EditorUtility.DisplayDialog("Multiple scene descriptors!", "Multiple scene descriptors found, remove any you aren't using and run the setup again.", "OK");
                }
                else
                {
                    SetupWorldPostProcessing(sceneDescriptors);
                }
            }
#endif
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Setup Post Processing", true)]
        private static bool PostProcessingSetupValidation()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            return !(Helper.BuildPlatform() is RuntimePlatform.Android);
#else
            return false;
#endif
        }

        [MenuItem("VRWorld Toolkit/Post Processing/Post Processing Guide", false, 13)]
        private static void PostProcessingGuide()
        {
            Application.OpenURL("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Post-Processing");
        }

#if UNITY_POST_PROCESSING_STACK_V2
        private static void SetupBasicPostProcessing()
        {
            GameObject camera = null;

            if (Camera.main != null)
            {
                camera = Camera.main.gameObject;
            }
            else
            {
                if (EditorUtility.DisplayDialog("No main camera!", "No main camera found in the current scene. The main camera is needed to create the Post Processing Volume.\r\n\r\nSelect Continue to create a new one.", "Continue", "Cancel"))
                {
                    camera = Helper.CreateMainCamera();
                }
            }

            if (camera != null)
            {
                SetupPostProcessingGenerics(camera);
            }
        }

        private static void SetupWorldPostProcessing(VRC_SceneDescriptor[] descriptors)
        {
            if (EditorUtility.DisplayDialog("Setup Post Processing?", "This will setup your scenes Reference Camera and make a new global volume using the included example Post Processing Profile.", "OK", "Cancel"))
            {
                var referenceCamera = descriptors.Length > 0 && descriptors[0].ReferenceCamera;

                GameObject camera = null;

                if (!referenceCamera && Camera.main is null)
                {
                    if (EditorUtility.DisplayDialog("No main camera!", "No main camera found in the current scene. The main camera is needed to create the Post Processing Volume.\r\n\r\nSelect Continue to create a new one.", "Continue", "Cancel"))
                    {
                        camera = Helper.CreateMainCamera();

                        descriptors[0].ReferenceCamera = camera;
                    }
                }
                else if (referenceCamera)
                {
                    camera = descriptors[0].ReferenceCamera;
                }
                else if (Camera.main != null)
                {
                    camera = Camera.main.gameObject;
                }

                if (camera != null)
                {
                    descriptors[0].ReferenceCamera = camera;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(descriptors[0]);

                    SetupPostProcessingGenerics(camera);
                }
            }
        }

        public static void SetupPostProcessingGenerics(GameObject camera)
        {
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
                var path = AssetDatabase.GetAssetPath(Resources.Load("PostProcessing/SilentProfile"));

                if (path != null)
                {
                    AssetDatabase.CopyAsset(path, "Assets/Post Processing/SilentProfile.asset");
                }
            }

            var profileFound = false;

            //Set up the post process volume
            var volume = Instantiate(PostProcessManager.instance.QuickVolume(16, 100f));
            if (File.Exists("Assets/Post Processing/SilentProfile.asset"))
            {
                volume.sharedProfile = (PostProcessProfile) AssetDatabase.LoadAssetAtPath("Assets/Post Processing/SilentProfile.asset", typeof(PostProcessProfile));
                profileFound = true;
            }

            // Set volume name and layer
            volume.gameObject.name = "Post Processing Volume";
            volume.gameObject.layer = LayerMask.NameToLayer(layer);

            // Mark the scene as dirty for saving
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            // Set the created volume as active selection in hierarchy
            Selection.activeGameObject = volume.gameObject;

            // Notify the user if the default profile was not found during setup
            if (!profileFound)
                EditorUtility.DisplayDialog("Default profile not found!", "Default Post Processing Profile was not found during setup, so it was automatically not set in the Post Processing Volume.\n\nCreate your profile to finish the setup.", "Ok");
        }
#endif
    }
}
#endif