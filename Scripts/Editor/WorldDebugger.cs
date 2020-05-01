using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace VRCWorldToolkit
{
    public class WorldDebugger : EditorWindow
    {
        static Texture _BadFPS;
        static Texture _GoodFPS;
        static Texture _Tips;
        static Texture _Info;
        static Texture _Error;
        static Texture _Warning;

        static bool recheck = true;

        int tabNumber = 0;
        string[] tabLabel = { "All", "General", "Optimization", "Lighting", "Post Processing" };

        public enum MessageType
        {
            BadFPS = 0,
            GoodFPS = 1,
            Tips = 2,
            Error = 3,
            Warning = 4,
            Info = 5
        }

        static Texture GetDebuggerIcon(MessageType info_type)
        {
            if (!_BadFPS)
                _BadFPS = Resources.Load<Texture>("DebuggerIcons/Bad_FPS_Icon");
            if (!_GoodFPS)
                _GoodFPS = Resources.Load<Texture>("DebuggerIcons/Good_FPS_Icon");
            if (!_Tips)
                _Tips = Resources.Load<Texture>("DebuggerIcons/Performance_Tips");
            if (!_Info)
                _Info = Resources.Load<Texture>("DebuggerIcons/Performance_Info");
            if (!_Error)
                _Error = Resources.Load<Texture>("DebuggerIcons/Error_Icon");
            if (!_Warning)
                _Warning = Resources.Load<Texture>("DebuggerIcons/Warning_Icon");

            switch (info_type)
            {
                case MessageType.BadFPS:
                    return _BadFPS;
                case MessageType.GoodFPS:
                    return _GoodFPS;
                case MessageType.Tips:
                    return _Tips;
                case MessageType.Info:
                    return _Info;
                case MessageType.Error:
                    return _Error;
                case MessageType.Warning:
                    return _Warning;
            }

            return _Info;
        }

        class DebuggerMessage
        {
            public string debuggerMessage;
            public MessageType messageType;
            public string variable;
            public string variable2;
            public GameObject[] selectObjects;
            public GameObject selectObject;
            public System.Action autoFix;
            public string assetPath;
            public bool textureCruncher;

            private string dynamicVariable = "%variable%";
            private string dynamicVariable2 = "%variable2%";

            public DebuggerMessage(string debuggerMessage, MessageType messageType)
            {
                this.debuggerMessage = debuggerMessage;
                this.messageType = messageType;
                this.variable = null;
                this.variable2 = null;
                this.selectObjects = null;
                this.selectObject = null;
                this.autoFix = null;
                this.textureCruncher = false;
            }

            public DebuggerMessage setVariable(string variable)
            {
                this.variable = variable;
                return this;
            }

            public DebuggerMessage setVariable2(string variable2)
            {
                this.variable2 = variable2;
                return this;
            }

            public DebuggerMessage setSelectObjects(GameObject[] selectObjects)
            {
                this.selectObjects = selectObjects;
                return this;
            }

            public DebuggerMessage setSelectObject(GameObject selectObject)
            {
                this.selectObject = selectObject;
                return this;
            }

            public DebuggerMessage setAutoFix(System.Action autoFix)
            {
                this.autoFix = autoFix;
                return this;
            }

            public DebuggerMessage setAssetLocation(string assetPath)
            {
                this.assetPath = assetPath;
                return this;
            }

            public DebuggerMessage isTextureCruncher()
            {
                this.textureCruncher = true;
                return this;
            }

            public void DrawMessage()
            {
                bool hasButtons = ((selectObjects != null) || (selectObject != null) || (autoFix != null));

                GUIStyle style = new GUIStyle("HelpBox");

                EditorGUILayout.BeginHorizontal();

                if (variable != null)
                {
                    debuggerMessage = debuggerMessage.Replace("%variable%", variable);
                }

                if (variable2 != null)
                {
                    debuggerMessage = debuggerMessage.Replace(dynamicVariable2, variable2);
                }

                var boxWidth = EditorGUIUtility.currentViewWidth - 107;
                var boxNoButtonsWidth = EditorGUIUtility.currentViewWidth - 18;
                var buttonWidth = 80;
                var buttonHeight = 20;

                if (hasButtons || textureCruncher)
                {
                    GUIContent Box = new GUIContent(debuggerMessage, GetDebuggerIcon(messageType));
                    GUILayout.Box(Box, style, GUILayout.MinHeight(42), GUILayout.MinWidth(boxWidth));
                    EditorGUILayout.BeginVertical();

                    //if (textureCruncher)
                    //{
                    //    if (GUILayout.Button("Show", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    //    {
                    //        TextureCrunchUtility.TextureCrunchUtilityWindow();
                    //    }
                    //}
                    if (assetPath != null)
                    {
                        if (GUILayout.Button("Select Asset", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                        {
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
                        }
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(selectObjects == null && selectObject == null);
                        if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                        {
                            if (selectObjects != null)
                                Selection.objects = selectObjects;

                            else if (selectObject != null)
                                Selection.activeGameObject = selectObject;
                        }
                        EditorGUI.EndDisabledGroup();
                    }


                    EditorGUI.BeginDisabledGroup(autoFix == null);
                    if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        autoFix();
                        recheck = true;
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndVertical();
                }
                else
                {
                    GUIContent Box = new GUIContent(debuggerMessage, GetDebuggerIcon(messageType));
                    GUILayout.Box(Box, style, GUILayout.MinHeight(42), GUILayout.MaxWidth(boxNoButtonsWidth));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        DebuggerMessages generalMessages = new DebuggerMessages();
        DebuggerMessages optimizationMessages = new DebuggerMessages();
        DebuggerMessages lightingMessages = new DebuggerMessages();
        DebuggerMessages postprocessingMessages = new DebuggerMessages();

        class DebuggerMessages
        {
            public List<DebuggerMessage> messagesList = new List<DebuggerMessage>();

            public void AddMessage(DebuggerMessage debuggerMessage)
            {
                messagesList.Add(debuggerMessage);
            }

            public void ClearMessages()
            {
                messagesList.Clear();
            }
        }

        Vector2 scrollPos;

        static string lastBuild = "Library/LastBuild.buildreport";

        static string buildReportDir = "Assets/_LastBuild/";

        static string assetPath = "Assets/_LastBuild/LastBuild.buildreport";

        static DateTime timeNow;

        private BuildReport buildReport;

        [MenuItem("VRWorld Toolkit/Open World Debugger", false, 0)]
        public static void ShowWindow()
        {
            if (!Directory.Exists(buildReportDir))
                Directory.CreateDirectory(buildReportDir);

            if (File.Exists(lastBuild))
            {
                File.Copy(lastBuild, assetPath, true);
                AssetDatabase.ImportAsset(assetPath);
            }

            var window = EditorWindow.GetWindow(typeof(WorldDebugger));
            window.titleContent = new GUIContent("World Debugger");
            window.minSize = new Vector2(530, 600);
            window.Show();
        }

        //System.Action AddPlayerMods(GameObject obj)
        //{
        //    return () =>
        //    {
        //        obj.AddComponent(typeof(VRC_PlayerMods));
        //        VRCPlayerModFactory.PlayerModType type = VRCPlayerModFactory.PlayerModType.Jump;
        //        VRCPlayerMod mod = VRCPlayerModFactory.Create(type);
        //    };
        //}

        System.Action SelectAsset(GameObject obj)
        {
            return () =>
            {
                Selection.activeObject = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
            };
        }

        System.Action SetGenerateLightmapUV(ModelImporter importer)
        {
            return () =>
            {
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
            };
        }

        System.Action SetGenerateLightmapUVCombined(List<ModelImporter> models)
        {
            return () =>
            {
                foreach (var model in models)
                {
                    model.generateSecondaryUV = true;
                    model.SaveAndReimport();
                }
            };
        }

        System.Action DisableComponent(Behaviour behaviour)
        {
            return () =>
            {
                behaviour.enabled = false;
            };
        }

        System.Action SetObjectLayer(string layer, GameObject obj)
        {
            return () =>
            {
                obj.layer = LayerMask.NameToLayer(layer);
            };
        }

        System.Action SetLightmapSize(int newSize)
        {
            return () =>
            {
                LightmapEditorSettings.maxAtlasSize = newSize;
            };
        }

        System.Action SetEnviromentReflections(DefaultReflectionMode reflections)
        {
            return () =>
            {
                RenderSettings.defaultReflectionMode = reflections;
            };
        }

        System.Action SetAmbientMode(AmbientMode ambientMode)
        {
            return () =>
            {
                RenderSettings.ambientMode = ambientMode;
            };
        }

        System.Action SetGameObjectTag(GameObject obj, string tag)
        {
            return () =>
            {
                obj.tag = tag;
            };
        }

#if UNITY_POST_PROCESSING_STACK_V2
        System.Action SetReferenceCamera(VRC_SceneDescriptor descriptor)
        {
            return () =>
            {
                if (Camera.main == null)
                {
                    GameObject camera = new GameObject("Main Camera");
                    camera.AddComponent<Camera>();
                    camera.AddComponent<AudioListener>();
                    camera.tag = "MainCamera";
                }
                descriptor.ReferenceCamera = Camera.main.gameObject;
                if (!Camera.main.gameObject.GetComponent<PostProcessLayer>())
                    Camera.main.gameObject.AddComponent(typeof(PostProcessLayer));
            };
        }

        System.Action AddDefaultVolume()
        {
            return () =>
            {
                PostProcessVolume volume = GameObject.Instantiate(PostProcessManager.instance.QuickVolume(16, 100f));
                if (!Directory.Exists("Assets/Post Processing"))
                    Directory.CreateDirectory("Assets/Post Processing");
                if (AssetDatabase.LoadAssetAtPath("Assets/Post Processing/SilentProfile.asset", typeof(PostProcessProfile)) == null)
                {
                    AssetDatabase.CreateFolder("Assets", "Post Processing");
                    AssetDatabase.CopyAsset("Assets/VRCWorldToolkit/Resources/PostProcessing/SilentProfile.asset", "Assets/Post Processing/SilentProfile.asset");
                }
                volume.sharedProfile = (PostProcessProfile)AssetDatabase.LoadAssetAtPath("Assets/Post Processing/SilentProfile.asset", typeof(PostProcessProfile));
                volume.gameObject.name = "Post Processing Volume";
                volume.gameObject.layer = LayerMask.NameToLayer("Water");
            };
        }

        public enum RemovePSEffect
        {
            AmbientOcclusion = 0,
            ScreenSpaceReflections = 1,
            BloomDirt = 2
        }

        System.Action RemovePostProcessSetting(PostProcessProfile postprocess_profile, RemovePSEffect effect)
        {
            return () =>
            {
                switch (effect)
                {
                    case RemovePSEffect.AmbientOcclusion:
                        postprocess_profile.RemoveSettings<AmbientOcclusion>();
                        break;
                    case RemovePSEffect.ScreenSpaceReflections:
                        postprocess_profile.RemoveSettings<ScreenSpaceReflections>();
                        break;
                    case RemovePSEffect.BloomDirt:
                        postprocess_profile.GetSetting<Bloom>().dirtTexture.value = null;
                        postprocess_profile.GetSetting<Bloom>().dirtIntensity.value = 0;
                        break;
                    default:
                        break;
                }
            };
        }
#endif

        private readonly string noSceneDescriptor = "Your world currently has no scene descriptor. Please add one yourself, or drag the VRCWorld prefab to your scene.";
        private readonly string tooManySceneDescriptors = "You have multiple scene descriptors in your world. You can only have one scene descriptor in a world.";
        private readonly string worldDescriptorFar = "Your scene descriptor is %variable% units far from the the zero point in Unity. Having your world center out this far will cause some noticable jittering on models. You should move your world closer to the zero point of your scene.";
        private readonly string worldDescriptorOff = "Your scene descriptor is %variable% units far from the the zero point in Unity. It's usually good practice to try to keep it as close as possible to the absolute zero point to avoid floating point errors.";
        private readonly string clippingPlane = "Consider lowering your cameras near clipping plane to accomodate people with smaller avatars. The value get's clamped between 0.01 to 0.05.";
        private readonly string colliderUnderSpawnIsTrigger = "The only collider (%variable%) under your spawn point %variable2% has been set as a trigger! Players spawning into this world will fall forever.";
        private readonly string noColliderUnderSpawn = "Your spawn point %variable% doesn't have anything underneath it. Players spawning into this world will fall forever.";
        private readonly string noPlayerMods = "Your world currently has no player mods. Player mods are used for adding jumping and changing walking speed.";
        private readonly string triggerTriggerNotTrigger = "You have an OnEnterTrigger or OnExitTrigger Trigger (%variable%), but it's collider has not been set to be a trigger. These Triggers need to have a collider set to be a trigger to work.";
        private readonly string colliderTriggerIsTrigger = "You have an OnEnterCollider or OnExitCollider Trigger (%variable%) that has a collider set to be a trigger. These only react if the collider on the object has not been set to be a trigger.";
        private readonly string triggerTriggerNoCollider = "You have an OnEnterTrigger or OnExitTrigger Trigger (%variable%) that doesn't have a trigger collider on it.";
        private readonly string triggerTriggerWrongLayer = "You have an OnEnterTrigger or OnExitTrigger Trigger (%variable%) that is not on the MirrorReflection layer. This can stop raycasts from working properly.";
        private readonly string mirrorOnByDefault = "Your mirror %variable% is on by default. This is a very bad practice and you should disable any mirrors in your world by default.";
        private readonly string bakedOcclusionCulling = "You currently have baked Occlusion Culling.";
        private readonly string noOcclusionCulling = "You haven't baked Occlusion Culling yet. Occlusion culling gives you a lot more performance in your world, especially in larger worlds that have multiple rooms/areas.";
        private readonly string activeCamerasOutputtingToRenderTextures = "Your scene has active cameras outputting to render textures, which will render constantly ingame. This will affect performance negatively. A good practice is to have them be disabled by default, and only enabled when needed.";
        private readonly string noToonShaders = "You shouldn't use toon shaders for world building, as they're missing crucial things for making worlds. For world building the most recommended shader is Standard.";
        private readonly string nonCrunchedTextures = "%variable%% of the textures used in your scene haven't been crunch compressed. Crunch compression can greatly reduce the size of your world's textures, allowing players to load in faster.";
        private readonly string switchToProgressive = "Your world is currently using Enlighten as your lightmapper, which is deprecated in newer versions of Unity. You should consider switching to Progressive.";
        private readonly string singleColorEnviromentLighting = "Consider changing your Enviroment Lighting to Gradient from Flat.";
        private readonly string darkEnviromentLighting = "Using dark colours for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if your world has dark lighting.";
        private readonly string customEnviromentReflectionsNull = "Your Enviroment Reflections have been set to custom, but you haven't defined a custom cubemap!";
        private readonly string noUV2Model = "You have a model (%variable%) set to be lightmapped that doesn't have Lightmap UVs. This causes issues when baking lighting. You can enable generating Lightmap UV's in the import settings of the model.";
        private readonly string noUV2ModelCombined = "You have %variable% models set to be lightmapped that don't have Lightmap UVs. This causes issues when baking lighting. You can enable generating Lightmap UV's in the import settings of the models.";
        private readonly string lightsNotBaked = "Your world's lighting is currently not baked. Consider baking your lights for improved performance.";
        private readonly string considerLargerLightmaps = "Consider increasing your Lightmap Size from %variable% to 4096. This allows for more stuff to fit on a single lightmap, leaving less textures that need to be sampled.";
        private readonly string nonBakedBakedLights = "Your world contains baked/mixed lights that haven't been baked! Baked lights that haven't been baked yet function as realtime lights ingame.";
        private readonly string lightingDataAssetInfo = "Your lighting data asset takes up %variable% MB of your world's size. This contains your world's light probe data and realtime GI data.";
        private readonly string noLightProbes = "Your world currently has no light probes, which means your baked lights won't affect dynamic objects.";
        private readonly string lightProbeCountNotBaked = "Your world currently contains %variable% light probes, but %variable2% of them haven't been baked yet.";
        private readonly string lightProbesRemovedNotReBaked = "You've removed some lightprobes after the last bake, bake them again to update your scenes lighting data. Currently the lighting data contains %variable% baked lightprobes and the scene has %variable2% lightprobes.";
        private readonly string lightProbeCount = "Your world currently contains %variable% light probes.";
        private readonly string noReflectionProbes = "Your world has no active reflection probes. Reflection probes are needed to have proper reflections on reflective materials.";
        private readonly string reflectionProbesSomeUnbaked = "Your world has %variable% reflection probes. But some of them (%variable2%) are unbaked.";
        private readonly string reflectionProbeCountText = "Your world has %variable% reflection probes.";
        private readonly string postProcessingImportedButNotSetup = "Your project has Post Processing imported, but you haven't set it up yet.";
        private readonly string noReferenceCameraSet = "Your Scene Descriptor has no Reference Camera set. Without a Reference Camera set, you won't be able to see Post Processing ingame.";
        private readonly string noPostProcessingVolumes = "You don't have any Post Processing Volumes in your scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";
        private readonly string referenceCameraNoPostProcessingLayer = "Your Reference Camera doesn't have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";
        private readonly string volumeBlendingLayerNotSet = "You don't have a Volume Blending Layer set in your Post Process Layer, so post processing won't work. Using the Water layer is recommended.";
        private readonly string postProcessingVolumeNotGlobalNoCollider = "The Post Processing Volume \"%variable%\" isn't marked as Global and doesn't have a collider. It won't affect the camera without one of these set on it.";
        private readonly string noProfileSet = "You don't have a profile set in the Post Processing Volume %variable%";
        private readonly string dontUseNoneForTonemapping = "Use either Neutral or ACES for Color Grading tonemapping, using None is the same as not using Color Grading.";
        private readonly string tooHighBloomIntensity = "Don't raise the Bloom intensity too high! You should use a low Bloom intensity, between 0.01 to 0.3.";
        private readonly string tooHighBloomThreshold = "You should avoid having your Bloom threshold set high. It might cause unexpected problems with avatars. Ideally you should keep it at 0, but always below 1.0.";
        private readonly string noBloomDirtInVR = "Don't use Bloom Dirt, it looks really bad in VR!";
        private readonly string noAmbientOcclusion = "Don't use Ambient Occlusion in VRChat! It has a super high rendering cost.";
        private readonly string depthOfFieldWarning = "Depth of field has a high performance cost, and is very disorientating in VR. If you really want to use depth of field, have it be disabled by default.";
        private readonly string screenSpaceReflectionsWarning = "Screen Space Reflections only works when using deferred rendering. VRchat isn't using deferred rendering, so this will have no effect on the main camera.";
        private readonly string vignetteWarning = "Only use vignette in very small amounts. A powerful vignette can cause sickness in VR.";
        private readonly string noProblemsInPostProcessing = "No problems detected in your post processing setup.";
        private readonly string noPostProcessingImported = "You haven't imported Post Processing to your project yet.";
        private readonly string questBakedLightingWarning = "You should bake lights for content build for Quest.";
        private readonly string ambientModeSetToCustom = "Your Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";
        private readonly string bakeryLightNotSetEditorOnly = "Your Bakery light named %variable% is not set to be EditorOnly this can cause errors loading into a world in VRChat because external scripts get removed in the upload process.";
        private readonly string bakeryLightUnityLight = "Your Bakery light named %variable% has a Unity Light component on it these will not get baked with Bakery and will keep acting as real time even if set to baked.";

        public void CheckScene()
        {
            //General Checks

            //Get Descriptors
            VRC_SceneDescriptor[] descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            long descriptorCount = descriptors.Length;
            VRC_SceneDescriptor sceneDescriptor;

            //Check if a descriptor exists
            if (descriptorCount == 0)
            {
                generalMessages.AddMessage(new DebuggerMessage(noSceneDescriptor, MessageType.Error));
                return;
            }
            else
            {
                sceneDescriptor = descriptors[0];

                //Make sure only one descriptor exists
                if (descriptorCount != 1)
                {
                    generalMessages.AddMessage(new DebuggerMessage(tooManySceneDescriptors, MessageType.Info).setSelectObjects(FindObjectsOfType(typeof(VRC_SceneDescriptor)) as GameObject[]));
                    return;
                }


                //Check how far the descriptor is from zero point for floating point errors
                float descriptorRemoteness = Vector3.Distance(sceneDescriptor.transform.position, new Vector3(0.0f, 0.0f, 0.0f));

                if (descriptorRemoteness > 1000)
                {
                    generalMessages.AddMessage(new DebuggerMessage(worldDescriptorFar, MessageType.Error).setVariable(descriptorRemoteness.ToString()));
                }
                else if (descriptorRemoteness > 250)
                {
                    generalMessages.AddMessage(new DebuggerMessage(worldDescriptorOff, MessageType.Warning).setVariable(descriptorRemoteness.ToString()));
                }

                //If there's a reference camera defined check if the near clipping plane has been changed
                if (sceneDescriptor.ReferenceCamera)
                {
                    if (sceneDescriptor.ReferenceCamera.GetComponent<Camera>().nearClipPlane > 0.05f)
                    {
                        generalMessages.AddMessage(new DebuggerMessage(clippingPlane, MessageType.Tips));
                    }
                }
            }



            //Get spawn points to check whether there's a collider under them or not
            GameObject[] spawns = new GameObject[sceneDescriptor.spawns.Length];

            foreach (var item in sceneDescriptor.spawns)
            {
                RaycastHit hit;
                if (!Physics.Raycast(item.position + new Vector3(0, 0.01f, 0), Vector3.down, out hit, Mathf.Infinity, 0, QueryTriggerInteraction.Ignore))
                {
                    if (Physics.Raycast(item.position + new Vector3(0, 0.01f, 0), Vector3.down, out hit, Mathf.Infinity))
                    {
                        if (hit.collider.isTrigger)
                        {
                            generalMessages.AddMessage(new DebuggerMessage(colliderUnderSpawnIsTrigger, MessageType.Error).setSelectObject(item.gameObject).setVariable(hit.collider.name).setVariable2(item.gameObject.name));
                        }
                    }
                    else
                        generalMessages.AddMessage(new DebuggerMessage(noColliderUnderSpawn, MessageType.Error).setSelectObject(item.gameObject).setVariable(item.gameObject.name));
                    continue;
                }
            }

#if !UDON
            //Check if the world has playermods defined
            VRC_PlayerMods[] playermods = FindObjectsOfType(typeof(VRC_PlayerMods)) as VRC_PlayerMods[];
            if (playermods.Length == 0)
            {
                generalMessages.AddMessage(new DebuggerMessage(noPlayerMods, MessageType.Tips));
            }

            //Get triggers in the world
            VRC_Trigger[] trigger_scripts = (VRC_Trigger[])VRC_Trigger.FindObjectsOfType(typeof(VRC_Trigger));

            //Check for OnEnterTriggers to make sure they are on mirrorreflection layer
            foreach (var trigger_script in trigger_scripts)
            {
                foreach (var trigger in trigger_script.Triggers)
                {
                    if (trigger.TriggerType.ToString() == "OnEnterTrigger" || trigger.TriggerType.ToString() == "OnExitTrigger" || trigger.TriggerType.ToString() == "OnEnterCollider" || trigger.TriggerType.ToString() == "OnExitCollider")
                    {
                        if (trigger_script.gameObject.GetComponent<Collider>())
                        {
                            var collider = trigger_script.gameObject.GetComponent<Collider>();
                            if ((trigger.TriggerType.ToString() == "OnExitTrigger" || trigger.TriggerType.ToString() == "OnEnterTrigger") && !collider.isTrigger)
                            {
                                generalMessages.AddMessage(new DebuggerMessage(triggerTriggerNotTrigger, MessageType.Error).setSelectObject(trigger_script.gameObject).setVariable(trigger_script.name));
                            }
                            else if ((trigger.TriggerType.ToString() == "OnExitCollider" || trigger.TriggerType.ToString() == "OnEnterCollider") && collider.isTrigger)
                            {
                                generalMessages.AddMessage(new DebuggerMessage(colliderTriggerIsTrigger, MessageType.Error).setSelectObject(trigger_script.gameObject).setVariable(trigger_script.name));
                            }
                        }
                        else
                        {
                            generalMessages.AddMessage(new DebuggerMessage(triggerTriggerNoCollider, MessageType.Error).setSelectObject(trigger_script.gameObject).setVariable(trigger_script.name));
                        }
                        if ((trigger.TriggerType.ToString() == "OnEnterTrigger" || trigger.TriggerType.ToString() == "OnExitTrigger") && trigger_script.gameObject.layer != LayerMask.NameToLayer("MirrorReflection"))
                        {
                            generalMessages.AddMessage(new DebuggerMessage(triggerTriggerWrongLayer, MessageType.Warning).setVariable(trigger_script.name).setSelectObject(trigger_script.gameObject).setAutoFix(SetObjectLayer("MirrorReflection", trigger_script.gameObject)));
                        }
                    }
                }
            }
#endif

            //Optimization Checks

            //Get active mirrors in the world and complain about them
            VRC_MirrorReflection[] mirrors = FindObjectsOfType(typeof(VRC_MirrorReflection)) as VRC_MirrorReflection[];
            if (mirrors.Length > 0)
            {
                foreach (var mirror in mirrors)
                {
                    optimizationMessages.AddMessage(new DebuggerMessage(mirrorOnByDefault, MessageType.BadFPS).setVariable(mirror.name).setSelectObject(mirror.gameObject));
                }
            }

            //Check for occlusion culling
            if (StaticOcclusionCulling.umbraDataSize > 0)
            {
                optimizationMessages.AddMessage(new DebuggerMessage(bakedOcclusionCulling, MessageType.GoodFPS));
            }
            else
            {
                optimizationMessages.AddMessage(new DebuggerMessage(noOcclusionCulling, MessageType.Tips));
            }

            //Check if there's any active cameras outputting to render textures
            List<GameObject> activeCameras = new List<GameObject>();
            int cameraCount = 0;
            Camera[] cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera.targetTexture)
                {
                    cameraCount++;
                    activeCameras.Add(camera.gameObject);
                }
            }
            if (cameraCount > 0)
            {
                optimizationMessages.AddMessage(new DebuggerMessage(activeCamerasOutputtingToRenderTextures, MessageType.Tips).setSelectObjects(activeCameras.ToArray()));
            }

            List<Texture> unCrunchedTextures = new List<Texture>();
            int badShaders = 0;
            int textureCount = 0;
            foreach (var item in sceneDescriptor.DynamicMaterials)
            {
                Shader shader = item.shader;

                //Check for toon shaders used in the world
                if (shader.name.StartsWith(".poiyomi") || shader.name.StartsWith("poiyomi") || shader.name.StartsWith("arktoon") || shader.name.StartsWith("Cubedparadox") || shader.name.StartsWith("Silent's Cel Shading") || shader.name.StartsWith("Xiexe"))
                    badShaders++;

                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture texture = item.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                        if (AssetDatabase.GetAssetPath(texture) != "")
                        {
                            TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                            if (textureImporter != null)
                            {
                                if (!unCrunchedTextures.Contains(texture))
                                {
                                    textureCount++;
                                }
                                if (!textureImporter.crunchedCompression && !unCrunchedTextures.Contains(texture) && !textureImporter.textureCompression.Equals(TextureImporterCompression.Uncompressed) && EditorTextureUtil.GetStorageMemorySize(texture) > 500000)
                                {
                                    unCrunchedTextures.Add(texture);
                                }
                            }
                        }
                    }
                }
            }

            //If more than 10% of shaders used in scene are toon shaders to leave room for people using them for avatar displays
            if (sceneDescriptor.DynamicMaterials.Count > 0)
                if ((badShaders / sceneDescriptor.DynamicMaterials.Count * 100) > 10)
                    optimizationMessages.AddMessage(new DebuggerMessage(noToonShaders, MessageType.Warning));

            //Suggest to crunch textures if there are any uncrunched textures found
            if (textureCount > 0)
                if ((unCrunchedTextures.ToArray().Length / textureCount * 100) > 20)
                    optimizationMessages.AddMessage(new DebuggerMessage(nonCrunchedTextures, MessageType.Tips).setVariable((unCrunchedTextures.ToArray().Length / textureCount * 100).ToString()));

            //Lighting Checks

            if (RenderSettings.ambientMode.Equals(AmbientMode.Custom))
            {
                lightingMessages.AddMessage(new DebuggerMessage(ambientModeSetToCustom, MessageType.Error).setAutoFix(SetAmbientMode(AmbientMode.Skybox)));
            }

            if (RenderSettings.ambientMode.Equals(AmbientMode.Flat))
            {
                lightingMessages.AddMessage(new DebuggerMessage(singleColorEnviromentLighting, MessageType.Info));
            }

            if ((Helper.GetBrightness(RenderSettings.ambientLight) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Flat)) || (Helper.GetBrightness(RenderSettings.ambientSkyColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientEquatorColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientGroundColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)))
            {
                lightingMessages.AddMessage(new DebuggerMessage(darkEnviromentLighting, MessageType.Info));
            }

            if (RenderSettings.defaultReflectionMode.Equals(DefaultReflectionMode.Custom) && !RenderSettings.customReflection)
            {
                lightingMessages.AddMessage(new DebuggerMessage(customEnviromentReflectionsNull, MessageType.Error).setAutoFix(SetEnviromentReflections(DefaultReflectionMode.Skybox)));
            }

            bool bakedLighting = false;

#if BAKERY_INCLUDED
            BakeryDirectLight[] bakeryDirectLights = FindObjectsOfType(typeof(BakeryDirectLight)) as BakeryDirectLight[];
            BakeryPointLight[] bakeryPointLights = FindObjectsOfType(typeof(BakeryPointLight)) as BakeryPointLight[];
            BakerySkyLight[] bakerySkyLights = FindObjectsOfType(typeof(BakerySkyLight)) as BakerySkyLight[];
            foreach (var item in bakeryDirectLights)
            {
                CheckBakeryLight(item.gameObject);
                bakedLighting = true;
            }
            foreach (var item in bakeryPointLights)
            {
                CheckBakeryLight(item.gameObject);
                bakedLighting = true;
            }
            foreach (var item in bakerySkyLights)
            {
                CheckBakeryLight(item.gameObject);
                bakedLighting = true;
            }

            void CheckBakeryLight(GameObject obj)
            {
                if (obj.tag != "EditorOnly")
                {
                    lightingMessages.AddMessage(new DebuggerMessage(bakeryLightNotSetEditorOnly, MessageType.Warning).setVariable(obj.name).setAutoFix(SetGameObjectTag(obj, "EditorOnly")).setSelectObject(obj));
                }

                if (obj.GetComponent<Light>())
                {
                    Light light = obj.GetComponent<Light>();
                    if (!light.bakingOutput.isBaked && light.enabled)
                    {
                        lightingMessages.AddMessage(new DebuggerMessage(bakeryLightUnityLight, MessageType.Warning).setVariable(light.name).setAutoFix(DisableComponent(light)).setSelectObject(light.gameObject));
                    }
                }
            }
#endif

            //Get lights in scene
            Light[] lights = FindObjectsOfType<Light>();

            List<GameObject> nonBakedLights = new List<GameObject>();

            //Go trough the lights to check if the scene contains lights set to be baked
            foreach (var light in lights)
            {
                if (light.lightmapBakeType == LightmapBakeType.Baked || light.lightmapBakeType == LightmapBakeType.Mixed)
                {
                    bakedLighting = true;
                    if (!light.bakingOutput.isBaked)
                    {
                        nonBakedLights.Add(light.gameObject);
                    }
                }
            }

            if (LightmapSettings.lightmaps.Length > 0)
            {
                bakedLighting = true;
            }

            var probes = LightmapSettings.lightProbes;

            //If the scene has baked lights complain about stuff important to baked lighting missing
            if (bakedLighting)
            {
                //Check whether if models in scene have UV2 for lightmapping 
                MeshFilter[] filters = FindObjectsOfType<MeshFilter>();
                List<ModelImporter> importers = new List<ModelImporter>();
                List<string> meshName = new List<string>();
                foreach (var filter in filters)
                {
                    if (GameObjectUtility.AreStaticEditorFlagsSet(filter.gameObject, StaticEditorFlags.LightmapStatic))
                    {
                        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(filter.sharedMesh)) != null)
                        {
                            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(filter.sharedMesh)) as ModelImporter;
                            if (!importers.Contains(modelImporter))
                            {
                                if (!modelImporter.generateSecondaryUV && filter.sharedMesh.uv2.Length == 0)
                                {
                                    importers.Add(modelImporter);
                                    meshName.Add(filter.sharedMesh.name);
                                }
                            }
                        }
                    }
                }

                var modelsCount = importers.ToArray().Length;
                if (modelsCount > 10)
                {
                    lightingMessages.AddMessage(new DebuggerMessage(noUV2ModelCombined, MessageType.Warning).setVariable(modelsCount.ToString()).setAutoFix(SetGenerateLightmapUVCombined(importers)));
                }
                else if (modelsCount > 0)
                {
                    for (int i = 0; i < modelsCount; i++)
                    {
                        string modelName = meshName[i];
                        ModelImporter modelImporter = importers[i];
                        lightingMessages.AddMessage(new DebuggerMessage(noUV2Model, MessageType.Warning).setVariable(modelName).setAutoFix(SetGenerateLightmapUV(modelImporter)).setAssetLocation(modelImporter.assetPath));
                    }
                }

                //Count lightmaps and suggest to use bigger lightmaps if needed
                int lightMapSize = 0;
                lightMapSize = LightmapEditorSettings.maxAtlasSize;
                if (lightMapSize != 4096 && LightmapSettings.lightmaps.Length > 1)
                {
                    if (LightmapSettings.lightmaps[0] != null)
                    {
                        if (LightmapSettings.lightmaps[0].lightmapColor.height != 4096)
                        {
                            lightingMessages.AddMessage(new DebuggerMessage(considerLargerLightmaps, MessageType.Tips).setVariable(lightMapSize.ToString()).setAutoFix(SetLightmapSize(4096)));
                        }
                    }
                }

                //Count how many light probes the scene has
                long probeCounter = 0;
                long bakedProbes = 0;
                if (probes != null)
                {
                    bakedProbes = probes.count;
                }
                LightProbeGroup[] lightprobegroups = GameObject.FindObjectsOfType<LightProbeGroup>();
                foreach (LightProbeGroup lightprobegroup in lightprobegroups)
                {
                    if (lightprobegroup.GetComponent<LightProbeGroup>() != null)
                        probeCounter += lightprobegroup.probePositions.Length;
                }
                if (probeCounter > 0)
                {
                    if ((probeCounter - bakedProbes) < 0)
                    {
                        lightingMessages.AddMessage(new DebuggerMessage(lightProbesRemovedNotReBaked, MessageType.Warning).setVariable(bakedProbes.ToString()).setVariable2(probeCounter.ToString()));
                    }
                    else
                    {
                        if ((bakedProbes - (0.9 * probeCounter)) < 0)
                        {
                            lightingMessages.AddMessage(new DebuggerMessage(lightProbeCountNotBaked, MessageType.Error).setVariable(probeCounter.ToString("n0")).setVariable2((probeCounter - bakedProbes).ToString("n0")));
                        }
                        else
                        {
                            lightingMessages.AddMessage(new DebuggerMessage(lightProbeCount, MessageType.Info).setVariable(probeCounter.ToString("n0")));
                        }
                    }
                }

                //Since the scene has baked lights complain if there's no lightprobes
                if (probes == null && probeCounter == 0)
                {
                    Debug.Log(probeCounter);
                    lightingMessages.AddMessage(new DebuggerMessage(noLightProbes, MessageType.Info));
                }

                //Check lighting data asset size if it exists
                long length = 0;
                if (Lightmapping.lightingDataAsset != null)
                {
                    string lmdName = Lightmapping.lightingDataAsset.name;
                    string pathTo = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    length = new System.IO.FileInfo(pathTo).Length;
                    lightingMessages.AddMessage(new DebuggerMessage(lightingDataAssetInfo, MessageType.Info).setVariable((length / 1024.0f / 1024.0f).ToString("F2")));
                }

#if !BAKERY_INCLUDED
                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.Enlighten))
                {
                    lightingMessages.AddMessage(new DebuggerMessage(switchToProgressive, MessageType.Tips));
                }
#endif

                if (nonBakedLights.Count != 0)
                    lightingMessages.AddMessage(new DebuggerMessage(nonBakedBakedLights, MessageType.BadFPS).setSelectObjects(nonBakedLights.ToArray()));
            }
            else
            {
#if UNITY_ANDROID
                lightingMessages.AddMessage(new DebuggerMessage(questBakedLightingWarning, MessageType.BadFPS));
#else
                lightingMessages.AddMessage(new DebuggerMessage(lightsNotBaked, MessageType.Tips).setSelectObjects(nonBakedLights.ToArray()));
#endif
            }

            //ReflectionProbes
            ReflectionProbe[] reflectionprobes = GameObject.FindObjectsOfType<ReflectionProbe>();
            List<GameObject> unbakedprobes = new List<GameObject>();
            int reflectionProbeCount = 0;
            int reflectionProbesUnbaked = 0;
            foreach (ReflectionProbe reflectionprobe in reflectionprobes)
            {
                reflectionProbeCount++;
                if (!reflectionprobe.texture)
                {
                    reflectionProbesUnbaked++;
                    unbakedprobes.Add(reflectionprobe.gameObject);
                }
            }

            if (reflectionProbeCount == 0)
            {
                lightingMessages.AddMessage(new DebuggerMessage(noReflectionProbes, MessageType.Tips));
            }
            else if (reflectionProbesUnbaked > 0)
            {
                lightingMessages.AddMessage(new DebuggerMessage(reflectionProbesSomeUnbaked, MessageType.Warning).setVariable(reflectionProbeCount.ToString()).setVariable2(reflectionProbesUnbaked.ToString()).setSelectObjects(unbakedprobes.ToArray()));
            }
            else
            {
                lightingMessages.AddMessage(new DebuggerMessage(reflectionProbeCountText, MessageType.Info).setVariable(reflectionProbeCount.ToString()));
            }

            //Post Processing Checks

#if UNITY_POST_PROCESSING_STACK_V2
            PostProcessVolume[] PostProcessVolumes = FindObjectsOfType(typeof(PostProcessVolume)) as PostProcessVolume[];
            if (!sceneDescriptor.ReferenceCamera && PostProcessVolumes.Length == 0 && !Camera.main.gameObject.GetComponent(typeof(PostProcessLayer)))
            {
                postprocessingMessages.AddMessage(new DebuggerMessage(postProcessingImportedButNotSetup, MessageType.Info));
            }
            else
            {
                //Start by checking if reference camera has been set in the Scene Descriptor
                if (!sceneDescriptor.ReferenceCamera)
                {
                    postprocessingMessages.AddMessage(new DebuggerMessage(noReferenceCameraSet, MessageType.Info).setAutoFix(SetReferenceCamera(sceneDescriptor)));
                }
                else
                {
                    //Check for post process volumes in the scene
                    if (PostProcessVolumes.Length == 0)
                    {
                        postprocessingMessages.AddMessage(new DebuggerMessage(noPostProcessingVolumes, MessageType.Info).setAutoFix(AddDefaultVolume()));
                    }
                    else
                    {
                        PostProcessLayer postprocess_layer = sceneDescriptor.ReferenceCamera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        if (postprocess_layer == null)
                        {
                            postprocessingMessages.AddMessage(new DebuggerMessage(referenceCameraNoPostProcessingLayer, MessageType.Error));
                        }

                        LayerMask volume_layer = postprocess_layer.volumeLayer;
                        if (volume_layer == LayerMask.GetMask("Nothing"))
                        {
                            postprocessingMessages.AddMessage(new DebuggerMessage(volumeBlendingLayerNotSet, MessageType.Error).setSelectObject(sceneDescriptor.ReferenceCamera.gameObject));
                        }
                        foreach (PostProcessVolume postprocess_volume in PostProcessVolumes)
                        {
                            //Check if the collider is either global or has a collider on it
                            if (!postprocess_volume.isGlobal && !postprocess_volume.GetComponent<Collider>())
                            {
                                GameObject[] objs = { postprocess_volume.gameObject };
                                postprocessingMessages.AddMessage(new DebuggerMessage(postProcessingVolumeNotGlobalNoCollider, MessageType.Error).setVariable(postprocess_volume.name).setSelectObject(postprocess_volume.gameObject));
                            }

                            //Check if the volume has a profile set
                            if (!postprocess_volume.profile && !postprocess_volume.sharedProfile)
                            {
                                postprocessingMessages.AddMessage(new DebuggerMessage(noProfileSet + postprocess_volume.gameObject.name, MessageType.Error).setVariable(postprocess_volume.gameObject.name));
                            }
                            else
                            {
                                //Go trough the profile settings and see if any bad one's are used
                                PostProcessProfile postprocess_profile;

                                if (postprocess_volume.profile)
                                    postprocess_profile = postprocess_volume.profile as PostProcessProfile;
                                else
                                    postprocess_profile = postprocess_volume.sharedProfile as PostProcessProfile;

                                if (postprocess_profile.GetSetting<ColorGrading>())
                                {
                                    if (postprocess_profile.GetSetting<ColorGrading>().tonemapper.value.ToString() == "None")
                                    {
                                        postprocessingMessages.AddMessage(new DebuggerMessage(dontUseNoneForTonemapping, MessageType.Error));
                                    }
                                }

                                if (postprocess_profile.GetSetting<Bloom>())
                                {
                                    if (postprocess_profile.GetSetting<Bloom>().intensity.value > 0.3f)
                                        postprocessingMessages.AddMessage(new DebuggerMessage(tooHighBloomIntensity, MessageType.Warning));

                                    if (postprocess_profile.GetSetting<Bloom>().threshold.value > 1f)
                                        postprocessingMessages.AddMessage(new DebuggerMessage(tooHighBloomThreshold, MessageType.Warning));

                                    if (postprocess_profile.GetSetting<Bloom>().dirtTexture.value || postprocess_profile.GetSetting<Bloom>().dirtIntensity.value != 0)
                                        postprocessingMessages.AddMessage(new DebuggerMessage(noBloomDirtInVR, MessageType.Error).setAutoFix(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.BloomDirt)));
                                }
                                if (postprocess_profile.GetSetting<AmbientOcclusion>())
                                    postprocessingMessages.AddMessage(new DebuggerMessage(noAmbientOcclusion, MessageType.Error).setAutoFix(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.AmbientOcclusion)));

                                if (postprocess_profile.GetSetting<DepthOfField>() && postprocess_volume.isGlobal)
                                    postprocessingMessages.AddMessage(new DebuggerMessage(depthOfFieldWarning, MessageType.Warning));

                                if (postprocess_profile.GetSetting<ScreenSpaceReflections>())
                                    postprocessingMessages.AddMessage(new DebuggerMessage(screenSpaceReflectionsWarning, MessageType.Warning).setAutoFix(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.ScreenSpaceReflections)));

                                if (postprocess_profile.GetSetting<Vignette>())
                                    postprocessingMessages.AddMessage(new DebuggerMessage(vignetteWarning, MessageType.Warning));
                            }
                        }
                    }
                }
            }
            if (postprocessingMessages.messagesList.ToArray().Length == 0)
                postprocessingMessages.AddMessage(new DebuggerMessage(noProblemsInPostProcessing, MessageType.Info));
#else
            postprocessingMessages.AddMessage(new DebuggerMessage(noPostProcessingImported, MessageType.Info));
#endif
        }

        string FormatTime(System.TimeSpan t)
        {
            return t.Days.ToString() + " days " + t.Hours.ToString() + " hours " + t.Minutes.ToString() + " minutes " + t.Seconds.ToString() + " seconds ago";
        }

        string FormatSize(ulong size)
        {
            if (size < 1024)
                return size + " B";
            if (size < 1024 * 1024)
                return (size / 1024.00).ToString("F2") + " KB";
            if (size < 1024 * 1024 * 1024)
                return (size / (1024.0 * 1024.0)).ToString("F2") + " MB";
            return (size / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
        }

        void refreshMessages()
        {
            generalMessages.ClearMessages();
            optimizationMessages.ClearMessages();
            lightingMessages.ClearMessages();
            postprocessingMessages.ClearMessages();
            CheckScene();
        }

        void Awake()
        {
            timeNow = DateTime.Now.ToUniversalTime();
            buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
        }

        void OnFocus()
        {
            timeNow = DateTime.Now.ToUniversalTime();
            recheck = true;
        }

        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;

            if (recheck)
            {
                refreshMessages();
                recheck = false;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (buildReport != null)
            {
                GUILayout.Label("<b>Last build size:</b> " + FormatSize(buildReport.summary.totalSize), style);

                GUILayout.Label("<b>Last build was done:</b> " + FormatTime(timeNow.Subtract(buildReport.summary.buildEndedAt)), style);

                GUILayout.Label("<b>Errors last build:</b> " + buildReport.summary.totalErrors.ToString(), style);

                GUILayout.Label("<b>Warnings last build:</b> " + buildReport.summary.totalWarnings.ToString(), style);
            }
            else
            {
                GUILayout.Label("No build done yet");
            }

            GUILayout.EndVertical();

            tabNumber = GUILayout.Toolbar(tabNumber, tabLabel);

            EditorGUILayout.BeginHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // General messages
            if (tabNumber == 0 || tabNumber == 1)
            {
                GUILayout.Label("General", EditorStyles.boldLabel);

                foreach (DebuggerMessage item in generalMessages.messagesList)
                {
                    item.DrawMessage();
                }
            }

            // Optimization messages
            if (tabNumber == 0 || tabNumber == 2)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Optimization", EditorStyles.boldLabel);

                foreach (DebuggerMessage item in optimizationMessages.messagesList)
                {
                    item.DrawMessage();
                }
            }

            // Lighting messages
            if (tabNumber == 0 || tabNumber == 3)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Lighting", EditorStyles.boldLabel);

                foreach (DebuggerMessage item in lightingMessages.messagesList)
                {
                    item.DrawMessage();
                }
            }

            // Post Processing messages
            if (tabNumber == 0 || tabNumber == 4)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Post Processing", EditorStyles.boldLabel);

                foreach (DebuggerMessage item in postprocessingMessages.messagesList)
                {
                    item.DrawMessage();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}