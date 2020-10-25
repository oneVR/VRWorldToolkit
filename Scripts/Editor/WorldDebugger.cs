#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
using VRC.Core;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using UnityEditor.IMGUI.Controls;
using VRWorldToolkit.DataStructures;
using Microsoft.Win32;
using System.Reflection;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using System.Diagnostics;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class WorldDebugger : EditorWindow
    {
        private static Texture badFPS;
        private static Texture goodFPS;
        private static Texture tips;
        private static Texture info;
        private static Texture error;
        private static Texture warning;

        private static bool recheck = true;
        private static bool autoRecheck = true;

        private enum MessageType
        {
            BadFPS = 0,
            GoodFPS = 1,
            Tips = 2,
            Error = 3,
            Warning = 4,
            Info = 5
        }

        static Texture GetDebuggerIcon(MessageType infoType)
        {
            if (!badFPS)
                badFPS = Resources.Load<Texture>("DebuggerIcons/Bad_FPS_Icon");
            if (!goodFPS)
                goodFPS = Resources.Load<Texture>("DebuggerIcons/Good_FPS_Icon");
            if (!tips)
                tips = Resources.Load<Texture>("DebuggerIcons/Performance_Tips");
            if (!info)
                info = Resources.Load<Texture>("DebuggerIcons/Performance_Info");
            if (!error)
                error = Resources.Load<Texture>("DebuggerIcons/Error_Icon");
            if (!warning)
                warning = Resources.Load<Texture>("DebuggerIcons/Warning_Icon");

            switch (infoType)
            {
                case MessageType.BadFPS:
                    return badFPS;
                case MessageType.GoodFPS:
                    return goodFPS;
                case MessageType.Tips:
                    return tips;
                case MessageType.Info:
                    return info;
                case MessageType.Error:
                    return error;
                case MessageType.Warning:
                    return warning;
            }

            return info;
        }

        [Serializable]
        private class SingleMessage
        {
            public string variable;
            public string variable2;
            public GameObject[] selectObjects;
            public Action AutoFix;
            public string assetPath;

            public SingleMessage(string variable)
            {
                this.variable = variable;
            }

            public SingleMessage(string variable, string variable2)
            {
                this.variable = variable;
                this.variable2 = variable2;
            }

            public SingleMessage(GameObject[] objs)
            {
                selectObjects = objs;
            }

            public SingleMessage(GameObject obj)
            {
                selectObjects = new[] {obj};
            }

            public SingleMessage(Action autoFix)
            {
                AutoFix = autoFix;
            }

            public SingleMessage SetSelectObject(GameObject[] objs)
            {
                selectObjects = objs;
                return this;
            }

            public SingleMessage SetSelectObject(GameObject obj)
            {
                selectObjects = new[] {obj};
                return this;
            }

            public SingleMessage SetAutoFix(Action autoFix)
            {
                AutoFix = autoFix;
                return this;
            }

            public SingleMessage SetAssetPath(string path)
            {
                assetPath = path;
                return this;
            }
        }

        private class MessageGroup : IEquatable<MessageGroup>
        {
            public readonly string Message;
            public readonly string CombinedMessage;
            public readonly string AdditionalInfo;

            public bool DisableCombinedSelection;

            public readonly MessageType MessageType;

            public string Documentation;

            public Action GroupAutoFix;

            public List<SingleMessage> MessageList = new List<SingleMessage>();

            public MessageGroup(string message, MessageType messageType)
            {
                this.Message = message;
                this.MessageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, MessageType messageType)
            {
                this.Message = message;
                this.CombinedMessage = combinedMessage;
                this.MessageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, string additionalInfo, MessageType messageType)
            {
                this.Message = message;
                this.CombinedMessage = combinedMessage;
                this.AdditionalInfo = additionalInfo;
                this.MessageType = messageType;
            }

            public MessageGroup SetGroupAutoFix(Action groupAutoFix)
            {
                this.GroupAutoFix = groupAutoFix;
                return this;
            }

            public MessageGroup SetDocumentation(string documentation)
            {
                this.Documentation = documentation;
                return this;
            }

            public MessageGroup AddSingleMessage(SingleMessage message)
            {
                MessageList.Add(message);
                return this;
            }

            public int GetTotalCount()
            {
                var count = 0;

                if (MessageList == null) return count;

                for (var i = 0; i < MessageList.Count; i++)
                {
                    var item = MessageList[i];
                    if (item.selectObjects != null)
                    {
                        count += item.selectObjects.Count();
                    }
                    else
                    {
                        if (item.assetPath != null)
                        {
                            count++;
                        }
                    }
                }

                return count == 0 ? MessageList.Count : count;
            }

            public MessageGroup SetCombinedSelectionDisabled(bool enabled)
            {
                DisableCombinedSelection = enabled;

                return this;
            }

            public GameObject[] GetSelectObjects()
            {
                var objs = new List<GameObject>();
                foreach (var item in MessageList.Where(o => o.selectObjects != null))
                {
                    objs.AddRange(item.selectObjects);
                }

                return objs.ToArray();
            }

            public string[] GetAssetPaths()
            {
                return MessageList.Where(a => a.assetPath != null).Select(item => item.assetPath).ToArray();
            }

            public Action[] GetSeparateActions()
            {
                return MessageList.Where(m => m.AutoFix != null).Select(m => m.AutoFix).ToArray();
            }

            public bool Buttons()
            {
                return GetSelectObjects().Any() || GetAssetPaths().Any() || GroupAutoFix != null || GetSeparateActions().Any() || GroupAutoFix != null || Documentation != null;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MessageGroup);
            }

            public bool Equals(MessageGroup other)
            {
                return other != null &&
                       Message == other.Message &&
                       CombinedMessage == other.CombinedMessage &&
                       AdditionalInfo == other.AdditionalInfo &&
                       MessageType == other.MessageType;
            }

            public override int GetHashCode()
            {
                var hashCode = 842570769;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CombinedMessage);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AdditionalInfo);
                hashCode = hashCode * -1521134295 + MessageType.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(MessageGroup group1, MessageGroup group2)
            {
                return EqualityComparer<MessageGroup>.Default.Equals(group1, group2);
            }

            public static bool operator !=(MessageGroup group1, MessageGroup group2)
            {
                return !(group1 == group2);
            }
        }

        [Serializable]
        private class MessageCategory
        {
            [SerializeField] public List<MessageGroup> MessageGroups;

            [SerializeField] private Dictionary<int, bool> expandedGroups;

            public string listName;
            public bool disabled;

            public MessageCategory()
            {
                MessageGroups = new List<MessageGroup>();
                expandedGroups = new Dictionary<int, bool>();
            }

            public MessageCategory(string listName)
            {
                MessageGroups = new List<MessageGroup>();
                expandedGroups = new Dictionary<int, bool>();

                this.listName = listName;
            }

            public MessageGroup AddMessageGroup(MessageGroup debuggerMessage)
            {
                MessageGroups.Add(debuggerMessage);

                return debuggerMessage;
            }

            public void ClearMessages()
            {
                MessageGroups.Clear();
            }

            public bool HasMessages()
            {
                return !(MessageGroups is null) && MessageGroups.Count != 0;
            }

            public bool IsExpanded(MessageGroup mg)
            {
                var hash = mg.GetHashCode();
                return expandedGroups.ContainsKey(hash) && expandedGroups[hash];
            }

            public void SetExpanded(MessageGroup mg, bool expanded)
            {
                var hash = mg.GetHashCode();
                if (expandedGroups.ContainsKey(hash))
                {
                    expandedGroups[hash] = expanded;
                }
                else
                {
                    expandedGroups.Add(hash, expanded);
                }
            }
        }

        [Serializable]
        private class MessageCategoryList
        {
            public List<MessageCategory> messageCategory = new List<MessageCategory>();

            public MessageCategory AddOrGetCategory(string listName)
            {
                var newMessageCategory = new MessageCategory(listName);

                var oldMessageCategory = messageCategory.Find(x => x.listName == listName);

                if (oldMessageCategory is null)
                {
                    messageCategory.Add(newMessageCategory);

                    return newMessageCategory;
                }

                return oldMessageCategory;
            }

            public void DrawTabSelector()
            {
                EditorGUILayout.BeginHorizontal();

                for (var i = 0; i < messageCategory.Count; i++)
                {
                    var item = messageCategory[i];

                    var button = "miniButtonMid";

                    if (messageCategory.First() == item)
                    {
                        button = "miniButtonLeft";
                    }
                    else if (messageCategory.Last() == item)
                    {
                        button = "miniButtonRight";
                    }

                    item.disabled = GUILayout.Toggle(item.disabled, item.listName, button);
                }

                EditorGUILayout.EndHorizontal();
            }

            public bool HasCategories()
            {
                return messageCategory.Count > 0;
            }

            public void ClearCategories()
            {
                messageCategory.ForEach(m => m.ClearMessages());
            }

            public void DrawMessages()
            {
                var drawList = messageCategory;

                for (var i = 0; i < drawList.Count; i++)
                {
                    var group = drawList[i];
                    if (!group.disabled)
                    {
                        GUILayout.Label(group.listName, EditorStyles.boldLabel);

                        const int buttonWidth = 80;
                        const int buttonHeight = 20;

                        if (!group.HasMessages())
                        {
                            DrawMessage("No messages found for " + group.listName + ".", MessageType.Info);

                            continue;
                        }

                        for (var l = 0; l < group.MessageGroups.Count; l++)
                        {
                            var messageGroup = group.MessageGroups[l];
                            var hasButtons = messageGroup.Buttons();

                            if (messageGroup.AdditionalInfo != null && messageGroup.GetTotalCount() == 0) continue;

                            if (messageGroup.MessageList.Count > 0)
                            {
                                if (messageGroup.CombinedMessage != null && messageGroup.MessageList.Count != 1)
                                {
                                    EditorGUILayout.BeginHorizontal();

                                    var finalMessage = string.Format(messageGroup.CombinedMessage, messageGroup.GetTotalCount().ToString());

                                    if (messageGroup.AdditionalInfo != null)
                                    {
                                        finalMessage += " " + messageGroup.AdditionalInfo;
                                    }

                                    if (hasButtons)
                                    {
                                        var box = new GUIContent(finalMessage, GetDebuggerIcon(messageGroup.MessageType));
                                        GUILayout.Box(box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                        EditorGUILayout.BeginVertical();

                                        if (messageGroup.Documentation != null)
                                        {
                                            if (GUILayout.Button("Info", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                if (messageGroup.Documentation != null)
                                                {
                                                    Application.OpenURL(messageGroup.Documentation);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            EditorGUI.BeginDisabledGroup(messageGroup.DisableCombinedSelection || messageGroup.GetSelectObjects().Length == 0);

                                            if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                Selection.objects = messageGroup.GetSelectObjects();
                                            }

                                            EditorGUI.EndDisabledGroup();
                                        }

                                        EditorGUI.EndDisabledGroup();

                                        EditorGUI.BeginDisabledGroup(messageGroup.GroupAutoFix == null);

                                        if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                        {
                                            messageGroup.GroupAutoFix();

                                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                                            recheck = true;
                                        }

                                        EditorGUI.EndDisabledGroup();

                                        EditorGUILayout.EndVertical();
                                    }
                                    else
                                    {
                                        DrawMessage(finalMessage, messageGroup.MessageType);
                                    }

                                    EditorGUILayout.EndHorizontal();

                                    var expanded = group.IsExpanded(messageGroup);

                                    expanded = EditorGUILayout.Foldout(expanded, "Show separate messages");

                                    group.SetExpanded(messageGroup, expanded);

                                    if (expanded)
                                    {
                                        for (var j = 0; j < messageGroup.MessageList.Count; j++)
                                        {
                                            var message = messageGroup.MessageList[j];

                                            EditorGUILayout.BeginHorizontal();

                                            var finalSingleMessage = string.Format(messageGroup.Message, message.variable, message.variable2);

                                            if (hasButtons)
                                            {
                                                var box = new GUIContent(finalSingleMessage);
                                                GUILayout.Box(box, Styles.HelpBoxPadded, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 121));

                                                EditorGUILayout.BeginVertical();

                                                EditorGUI.BeginDisabledGroup(!(message.selectObjects != null || message.assetPath != null));

                                                if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    if (message.assetPath != null)
                                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(message.assetPath));

                                                    else
                                                        Selection.objects = message.selectObjects;
                                                }

                                                EditorGUI.EndDisabledGroup();

                                                EditorGUI.BeginDisabledGroup(message.AutoFix == null);

                                                if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    message.AutoFix();

                                                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                                                    recheck = true;
                                                }

                                                EditorGUI.EndDisabledGroup();

                                                EditorGUILayout.EndVertical();
                                            }
                                            else
                                            {
                                                DrawMessage(finalSingleMessage, messageGroup.MessageType);
                                            }

                                            EditorGUILayout.EndHorizontal();
                                        }

                                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                                    }
                                }
                                else
                                {
                                    for (var j = 0; j < messageGroup.MessageList.Count; j++)
                                    {
                                        var message = messageGroup.MessageList[j];
                                        EditorGUILayout.BeginHorizontal();

                                        var finalMessage = string.Format(messageGroup.Message, message.variable, message.variable2);

                                        if (messageGroup.AdditionalInfo != null)
                                        {
                                            finalMessage = string.Concat(finalMessage, " ", messageGroup.AdditionalInfo);
                                        }

                                        if (hasButtons)
                                        {
                                            var box = new GUIContent(finalMessage, GetDebuggerIcon(messageGroup.MessageType));
                                            GUILayout.Box(box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                            EditorGUILayout.BeginVertical();

                                            EditorGUI.BeginDisabledGroup(!(message.selectObjects != null || message.assetPath != null));

                                            if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                if (message.assetPath != null)
                                                {
                                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(message.assetPath));
                                                }
                                                else
                                                {
                                                    Selection.objects = message.selectObjects;
                                                }
                                            }

                                            EditorGUI.EndDisabledGroup();

                                            EditorGUI.BeginDisabledGroup(message.AutoFix == null);

                                            if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                message.AutoFix();

                                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                                                recheck = true;
                                            }

                                            EditorGUI.EndDisabledGroup();

                                            EditorGUILayout.EndVertical();
                                        }
                                        else
                                        {
                                            DrawMessage(finalMessage, messageGroup.MessageType);
                                        }

                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                            }
                            else
                            {
                                if (hasButtons)
                                {
                                    EditorGUILayout.BeginHorizontal();

                                    var box = new GUIContent(messageGroup.Message, GetDebuggerIcon(messageGroup.MessageType));
                                    GUILayout.Box(box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                    EditorGUILayout.BeginVertical();

                                    EditorGUI.BeginDisabledGroup(messageGroup.Documentation == null);

                                    if (GUILayout.Button("Info", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                    {
                                        if (messageGroup.Documentation != null)
                                        {
                                            Application.OpenURL(messageGroup.Documentation);
                                        }
                                    }

                                    EditorGUI.EndDisabledGroup();

                                    EditorGUI.BeginDisabledGroup(messageGroup.GroupAutoFix == null);

                                    if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                    {
                                        if (messageGroup.GroupAutoFix != null)
                                        {
                                            messageGroup.GroupAutoFix();
                                            recheck = true;
                                        }
                                    }

                                    EditorGUI.EndDisabledGroup();

                                    EditorGUILayout.EndVertical();

                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    DrawMessage(messageGroup.Message, messageGroup.MessageType);
                                }
                            }
                        }
                    }
                }
            }

            private static void DrawMessage(string messageText, MessageType type)
            {
                var box = new GUIContent(messageText, GetDebuggerIcon(type));
                GUILayout.Box(box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 18));
            }
        }

        private Vector2 scrollPos;

        [SerializeField] private int tab;

        [MenuItem("VRWorld Toolkit/World Debugger", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(WorldDebugger));
            window.titleContent = new GUIContent("World Debugger");
            window.minSize = new Vector2(530, 600);
            window.Show();
        }

        #region Actions

        public static Action SelectAsset(GameObject obj)
        {
            return () => { Selection.activeObject = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)); };
        }

        public static Action SetGenerateLightmapUV(ModelImporter importer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Enable lightmap UV generation?", "This operation will enable the lightmap UV generation on the mesh \"" + Path.GetFileName(AssetDatabase.GetAssetPath(importer)) + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    importer.generateSecondaryUV = true;
                    importer.SaveAndReimport();
                }
            };
        }

        public static Action SetGenerateLightmapUV(List<ModelImporter> importers)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Enable lightmap UV generation?", "This operation will enable the lightmap UV generation on " + importers.Count + " meshes.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    importers.ForEach(i =>
                    {
                        i.generateSecondaryUV = true;
                        i.SaveAndReimport();
                    });
                }
            };
        }

        public static Action RemoveBadPipelineManagers(PipelineManager[] pipelineManagers)
        {
            return () =>
            {
                foreach (var pipelineManager in pipelineManagers)
                {
                    if (pipelineManager.gameObject.GetComponent<VRC_SceneDescriptor>())
                        continue;

                    DestroyImmediate(pipelineManager.gameObject.GetComponent<PipelineManager>());
                }
            };
        }

        public static Action SetLegacyBlendShapeNormals(ModelImporter importer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Enable Legacy Blend Shape Normals?", "This operation will enable Legacy Blend Shape Normals on the model \"" + Path.GetFileName(AssetDatabase.GetAssetPath(importer)) + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    ModelImporterUtil.SetLegacyBlendShapeNormals(importer, true);
                    importer.SaveAndReimport();
                }
            };
        }

        public static Action DisableComponent(Behaviour behaviour)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviour.GetType() + " on the GameObject \"" + behaviour.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    behaviour.enabled = false;
                }
            };
        }

        public static Action DisableComponent(Behaviour[] behaviours)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviours[0].GetType() + " component on " + behaviours.Count().ToString() + " GameObjects.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    behaviours.ToList().ForEach(b => b.enabled = false);
                }
            };
        }

        public static Action SetObjectLayer(GameObject obj, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change the layer of " + obj.name + " to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    obj.layer = LayerMask.NameToLayer(layer);
                }
            };
        }

        public static Action SetObjectLayer(GameObject[] objs, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change layer?", "This operation will change " + objs.Length + " GameObjects layer to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.layer = LayerMask.NameToLayer(layer));
                }
            };
        }

        public static Action SetSelectableNavigationMode(Selectable selectable, Navigation.Mode mode)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Navigation mode?", "This operation will change the Navigation mode on UI Element \"" + selectable.gameObject.name + "\" to " + mode.ToString() + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    var navigation = selectable.navigation;

                    navigation.mode = Navigation.Mode.None;

                    selectable.navigation = navigation;
                }
            };
        }

        public static Action SetSelectableNavigationMode(Selectable[] selectables, Navigation.Mode mode)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Navigation mode?", "This operation will change " + selectables.Length + " UI Elements Navigation to " + mode.ToString() + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    for (var i = 0; i < selectables.Length; i++)
                    {
                        var navigation = selectables[i].navigation;

                        navigation.mode = Navigation.Mode.None;

                        selectables[i].navigation = navigation;
                    }
                }
            };
        }

        public static Action SetLightmapSize(int newSize)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change lightmap size?", "This operation will change your lightmap size from " + LightmapEditorSettings.maxAtlasSize + " to " + newSize + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    LightmapEditorSettings.maxAtlasSize = newSize;
                }
            };
        }

        public static Action SetLightmapOverrideForQuest(TextureImporter[] textureImporters)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Set lightmap compression override?", "This operation will set the platform-specific overrides for all lightmaps (" + textureImporters.Length + ") to ATCS 4x4 block format on Android.\n\nWarning this can take a while depending on lightmap size and how many there are.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    foreach (var item in textureImporters)
                    {
                        var settings = item.GetPlatformTextureSettings("Android");

                        settings.overridden = true;

                        settings.format = TextureImporterFormat.ASTC_RGB_4x4;

                        item.SetPlatformTextureSettings(settings);

                        item.SaveAndReimport();
                    }
                }
            };
        }

        public static Action SetLightmapOverrideForQuest(TextureImporter textureImporter, string lightmapName)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Set lightmap compression override?", "This operation will set the platform-specific overrides for \"" + lightmapName + "\" to ATCS 4x4 block format on Android.\n\nWarning this can take a while depending on lightmap size.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    var settings = textureImporter.GetPlatformTextureSettings("Android");

                    settings.overridden = true;

                    settings.format = TextureImporterFormat.ASTC_RGB_4x4;

                    textureImporter.SetPlatformTextureSettings(settings);

                    textureImporter.SaveAndReimport();
                }
            };
        }

        public static Action SetEnviromentReflections(DefaultReflectionMode reflections)
        {
            return () => { RenderSettings.defaultReflectionMode = reflections; };
        }

        public static Action SetAmbientMode(AmbientMode ambientMode)
        {
            return () => { RenderSettings.ambientMode = ambientMode; };
        }

        public static Action SetGameObjectTag(GameObject obj, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change the Tag of " + obj.name + " to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    obj.tag = tag;
                }
            };
        }

        public static Action SetGameObjectTag(GameObject[] objs, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + objs.Length + " GameObjects tag to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.tag = tag);
                }
            };
        }

        public static Action ChangeShader(Material material, string shader)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change shader?", "This operation will change the shader of the material " + material.name + " to " + shader + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Shader standard = Shader.Find(shader);

                    material.shader = standard;
                }
            };
        }

        public static Action ChangeShader(Material[] materials, string shader)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change shader?", "This operation will change the shader of " + materials.Length + " materials to " + shader + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Shader newShader = Shader.Find(shader);

                    materials.ToList().ForEach(m => m.shader = newShader);
                }
            };
        }

        public static Action RemoveOverlappingLightprobes(LightProbeGroup lpg)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes in the group \"" + lpg.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    lpg.probePositions = lpg.probePositions.Distinct().ToArray();
                }
            };
        }

        public static Action RemoveOverlappingLightprobes(LightProbeGroup[] lpgs)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes found in the current scene.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    foreach (var lpg in lpgs)
                    {
                        lpg.probePositions = lpg.probePositions.Distinct().ToArray();
                    }
                }
            };
        }

        public static Action RemoveRedundantLightProbes(LightProbeGroup[] lpgs)
        {
            return () =>
            {
                if (LightmapSettings.lightProbes != null)
                {
                    var probes = LightmapSettings.lightProbes.positions;
                    if (EditorUtility.DisplayDialog("Remove redundant light probes?", "This operation will attempt to remove any redundant light probes in the current scene. Bake your lighting before this operation to avoid any correct light probes getting removed.\n\nDo you want to continue?", "Yes", "Cancel"))
                    {
                        foreach (var lpg in lpgs)
                        {
                            lpg.probePositions = lpg.probePositions.Distinct().Where(p => !probes.Contains(p)).ToArray();
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Baked lightprobes not found!", "Bake your lighting first before attempting to remove redundant light probes.", "Ok");
                }
            };
        }

        public static Action ClearOcclusionCache(long fileCount)
        {
            return async () =>
            {
                if (EditorUtility.DisplayDialog("Clear Occlusion Cache?", "This will clear your occlusion culling cache. Which has " + fileCount + " files currently. Deleting a massive amount of files can take a while.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    long deleteCount = 0;

                    var tokenSource = new CancellationTokenSource();

                    var deleteFiles = new Progress<string>(fileName =>
                    {
                        deleteCount++;
                        if (EditorUtility.DisplayCancelableProgressBar("Clearing Occlusion Cache", fileName, (float) deleteCount / fileCount))
                        {
                            tokenSource.Cancel();
                        }
                    });

                    var token = tokenSource.Token;

                    await Task.Run(() => DeleteFiles(deleteFiles, token));
                    EditorUtility.ClearProgressBar();

                    occlusionCacheFiles = 0;
                    EditorUtility.DisplayDialog("Files Deleted", "Deleted " + deleteCount + " files.", "Ok");
                }
            };
        }

        public static void DeleteFiles(IProgress<string> deleted, CancellationToken cancellationToken)
        {
            Parallel.ForEach(Directory.EnumerateFiles("Library/Occlusion/"), (file, state) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    state.Break();
                }

                File.Delete(file);
                deleted.Report(file);
            });
        }

        public static Action FixSpawns(VRC_SceneDescriptor descriptor)
        {
            return () =>
            {
                descriptor.spawns = descriptor.spawns.Where(c => c != null).ToArray();
                if (descriptor.spawns.Length == 0)
                {
                    descriptor.spawns = new[] {descriptor.gameObject.transform};
                }
            };
        }

        public static Action SetErrorPause(bool enabled)
        {
            return () => { ConsoleFlagUtil.SetConsoleErrorPause(enabled); };
        }

        public static Action SetVRChatLayers()
        {
            return () => { UpdateLayers.SetupEditorLayers(); };
        }

        public static Action SetVRChatCollisionMatrix()
        {
            return () => { UpdateLayers.SetupCollisionLayerMatrix(); };
        }

        public static Action SetReferenceCamera(VRC_SceneDescriptor descriptor, Camera camera)
        {
            return () => { descriptor.ReferenceCamera = camera.gameObject; };
        }

        public static Action SetVRCInstallPath()
        {
            return () =>
            {
                var clientPath = Helper.GetSteamVrcExecutablePath();

                SDKClientUtilities.GetSavedVRCInstallPath();

                if (clientPath != null)
                {
                    SDKClientUtilities.SetVRCInstallPath(clientPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("VRChat Executable Path Not Found", "Could not find the VRChat executable path automatically you can set it manually from the VRCSDK Settings page.", "Ok");
                }
            };
        }

#if UNITY_POST_PROCESSING_STACK_V2
        public enum RemovePpEffect
        {
            AmbientOcclusion = 0,
            ScreenSpaceReflections = 1,
            BloomDirt = 2
        }

        public static Action DisablePostProcessEffect(PostProcessProfile postprocessProfile, RemovePpEffect effect)
        {
            return () =>
            {
                switch (effect)
                {
                    case RemovePpEffect.AmbientOcclusion:
                        postprocessProfile.GetSetting<AmbientOcclusion>().active = false;
                        break;
                    case RemovePpEffect.ScreenSpaceReflections:
                        postprocessProfile.GetSetting<ScreenSpaceReflections>().active = false;
                        break;
                    case RemovePpEffect.BloomDirt:
                        postprocessProfile.GetSetting<Bloom>().dirtTexture.overrideState = false;
                        postprocessProfile.GetSetting<Bloom>().dirtIntensity.overrideState = false;
                        break;
                }
            };
        }

        public static Action SetPostProcessingInScene(SceneView.SceneViewState sceneViewState, bool isActive)
        {
            return () => { sceneViewState.showImageEffects = isActive; };
        }

        public static Action SetPostProcessingLayerResources(PostProcessLayer postProcessLayer, PostProcessResources resources)
        {
            return () => { postProcessLayer.Init(resources); };
        }
#endif

        #endregion

        #region Texts

        private const string NO_SCENE_DESCRIPTOR = "The current scene has no Scene Descriptor. Please add one, or drag the VRCWorld prefab to the scene.";

        private const string TOO_MANY_SCENE_DESCRIPTORS = "Multiple Scene Descriptors were found. Only one scene descriptor can exist in a single scene.";

        private const string TOO_MANY_PIPELINE_MANAGERS = "The current scene has multiple Pipeline Managers in it. This can break the world upload process and cause you not to be able to load into the world.";

        private const string WORLD_DESCRIPTOR_FAR = "Scene Descriptor is {0} units far from the zero point in Unity. Having your world center out this far will cause some noticeable jittering on models. You should move your world closer to the zero point of your scene.";

        private const string WORLD_DESCRIPTOR_OFF = "Scene Descriptor is {0} units far from the zero point in Unity. It is usually good practice to keep it as close as possible to the absolute zero point to avoid floating-point errors.";

        private const string NO_SPAWN_POINT_SET = "There are no spawn points set in your Scene Descriptor. Spawning into a world with no spawn point will cause you to get thrown back to your homeworld.";

        private const string NULL_SPAWN_POINT = "Null spawn point set Scene Descriptor. Spawning into a null spawn point will cause you to get thrown back to your homeworld.";

        private const string COLLIDER_UNDER_SPAWN_IS_TRIGGER = "The collider \"{0}\" under your spawn point {1} has been set as Is Trigger! Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string NO_COLLIDER_UNDER_SPAWN = "Spawn point \"{0}\" does not have anything underneath it. Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string NO_PLAYER_MODS = "No Player Mods were found in the scene. Player mods are needed for adding jumping and changing walking speed.";

        private const string TRIGGER_TRIGGER_NO_COLLIDER = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that does not have a Collider on it.";
        private const string COLLIDER_TRIGGER_NO_COLLIDER = "You have an OnEnterCollider or OnExitCollider Trigger \"{0}\" that does not have a Collider on it.";

        private const string TRIGGER_TRIGGER_WRONG_LAYER = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that is not on the MirrorReflection layer.";
        private const string TRIGGER_TRIGGER_WRONG_LAYER_COMBINED = "You have {0} OnEnterTrigger or OnExitTrigger Triggers that are not on the MirrorReflection layer.";
        private const string TRIGGER_TRIGGER_WRONG_LAYER_INFO = "This can stop raycasts from working correctly, making you unable to interact with objects and UI Buttons.";

        private const string MIRROR_ON_BY_DEFAULT = "The mirror \"{0}\" is on by default.";
        private const string MIRROR_ON_BY_DEFAULT_COMBINED = "The scene has {0} mirrors on by default.";
        private const string MIRROR_ON_BY_DEFAULT_INFO = "This is an awful practice. Any mirrors in worlds should be disabled by default.";

        private const string MIRROR_WITH_DEFAULT_LAYERS = "The mirror \"{0}\" has the default Reflect Layers set.";
        private const string MIRROR_WITH_DEFAULT_LAYERS_COMBINED = "You have {0} mirrors that have the default Reflect Layers set.";
        private const string MIRROR_WITH_DEFAULT_LAYERS_INFO = "Only having the layers needed to have enabled in mirrors can save a lot of frames, especially in populated instances.";

        private const string LEGACY_BLEND_SHAPE_ISSUES = "Skinned mesh renderer found with model {0} ({1}) without Legacy Blend Shape Normals enabled.";
        private const string LEGACY_BLEND_SHAPE_ISSUES_COMBINED = "Found {0} models without Legacy Blend Shape Normals enabled.";
        private const string LEGACY_BLEND_SHAPE_ISSUES_INFO = "This can significantly increase the size of the world.";

        private const string BAKED_OCCLUSION_CULLING = "Baked Occlusion Culling found.";

        private const string NO_OCCLUSION_AREAS = "No occlusion areas were found. Occlusion Areas are recommended to help generate higher precision data where the camera is likely to be. If no set, the area is created automatically containing all Occluders and Occludees.";

        private const string DISABLED_OCCLUSION_AREA = "Occlusion Area {0} found with Is View Volume disabled.";
        private const string DISABLED_OCCLUSION_AREA_COMBINED = "Occlusion Areas found with Is View Volume disabled.";
        private const string DISABLED_OCCLUSION_AREA_INFO = "Without this enabled, the Occlusion Area does not get used for the occlusion bake.";

        private const string NO_OCCLUSION_CULLING = "The current scene does not have baked Occlusion Culling. Occlusion culling gives a lot more performance for the world, especially in more massive worlds with multiple rooms or areas.";

        private const string OCCLUSION_CULLING_CACHE_WARNING = "The current project's occlusion culling cache has {0} files. When the occlusion culling cache grows too big, baking occlusion culling can take much longer than intended. It can be cleared with no adverse effects.";

        private const string ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE = "The current scene has an active camera \"{0}\" outputting to a render texture.";
        private const string ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE_COMBINED = "The current scene has {0} active cameras outputting to render textures.";
        private const string ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE_INFO = "This will affect performance negatively by causing more draw calls to happen. They should only be enabled when needed.";

        private const string NO_TOON_SHADERS = "Toon shaders should be avoided for world-building, as they are missing crucial things for making worlds. For world-building, the most recommended shader is Standard.";

        private const string NON_CRUNCHED_TEXTURES = "{0}% of the textures used in the scene have not been crunch compressed. Crunch compression can significantly reduce the size of the world download. It can be found from the texture's import settings.";

        private const string SWITCH_TO_PROGRESSIVE = "The current scene is using the Enlighten lightmapper, which has been deprecated in newer versions of Unity. It would be best to consider switching to Progressive for improved fidelity and performance.";

        private const string SINGLE_COLOR_ENVIRONMENT_LIGHTING = "Consider changing the Environment Lighting to Gradient from Flat.";

        private const string DARK_ENVIRONMENT_LIGHTING = "Using dark colors for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if the world has dark lighting.";

        private const string CUSTOM_ENVIRONMENT_REFLECTIONS_NULL = "The current scenes Environment Reflections have been set to custom, but a custom cubemap has not been defined.";

        private const string NO_LIGHTMAP_UV = "The model found in the scene \"{0}\" is set to be lightmapped but does not have Lightmap UVs.";
        private const string NO_LIGHTMAP_UV_COMBINED = "The current scene has {0} models set to be lightmapped that do not have Lightmap UVs.";
        private const string NO_LIGHTMAP_UV_INFO = "This can cause issues when baking lighting. You can enable generating Lightmap UV's in the model's import settings.";

        private const string LIGHTS_NOT_BAKED = "The current scene is using realtime lighting. Consider baked lighting for improved performance.";

        private const string CONSIDER_LARGER_LIGHTMAPS = "Consider increasing Lightmap Size from {0} to 2048 or 4096. This allows for more stuff to fit on a single lightmap, leaving fewer textures that need to be sampled.";

        private const string CONSIDER_SMALLER_LIGHTMAPS = "Baking lightmaps at 4096 with Progressive GPU will silently fall back to CPU Progressive. More than 12GB GPU Memory is needed to bake 4k lightmaps with GPU Progressive.";

        private const string NON_BAKED_BAKED_LIGHT = "The light {0} is set to be baked/mixed, but it has not been baked yet!";
        private const string NON_BAKED_BAKED_LIGHT_COMBINED = "The scene contains {0} baked/mixed lights that have not been baked!";
        private const string NON_BAKED_BAKED_LIGHT_INFO = "Baked lights that have not been baked yet function as realtime lights in-game.";

        private const string LIGHTING_DATA_ASSET_INFO = "The current scene's lighting data asset takes up {0} MB of the world's size. This contains the scene's light probe data and realtime GI data.";

        private const string NO_LIGHT_PROBES = "No light probes found in the current scene. Without baked light probes baked lights are not able to affect dynamic objects such as players and pickups.";

        private const string LIGHT_PROBE_COUNT_NOT_BAKED = "The current scene contains {0} light probes, but {1} of them have not been baked yet.";

        private const string LIGHT_PROBES_REMOVED_NOT_RE_BAKED = "Some light probes have been removed after the last bake. Bake them again to update the scene's lighting data. The lighting data contains {0} baked light probes, and the current scene has {1} light probes.";

        private const string LIGHT_PROBE_COUNT = "The current scene contains {0} baked light probes.";

        private const string OVERLAPPING_LIGHT_PROBES = "Light Probe Group \"{0}\" has {1} overlapping light probes.";
        private const string OVERLAPPING_LIGHT_PROBES_COMBINED = "Found {0} Light Probe Groups with overlapping light probes.";
        private const string OVERLAPPING_LIGHT_PROBES_INFO = "These can cause a slowdown in the editor and will not get baked because Unity will skip any extra overlapping probes.";

        private const string NO_REFLECTION_PROBES = "The current scene has no active reflection probes. Reflection probes are needed to have proper reflections on reflective materials.";

        private const string REFLECTION_PROBES_SOME_UNBAKED = "The reflection probe \"{0}\" is unbaked.";
        private const string REFLECTION_PROBES_SOME_UNBAKED_COMBINED = "The current scene has {0} unbaked reflection probes.";

        private const string REFLECTION_PROBE_COUNT_TEXT = "The current scene has {0} reflection probes.";

        private const string POST_PROCESSING_IMPORTED_BUT_NOT_SETUP = "The current project has Post Processing imported, but you have not set it up yet.";

        private const string POST_PROCESSING_DISABLED_IN_SCENE_VIEW = "Post-processing is disabled in the scene view. You will not be able to preview any post-processing effects without enabling it first.";

        private const string NO_REFERENCE_CAMERA_SET = "The current scenes Scene Descriptor has no Reference Camera set. Without a Reference Camera set Post Processing will not be visible in-game.";

        private const string NO_POST_PROCESSING_VOLUMES = "No enabled Post Processing Volumes found in the scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";

        private const string REFERENCE_CAMERA_NO_POST_PROCESSING_LAYER = "The current Reference Camera does not have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";

        private const string POST_PROCESS_LAYER_USING_RESERVED_LAYER = "Your current Post Process Layer uses one of the VRChat reserved layers. Using these will break post-processing while in-game.";

        private const string VOLUME_BLENDING_LAYER_NOT_SET = "You don't have a Volume Blending Layer set in the Post Process Layer, so post-processing will not work. Using the Water or PostProcessing layer is recommended.";

        private const string POST_PROCESSING_VOLUME_NOT_GLOBAL_NO_COLLIDER = "Post Processing Volume \"{0}\" is not marked as Global and does not have a collider. It will not affect the camera without one of these set on it.";

        private const string NO_PROFILE_SET = "Post Processing Volume \"{0}\" does not have a profile set.";

        private const string VOLUME_ON_WRONG_LAYER = "Post Processing Volume \"{0}\" is not on one of the layers set in the cameras Post Processing Layer setting. (Currently: {1})";

        private const string DONT_USE_NONE_FOR_TONEMAPPING = "Use either Neutral or ACES for Color Grading Tonemapping. Selecting None for Tonemapping is essentially the same as leaving Tonemapping unchecked.";

        private const string TOO_HIGH_BLOOM_INTENSITY = "Do not raise the Bloom intensity too high! It is best to use a low Bloom intensity, between 0.01 to 0.3.";

        private const string TOO_HIGH_BLOOM_THRESHOLD = "You should avoid having the Bloom Threshold set high. It might cause unexpected problems with avatars. Ideally, it should be kept at 0, but always below 1.0.";

        private const string NO_BLOOM_DIRT_IN_VR = "Avoid using Bloom Dirt. It looks terrible in VR!";

        private const string NO_AMBIENT_OCCLUSION = "Do not use Ambient Occlusion in VRChat! VRchat uses Forward rendering, so it gets applied on top of everything else, which is bad! It also has a super high rendering cost in VR.";

        private const string DEPTH_OF_FIELD_WARNING = "Depth of field has a high-performance cost and is very disorientating in VR. If you want to use depth of field, it should be disabled by default.";

        private const string SCREEN_SPACE_REFLECTIONS_WARNING = "Screen-space Reflections only works when using deferred rendering. Because VRchat is not using deferred rendering, so this should not be used.";

        private const string VIGNETTE_WARNING = "Only use Post Processing vignette in minimal amounts. A powerful vignette can cause sickness in VR.";

        private const string NO_POST_PROCESSING_IMPORTED = "Post Processing package not found in the project.";

        private const string QUEST_BAKED_LIGHTING_WARNING = "Realtime lighting for Quest content should be avoided and instead have a properly baked lighting setup for performance.";

        private const string AMBIENT_MODE_SET_TO_CUSTOM = "The current scenes Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";

        private const string NO_PROBLEMS_FOUND_IN_PP = "No problems were found in your post-processing setup. In some cases where post-processing is working in the editor but not in-game, some imported assets may be causing it not to function correctly.";

        private const string BAKERY_LIGHT_NOT_SET_EDITOR_ONLY = "Your Bakery light named \"{0}\" is not set to be EditorOnly.";
        private const string BAKERY_LIGHT_NOT_SET_EDITOR_ONLY_COMBINED = "You have {0} Bakery lights are not set to be EditorOnly.";
        private const string BAKERY_LIGHT_NOT_SET_EDITOR_ONLY_INFO = "This causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";

        private const string BAKERY_LIGHT_UNITY_LIGHT = "Your Bakery light named \"{0}\" has an active Unity Light component on it.";
        private const string BAKERY_LIGHT_UNITY_LIGHT_COMBINED = "You have {0} Bakery lights that have an active Unity Light component on it.";
        private const string BAKERY_LIGHT_UNITY_LIGHT_INFO = "These will not get baked with Bakery and will keep acting as realtime lights even if set to baked.";

        private const string QUEST_LIGHTMAP_COMPRESSION_OVERRIDE = "Lightmap \"{0}\" does not have a platform-specific override set for Android.";
        private const string QUEST_LIGHTMAP_COMPRESSION_OVERRIDE_COMBINED = "No platform-specific override set on {0} lightmaps for Android.";
        private const string QUEST_LIGHTMAP_COMPRESSION_OVERRIDE_INFO = "Without setting proper compression override when building for Quest lightmaps can show noticeable banding. Suggested format \"ASTC 4x4 block\".";

        private const string MISSING_SHADER_WARNING = "The material \"{0}\" found in the scene has a missing or broken shader.";
        private const string MISSING_SHADER_WARNING_COMBINED = "Found {0} materials in the current scene that have missing or broken shaders.";
        private const string MISSING_SHADER_WARNING_INFO = "These will fallback to the pink error shader.";

        private const string ERROR_PAUSE_WARNING = "You have Error Pause enabled in your console. This can cause your world upload to fail by interrupting the build process.";

        private const string MULTIPLE_SCENES_LOADED = "Multiple scenes loaded, this is not supported by VRChat and can cause the world upload to fail. Only one scene should be used for world creation at a time.";

        private const string LAYERS_NOT_SETUP = "Project layers are not set up for VRChat yet.";

        private const string COLLISION_MATRIX_NOT_SETUP = "The projects Collision Matrix is not set up for VRChat yet.";

        private const string MATERIAL_WITH_GRAB_PASS_SHADER = "A material ({0}) in the scene is using a GrabPass due to shader \"{1}\".";
        private const string MATERIAL_WITH_GRAB_PASS_SHADER_COMBINED = "Found {0} materials in the scene using a GrabPass.";
        private const string MATERIAL_WITH_GRAB_PASS_SHADER_INFO_PC = "A GrabPass will halt the rendering to copy the screen's contents into a texture for the shader to read. This has a notable effect on performance.";
        private const string MATERIAL_WITH_GRAB_PASS_SHADER_INFO_QUEST = "Please change the shader for this material. When a shader uses a GrabPass on Quest, it will cause painful visual artifacts to occur, as they are not supported.";

        private const string DISABLED_PORTALS_WARNING = "Portal \"{0}\" disabled by default.";
        private const string DISABLED_PORTALS_WARNING_COMBINED = "Found {0} portals disabled by default.";
        private const string DISABLED_PORTALS_WARNING_INFO = "Having a portal disabled by default will cause players that are entering to end up in different instances.";

        private const string SHRNM_DIRECTIONAL_MODE_BAKERY_ERROR = "SH or RNM directional mode detected in Bakery. Using SH directional mode is not supported in VRChat by default. It requires the usage of VRC Bakery Adapter by Merlin for it to function in-game.";

        private const string BUILD_AND_TEST_BROKEN_ERROR = "VRChat link association has not been set up, and the VRChat client path has not been set in the VRCSDK settings. Without one of these settings set Build & Test will not function.";

        private const string BUILD_AND_TEST_FORCE_NON_VR_ERROR = "VRChat client path has not been set to point directly to the VRChat executable in the VRCSDK settings. This will cause Force Non-VR setting for Build & Test not to work.";

        private const string MATERIAL_WITH_NON_WHITELISTED_SHADER = "Material \"{0}\" is using an unsupported shader \"{1}\".";
        private const string MATERIAL_WITH_NON_WHITELISTED_SHADER_COMBINED = "Found {0} materials with unsupported shaders.";
        private const string MATERIAL_WITH_NON_WHITELISTED_SHADER_INFO = "Unsupported shaders can cause problems on the Quest platform unless appropriately used.";

        private const string UI_ELEMENT_WITH_NAVIGATION_NOT_NONE = "The UI Element \"{0}\" does not have its Navigation set to None.";
        private const string UI_ELEMENT_WITH_NAVIGATION_NOT_NONE_COMBINED = "Found {0} UI Elements with their Navigation not set to None.";
        private const string UI_ELEMENT_WITH_NAVIGATION_NOT_NONE_INFO = "Setting Navigation to None on UI Elements stops accidental interactions with them while just trying to walk around.";

        #endregion

        private static long occlusionCacheFiles;

        // TODO: Better check threading
        private static void CountOcclusionCacheFiles()
        {
            occlusionCacheFiles = Directory.EnumerateFiles("Library/Occlusion/").Count();

            if (occlusionCacheFiles > 0)
            {
                recheck = true;
            }
        }

        private void CheckScene()
        {
            masterList.ClearCategories();

            // General Checks

            // Get Descriptors
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            long descriptorCount = descriptors.Length;
            VRC_SceneDescriptor sceneDescriptor;
            var pipelines = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];

            // Check if a descriptor exists
            if (descriptorCount == 0)
            {
                general.AddMessageGroup(new MessageGroup(NO_SCENE_DESCRIPTOR, MessageType.Error));
                return;
            }
            else
            {
                sceneDescriptor = descriptors[0];

                // Make sure only one descriptor exists
                if (descriptorCount > 1)
                {
                    general.AddMessageGroup(new MessageGroup(TOO_MANY_SCENE_DESCRIPTORS, MessageType.Info).AddSingleMessage(new SingleMessage(Array.ConvertAll(descriptors, s => s.gameObject))));
                    return;
                }

                // Check for multiple pipeline managers
                if (pipelines.Length > 1)
                {
                    general.AddMessageGroup(new MessageGroup(TOO_MANY_PIPELINE_MANAGERS, MessageType.Error).AddSingleMessage(new SingleMessage(Array.ConvertAll(pipelines, s => s.gameObject)).SetAutoFix(RemoveBadPipelineManagers(pipelines))));
                }

                // Check how far the descriptor is from zero point for floating point errors
                var descriptorRemoteness = (int) Vector3.Distance(sceneDescriptor.transform.position, new Vector3(0.0f, 0.0f, 0.0f));

                if (descriptorRemoteness > 1000)
                {
                    general.AddMessageGroup(new MessageGroup(WORLD_DESCRIPTOR_FAR, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
                else if (descriptorRemoteness > 250)
                {
                    general.AddMessageGroup(new MessageGroup(WORLD_DESCRIPTOR_OFF, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
            }

            if (!UpdateLayers.AreLayersSetup())
            {
                general.AddMessageGroup(new MessageGroup(LAYERS_NOT_SETUP, MessageType.Warning).SetGroupAutoFix(SetVRChatLayers()));
            }

            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                general.AddMessageGroup(new MessageGroup(COLLISION_MATRIX_NOT_SETUP, MessageType.Warning).SetGroupAutoFix(SetVRChatCollisionMatrix()));
            }

            // Check if multiple scenes loaded
            if (SceneManager.sceneCount > 1)
            {
                general.AddMessageGroup(new MessageGroup(MULTIPLE_SCENES_LOADED, MessageType.Error));
            }

            // Check if console has error pause on
            if (ConsoleFlagUtil.GetConsoleErrorPause())
            {
                general.AddMessageGroup(new MessageGroup(ERROR_PAUSE_WARNING, MessageType.Error).AddSingleMessage(new SingleMessage(SetErrorPause(false))));
            }

#if UNITY_EDITOR_WIN
            // Check for problems with Build & Test
            if (SDKClientUtilities.GetSavedVRCInstallPath() == "\\VRChat.exe" || SDKClientUtilities.GetSavedVRCInstallPath() == "")
            {
                if (Registry.ClassesRoot.OpenSubKey(@"VRChat\shell\open\command") is null)
                {
                    general.AddMessageGroup(new MessageGroup(BUILD_AND_TEST_BROKEN_ERROR, MessageType.Error).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
                else
                {
                    general.AddMessageGroup(new MessageGroup(BUILD_AND_TEST_FORCE_NON_VR_ERROR, MessageType.Warning).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
            }
#endif

            // Get spawn points for any possible problems
            var spawns = sceneDescriptor.spawns.Where(s => s != null).ToArray();

            var spawnsLength = sceneDescriptor.spawns.Length;
            var emptySpawns = spawnsLength != spawns.Length;

            if (spawns.Length == 0)
            {
                general.AddMessageGroup(new MessageGroup(NO_SPAWN_POINT_SET, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
            }
            else
            {
                if (emptySpawns)
                {
                    general.AddMessageGroup(new MessageGroup(NULL_SPAWN_POINT, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
                }

                for (int i = 0; i < sceneDescriptor.spawns.Length; i++)
                {
                    if (sceneDescriptor.spawns[i] == null)
                    {
                        continue;
                    }

                    if (!Physics.Raycast(sceneDescriptor.spawns[i].position + new Vector3(0, 0.01f, 0), Vector3.down, out RaycastHit hit, Mathf.Infinity, 0, QueryTriggerInteraction.Ignore))
                    {
                        if (Physics.Raycast(sceneDescriptor.spawns[i].position + new Vector3(0, 0.01f, 0), Vector3.down, out hit, Mathf.Infinity))
                        {
                            if (hit.collider.isTrigger)
                            {
                                general.AddMessageGroup(new MessageGroup(COLLIDER_UNDER_SPAWN_IS_TRIGGER, MessageType.Error).AddSingleMessage(new SingleMessage(hit.collider.name, sceneDescriptor.spawns[i].gameObject.name).SetSelectObject(sceneDescriptor.spawns[i].gameObject)));
                            }
                        }
                        else
                        {
                            general.AddMessageGroup(new MessageGroup(NO_COLLIDER_UNDER_SPAWN, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.spawns[i].gameObject.name).SetSelectObject(sceneDescriptor.spawns[i].gameObject)));
                        }
                    }
                }
            }

#if VRC_SDK_VRCSDK2
            // Check if the world has playermods defined
            var playermods = FindObjectsOfType(typeof(VRC_PlayerMods)) as VRC_PlayerMods[];
            if (playermods.Length == 0)
            {
                general.AddMessageGroup(new MessageGroup(NO_PLAYER_MODS, MessageType.Tips));
            }

            // Get triggers in the world
            var triggerScripts = (VRC_Trigger[]) VRC_Trigger.FindObjectsOfType(typeof(VRC_Trigger));

            var triggerWrongLayer = new List<GameObject>();

            // Check for OnEnterTriggers to make sure they are on mirrorreflection layer
            foreach (var triggerScript in triggerScripts)
            {
                foreach (var trigger in triggerScript.Triggers)
                {
                    if (trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnExitTrigger || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnExitCollider)
                    {
                        if (!triggerScript.gameObject.GetComponent<Collider>())
                        {
                            if (trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnExitTrigger)
                            {
                                general.AddMessageGroup(new MessageGroup(TRIGGER_TRIGGER_NO_COLLIDER, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
                            }
                            else if (trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnExitCollider)
                            {
                                general.AddMessageGroup(new MessageGroup(COLLIDER_TRIGGER_NO_COLLIDER, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
                            }
                        }

                        if ((trigger.TriggerType.ToString() == "OnEnterTrigger" || trigger.TriggerType.ToString() == "OnExitTrigger") && triggerScript.gameObject.layer != LayerMask.NameToLayer("MirrorReflection"))
                        {
                            var collides = true;

                            var triggerLayers = Helper.GetAllLayerNumbersFromMask(trigger.Layers);
                            for (var i = 0; i < triggerLayers.Length; i++)
                            {
                                var item = triggerLayers[i];

                                if (Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("MirrorReflection"), item))
                                {
                                    collides = false;
                                    break;
                                }
                            }

                            if (collides)
                            {
                                triggerWrongLayer.Add(triggerScript.gameObject);
                            }
                        }
                    }
                }
            }

            if (triggerWrongLayer.Count > 0)
            {
                var triggerWrongLayerGroup = new MessageGroup(TRIGGER_TRIGGER_WRONG_LAYER, TRIGGER_TRIGGER_WRONG_LAYER_COMBINED, TRIGGER_TRIGGER_WRONG_LAYER_INFO, MessageType.Warning);
                for (var i = 0; i < triggerWrongLayer.Count; i++)
                {
                    triggerWrongLayerGroup.AddSingleMessage(new SingleMessage(triggerWrongLayer[i].name).SetSelectObject(triggerWrongLayer[i].gameObject).SetAutoFix(SetObjectLayer(triggerWrongLayer[i].gameObject, "MirrorReflection")));
                }

                general.AddMessageGroup(triggerWrongLayerGroup.SetGroupAutoFix(SetObjectLayer(triggerWrongLayerGroup.GetSelectObjects(), "MirrorReflection")));
            }
#endif

            // Optimization Checks

            // Check for occlusion culling
            if (StaticOcclusionCulling.umbraDataSize > 0)
            {
                optimization.AddMessageGroup(new MessageGroup(BAKED_OCCLUSION_CULLING, MessageType.GoodFPS));

                var occlusionAreas = GameObject.FindObjectsOfType<OcclusionArea>();

                if (occlusionAreas.Length == 0)
                {
                    optimization.AddMessageGroup(new MessageGroup(NO_OCCLUSION_AREAS, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/class-OcclusionArea.html"));
                }
                else
                {
                    var disabledOcclusionAreasGroup = optimization.AddMessageGroup(new MessageGroup(DISABLED_OCCLUSION_AREA, DISABLED_OCCLUSION_AREA_COMBINED, DISABLED_OCCLUSION_AREA_INFO, MessageType.Warning));

                    foreach (var occlusionArea in occlusionAreas)
                    {
                        var so = new SerializedObject(occlusionArea);
                        var sp = so.FindProperty("m_IsViewVolume");

                        if (!sp.boolValue)
                        {
                            disabledOcclusionAreasGroup.AddSingleMessage(new SingleMessage(occlusionArea.name).SetSelectObject(occlusionArea.gameObject));
                        }
                    }
                }
            }
            else
            {
                optimization.AddMessageGroup(new MessageGroup(NO_OCCLUSION_CULLING, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/occlusion-culling-getting-started.html"));
            }

            if (occlusionCacheFiles > 0)
            {
                // Set the message type depending on how many files found
                var cacheWarningType = MessageType.Info;
                if (occlusionCacheFiles > 50000)
                {
                    cacheWarningType = MessageType.Error;
                }
                else if (occlusionCacheFiles > 5000)
                {
                    cacheWarningType = MessageType.Warning;
                }

                optimization.AddMessageGroup(new MessageGroup(OCCLUSION_CULLING_CACHE_WARNING, cacheWarningType).AddSingleMessage(new SingleMessage(occlusionCacheFiles.ToString()).SetAutoFix(ClearOcclusionCache(occlusionCacheFiles))));
            }

            // Check if there's any active cameras outputting to render textures
            var activeCameras = new List<GameObject>();
            var cameraCount = 0;
            var cameras = GameObject.FindObjectsOfType<Camera>();

            for (var i = 0; i < cameras.Length; i++)
            {
                if (!cameras[i].enabled || (!cameras[i].targetTexture || cameras[i].name == "VRCCam")) continue;

                cameraCount++;
                activeCameras.Add(cameras[i].gameObject);
            }

            if (cameraCount > 0)
            {
                var activeCamerasMessages = new MessageGroup(ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE, ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE_COMBINED, ACTIVE_CAMERA_OUTPUTTING_TO_RENDER_TEXTURE_INFO, MessageType.BadFPS);
                for (var i = 0; i < activeCameras.Count; i++)
                {
                    activeCamerasMessages.AddSingleMessage(new SingleMessage(activeCameras[i].name).SetSelectObject(activeCameras[i].gameObject));
                }

                optimization.AddMessageGroup(activeCamerasMessages);
            }

            // Get active mirrors in the world and complain about them
            var mirrors = FindObjectsOfType(typeof(VRC_MirrorReflection)) as VRC_MirrorReflection[];

            if (mirrors.Length > 0)
            {
                var activeCamerasMessage = new MessageGroup(MIRROR_ON_BY_DEFAULT, MIRROR_ON_BY_DEFAULT_COMBINED, MIRROR_ON_BY_DEFAULT_INFO, MessageType.BadFPS);
                for (var i = 0; i < mirrors.Length; i++)
                {
                    if (mirrors[i].enabled)
                    {
                        activeCamerasMessage.AddSingleMessage(new SingleMessage(mirrors[i].name).SetSelectObject(mirrors[i].gameObject));
                    }
                }

                optimization.AddMessageGroup(activeCamerasMessage);
            }

            // Lighting Checks

            switch (RenderSettings.ambientMode)
            {
                case AmbientMode.Custom:
                    lighting.AddMessageGroup(new MessageGroup(AMBIENT_MODE_SET_TO_CUSTOM, MessageType.Error).AddSingleMessage(new SingleMessage(SetAmbientMode(AmbientMode.Skybox))));
                    break;
                case AmbientMode.Flat:
                    lighting.AddMessageGroup(new MessageGroup(SINGLE_COLOR_ENVIRONMENT_LIGHTING, MessageType.Tips));
                    break;
            }

            if (Helper.GetBrightness(RenderSettings.ambientLight) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Flat) ||
                Helper.GetBrightness(RenderSettings.ambientSkyColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight) ||
                Helper.GetBrightness(RenderSettings.ambientEquatorColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight) ||
                Helper.GetBrightness(RenderSettings.ambientGroundColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight))
            {
                lighting.AddMessageGroup(new MessageGroup(DARK_ENVIRONMENT_LIGHTING, MessageType.Tips));
            }

            if (RenderSettings.defaultReflectionMode.Equals(DefaultReflectionMode.Custom) && !RenderSettings.customReflection)
            {
                lighting.AddMessageGroup(new MessageGroup(CUSTOM_ENVIRONMENT_REFLECTIONS_NULL, MessageType.Error).AddSingleMessage(new SingleMessage(SetEnviromentReflections(DefaultReflectionMode.Skybox))));
            }

            var bakedLighting = false;

#if BAKERY_INCLUDED
            var bakeryLights = new List<GameObject>();
            // TODO: Investigate whether or not these should be included
            // bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryDirectLight)) as BakeryDirectLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryPointLight)) as BakeryPointLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakerySkyLight)) as BakerySkyLight[], s => s.gameObject));

            var bakerySettings = ftRenderLightmap.FindRenderSettingsStorage();

            switch ((ftRenderLightmap.RenderDirMode) bakerySettings.renderSettingsRenderDirMode)
            {
                case ftRenderLightmap.RenderDirMode.RNM:
                case ftRenderLightmap.RenderDirMode.SH:
                    const string className = "Merlin.VRCBakeryAdapter";

                    if (Helper.GetTypeFromName(className) is null)
                    {
                        lighting.AddMessageGroup(new MessageGroup(SHRNM_DIRECTIONAL_MODE_BAKERY_ERROR, MessageType.Error).SetDocumentation("https://github.com/Merlin-san/VRC-Bakery-Adapter"));
                    }

                    break;
            }

            if (bakeryLights.Count > 0)
            {
                var notEditorOnly = new List<GameObject>();
                var unityLightOnBakeryLight = new List<GameObject>();

                bakedLighting = true;

                for (var i = 0; i < bakeryLights.Count; i++)
                {
                    if (!bakeryLights[i].CompareTag("EditorOnly"))
                    {
                        notEditorOnly.Add(bakeryLights[i]);
                    }

                    if (!bakeryLights[i].GetComponent<Light>()) continue;

                    var light = bakeryLights[i].GetComponent<Light>();
                    if (!light.bakingOutput.isBaked && light.enabled)
                    {
                        unityLightOnBakeryLight.Add(bakeryLights[i]);
                    }
                }

                if (notEditorOnly.Count > 0)
                {
                    var notEditorOnlyGroup = new MessageGroup(BAKERY_LIGHT_NOT_SET_EDITOR_ONLY, BAKERY_LIGHT_NOT_SET_EDITOR_ONLY_COMBINED, BAKERY_LIGHT_NOT_SET_EDITOR_ONLY_INFO, MessageType.Warning);
                    foreach (var item in notEditorOnly)
                    {
                        notEditorOnlyGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(SetGameObjectTag(item, "EditorOnly")).SetSelectObject(item));
                    }

                    lighting.AddMessageGroup(notEditorOnlyGroup.SetGroupAutoFix(SetGameObjectTag(notEditorOnly.ToArray(), "EditorOnly")));
                }

                if (unityLightOnBakeryLight.Count > 0)
                {
                    var unityLightGroup = new MessageGroup(BAKERY_LIGHT_UNITY_LIGHT, BAKERY_LIGHT_UNITY_LIGHT_COMBINED, BAKERY_LIGHT_UNITY_LIGHT_INFO, MessageType.Warning);
                    foreach (var item in unityLightOnBakeryLight)
                    {
                        unityLightGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(DisableComponent(item.GetComponent<Light>())).SetSelectObject(item));
                    }

                    lighting.AddMessageGroup(unityLightGroup.SetGroupAutoFix(DisableComponent(Array.ConvertAll(unityLightOnBakeryLight.ToArray(), s => s.GetComponent<Light>()))));
                }
            }
#endif

            // Get lights in scene
            var lights = FindObjectsOfType<Light>();

            var nonBakedLights = new List<GameObject>();

            // Go trough the lights to check if the scene contains lights set to be baked
            for (var i = 0; i < lights.Length; i++)
            {
                // Skip checking realtime lights
                if (lights[i].lightmapBakeType == LightmapBakeType.Realtime) continue;

                bakedLighting = true;

                if (!lights[i].bakingOutput.isBaked && lights[i].GetComponent<Light>().enabled)
                {
                    nonBakedLights.Add(lights[i].gameObject);
                }
            }

            if (LightmapSettings.lightmaps.Length > 0)
            {
                bakedLighting = true;

                if (Helper.BuildPlatform() == RuntimePlatform.Android)
                {
                    var lightmaps = LightmapSettings.lightmaps;

                    var androidCompressionGroup = lighting.AddMessageGroup(new MessageGroup(QUEST_LIGHTMAP_COMPRESSION_OVERRIDE, QUEST_LIGHTMAP_COMPRESSION_OVERRIDE_COMBINED, QUEST_LIGHTMAP_COMPRESSION_OVERRIDE_INFO, MessageType.Tips));

                    var lightmapTextureImporters = new List<TextureImporter>();

                    for (var i = 0; i < lightmaps.Length; i++)
                    {
                        Object lightmap = lightmaps[i].lightmapColor;

                        var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lightmaps[i].lightmapColor)) as TextureImporter;

                        var platformSettings = textureImporter.GetPlatformTextureSettings("Android");

                        if (!platformSettings.overridden)
                        {
                            lightmapTextureImporters.Add(textureImporter);

                            androidCompressionGroup.AddSingleMessage(new SingleMessage(lightmap.name).SetAssetPath(textureImporter.assetPath).SetAutoFix(SetLightmapOverrideForQuest(textureImporter, lightmap.name)));
                        }
                    }

                    if (androidCompressionGroup.GetTotalCount() > 0)
                    {
                        androidCompressionGroup.SetGroupAutoFix(SetLightmapOverrideForQuest(lightmapTextureImporters.ToArray()));
                    }
                }
            }

            var probes = LightmapSettings.lightProbes;

            // If the scene has baked lights complain about stuff important to baked lighting missing
            if (bakedLighting)
            {
                // Count lightmaps and suggest to use bigger lightmaps if needed
                var lightMapSize = LightmapEditorSettings.maxAtlasSize;
                if (lightMapSize != 4096 && LightmapSettings.lightmaps.Length > 1 && !LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU))
                {
                    if (LightmapSettings.lightmaps[0] != null)
                    {
                        if (LightmapSettings.lightmaps[0].lightmapColor.height != 4096)
                        {
                            lighting.AddMessageGroup(new MessageGroup(CONSIDER_LARGER_LIGHTMAPS, MessageType.Tips).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(4096))));
                        }
                    }
                }

                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU) && lightMapSize == 4096 && SystemInfo.graphicsMemorySize < 12000)
                {
                    lighting.AddMessageGroup(new MessageGroup(CONSIDER_SMALLER_LIGHTMAPS, MessageType.Warning).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(2048))));
                }

                // Count how many light probes the scene has
                long probeCounter = 0;
                long bakedProbes = probes != null ? probes.count : 0;

                var lightprobegroups = GameObject.FindObjectsOfType<LightProbeGroup>();

                var overlappingLightProbesGroup = new MessageGroup(OVERLAPPING_LIGHT_PROBES, OVERLAPPING_LIGHT_PROBES_COMBINED, OVERLAPPING_LIGHT_PROBES_INFO, MessageType.Info);

                for (var i = 0; i < lightprobegroups.Length; i++)
                {
                    if (lightprobegroups[i].probePositions.GroupBy(p => p).Any(g => g.Count() > 1))
                    {
                        overlappingLightProbesGroup.AddSingleMessage(new SingleMessage(lightprobegroups[i].name, (lightprobegroups[i].probePositions.Length - lightprobegroups[i].probePositions.Distinct().ToArray().Length).ToString()).SetSelectObject(lightprobegroups[i].gameObject).SetAutoFix(RemoveOverlappingLightprobes(lightprobegroups[i])));
                    }

                    probeCounter += lightprobegroups[i].probePositions.Length;
                }

                if (probeCounter > 0)
                {
                    if (probeCounter - bakedProbes < 0)
                    {
                        lighting.AddMessageGroup(new MessageGroup(LIGHT_PROBES_REMOVED_NOT_RE_BAKED, MessageType.Warning).AddSingleMessage(new SingleMessage(bakedProbes.ToString(), probeCounter.ToString())));
                    }
                    else
                    {
                        if (bakedProbes - (0.9 * probeCounter) < 0)
                        {
                            lighting.AddMessageGroup(new MessageGroup(LIGHT_PROBE_COUNT_NOT_BAKED, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"), (probeCounter - bakedProbes).ToString("n0"))));
                        }
                        else
                        {
                            lighting.AddMessageGroup(new MessageGroup(LIGHT_PROBE_COUNT, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"))));
                        }
                    }
                }

                if (overlappingLightProbesGroup.GetTotalCount() > 0)
                {
                    if (overlappingLightProbesGroup.GetTotalCount() > 1)
                    {
                        overlappingLightProbesGroup.SetGroupAutoFix(RemoveOverlappingLightprobes(lightprobegroups));
                    }

                    lighting.AddMessageGroup(overlappingLightProbesGroup);
                }

                // Since the scene has baked lights complain if there's no lightprobes
                else if (probes == null && probeCounter == 0)
                {
                    lighting.AddMessageGroup(new MessageGroup(NO_LIGHT_PROBES, MessageType.Info).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightProbes.html"));
                }

                // Check lighting data asset size if it exists
                if (Lightmapping.lightingDataAsset != null)
                {
                    var pathTo = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    var length = new FileInfo(pathTo).Length;
                    lighting.AddMessageGroup(new MessageGroup(LIGHTING_DATA_ASSET_INFO, MessageType.Info).AddSingleMessage(new SingleMessage((length / 1024.0f / 1024.0f).ToString("F2"))));
                }

#if !BAKERY_INCLUDED
                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.Enlighten))
                {
                    lighting.AddMessageGroup(new MessageGroup(SWITCH_TO_PROGRESSIVE, MessageType.Tips));
                }
#endif

                if (nonBakedLights.Count != 0)
                {
                    var nonBakedLightsGroup = new MessageGroup(NON_BAKED_BAKED_LIGHT, NON_BAKED_BAKED_LIGHT_COMBINED, NON_BAKED_BAKED_LIGHT_INFO, MessageType.Warning);
                    for (var i = 0; i < nonBakedLights.Count; i++)
                    {
                        nonBakedLightsGroup.AddSingleMessage(new SingleMessage(nonBakedLights[i].name).SetSelectObject(nonBakedLights[i].gameObject));
                    }

                    lighting.AddMessageGroup(nonBakedLightsGroup);
                }
            }
            else
            {
#if UNITY_ANDROID
                lighting.AddMessageGroup(new MessageGroup(QUEST_BAKED_LIGHTING_WARNING, MessageType.BadFPS).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/Lightmapping.html"));
#else
                lighting.AddMessageGroup(new MessageGroup(LIGHTS_NOT_BAKED, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/Lightmapping.html"));
#endif
            }

            // ReflectionProbes
            var reflectionprobes = FindObjectsOfType<ReflectionProbe>();
            var unbakedprobes = new List<GameObject>();
            var reflectionProbeCount = reflectionprobes.Count();
            for (var i = 0; i < reflectionprobes.Length; i++)
            {
                if (!reflectionprobes[i].bakedTexture && reflectionprobes[i].mode == ReflectionProbeMode.Baked)
                {
                    unbakedprobes.Add(reflectionprobes[i].gameObject);
                }
            }

            if (reflectionProbeCount == 0)
            {
                lighting.AddMessageGroup(new MessageGroup(NO_REFLECTION_PROBES, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/class-ReflectionProbe.html"));
            }
            else if (reflectionProbeCount > 0)
            {
                lighting.AddMessageGroup(new MessageGroup(REFLECTION_PROBE_COUNT_TEXT, MessageType.Info).AddSingleMessage(new SingleMessage(reflectionProbeCount.ToString())));

                if (unbakedprobes.Count > 0)
                {
                    var probesUnbakedGroup = new MessageGroup(REFLECTION_PROBES_SOME_UNBAKED, REFLECTION_PROBES_SOME_UNBAKED_COMBINED, MessageType.Warning);

                    foreach (var item in unbakedprobes)
                    {
                        probesUnbakedGroup.AddSingleMessage(new SingleMessage(item.name).SetSelectObject(item));
                    }

                    lighting.AddMessageGroup(probesUnbakedGroup);
                }
            }

            // Post Processing Checks

#if UNITY_POST_PROCESSING_STACK_V2
            var postProcessVolumes = FindObjectsOfType(typeof(PostProcessVolume)) as PostProcessVolume[];
            PostProcessLayer mainPostProcessLayer = null;

            // Attempt to find the main post process layer
            if (sceneDescriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)))
            {
                mainPostProcessLayer = sceneDescriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
            }
            else
            {
                if (Camera.main != null)
                {
                    if (Camera.main.gameObject.GetComponent(typeof(PostProcessLayer)))
                    {
                        mainPostProcessLayer = Camera.main.gameObject.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                    }
                }
            }

            // Check if the post processing layer has resources properly set
            if (mainPostProcessLayer)
            {
                var resourcesInfo = typeof(PostProcessLayer).GetField("m_Resources", BindingFlags.NonPublic | BindingFlags.Instance);

                var postProcessResources = resourcesInfo.GetValue(mainPostProcessLayer) as PostProcessResources;

                if (postProcessResources is null)
                {
                    var singleMessage = new SingleMessage(mainPostProcessLayer.gameObject.name).SetSelectObject(mainPostProcessLayer.gameObject);

                    postProcessing.AddMessageGroup(new MessageGroup("The Post Process Layer on \"{0}\" does not have its resources field set properly. This causes post-processing to error out. This can be fixed by recreating the Post Processing Layer on the GameObject.", MessageType.Warning).AddSingleMessage(singleMessage));

                    var resources = (PostProcessResources) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("d82512f9c8e5d4a4d938b575d47f88d4"), typeof(PostProcessResources));

                    if (resources != null) singleMessage.SetAutoFix(SetPostProcessingLayerResources(mainPostProcessLayer, resources));
                }
            }

            // If post processing is imported but no setup isn't detected show a message
            if (postProcessVolumes.Length == 0 && mainPostProcessLayer is null)
            {
                postProcessing.AddMessageGroup(new MessageGroup(POST_PROCESSING_IMPORTED_BUT_NOT_SETUP, MessageType.Info));
            }
            else
            {
                // Check the scene view for post processing effects being off
                var sceneViewState = SceneView.lastActiveSceneView.sceneViewState;
                if (!sceneViewState.showImageEffects)
                {
                    postProcessing.AddMessageGroup(new MessageGroup(POST_PROCESSING_DISABLED_IN_SCENE_VIEW, MessageType.Info).SetGroupAutoFix(SetPostProcessingInScene(sceneViewState, true)));
                }

                // Start by checking if reference camera has been set in the Scene Descriptor
                if (!sceneDescriptor.ReferenceCamera)
                {
                    var noReferenceCameraMessage = new SingleMessage(sceneDescriptor.gameObject);

                    if (Camera.main && Camera.main.GetComponent<PostProcessLayer>())
                    {
                        noReferenceCameraMessage.SetAutoFix(SetReferenceCamera(sceneDescriptor, Camera.main));
                    }

                    postProcessing.AddMessageGroup(new MessageGroup(NO_REFERENCE_CAMERA_SET, MessageType.Warning).AddSingleMessage(noReferenceCameraMessage));
                }
                else
                {
                    // Check for post process volumes in the scene
                    if (postProcessVolumes.Length == 0)
                    {
                        postProcessing.AddMessageGroup(new MessageGroup(NO_POST_PROCESSING_VOLUMES, MessageType.Info));
                    }
                    else
                    {
                        var postprocessLayer = sceneDescriptor.ReferenceCamera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        if (postprocessLayer == null)
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(REFERENCE_CAMERA_NO_POST_PROCESSING_LAYER, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                        }

                        var volumeLayer = postprocessLayer.volumeLayer;
                        if (volumeLayer == 0)
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(VOLUME_BLENDING_LAYER_NOT_SET, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.gameObject)));
                        }

                        // Check for usage of reserved layers since they break post processing
                        var numbersFromMask = Helper.GetAllLayerNumbersFromMask(volumeLayer);
                        if (numbersFromMask.Contains(19) | numbersFromMask.Contains(20) | numbersFromMask.Contains(21))
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(POST_PROCESS_LAYER_USING_RESERVED_LAYER, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject.name).SetSelectObject(postprocessLayer.gameObject)));
                        }

                        foreach (var postProcessVolume in postProcessVolumes)
                        {
                            // Check if the layer matches the cameras post processing layer
                            if (volumeLayer != 0 && (postprocessLayer.volumeLayer != (postprocessLayer.volumeLayer | (1 << postProcessVolume.gameObject.layer))))
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(VOLUME_ON_WRONG_LAYER, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject.name, Helper.GetAllLayersFromMask(postprocessLayer.volumeLayer)).SetSelectObject(postProcessVolume.gameObject)));
                            }

                            // Check if the volume has a profile set
                            if (!postProcessVolume.profile && !postProcessVolume.sharedProfile)
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(NO_PROFILE_SET, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject.name)));
                                continue;
                            }

                            // Check if the collider is either global or has a collider on it
                            if (!postProcessVolume.isGlobal && !postProcessVolume.GetComponent<Collider>())
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(POST_PROCESSING_VOLUME_NOT_GLOBAL_NO_COLLIDER, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.name).SetSelectObject(postProcessVolume.gameObject)));
                            }
                            else
                            {
                                // Go trough the profile settings and see if any bad one's are used
                                PostProcessProfile postProcessProfile;

                                if (postProcessVolume.profile)
                                {
                                    postProcessProfile = postProcessVolume.profile;
                                }
                                else
                                {
                                    postProcessProfile = postProcessVolume.sharedProfile;
                                }

                                if (postProcessProfile.GetSetting<ColorGrading>() && postProcessProfile.GetSetting<ColorGrading>().enabled && postProcessProfile.GetSetting<ColorGrading>().active)
                                {
                                    if (postProcessProfile.GetSetting<ColorGrading>().tonemapper.value == Tonemapper.None)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(DONT_USE_NONE_FOR_TONEMAPPING, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }
                                }

                                if (postProcessProfile.GetSetting<Bloom>() && postProcessProfile.GetSetting<Bloom>().enabled && postProcessProfile.GetSetting<Bloom>().active)
                                {
                                    var bloom = postProcessProfile.GetSetting<Bloom>();

                                    if (bloom.intensity.overrideState && bloom.intensity.value > 0.3f)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(TOO_HIGH_BLOOM_INTENSITY, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }

                                    if (bloom.threshold.overrideState && bloom.threshold.value > 1f)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(TOO_HIGH_BLOOM_THRESHOLD, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }

                                    if (bloom.dirtTexture.overrideState && bloom.dirtTexture.value || bloom.dirtIntensity.overrideState && bloom.dirtIntensity.value > 0)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(NO_BLOOM_DIRT_IN_VR, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.BloomDirt)).SetSelectObject(postProcessVolume.gameObject)));
                                    }
                                }

                                if (postProcessProfile.GetSetting<AmbientOcclusion>() && postProcessProfile.GetSetting<AmbientOcclusion>().enabled && postProcessProfile.GetSetting<AmbientOcclusion>().active)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(NO_AMBIENT_OCCLUSION, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.AmbientOcclusion)).SetSelectObject(postProcessVolume.gameObject)));
                                }

                                if (postProcessVolume.isGlobal && postProcessProfile.GetSetting<DepthOfField>() && postProcessProfile.GetSetting<DepthOfField>().enabled && postProcessProfile.GetSetting<DepthOfField>().active)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(DEPTH_OF_FIELD_WARNING, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                }

                                if (postProcessProfile.GetSetting<ScreenSpaceReflections>() && postProcessProfile.GetSetting<ScreenSpaceReflections>().enabled && postProcessProfile.GetSetting<ScreenSpaceReflections>().active)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(SCREEN_SPACE_REFLECTIONS_WARNING, MessageType.Warning).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.ScreenSpaceReflections)).SetSelectObject(postProcessVolume.gameObject)));
                                }

                                if (postProcessProfile.GetSetting<Vignette>() && postProcessProfile.GetSetting<Vignette>().enabled && postProcessProfile.GetSetting<Vignette>().active)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(VIGNETTE_WARNING, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                }
                            }
                        }
                    }
                }

                if (!postProcessing.HasMessages())
                {
                    postProcessing.AddMessageGroup(new MessageGroup(NO_PROBLEMS_FOUND_IN_PP, MessageType.Info));
                }
            }
#else
            postProcessing.AddMessageGroup(new MessageGroup(NO_POST_PROCESSING_IMPORTED, MessageType.Info));
#endif

            // GameObject checks

            var importers = new List<ModelImporter>();

            var unCrunchedTextures = new List<Texture>();
            var badShaders = 0;
            var textureCount = 0;

            var missingShaders = new List<Material>();

            var checkedMaterials = new List<Material>();
            var checkedShaders = new List<Shader>();
            var selectablesNotNone = new List<Selectable>();

            var mirrorsDefaultLayers = optimization.AddMessageGroup(new MessageGroup(MIRROR_WITH_DEFAULT_LAYERS, MIRROR_WITH_DEFAULT_LAYERS_COMBINED, MIRROR_WITH_DEFAULT_LAYERS_INFO, MessageType.Tips));
            var legacyBlendShapeIssues = general.AddMessageGroup(new MessageGroup(LEGACY_BLEND_SHAPE_ISSUES, LEGACY_BLEND_SHAPE_ISSUES_COMBINED, LEGACY_BLEND_SHAPE_ISSUES_INFO, MessageType.Warning));
            var grabPassShaders = general.AddMessageGroup(new MessageGroup(MATERIAL_WITH_GRAB_PASS_SHADER, MATERIAL_WITH_GRAB_PASS_SHADER_COMBINED, Helper.BuildPlatform() == RuntimePlatform.WindowsPlayer ? MATERIAL_WITH_GRAB_PASS_SHADER_INFO_PC : MATERIAL_WITH_GRAB_PASS_SHADER_INFO_QUEST, Helper.BuildPlatform() == RuntimePlatform.Android ? MessageType.Error : MessageType.Info));
            var disabledPortals = general.AddMessageGroup(new MessageGroup(DISABLED_PORTALS_WARNING, DISABLED_PORTALS_WARNING_COMBINED, DISABLED_PORTALS_WARNING_INFO, MessageType.Warning));
            var materialWithNonWhitelistedShader = general.AddMessageGroup(new MessageGroup(MATERIAL_WITH_NON_WHITELISTED_SHADER, MATERIAL_WITH_NON_WHITELISTED_SHADER_COMBINED, MATERIAL_WITH_NON_WHITELISTED_SHADER_INFO, MessageType.Warning).SetCombinedSelectionDisabled(true));
            var uiElementNavigation = general.AddMessageGroup(new MessageGroup(UI_ELEMENT_WITH_NAVIGATION_NOT_NONE, UI_ELEMENT_WITH_NAVIGATION_NOT_NONE_COMBINED, UI_ELEMENT_WITH_NAVIGATION_NOT_NONE_INFO, MessageType.Tips));

            var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (var i = 0; i < allGameObjects.Length; i++)
            {
                var gameObject = allGameObjects[i] as GameObject;

                if (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject))
                    continue;

                if (gameObject.GetComponent<Renderer>())
                {
                    var renderer = gameObject.GetComponent<Renderer>();

                    // If baked lighting in the scene check for lightmap uvs
                    if (bakedLighting)
                    {
                        if (GameObjectUtility.AreStaticEditorFlagsSet(gameObject, StaticEditorFlags.LightmapStatic) && gameObject.GetComponent<MeshRenderer>())
                        {
                            var meshFilter = gameObject.GetComponent<MeshFilter>();

                            if (meshFilter == null)
                            {
                                continue;
                            }

                            var sharedMesh = meshFilter.sharedMesh;

                            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sharedMesh)) != null)
                            {
                                var modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sharedMesh)) as ModelImporter;

                                if (!importers.Contains(modelImporter))
                                {
                                    if (modelImporter != null)
                                    {
                                        SerializedObject so = new SerializedObject(renderer);

                                        if (!modelImporter.generateSecondaryUV && sharedMesh.uv2.Length == 0 && so.FindProperty("m_ScaleInLightmap").floatValue != 0)
                                        {
                                            importers.Add(modelImporter);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (gameObject.GetComponent<VRC_MirrorReflection>())
                    {
                        var mirrorMask = gameObject.GetComponent<VRC_MirrorReflection>().m_ReflectLayers;

                        if (mirrorMask.value == -1025)
                        {
                            mirrorsDefaultLayers.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                        }
                    }

                    if (gameObject.GetComponent<SkinnedMeshRenderer>())
                    {
                        var mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;

                        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mesh)) != null)
                        {
                            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mesh)) as ModelImporter;

                            if (importer != null)
                            {
                                if (mesh.blendShapeCount > 0 && importer.importBlendShapeNormals == ModelImporterNormals.Calculate && !ModelImporterUtil.GetLegacyBlendShapeNormals(importer))
                                {
                                    legacyBlendShapeIssues.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(mesh)), EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(mesh))).SetAssetPath(importer.assetPath).SetAutoFix(SetLegacyBlendShapeNormals(importer)));
                                }
                            }
                        }
                    }

                    // Check materials for problems
                    for (var l = 0; l < renderer.sharedMaterials.Length; l++)
                    {
                        var material = renderer.sharedMaterials[l];

                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        var shader = material.shader;

                        if (Helper.BuildPlatform() is RuntimePlatform.Android && !VRCSDK2.Validation.WorldValidation.ShaderWhiteList.Contains(shader.name))
                        {
                            var singleMessage = new SingleMessage(material.name, shader.name);

                            if (AssetDatabase.GetAssetPath(material).EndsWith(".mat"))
                            {
                                singleMessage.SetAssetPath(AssetDatabase.GetAssetPath(material));
                            }
                            else
                            {
                                singleMessage.SetSelectObject(gameObject);
                            }

                            materialWithNonWhitelistedShader.AddSingleMessage(singleMessage);
                        }

                        if (!checkedShaders.Contains(shader) && AssetDatabase.GetAssetPath(shader) != null)
                        {
                            var assetPath = AssetDatabase.GetAssetPath(shader);

                            if (File.Exists(assetPath))
                            {
                                // Read shader file to string
                                var word = File.ReadAllText(assetPath);

                                // Strip comments
                                word = Regex.Replace(word, "(\\/\\/.*)|(\\/\\*)(.*)(\\*\\/)", "");

                                // Match for GrabPass
                                if (Regex.IsMatch(word, "GrabPass\\s*{"))
                                {
                                    grabPassShaders.AddSingleMessage(new SingleMessage(material.name, shader.name).SetAssetPath(AssetDatabase.GetAssetPath(material)));
                                }
                            }

                            checkedShaders.Add(shader);
                        }

                        if (shader.name == "Hidden/InternalErrorShader" && !missingShaders.Contains(material))
                            missingShaders.Add(material);

                        if (shader.name.StartsWith(".poiyomi") || shader.name.StartsWith("poiyomi") || shader.name.StartsWith("arktoon") || shader.name.StartsWith("Cubedparadox") || shader.name.StartsWith("Silent's Cel Shading") || shader.name.StartsWith("Xiexe"))
                            badShaders++;

                        for (var j = 0; j < ShaderUtil.GetPropertyCount(shader); j++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                var texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, j));

                                if (AssetDatabase.GetAssetPath(texture) != "" && !unCrunchedTextures.Contains(texture))
                                {
                                    var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

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
                }

                if (gameObject.GetComponent<Selectable>())
                {
                    var selectable = gameObject.GetComponent<Selectable>();

                    if (selectable.navigation.mode != Navigation.Mode.None)
                    {
                        uiElementNavigation.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject).SetAutoFix(SetSelectableNavigationMode(selectable, Navigation.Mode.None)));

                        selectablesNotNone.Add(selectable);
                    }
                }

                if (gameObject.activeInHierarchy == false)
                {
                    if (gameObject.GetComponent<VRC_PortalMarker>())
                    {
                        disabledPortals.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                    }
                }
            }

            if (selectablesNotNone.Count > 1)
            {
                uiElementNavigation.SetGroupAutoFix(SetSelectableNavigationMode(selectablesNotNone.ToArray(), Navigation.Mode.None));
            }

            // If more than 10% of shaders used in scene are toon shaders to leave room for people using them for avatar displays
            if (checkedMaterials.Count > 0)
            {
                if (badShaders / checkedMaterials.Count * 100 > 10)
                {
                    optimization.AddMessageGroup(new MessageGroup(NO_TOON_SHADERS, MessageType.Warning));
                }
            }

            // Suggest to crunch textures if there are any uncrunched textures found
            if (textureCount > 0)
            {
                var percent = (int) ((float) unCrunchedTextures.Count / (float) textureCount * 100f);
                if (percent > 20)
                {
                    optimization.AddMessageGroup(new MessageGroup(NON_CRUNCHED_TEXTURES, MessageType.Tips).AddSingleMessage(new SingleMessage(percent.ToString())));
                }
            }


            var modelsCount = importers.Count;
            if (modelsCount > 0)
            {
                var noUVGroup = new MessageGroup(NO_LIGHTMAP_UV, NO_LIGHTMAP_UV_COMBINED, NO_LIGHTMAP_UV_INFO, MessageType.Warning);
                for (var i = 0; i < modelsCount; i++)
                {
                    var modelImporter = importers[i];

                    noUVGroup.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(modelImporter))).SetAutoFix(SetGenerateLightmapUV(modelImporter)).SetAssetPath(modelImporter.assetPath));
                }

                lighting.AddMessageGroup(noUVGroup.SetGroupAutoFix(SetGenerateLightmapUV(importers)).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html"));
            }

            var missingShadersCount = missingShaders.Count;
            if (missingShadersCount > 0)
            {
                var missingShadersGroup = new MessageGroup(MISSING_SHADER_WARNING, MISSING_SHADER_WARNING_COMBINED, MISSING_SHADER_WARNING_INFO, MessageType.Error);
                for (var i = 0; i < missingShaders.Count; i++)
                {
                    missingShadersGroup.AddSingleMessage(new SingleMessage(missingShaders[i].name).SetAssetPath(AssetDatabase.GetAssetPath(missingShaders[i])).SetAutoFix(ChangeShader(missingShaders[i], "Standard")));
                }

                general.AddMessageGroup(missingShadersGroup.SetGroupAutoFix(ChangeShader(missingShaders.ToArray(), "Standard")));
            }
        }

        private void OnFocus()
        {
            recheck = true;
        }

        private const string LAST_BUILD = "Library/LastBuild.buildreport";

        private const string BUILD_REPORT_DIR = "Assets/_LastBuild/";

        private const string LAST_BUILD_REPORT_PATH = "Assets/_LastBuild/LastBuild.buildreport";
        private const string WINDOWS_BUILD_REPORT_PATH = "Assets/_LastBuild/LastWindowsBuild.buildreport";
        private const string QUEST_BUILD_REPORT_PATH = "Assets/_LastBuild/LastQuestBuild.buildreport";

        [SerializeField] private BuildReport buildReportWindows;
        [SerializeField] private BuildReport buildReportQuest;

        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        private BuildReportTreeView m_TreeView;
        private SearchField m_SearchField;

        private void RefreshBuild()
        {
            if (!Directory.Exists(BUILD_REPORT_DIR))
                Directory.CreateDirectory(BUILD_REPORT_DIR);

            if (File.Exists(LAST_BUILD) && (!File.Exists(LAST_BUILD_REPORT_PATH) || File.GetLastWriteTime(LAST_BUILD) > File.GetLastWriteTime(LAST_BUILD_REPORT_PATH)))
            {
                File.Copy(LAST_BUILD, LAST_BUILD_REPORT_PATH, true);
                AssetDatabase.ImportAsset(LAST_BUILD_REPORT_PATH);
            }

            if (File.Exists(LAST_BUILD_REPORT_PATH))
            {
                switch (AssetDatabase.LoadAssetAtPath<BuildReport>(LAST_BUILD_REPORT_PATH).summary.platform)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        if (File.GetLastWriteTime(LAST_BUILD_REPORT_PATH) > File.GetLastWriteTime(WINDOWS_BUILD_REPORT_PATH))
                        {
                            AssetDatabase.CopyAsset(LAST_BUILD_REPORT_PATH, WINDOWS_BUILD_REPORT_PATH);
                            buildReportWindows = (BuildReport) AssetDatabase.LoadAssetAtPath(WINDOWS_BUILD_REPORT_PATH, typeof(BuildReport));
                        }

                        break;
                    case BuildTarget.Android:
                        if (File.GetLastWriteTime(LAST_BUILD_REPORT_PATH) > File.GetLastWriteTime(QUEST_BUILD_REPORT_PATH))
                        {
                            AssetDatabase.CopyAsset(LAST_BUILD_REPORT_PATH, QUEST_BUILD_REPORT_PATH);
                            buildReportQuest = (BuildReport) AssetDatabase.LoadAssetAtPath(QUEST_BUILD_REPORT_PATH, typeof(BuildReport));
                        }

                        break;
                }
            }

            if (buildReportWindows is null && File.Exists(WINDOWS_BUILD_REPORT_PATH))
            {
                buildReportWindows = (BuildReport) AssetDatabase.LoadAssetAtPath(WINDOWS_BUILD_REPORT_PATH, typeof(BuildReport));
            }

            if (buildReportQuest is null && File.Exists(QUEST_BUILD_REPORT_PATH))
            {
                buildReportQuest = (BuildReport) AssetDatabase.LoadAssetAtPath(QUEST_BUILD_REPORT_PATH, typeof(BuildReport));
            }

            if (!m_TreeView.HasReport())
            {
                if (buildReportWindows != null)
                {
                    m_TreeView.SetReport(buildReportWindows);
                }
                else if (buildReportQuest != null)
                {
                    m_TreeView.SetReport(buildReportWindows);
                }
            }
        }

        private static void DrawBuildSummary(BuildReport report)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (report != null)
            {
                GUILayout.Label("<b>Build size:</b> " + EditorUtility.FormatBytes((long) report.summary.totalSize), Styles.LabelRichText);

                GUILayout.Label("<b>Build done:</b> " + report.summary.buildEndedAt.ToLocalTime(), Styles.LabelRichText);

                GUILayout.Label("<b>Errors during build:</b> " + report.summary.totalErrors, Styles.LabelRichText);

                GUILayout.Label("<b>Warnings during build:</b> " + report.summary.totalWarnings, Styles.LabelRichText);

                GUILayout.Label("<b>Build result:</b> " + report.summary.result, Styles.LabelRichText);
            }

            GUILayout.EndVertical();
        }

        [NonSerialized] private bool initDone;

        [SerializeField] private MessageCategoryList masterList;

        private MessageCategory general;
        private MessageCategory optimization;
        private MessageCategory lighting;
        private MessageCategory postProcessing;

        private void InitWhenNeeded()
        {
            if (!initDone)
            {
                if (masterList is null)
                    masterList = new MessageCategoryList();

                general = masterList.AddOrGetCategory("General");

                optimization = masterList.AddOrGetCategory("Optimization");

                lighting = masterList.AddOrGetCategory("Lighting");

                postProcessing = masterList.AddOrGetCategory("Post Processing");

                var firstInit = m_MultiColumnHeaderState == null;
                var headerState = BuildReportTreeView.CreateDefaultMultiColumnHeaderState(EditorGUIUtility.currentViewWidth - 121);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                if (m_TreeViewState is null)
                {
                    m_TreeViewState = new TreeViewState();
                }

                if (buildReportWindows is null && File.Exists(WINDOWS_BUILD_REPORT_PATH))
                {
                    buildReportWindows = (BuildReport) AssetDatabase.LoadAssetAtPath(WINDOWS_BUILD_REPORT_PATH, typeof(BuildReport));
                }

                if (buildReportQuest is null && File.Exists(QUEST_BUILD_REPORT_PATH))
                {
                    buildReportQuest = (BuildReport) AssetDatabase.LoadAssetAtPath(QUEST_BUILD_REPORT_PATH, typeof(BuildReport));
                }

                var report = buildReportWindows != null ? buildReportWindows : buildReportQuest;

                m_TreeView = new BuildReportTreeView(m_TreeViewState, multiColumnHeader, report);
                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                initDone = true;
            }
        }

        private static readonly Stopwatch CheckTime = new Stopwatch();

        private void Refresh()
        {
            if (recheck && autoRecheck)
            {
                RefreshBuild();

                // Check for bloat in occlusion cache
                if (occlusionCacheFiles == 0 && Directory.Exists("Library/Occlusion/"))
                {
                    Task.Run(CountOcclusionCacheFiles);
                }

                CheckTime.Restart();
                CheckScene();
                CheckTime.Stop();

                if (CheckTime.ElapsedMilliseconds > 2000)
                {
                    autoRecheck = false;
                }

#if VRWT_BENCHMARK
                Debug.Log("Scene checked in: " + CheckTime.ElapsedMilliseconds + " ms.");
#endif

                recheck = false;
            }
        }

        private enum BuildReportType
        {
            Windows = 0,
            Quest = 1
        }

        private readonly string[] buildReportToolbar = {"Windows", "Quest"};

        [SerializeField] private int selectedBuildReport;
        [SerializeField] private bool overallStatsFoldout;
        [SerializeField] private bool buildReportMessagesFoldout;

        private void OnGUI()
        {
            InitWhenNeeded();
            Refresh();

            GUILayout.BeginHorizontal();

            if (buildReportWindows)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Last found Windows build:", EditorStyles.boldLabel);

                DrawBuildSummary(buildReportWindows);

                GUILayout.EndVertical();
            }

            if (buildReportQuest)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Last found Quest build:", EditorStyles.boldLabel);

                DrawBuildSummary(buildReportQuest);

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            tab = GUILayout.Toolbar(tab, new[] {"Messages", "Build Report"});

            switch (tab)
            {
                case 0:
                    if (!autoRecheck && GUILayout.Button("Refresh"))
                    {
                        recheck = true;
                        autoRecheck = true;
                    }

                    masterList.DrawTabSelector();

                    EditorGUILayout.BeginVertical();
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    masterList.DrawMessages();

                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                    break;
                case 1:
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);

                    if (buildReportWindows != null && buildReportQuest != null)
                    {
                        EditorGUI.BeginChangeCheck();

                        selectedBuildReport = GUILayout.Toolbar(selectedBuildReport, buildReportToolbar, EditorStyles.toolbarButton);

                        if (EditorGUI.EndChangeCheck())
                        {
                            switch ((BuildReportType) selectedBuildReport)
                            {
                                case BuildReportType.Windows:
                                    m_TreeView.SetReport(buildReportWindows);
                                    break;
                                case BuildReportType.Quest:
                                    m_TreeView.SetReport(buildReportQuest);
                                    break;
                            }
                        }

                        GUILayout.Space(10);
                    }

                    overallStatsFoldout = GUILayout.Toggle(overallStatsFoldout, "Stats", EditorStyles.toolbarButton);

                    buildReportMessagesFoldout = GUILayout.Toggle(buildReportMessagesFoldout, "Messages", EditorStyles.toolbarButton);

                    GUILayout.Space(10);

                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                    {
                        if (m_TreeView.HasReport())
                        {
                            m_TreeView.Reload();
                        }
                        else
                        {
                            if (buildReportWindows != null)
                            {
                                m_TreeView.SetReport(buildReportWindows);
                            }
                            else if (buildReportQuest != null)
                            {
                                m_TreeView.SetReport(buildReportQuest);
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();

                    m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();

                    if (buildReportMessagesFoldout)
                    {
                        m_TreeView.DrawMessages();
                    }

                    if (overallStatsFoldout)
                    {
                        m_TreeView.DrawOverallStats();
                    }

                    var treeViewRect = EditorGUILayout.BeginVertical();

                    if (m_TreeView.HasReport())
                    {
                        m_TreeView.OnGUI(treeViewRect);
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndVertical();
                    break;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
#endif