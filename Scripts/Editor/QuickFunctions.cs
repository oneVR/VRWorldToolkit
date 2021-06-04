#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.Core;
#endif
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRWorldToolkit
{
    public class QuickFunctions : EditorWindow
    {
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", false, 15)]
        public static void CopyWorldID()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager)
                {
                    EditorGUIUtility.systemCopyBuffer = pipelineManager.blueprintId;
                }
            }
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Copy World ID", true)]
        private static bool CopyWorldIDValidate()
        {
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];

            if (descriptors.Length is 1)
            {
                var pipelineManager = descriptors[0].GetComponent<PipelineManager>();

                if (pipelineManager)
                    return pipelineManager.blueprintId.Length > 0;
            }

            return false;
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", false, 16)]
        public static void SetupLayersCollisionMatrix()
        {
            if (!UpdateLayers.AreLayersSetup())
            {
                UpdateLayers.SetupEditorLayers();
            }

            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                UpdateLayers.SetupCollisionLayerMatrix();
            }
        }

        [MenuItem("VRWorld Toolkit/Quick Functions/Setup Layers and Collision Matrix", true)]
        private static bool SetupLayersCollisionMatrixValidate()
        {
            if (UpdateLayers.AreLayersSetup() && UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                return false;
            }

            return true;
        }
#endif
        [MenuItem("VRWorld Toolkit/Quick Functions/Remove Missing Scripts From Scene", false, 17)]
        private static void FindAndRemoveMissingScripts()
        {
            if (EditorUtility.DisplayDialog("Remove Missing Scripts", "Running this will go through all GameObjects in the open scene and remove any components with missing scripts. This action can't be reversed!\n\nAre you sure you want to continue?", "Continue", "Cancel"))
            {
                var overallRemovedCount = 0;
                var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                var allGameObjectsLength = allGameObjects.Length;
                for (var i = 0; i < allGameObjectsLength; i++)
                {
                    var gameObject = allGameObjects[i] as GameObject;

                    if (gameObject != null && (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject))) continue;

                    if (EditorUtility.DisplayCancelableProgressBar("Checking For Missing Scripts", gameObject.name, (float) i / allGameObjectsLength)) break;

                    var removedCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                    if (removedCount > 0)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
                        overallRemovedCount += removedCount;
                    }
                }

                EditorUtility.ClearProgressBar();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                var message = overallRemovedCount > 0 ? $"Removed total of {overallRemovedCount} components with missing scripts." : "No components with missing scripts were found.";
                EditorUtility.DisplayDialog("Remove Missing Scripts", message, "Ok");
            }
        }

        /// <summary>
        /// Sourced from: https://docs.vrchat.com/v2021.2.3/docs/vrchat-configuration-window
        /// </summary>
        [MenuItem("VRWorld Toolkit/Quick Functions/Add or Match VRChat Quality Settings", false, 18)]
        private static void AddOrMatchVRChatQualitySettings()
        {
            var selection = EditorUtility.DisplayDialogComplex("Match Quality Settings To VRChat",
                "Running this will replace or match existing Quality Settings with what VRChat uses.\n\nSelecting Replace will remove unrelated Quality Settings. Add or Match will add the Settings after the existing ones if there are no Quality Settings named the same as VRChat uses. VRHigh will be set as the Standalone default and Quest 2 as Android default.\n\nVRHigh, VRLow, DesktopLow, Quest 2, Quest 1",
                "Replace", "Add or Match", "Cancel");

            if (selection != 2)
            {
                var qualitySettings = AssetDatabase.LoadAssetAtPath<QualitySettings>("ProjectSettings/QualitySettings.asset");
                var serializedQualitySettings = new SerializedObject(qualitySettings);
                var qualitySettingsProperty = serializedQualitySettings.FindProperty("m_QualitySettings");
                var perPlatformDefaultQualityProperty = serializedQualitySettings.FindProperty("m_PerPlatformDefaultQuality");

                if (selection == 0)
                {
                    qualitySettingsProperty.ClearArray();
                }

                var exceptionHappened = SetQualitySettings(AddOrGetQualitySetting("VRHigh", qualitySettingsProperty), 8, 0, 2, 2, true, true, true, 1, false, 0, 2, 3, 1, 150, 2, 2, 0.3333333f, new Vector3(0.1f, 0.2f, 0.5f), 2, 0, 2, 0, 4096, 2, 4, true);

                exceptionHappened = exceptionHappened || SetQualitySettings(AddOrGetQualitySetting("VRLow", qualitySettingsProperty), 4, 0, 2, 2, false, true, true, 1, false, 0, 2, 2, 1, 75, 2, 1, 0.3333333f, new Vector3(0.1f, 0.2f, 0.5f), 2, 0, 1, 0, 1024, 2, 4, true);

                exceptionHappened = exceptionHappened || SetQualitySettings(AddOrGetQualitySetting("DesktopLow", qualitySettingsProperty), 4, 0, 2, 0, false, true, true, 1, false, 0, 2, 2, 1, 75, 2, 1, 0.3333333f, new Vector3(0.1f, 0.2f, 0.5f), 2, 0, 1, 0, 1024, 2, 4, true);

                exceptionHappened = exceptionHappened || SetQualitySettings(AddOrGetQualitySetting("Quest 2", qualitySettingsProperty), 2, 0, 1, 2, false, true, true, 1, false, 1, 2, 1, 1, 40, 3, 1, 0.3333333f, new Vector3(0.1f, 0.2f, 0.5f), 1, 0, 1, 0, 256, 2, 16, true);

                exceptionHappened = exceptionHappened || SetQualitySettings(AddOrGetQualitySetting("Quest 1", qualitySettingsProperty), 2, 0, 1, 1, false, true, true, 1, false, 1, 2, 1, 1, 40, 3, 1, 0.3333333f, new Vector3(0.1f, 0.2f, 0.5f), 1, 0, 1, 0, 256, 2, 16, true);

                if (exceptionHappened)
                {
                    EditorUtility.DisplayDialog("Failed Applying Quality Settings", "Something went wrong applying new Quality Settings check console for more details.", "Ok");
                }
                else
                {
                    for (var i = 0; i < perPlatformDefaultQualityProperty.arraySize; i++)
                    {
                        var serializedPropertyPlatform = perPlatformDefaultQualityProperty.GetArrayElementAtIndex(i);
                        var serializedPropertySetting = serializedPropertyPlatform.FindPropertyRelative("second");
                        switch (serializedPropertyPlatform.displayName)
                        {
                            case "Standalone":
                                serializedPropertySetting.intValue = GetQualitySettingIndex("VRHigh", qualitySettingsProperty);
                                break;
                            case "Android":
                                serializedPropertySetting.intValue = GetQualitySettingIndex("Quest 2", qualitySettingsProperty);
                                break;
                        }
                    }

                    serializedQualitySettings.ApplyModifiedPropertiesWithoutUndo();

                    QualitySettings.SetQualityLevel(GetQualitySettingIndex("VRHigh", qualitySettingsProperty), true);
                }
            }

            SerializedProperty AddOrGetQualitySetting(string name, SerializedProperty qualitySettingsProperty)
            {
                for (var i = 0; i < qualitySettingsProperty.arraySize; i++)
                {
                    var setting = qualitySettingsProperty.GetArrayElementAtIndex(i);
                    if (setting.FindPropertyRelative("name").stringValue == name)
                    {
                        return setting;
                    }
                }

                qualitySettingsProperty.arraySize++;
                var newSetting = qualitySettingsProperty.GetArrayElementAtIndex(qualitySettingsProperty.arraySize - 1);
                var nameProperty = newSetting.FindPropertyRelative("name");
                nameProperty.stringValue = name;
                return newSetting;
            }

            int GetQualitySettingIndex(string name, SerializedProperty qualitySettingsProperty)
            {
                for (var i = 0; i < qualitySettingsProperty.arraySize; i++)
                {
                    var setting = qualitySettingsProperty.GetArrayElementAtIndex(i);
                    if (setting.FindPropertyRelative("name").stringValue == name)
                    {
                        return i;
                    }
                }

                return 0;
            }

            bool SetQualitySettings(SerializedProperty qualitySetting,
                // Rendering
                int pixelLightCount, int textureQuality, int anisotropicTextures, int antiAliasing, bool softParticles, bool realtimeReflectionProbes, bool billboardsFaceCameraPosition, float resolutionScalingFixedDPIFactor, bool streamingMipmapsActive,
                // Shadows
                int shadowmaskMode, int shadows, int shadowResolution, int shadowProjection, float shadowDistance, int shadowNearPlaneOffset, int shadowCascades, float shadowCascade2Split, Vector3 shadowCascade4Split,
                // Other
                int blendWeights, int vSyncCount, float lodBias, int maximumLODLevel, int particleRaycastBudget, int asyncUploadTimeSlice, int asyncUploadBufferSize, bool asyncUploadPersistentBuffer)
            {
                try
                {
                    // Rendering
                    qualitySetting.FindPropertyRelative("pixelLightCount").intValue = pixelLightCount;
                    qualitySetting.FindPropertyRelative("textureQuality").enumValueIndex = textureQuality;
                    qualitySetting.FindPropertyRelative("anisotropicTextures").enumValueIndex = anisotropicTextures;
                    qualitySetting.FindPropertyRelative("antiAliasing").enumValueIndex = antiAliasing;
                    qualitySetting.FindPropertyRelative("softParticles").boolValue = softParticles;
                    qualitySetting.FindPropertyRelative("realtimeReflectionProbes").boolValue = realtimeReflectionProbes;
                    qualitySetting.FindPropertyRelative("billboardsFaceCameraPosition").boolValue = billboardsFaceCameraPosition;
                    qualitySetting.FindPropertyRelative("resolutionScalingFixedDPIFactor").floatValue = resolutionScalingFixedDPIFactor;
                    qualitySetting.FindPropertyRelative("streamingMipmapsActive").boolValue = streamingMipmapsActive;

                    // Shadows
                    qualitySetting.FindPropertyRelative("shadowmaskMode").enumValueIndex = shadowmaskMode;
                    qualitySetting.FindPropertyRelative("shadows").enumValueIndex = shadows;
                    qualitySetting.FindPropertyRelative("shadowResolution").enumValueIndex = shadowResolution;
                    qualitySetting.FindPropertyRelative("shadowProjection").enumValueIndex = shadowProjection;
                    qualitySetting.FindPropertyRelative("shadowDistance").floatValue = shadowDistance;
                    qualitySetting.FindPropertyRelative("shadowNearPlaneOffset").floatValue = shadowNearPlaneOffset;
                    qualitySetting.FindPropertyRelative("shadowCascades").enumValueIndex = shadowCascades;
                    qualitySetting.FindPropertyRelative("shadowCascade2Split").floatValue = shadowCascade2Split;
                    qualitySetting.FindPropertyRelative("shadowCascade4Split").vector3Value = shadowCascade4Split;

                    // Other
                    qualitySetting.FindPropertyRelative("blendWeights").enumValueIndex = blendWeights;
                    qualitySetting.FindPropertyRelative("vSyncCount").enumValueIndex = vSyncCount;
                    qualitySetting.FindPropertyRelative("lodBias").floatValue = lodBias;
                    qualitySetting.FindPropertyRelative("maximumLODLevel").intValue = maximumLODLevel;
                    qualitySetting.FindPropertyRelative("particleRaycastBudget").intValue = particleRaycastBudget;
                    qualitySetting.FindPropertyRelative("asyncUploadTimeSlice").intValue = asyncUploadTimeSlice;
                    qualitySetting.FindPropertyRelative("asyncUploadBufferSize").intValue = asyncUploadBufferSize;
                    qualitySetting.FindPropertyRelative("asyncUploadPersistentBuffer").boolValue = asyncUploadPersistentBuffer;

                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return true;
                }
            }
        }
    }
}