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
using System.Globalization;
using UnityEngine.Assertions;
using UnityEngine.Networking;

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

        [Serializable]
        private class MessageGroup : IEquatable<MessageGroup>
        {
            public readonly string Message;
            public readonly string CombinedMessage;
            public readonly string AdditionalInfo;

            private bool? disableCombinedSelection = null;
            private int? objectCount = null;

            public readonly MessageType MessageType;

            public string Documentation;

            public Action GroupAutoFix;

            public readonly List<SingleMessage> MessageList = new List<SingleMessage>();

            public MessageGroup(string message, MessageType messageType)
            {
                Message = message;
                MessageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, MessageType messageType)
            {
                Message = message;
                CombinedMessage = combinedMessage;
                MessageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, string additionalInfo, MessageType messageType)
            {
                Message = message;
                CombinedMessage = combinedMessage;
                AdditionalInfo = additionalInfo;
                MessageType = messageType;
            }

            public MessageGroup SetGroupAutoFix(Action groupAutoFix)
            {
                GroupAutoFix = groupAutoFix;
                return this;
            }

            public MessageGroup SetDocumentation(string documentation)
            {
                Documentation = documentation;
                return this;
            }

            public MessageGroup AddSingleMessage(SingleMessage message)
            {
                MessageList.Add(message);
                return this;
            }

            public int GetTotalCount()
            {
                if (objectCount is null)
                {
                    var count = 0;

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

                    objectCount = count;
                }

                return (int) objectCount;
            }

            public bool HasSelectGameObjects()
            {
                if (disableCombinedSelection is null)
                {
                    for (var i = 0; i < MessageList.Count; i++)
                    {
                        var item = MessageList[i];
                        if (item.selectObjects != null && item.selectObjects.Any())
                        {
                            disableCombinedSelection = true;
                        }
                    }

                    if (disableCombinedSelection == null)
                        disableCombinedSelection = false;
                }

                return (bool) disableCombinedSelection;
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
            public string listName;

            [SerializeField] public List<MessageGroup> MessageGroups;
            private Dictionary<int, bool> expandedGroups;
            [SerializeField] public bool disabled;

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
                var count = 0;

                for (var i = 0; i < MessageGroups.Count; i++)
                {
                    var group = MessageGroups[i];

                    if (group.CombinedMessage != null && group.GetTotalCount() > 0)
                    {
                        count++;
                    }
                    else if (group.CombinedMessage is null)
                    {
                        count++;
                    }
                }

                return count > 0;
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
            [SerializeField] public List<MessageCategory> messageCategory = new List<MessageCategory>();
            private List<MessageCategory> drawList = new List<MessageCategory>();

            [SerializeField] private Vector2 scrollPos;

            public MessageCategory CreateOrGetCategory(string listName)
            {
                var oldMessageCategory = messageCategory.Find(x => x.listName == listName);

                if (oldMessageCategory is null)
                {
                    var newMessageCategory = new MessageCategory(listName);
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

            private const int ButtonWidth = 75;
            private const int ButtonHeight = 20;

            public void DrawMessages()
            {
                if (Event.current.type == EventType.Layout)
                {
                    drawList = messageCategory;
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = scrollView.scrollPosition;

                    for (var i = 0; i < drawList.Count; i++)
                    {
                        if (drawList[i].disabled) continue;

                        var group = drawList[i];

                        GUILayout.Label(group.listName, EditorStyles.boldLabel);

                        if (!group.HasMessages())
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                DrawMessage("No messages found for " + group.listName + ".", MessageType.Info);
                            }

                            continue;
                        }

                        for (var l = 0; l < group.MessageGroups.Count; l++)
                        {
                            var messageGroup = group.MessageGroups[l];

                            if (messageGroup.MessageList is null || messageGroup.CombinedMessage != null && messageGroup.MessageList.Count == 0) continue;

                            var singleCombinedMessage = messageGroup.MessageList.Count == 1;
                            var expanded = !singleCombinedMessage && group.IsExpanded(messageGroup);
                            var hasButtons = messageGroup.Buttons();

                            string finalMessage;

                            if (messageGroup.MessageList.Count == 0)
                            {
                                finalMessage = messageGroup.Message;
                            }
                            else
                            {
                                finalMessage = singleCombinedMessage ? string.Format(messageGroup.Message, messageGroup.MessageList[0].variable, messageGroup.MessageList[0].variable2) : string.Format(messageGroup.CombinedMessage ?? string.Empty, messageGroup.GetTotalCount().ToString());
                            }

                            if (messageGroup.AdditionalInfo != null)
                            {
                                finalMessage += " " + messageGroup.AdditionalInfo;
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                DrawMessage(finalMessage, messageGroup.MessageType);

                                if (hasButtons)
                                {
                                    if (singleCombinedMessage)
                                    {
                                        var message = messageGroup.MessageList[0];
                                        DrawButtons(message.selectObjects, messageGroup.Documentation, message.assetPath, message.AutoFix, messageGroup.HasSelectGameObjects());
                                    }
                                    else
                                    {
                                        DrawButtons(messageGroup.GetSelectObjects(), messageGroup.Documentation, null, messageGroup.GroupAutoFix, messageGroup.HasSelectGameObjects());
                                    }
                                }
                            }

                            if (messageGroup.MessageList.Count > 1)
                            {
                                expanded = EditorGUILayout.Foldout(expanded, "Show separate messages");
                                group.SetExpanded(messageGroup, expanded);

                                if (!expanded) continue;

                                for (var j = 0; j < messageGroup.MessageList.Count; j++)
                                {
                                    var message = messageGroup.MessageList[j];

                                    var finalSingleMessage = string.Format(messageGroup.Message, message.variable, message.variable2);

                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        DrawPaddedMessage(finalSingleMessage);
                                        DrawButtons(message.selectObjects, null, message.assetPath, message.AutoFix, true);
                                    }
                                }

                                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();
                }

                void DrawPaddedMessage(string messageText)
                {
                    var box = new GUIContent(messageText);
                    GUILayout.Box(box, Styles.HelpBoxPadded, GUILayout.ExpandHeight(true), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 116));
                }

                void DrawMessage(string messageText, MessageType type)
                {
                    var box = new GUIContent(messageText, GetDebuggerIcon(type));
                    GUILayout.Box(box, Styles.HelpBoxRichText, GUILayout.ExpandHeight(true), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 18));
                }

                void DrawButtons(GameObject[] selectObjects, string infoLink, string assetPath, Action autoFix, bool hasGameObjects)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        var infoLinkSet = infoLink != null;
                        var autoFixSet = autoFix != null;
                        var assetPathSet = assetPath != null;

                        if (infoLinkSet && GUILayout.Button("More Info", GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)))
                        {
                            Application.OpenURL(infoLink);
                        }

                        if (!infoLinkSet || assetPathSet || hasGameObjects)
                        {
                            using (new EditorGUI.DisabledScope(!assetPathSet && !hasGameObjects))
                            {
                                if (assetPathSet)
                                {
                                    if (GUILayout.Button("Ping Asset", GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)))
                                    {
                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("Select", GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)))
                                    {
                                        if (selectObjects != null)
                                        {
                                            Selection.objects = selectObjects;
                                        }
                                    }
                                }
                            }
                        }

                        if (!(infoLinkSet && (assetPathSet || hasGameObjects)))
                        {
                            using (new EditorGUI.DisabledScope(!autoFixSet))
                            {
                                if (GUILayout.Button("Auto Fix", GUILayout.Width(ButtonWidth), GUILayout.Height(ButtonHeight)))
                                {
                                    autoFix?.Invoke();

                                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                                    autoRecheck = true;
                                    recheck = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        [SerializeField] private int tab;

        [MenuItem("VRWorld Toolkit/World Debugger", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(WorldDebugger));
            window.titleContent = new GUIContent("World Debugger");
            window.minSize = new Vector2(520, 600);
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

        public static Action SetLegacyBlendShapeNormals(ModelImporter[] importers)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Enable Legacy Blend Shape Normals?", "This operation will enable Legacy Blend Shape Normals on " + importers.Length + " models. This can take some time, depending on the number of models and their size.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    for (var i = 0; i < importers.Length; i++)
                    {
                        ModelImporterUtil.SetLegacyBlendShapeNormals(importers[i], true);
                        importers[i].SaveAndReimport();
                    }
                }
            };
        }

        public static Action DisableComponent(Behaviour behaviour)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviour.GetType() + " on the GameObject \"" + behaviour.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(behaviour, "Disable Component");
                    behaviour.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                }
            };
        }

        public static Action DisableComponent(Behaviour[] behaviours)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviours[0].GetType() + " component on " + behaviours.Count().ToString() + " GameObjects.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(behaviours.ToArray<Object>(), "Mass Disable Components");

                    for (var i = 0; i < behaviours.Length; i++)
                    {
                        var b = behaviours.ToList()[i];
                        b.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(b);
                    }
                }
            };
        }

        public static Action SetObjectLayer(GameObject obj, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change layer?", "This operation will change the layer of " + obj.name + " to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(obj, "Layer Change");
                    obj.layer = LayerMask.NameToLayer(layer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            };
        }

        public static Action SetObjectLayer(GameObject[] objs, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change layer?", "This operation will change " + objs.Length + " GameObjects layer to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(objs.ToArray<Object>(), "Mass Layer Change");

                    for (var index = 0; index < objs.ToList().Count; index++)
                    {
                        var o = objs.ToList()[index];
                        o.layer = LayerMask.NameToLayer(layer);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                    }
                }
            };
        }

        public static Action SetSelectableNavigationMode(Selectable selectable, Navigation.Mode mode)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Navigation mode?", "This operation will change the Navigation mode on UI Element \"" + selectable.gameObject.name + "\" to " + mode.ToString() + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(selectable, "Navigation Mode Change");

                    var navigation = selectable.navigation;

                    navigation.mode = Navigation.Mode.None;

                    selectable.navigation = navigation;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(selectable);
                }
            };
        }

        public static Action SetSelectableNavigationMode(Selectable[] selectables, Navigation.Mode mode)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Navigation mode?", "This operation will change " + selectables.Length + " UI Elements Navigation to " + mode.ToString() + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(selectables.ToArray<Object>(), "Mass Navigation Mode Change");

                    for (var i = 0; i < selectables.Length; i++)
                    {
                        var navigation = selectables[i].navigation;

                        navigation.mode = Navigation.Mode.None;

                        selectables[i].navigation = navigation;

                        PrefabUtility.RecordPrefabInstancePropertyModifications(selectables[i]);
                    }
                }
            };
        }

        public static Action SetScrollRectScrollSensitivity(ScrollRect scrollRect, float scrollSensitivity)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Scroll Sensitivity?", "This operation will change the Scroll Sensitivity on ScrollRect component \"" + scrollRect.gameObject.name + "\" to " + scrollSensitivity + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(scrollRect, "ScrollRect Scroll Sensitivity Change");

                    scrollRect.scrollSensitivity = scrollSensitivity;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(scrollRect);
                }
            };
        }

        public static Action SetScrollRectScrollSensitivity(ScrollRect[] scrollRects, float scrollSensitivity)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change Scroll Sensitivity?", "This operation will change " + scrollRects.Length + " ScrollRect components Scroll Sensitivity to " + scrollSensitivity + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(scrollRects.ToArray<Object>(), "Mass ScrollRect Scroll Sensitivity Change");

                    for (var i = 0; i < scrollRects.Length; i++)
                    {
                        var scrollRect = scrollRects[i];

                        scrollRect.scrollSensitivity = scrollSensitivity;

                        PrefabUtility.RecordPrefabInstancePropertyModifications(scrollRects[i]);
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
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change the tag of " + obj.name + " to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(obj, "Change Tag");
                    obj.tag = tag;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            };
        }

        public static Action SetGameObjectTag(GameObject[] objs, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + objs.Length + " GameObjects tag to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(objs.ToArray<Object>(), "Mass Change Tag");

                    for (var i = 0; i < objs.ToList().Count; i++)
                    {
                        var o = objs.ToList()[i];
                        o.tag = tag;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                    }
                }
            };
        }

        public static Action ChangeShader(Material material, string shader)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change shader?", "This operation will change the shader of the material " + material.name + " to " + shader + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    var standard = Shader.Find(shader);
                    Undo.RegisterCompleteObjectUndo(material, "Changed Shader");
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
                    var newShader = Shader.Find(shader);
                    Undo.RegisterCompleteObjectUndo(materials.ToArray<Object>(), "Changed Shaders");
                    materials.ToList().ForEach(m => m.shader = newShader);
                }
            };
        }

        public static Action RemoveOverlappingLightProbes(LightProbeGroup lightProbeGroup)
        {
            return () =>
            {
                Undo.RegisterCompleteObjectUndo(lightProbeGroup, "Removed Overlapping Light Probes");
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes in the group \"" + lightProbeGroup.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    lightProbeGroup.probePositions = lightProbeGroup.probePositions.Distinct().ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(lightProbeGroup);
                }
            };
        }

        public static Action RemoveOverlappingLightProbes(LightProbeGroup[] lightProbeGroups)
        {
            return () =>
            {
                Undo.RegisterCompleteObjectUndo(lightProbeGroups, "Removed Overlapping Light Probes");
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes found in the current scene.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    foreach (var lpg in lightProbeGroups)
                    {
                        lpg.probePositions = lpg.probePositions.Distinct().ToArray();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(lpg);
                    }
                }
            };
        }

        public static Action RemoveRedundantLightProbes(LightProbeGroup[] lightProbeGroups)
        {
            return () =>
            {
                if (LightmapSettings.lightProbes != null)
                {
                    var probes = LightmapSettings.lightProbes.positions;
                    if (EditorUtility.DisplayDialog("Remove redundant light probes?", "This operation will attempt to remove any redundant light probes in the current scene. Bake your lighting before this operation to avoid any correct light probes getting removed.\n\nDo you want to continue?", "Yes", "Cancel"))
                    {
                        foreach (var lpg in lightProbeGroups)
                        {
                            lpg.probePositions = lpg.probePositions.Distinct().Where(p => !probes.Contains(p)).ToArray();
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Baked light probes not found!", "Bake your lighting first before attempting to remove redundant light probes.", "Ok");
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

                    await Task.Run(() => DeleteFiles(deleteFiles, token), token);
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
                Undo.RegisterCompleteObjectUndo(descriptor, "Spawn Points Fixed");
                if (descriptor.spawns is null || descriptor.spawns.Length == 0)
                {
                    descriptor.spawns = new[] {descriptor.gameObject.transform};
                }

                descriptor.spawns = descriptor.spawns.Where(c => c != null).ToArray();

                PrefabUtility.RecordPrefabInstancePropertyModifications(descriptor);
            };
        }

        public static Action ChangeRespawnHeight(VRC_SceneDescriptor descriptor, float newHeight)
        {
            return () =>
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Respawn Height Change");

                descriptor.RespawnHeightY = newHeight;

                PrefabUtility.RecordPrefabInstancePropertyModifications(descriptor);
            };
        }

        public static Action SanitizeBuildPath()
        {
            return () =>
            {
                PlayerSettings.companyName = UnityWebRequest.UnEscapeURL(PlayerSettings.companyName).Trim();
                PlayerSettings.productName = UnityWebRequest.UnEscapeURL(PlayerSettings.productName).Trim();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            };
        }

        public static Action FixVRCProjectSettings(VRCProjectSettings settings)
        {
            return () =>
            {
                // TODO: Cleaner solution to storing these
                var newLayers = new[] {"Default", "TransparentFX", "Ignore Raycast", "", "Water", "UI", "", "", "Interactive", "Player", "PlayerLocal", "Environment", "UiMenu", "Pickup", "PickupNoEnvironment", "StereoLeft", "StereoRight", "Walkthrough", "MirrorReflection", "reserved2", "reserved3", "reserved4"};

                var newCollisionArr = new[]
                {
                    true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
                    false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, false, false, true, false, false, false, false, false, false, true,
                    true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, false, true,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, false, true, true, true, false,
                    false, true, false, true, true, true, true, true, false, false, false, false, true, true, true, true, true, true, true, true, true, true, false, false, false, true, false, false, true, true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    false, true, true, true, false, false, true, false, true, false, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, false, true, true, true, false, false, true, false, true, false, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false,
                    false, true, true, true, true, true, false, true, true, true, false, false, true, false, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true,
                    true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, false, false, true, false, true, true, false, false, true, true, true, true, true, false,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false,
                    true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true,
                    false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, false, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true
                };

                var so = new SerializedObject(settings);

                var layersSerializedProperty = so.FindProperty("layers");

                layersSerializedProperty.arraySize = newLayers.Length;
                for (var i = 0; i < newLayers.Length; i++)
                {
                    layersSerializedProperty.GetArrayElementAtIndex(i).stringValue = newLayers[i];
                }

                so.FindProperty("numLayers").intValue = 22;

                var collisionArr = so.FindProperty("layerCollisionArr");

                collisionArr.arraySize = newCollisionArr.Length;
                for (var i = 0; i < newCollisionArr.Length; i++)
                {
                    collisionArr.GetArrayElementAtIndex(i).boolValue = newCollisionArr[i];
                }

                so.ApplyModifiedProperties();

                var systemType = Assembly.Load("VRCCore-Editor").GetType("UpdateLayers");
                var setupLayersToSet = systemType.GetMethod("SetupLayersToSet", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.IsNotNull(setupLayersToSet);
                setupLayersToSet.Invoke(null, null);
            };
        }

        public static Action SetErrorPause(bool enabled)
        {
            return () => { ConsoleFlagUtil.SetConsoleErrorPause(enabled); };
        }

        public static Action SetVRChatLayers()
        {
            return UpdateLayers.SetupEditorLayers;
        }

        public static Action SetVRChatCollisionMatrix()
        {
            return UpdateLayers.SetupCollisionLayerMatrix;
        }

        public static Action SetFutureProofPublish(bool state)
        {
            return () => { EditorPrefs.SetBool("futureProofPublish", state); };
        }

        public static Action SetReferenceCamera(VRC_SceneDescriptor descriptor, Camera camera)
        {
            return () =>
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Reference Camera Set");
                descriptor.ReferenceCamera = camera.gameObject;
                PrefabUtility.RecordPrefabInstancePropertyModifications(descriptor);
            };
        }

        public static Action SetVRCInstallPath()
        {
            return () =>
            {
                var clientPath = Helper.GetSteamVrcExecutablePath();

                if (clientPath != null)
                {
                    SDKClientUtilities.SetVRCInstallPath(clientPath);
                }
                else if (EditorUtility.DisplayDialog("VRChat Executable Path Not Found", "Could not find the VRChat executable path automatically.\n\nPress Ok to locate it manually.", "Ok", "Cancel"))
                {
                    var newPath = EditorUtility.OpenFilePanel("Locate VRChat.exe", Application.dataPath, "exe");
                    SDKClientUtilities.SetVRCInstallPath(newPath);
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

        private const string NoSceneDescriptor = "The current scene has no Scene Descriptor. Please add one, or drag the VRCWorld prefab to the scene.";

        private const string TooManySceneDescriptors = "Multiple Scene Descriptors were found. Only one scene descriptor can exist in a single scene.";

        private const string TooManyPipelineManagers = "The current scene has multiple Pipeline Managers in it. This can break the world upload process and prevent you from being able to load into the world.";

        private const string WorldDescriptorFar = "Scene Descriptor is {0} units far from the zero point in Unity. Having your world center out this far will cause some noticeable jittering on models. You should move your world closer to the zero point of your scene.";

        private const string WorldDescriptorOff = "Scene Descriptor is {0} units far from the zero point in Unity. It is usually good practice if possible to keep it as close as possible to the absolute zero point to avoid floating-point errors.";

        private const string DifferingSanitizedBuildPath = "The last build path differs from the one seen by the VRCSDK. This can happen with certain characters that get stripped from the path only during Build & Publish. The build path is created using the Company and Product name in the projects Player Settings.";

        private const string ImproperlySetupVRCProjectSettings = "Improperly setup VRCProjectSettings detected. This will cause the Control Panel Builder tab to appear empty.";

        private const string VRCProjectSettingsMissing = "VRCProjectSettings not found. The SDK needs it, and missing it will cause the SDK to error out. To fix the problem, reimport the SDK.";

        private const string LastBuildFailed = "Last build failed! Check the Console for compile errors to find the cause. If the error script is in the SDK, try reimporting it. Otherwise, remove or update the problem asset.";

        private const string NoSpawnPointSet = "There are no spawn points set in your Scene Descriptor. Spawning into a world with no spawn point will cause you to get thrown back to your homeworld.";

        private const string NullSpawnPoint = "Null spawn point set Scene Descriptor. Spawning into a null spawn point will cause you to get thrown back to your homeworld.";

        private const string ReferenceCameraClearFlagsNotSkybox = "The current reference camera's clear flags are not set to Skybox. This can cause rendering problems in-game.";

        private const string ReferenceCameraClippingPlaneRatio = "Too high of a ratio between reference camera's near ({0}) and far ({1}) clip values can cause rendering issues in-game.";

        private const string ReferenceCameraNearClipPlaneOver = "The current reference camera's near clip value is {0}. This value gets clamped to be between 0.01 and 0.05.";

        private const string NoReferenceCameraSetGeneral = "No reference camera set in the Scene Descriptor. Using a reference camera allows the world's rendering distance to be changed by changing the camera's near and far clipping planes.";

        private const string ReferenceCameraHasNoCameraComponent = "The GameObject \"{0}\" currently set in the Scene Descriptor as a reference camera does not have a camera component. This can cause various problems in-game.";

        private const string ColliderUnderSpawnIsTrigger = "The collider \"{0}\" under your spawn point {1} has been set as Is Trigger.";
        private const string ColliderUnderSpawnIsTriggerCombined = "Found \"{0}\" spawn points which have a collider set as Is Trigger underneath.";
        private const string ColliderUnderSpawnIsTriggerInfo = "Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string SpawnUnderRespawnHeight = "Spawn point \"{0}\" is placed {1} units under the Respawn Height set in Scene Descriptor.";
        private const string SpawnUnderRespawnHeightCombined = "Found {0} spawn points under Respawn Height set in Scene Descriptor.";
        private const string SpawnUnderRespawnHeightInfo = "Spawning under the Respawn Height causes players to get stuck while respawning infinitely.";

        private const string NoColliderUnderSpawn = "Spawn point \"{0}\" does not have a collider under it.";
        private const string NoColliderUnderSpawnCombined = "Found {0} spawn points with no collider under them.";
        private const string NoColliderUnderSpawnInfo = "Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string RespawnHeightAboveCollider = "The collider below spawn point \"{1}\" is below respawn height set in scene descriptor.";
        private const string RespawnHeightAboveColliderCombined = "Found {0} spawn points where the collider is below the respawn height.";
        private const string RespawnHeightAboveColliderInfo = "This will cause players to get stuck while respawning infinitely.";

        private const string NoPlayerMods = "No Player Mods were found in the scene. Player mods are needed for adding jumping and changing walking speed.";

        private const string TriggerTriggerNoCollider = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that does not have a Collider on it.";
        private const string ColliderTriggerNoCollider = "You have an OnEnterCollider or OnExitCollider Trigger \"{0}\" that does not have a Collider on it.";

        private const string TriggerTriggerWrongLayer = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that is not on the MirrorReflection layer.";
        private const string TriggerTriggerWrongLayerCombined = "You have {0} OnEnterTrigger or OnExitTrigger Triggers that are not on the MirrorReflection layer.";
        private const string TriggerTriggerWrongLayerInfo = "This can stop raycasts from working correctly, making you unable to interact with objects and UI Buttons.";

        private const string MirrorONByDefault = "The mirror \"{0}\" is on by default.";
        private const string MirrorONByDefaultCombined = "The scene has {0} mirrors on by default.";
        private const string MirrorONByDefaultInfo = "This is an awful practice. Any mirrors in worlds should be disabled by default.";

        private const string MirrorWithDefaultLayers = "The mirror \"{0}\" has the default Reflect Layers set.";
        private const string MirrorWithDefaultLayersCombined = "You have {0} mirrors that have the default Reflect Layers set.";
        private const string MirrorWithDefaultLayersInfo = "Only having the layers needed to have enabled in mirrors can save a lot of frames, especially in populated instances.";

        private const string LegacyBlendShapeIssues = "Skinned mesh renderer found with model {0} ({1}) without Legacy Blend Shape Normals enabled.";
        private const string LegacyBlendShapeIssuesCombined = "Found {0} models without Legacy Blend Shape Normals enabled.";
        private const string LegacyBlendShapeIssuesInfo = "This can significantly increase the size of the world.";

        private const string BakedOcclusionCulling = "Baked Occlusion Culling found.";

        private const string NoOcclusionAreas = "No occlusion areas were found. Occlusion Areas are recommended to help generate higher precision data where the camera is likely to be. If none exist, an area is created automatically containing all Occluders and Occludees.";

        private const string DisabledOcclusionArea = "Occlusion Area {0} found with Is View Volume disabled.";
        private const string DisabledOcclusionAreaCombined = "Occlusion Areas found with Is View Volume disabled.";
        private const string DisabledOcclusionAreaInfo = "Without this enabled, the Occlusion Area does not get used for the occlusion bake.";

        private const string NoOcclusionCulling = "The current scene does not have baked Occlusion Culling. Occlusion culling often gives a large performance boost, especially in larger worlds with multiple rooms or areas.";

        private const string OcclusionCullingCacheWarning = "The current project's occlusion culling cache has {0} files. When the occlusion culling cache grows too big, baking occlusion culling can take much longer than intended. It can be cleared with no adverse effects.";

        private const string ActiveCameraOutputtingToRenderTexture = "Active camera \"{0}\" outputting to a render texture.";
        private const string ActiveCameraOutputtingToRenderTextureCombined = "The current scene has {0} active cameras outputting to render textures.";
        private const string ActiveCameraOutputtingToRenderTextureInfo = "This will affect performance negatively by causing more draw calls to happen. They should only be enabled when needed.";

        private const string ActiveCameraWithOverZeroDepth = "Active camera \"{0}\" targeting display 1 with render depth over 0.";
        private const string ActiveCameraWithOverZeroDepthCombined = "The current scene has {0} active cameras targeting display 1 with render depth over 0.";
        private const string ActiveCameraWithOverZeroDepthInfo = "This will cause it to render over the upload screen, not allowing you to upload.";

        private const string NoToonShaders = "Toon shaders should be avoided for world-building, as they are missing crucial things for making worlds. For world-building, the most recommended shader is Standard.";

        private const string NonCrunchedTextures = "{0}% of the textures used in the scene have not been crunch compressed. Crunch compression can significantly reduce the size of the world download. It can be found from the texture's import settings.";

        private const string SingleColorEnvironmentLighting = "Consider changing the Environment Lighting Source from Color to Gradient for better ambient lighting.";

        private const string DarkEnvironmentLighting = "Using dark colors for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if the world has dark lighting.";

        private const string CustomEnvironmentReflectionsNull = "The current scenes Environment Reflections have been set to custom, but a custom cubemap has not been defined.";

        private const string NoLightmapUV = "The model found in the scene \"{0}\" is set to be lightmapped, but does not have Lightmap UVs.";
        private const string NoLightmapUVCombined = "The current scene has {0} models set to be lightmapped that do not have Lightmap UVs.";
        private const string NoLightmapUVInfo = "This can cause issues when baking lighting if the main UV is not suitable for lightmapping. You can enable generating Lightmap UVs in the model's import settings.";

        private const string LightsNotBaked = "The current scene is using realtime lighting. Consider baked lighting for improved performance.";

        private const string ConsiderLargerLightmaps = "Possibly unoptimized lighting setup detected with a high amount of separate lightmaps compared to the currently set Lightmap Size.\nConsider increasing Lightmap Size from {0} to 2048 or larger and adjusting the individual Scale In Lightmap value on mesh renderers to fit things on a smaller amount of lightmaps.";

        private const string ConsiderSmallerLightmaps = "Baking lightmaps at 4096 with Progressive GPU will silently fall back to CPU Progressive. More than 12GB GPU Memory is needed to bake 4k lightmaps with GPU Progressive.";

        private const string NonBakedBakedLight = "The light {0} is set to be baked/mixed, but it has not been baked yet!";
        private const string NonBakedBakedLightCombined = "The scene contains {0} baked/mixed lights that have not been baked!";
        private const string NonBakedBakedLightInfo = "Baked lights that have not been baked yet function as realtime lights in-game.";

        private const string LightingDataAssetInfo = "The current scene's lighting data asset takes up {0} MB of the world's size. This contains the scene's light probe data and realtime GI data.";

        private const string NoLightProbes = "No light probes found in the current scene. Without light probes, baked lights are not able to affect dynamic objects such as players and pickups.";

        private const string LightProbeCountNotBaked = "The current scene contains {0} light probes, but {1} of them have not been baked yet.";

        private const string LightProbesRemovedNotReBaked = "Some light probes have been removed after the last bake. Bake them again to update the scene's lighting data. The lighting data contains {0} baked light probes, and the current scene has {1} light probes.";

        private const string LightProbeCount = "The current scene contains {0} baked light probes.";

        private const string OverlappingLightProbes = "Light Probe Group \"{0}\" has {1} overlapping light probes.";
        private const string OverlappingLightProbesCombined = "Found {0} Light Probe Groups with overlapping light probes.";
        private const string OverlappingLightProbesInfo = "These can cause a slowdown in the editor and will not get baked because Unity will skip any extra overlapping probes.";

        private const string NoReflectionProbes = "The current scene has no active reflection probes. Reflection probes are needed to have proper reflections on reflective materials.";

        private const string ReflectionProbesSomeUnbaked = "The reflection probe \"{0}\" is unbaked.";
        private const string ReflectionProbesSomeUnbakedCombined = "The current scene has {0} unbaked reflection probes.";

        private const string ReflectionProbeCountText = "The current scene has {0} reflection probes.";

        private const string PostProcessingImportedButNotSetup = "The current project has Post Processing imported, but you have not set it up yet.";

        private const string PostProcessingDisabledInSceneView = "Post-processing is disabled in the scene view. You will not be able to preview any post-processing effects without enabling it first.";

        private const string PostProcessingNoResourcesSet = "The Post Process Layer on \"{0}\" does not have its resources field set properly. This causes post-processing to error out. This can be fixed by recreating the Post Processing Layer on the GameObject.";

        private const string NoReferenceCameraSetPp = "The current scene's Scene Descriptor has no Reference Camera set. Without a Reference Camera set, post-processing will not be visible in-game.";

        private const string NoPostProcessingVolumes = "No enabled Post Processing Volumes found in the scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";

        private const string ReferenceCameraNoPostProcessingLayer = "The current Reference Camera does not have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";

        private const string PostProcessLayerUsingReservedLayer = "Your current Post Process Layer uses one of the VRChat reserved layers. Using these will break post-processing while in-game.";

        private const string VolumeBlendingLayerNotSet = "You don't have a Volume Blending Layer set in the Post Process Layer, so post-processing will not work. Using the Water or PostProcessing layer is recommended.";

        private const string PostProcessingVolumeNotGlobalNoCollider = "Post Processing Volume \"{0}\" is not marked as Global and does not have a collider.";
        private const string PostProcessingVolumeNotGlobalNoColliderCombined = "Found {0} Post Processing Volumes that are not marked as Global and do not have a collider.";
        private const string PostProcessingVolumeNotGlobalNoColliderInfo = "The volume will not affect the camera without one of these set on it.";

        private const string NoProfileSet = "Post Processing Volume \"{0}\" does not have a profile set.";
        private const string NoProfileSetCombined = "Found {0} Post Processing Volumes with no profile set.";

        private const string NoMatchingLayersFound = "No enabled Post Processing Volumes found with matching layers to the main Post Processing Layer. Layers currently set to: {0}";

        private const string TonemapperMissing = "No global Tonemapper found. When there is no Tonemapper set, the colors in the scene will be distorted. Ideally, use Neutral or ACES.";

        private const string TooHighBloomIntensity = "Do not raise the Bloom intensity too high! It is best to use a low Bloom intensity, between 0.01 to 0.3.";

        private const string TooHighBloomThreshold = "You should avoid having the Bloom Threshold be set to a high value, as it might cause unexpected problems with bright avatars. Ideally, it should be kept at 0, but always below 1.0.";

        private const string NoBloomDirtInVR = "Avoid using Bloom Dirt, it looks terrible in VR!";

        private const string NoAmbientOcclusion = "Do not use Post Processing Ambient Occlusion in VRChat! VRChat uses Forward rendering, so it gets applied on top of EVERYTHING, which is bad! It also has a super high rendering cost in VR.";

        private const string DepthOfFieldWarning = "Depth of field has a high performance cost and is very disorienting in VR. If you want to use depth of field, it should be disabled by default.";

        private const string ScreenSpaceReflectionsWarning = "Screen-space Reflections only works when using deferred rendering. Because VRChat uses Forward rendering, this should not be used.";

        private const string VignetteWarning = "Only use Post Processing Vignette in small amounts. A powerful vignette can cause sickness in VR.";

        private const string NoPostProcessingImported = "Post Processing package not found in the project.";

        private const string QuestBakedLightingWarning = "Realtime lighting for Quest content should be avoided and instead have a properly baked lighting setup for optimal performance.";

        private const string AmbientModeSetToCustom = "The current scene's Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";

        private const string NoProblemsFoundInPp = "No problems were found in your post-processing setup. In some cases where post-processing is working in the editor but not in-game, some imported assets may be causing it not to function correctly.";

        private const string BakeryLightNotSetEditorOnly = "Your Bakery light named \"{0}\" is not set to be EditorOnly.";
        private const string BakeryLightNotSetEditorOnlyCombined = "You have {0} Bakery lights are not set to be EditorOnly.";
        private const string BakeryLightNotSetEditorOnlyInfo = "This causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";

        private const string BakeryLightUnityLight = "Your Bakery light named \"{0}\" has an active Unity Light component on it.";
        private const string BakeryLightUnityLightCombined = "You have {0} Bakery lights that have an active Unity Light component on it.";
        private const string BakeryLightUnityLightInfo = "These will not get baked with Bakery and will keep acting as realtime lights even if set to baked.";

        private const string QuestLightmapCompressionOverride = "Lightmap \"{0}\" does not have a platform-specific override set for Android.";
        private const string QuestLightmapCompressionOverrideCombined = "No platform-specific override set on {0} lightmaps for Android.";
        private const string QuestLightmapCompressionOverrideInfo = "Without setting a proper platform-specific override when building for Android, lightmaps can show noticeable banding. Suggested format \"ASTC 4x4 block\".";

        private const string MissingShaderWarning = "The material \"{0}\" found in the scene has a missing or broken shader.";
        private const string MissingShaderWarningCombined = "Found {0} materials in the current scene that have missing or broken shaders.";
        private const string MissingShaderWarningInfo = "These will fallback to the pink error shader.";

        private const string ErrorPauseWarning = "You have Error Pause enabled in your console. This can cause your world upload to fail by interrupting the build process.";

        private const string MultipleScenesLoaded = "Multiple scenes loaded, this is not supported by VRChat and can cause the world upload to fail. Only one scene should be used for world creation at a time.";

        private const string LayersNotSetup = "Project layers are not set up for VRChat yet.";

        private const string CollisionMatrixNotSetup = "The project's Collision Matrix is not set up for VRChat yet.";

        private const string MaterialWithGrabPassShader = "A material ({0}) in the scene has an active GrabPass due to shader \"{1}\".";
        private const string MaterialWithGrabPassShaderCombined = "Found {0} materials in the scene using a GrabPass.";
        private const string MaterialWithGrabPassShaderInfoPC = "A GrabPass will halt the rendering to copy the screen's contents into a texture for the shader to read. This has a notable effect on performance.";
        private const string MaterialWithGrabPassShaderInfoQuest = "Please change the shader for this material. When a shader uses a GrabPass on Quest, it will cause painful visual artifacts to occur, as they are not supported.";

        private const string ShrnmDirectionalModeBakeryError = "SH or RNM directional mode detected in Bakery. Using SH directional mode is not supported in VRChat by default. It requires the usage of VRC Bakery Adapter by Merlin for it to function in-game.";

        private const string BuildANDTestBrokenError = "VRChat link association has not been set up, and the VRChat client path has not been set in the VRCSDK settings. Without one of these settings set, Build & Test will not function.";

        private const string BuildANDTestForceNonVRError = "VRChat client path has not been set to point directly to the VRChat executable in the VRCSDK settings. The Force Non-VR setting for Build & Test will not work.";

        private const string BuildANDTestNoExecutableFound = "Current client path set in the VRCSDK settings does not contain the VRChat executable. This will cause problems with Build & Test functionality.";

        private const string MaterialWithNonWhitelistedShader = "Material \"{0}\" is using an unsupported shader \"{1}\".";
        private const string MaterialWithNonWhitelistedShaderCombined = "Found {0} materials with unsupported shaders.";
        private const string MaterialWithNonWhitelistedShaderInfo = "Unsupported shaders can cause problems on the Quest platform if not appropriately used.";

        private const string UIElementWithNavigationNotNone = "The UI Element \"{0}\" does not have its Navigation set to None.";
        private const string UIElementWithNavigationNotNoneCombined = "Found {0} UI Elements with their Navigation not set to None.";
        private const string UIElementWithNavigationNotNoneInfo = "Setting Navigation to None on UI Elements can stop accidental interactions with them while trying to walk around.";

        private const string ScrollRectWithScrollSensitivityNotZero = "The ScrollRect component \"{0}\" does not have its Scroll Sensitivity set to 0.";
        private const string ScrollRectWithScrollSensitivityNotZeroCombined = "Found {0} ScrollRect components with their Scroll Sensitivity not set to 0.";
        private const string ScrollRectWithScrollSensitivityNotZeroInfo = "Setting Scroll Sensitivity not set to 0 on ScrollRect components can stop accidental interactions with them while trying to walk around.";

        private const string NullTriggerReceiver = "Null receiver found on trigger {0}.";
        private const string NullTriggerReceiverCombined = "Found {0} null receivers in scene triggers.";
        private const string NullTriggerReceiverInfo = "This causes the trigger to target itself, which can sometimes be intentional.";

        private const string TextMeshLightmapStatic = "Text Mesh \"{0}\" marked as lightmap static.";
        private const string TextMeshLightmapStaticCombined = "Found {0} Text Meshes marked as lightmap static.";
        private const string TextMeshLightmapStaticInfo = "This will cause warnings as the mesh has no normals.";

        private const string UnsupportedCompressionFormatQuest = "Texture {0} using compression format {1} that is not supported on Quest.";
        private const string UnsupportedCompressionFormatQuestCombined = "Found {0} textures with compression format not supported on Quest.";
        private const string UnsupportedCompressionFormatQuestInfo = "These will appear fine in editor but black in game.";

        private const string HeyYouFoundABug = "Hey, you found a bug! Please send it my way so I can fix it! Check About VRWorld Toolkit to find all the ways to contact me. \"{0}\" on line {1}.";

        private const string FutureProofPublishEnabled = "Future Proof Publish is currently enabled. This is a legacy feature that has no planned functions as of right now. Having it enabled will increase upload times and sometimes cause uploading to fail.";

        #endregion

        private static long occlusionCacheFiles;

        // TODO: Better check threading
        private void CountOcclusionCacheFiles()
        {
            occlusionCacheFiles = Directory.EnumerateFiles("Library/Occlusion/").Count();

            OcclusionMessageCheck();
        }

        private void OcclusionMessageCheck()
        {
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

                optimization.AddMessageGroup(new MessageGroup(OcclusionCullingCacheWarning, cacheWarningType).AddSingleMessage(new SingleMessage(occlusionCacheFiles.ToString()).SetAutoFix(ClearOcclusionCache(occlusionCacheFiles))));
            }
        }

        private class CheckedShaderProperties
        {
            public bool IncludesGrabPass = false;
            public readonly List<string> GrabPassLightModeTags = new List<string>();
        }

        private void CheckScene()
        {
            masterList.ClearCategories();

            try
            {
                // Cache repeatedly used values
                var androidBuildPlatform = Helper.BuildPlatform() == RuntimePlatform.Android;

                // Get Descriptors
                var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
                var pipelines = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];

                // Check if a descriptor exists
                if (descriptors.Length == 0)
                {
                    general.AddMessageGroup(new MessageGroup(NoSceneDescriptor, MessageType.Error));
                    return;
                }

                var sceneDescriptor = descriptors[0];

                // General Checks

                // Make sure only one descriptor exists
                if (descriptors.Length > 1)
                {
                    general.AddMessageGroup(new MessageGroup(TooManySceneDescriptors, MessageType.Info).AddSingleMessage(new SingleMessage(Array.ConvertAll(descriptors, s => s.gameObject))));
                    return;
                }

                // Check for multiple pipeline managers
                if (pipelines.Length > 1)
                {
                    general.AddMessageGroup(new MessageGroup(TooManyPipelineManagers, MessageType.Error).AddSingleMessage(new SingleMessage(Array.ConvertAll(pipelines, s => s.gameObject)).SetAutoFix(RemoveBadPipelineManagers(pipelines))));
                }

                // Check how far the descriptor is from zero point for floating point errors
                var descriptorRemoteness = (int) Vector3.Distance(sceneDescriptor.transform.position, new Vector3(0.0f, 0.0f, 0.0f));

                if (descriptorRemoteness > 1500)
                {
                    general.AddMessageGroup(new MessageGroup(WorldDescriptorFar, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
                else if (descriptorRemoteness > 500)
                {
                    general.AddMessageGroup(new MessageGroup(WorldDescriptorOff, MessageType.Tips).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }

                var lastVRCPath = $"{PlayerSettings.productName}/{PlayerSettings.companyName}";
                if (!string.IsNullOrEmpty(lastVRCPath))
                {
                    var lastEscapedVRCPath = UnityWebRequest.UnEscapeURL(lastVRCPath);
                    if (lastVRCPath != lastEscapedVRCPath)
                    {
                        general.AddMessageGroup(new MessageGroup(DifferingSanitizedBuildPath, MessageType.Error).AddSingleMessage(new SingleMessage(lastVRCPath, lastEscapedVRCPath).SetAutoFix(SanitizeBuildPath())));
                    }
                }

                var vrcProjectSettings = Resources.Load<VRCProjectSettings>("VRCProjectSettings");
                if (vrcProjectSettings)
                {
                    if (vrcProjectSettings.layers is null || vrcProjectSettings.layers.Length == 0 || vrcProjectSettings.layerCollisionArr is null || vrcProjectSettings.layerCollisionArr.Length == 0)
                    {
                        general.AddMessageGroup(new MessageGroup(ImproperlySetupVRCProjectSettings, MessageType.Error).SetGroupAutoFix(FixVRCProjectSettings(vrcProjectSettings)));
                    }
                    else
                    {
                        if (!UpdateLayers.AreLayersSetup())
                        {
                            general.AddMessageGroup(new MessageGroup(LayersNotSetup, MessageType.Error).SetGroupAutoFix(SetVRChatLayers()));
                        }

                        if (!UpdateLayers.IsCollisionLayerMatrixSetup())
                        {
                            general.AddMessageGroup(new MessageGroup(CollisionMatrixNotSetup, MessageType.Error).SetGroupAutoFix(SetVRChatCollisionMatrix()));
                        }
                    }
                }
                else
                {
                    general.AddMessageGroup(new MessageGroup(VRCProjectSettingsMissing, MessageType.Error).SetDocumentation("https://docs.vrchat.com/docs/updating-the-sdk"));
                }

                if (buildReportWindows != null && buildReportWindows.summary.result == BuildResult.Failed || buildReportQuest != null && buildReportQuest.summary.result == BuildResult.Failed)
                {
                    general.AddMessageGroup(new MessageGroup(LastBuildFailed, MessageType.Error).SetDocumentation("https://github.com/oneVR/VRWorldToolkit/wiki/Fixing-Build-Problems"));
                }

                // Check if multiple scenes loaded
                if (SceneManager.sceneCount > 1)
                {
                    general.AddMessageGroup(new MessageGroup(MultipleScenesLoaded, MessageType.Error));
                }

                if (EditorPrefs.GetBool("futureProofPublish", true))
                {
                    general.AddMessageGroup(new MessageGroup(FutureProofPublishEnabled, MessageType.Error).SetGroupAutoFix(SetFutureProofPublish(false)));
                }

                // Check if console has error pause on
                if (ConsoleFlagUtil.GetConsoleErrorPause())
                {
                    general.AddMessageGroup(new MessageGroup(ErrorPauseWarning, MessageType.Error).AddSingleMessage(new SingleMessage(SetErrorPause(false))));
                }

                // Check reference camera for possible problems
                if (sceneDescriptor.ReferenceCamera != null)
                {
                    var camera = sceneDescriptor.ReferenceCamera.GetComponent<Camera>();
                    if (camera != null)
                    {
                        if (camera.clearFlags != CameraClearFlags.Skybox)
                        {
                            general.AddMessageGroup(new MessageGroup(ReferenceCameraClearFlagsNotSkybox, MessageType.Warning).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera)));
                        }

                        // TODO: Investigate better sanity value
                        if (camera.farClipPlane / camera.nearClipPlane > 200000f)
                        {
                            general.AddMessageGroup(new MessageGroup(ReferenceCameraClippingPlaneRatio, MessageType.Warning).AddSingleMessage(new SingleMessage(camera.nearClipPlane.ToString(CultureInfo.InvariantCulture), camera.farClipPlane.ToString(CultureInfo.InvariantCulture)).SetSelectObject(camera.gameObject)));
                        }

                        if (camera.nearClipPlane > 0.05f)
                        {
                            general.AddMessageGroup(new MessageGroup(ReferenceCameraNearClipPlaneOver, MessageType.Tips).AddSingleMessage(new SingleMessage(camera.nearClipPlane.ToString(CultureInfo.InvariantCulture)).SetSelectObject(camera.gameObject)));
                        }
                    }
                    else
                    {
                        general.AddMessageGroup(new MessageGroup(ReferenceCameraHasNoCameraComponent, MessageType.Error)).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.name).SetSelectObject(sceneDescriptor.ReferenceCamera).SetAutoFix(() =>
                        {
                            sceneDescriptor.ReferenceCamera = null;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(sceneDescriptor.gameObject);
                        }));
                    }
                }
                else
                {
                    general.AddMessageGroup(new MessageGroup(NoReferenceCameraSetGeneral, MessageType.Tips).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject)));
                }

#if UNITY_EDITOR_WIN
                // Check for problems with Build & Test
                var commandPath = Registry.ClassesRoot.OpenSubKey(@"VRChat\shell\open\command");
                var savedVRCInstallPath = SDKClientUtilities.GetSavedVRCInstallPath();
                if (commandPath is null && savedVRCInstallPath == "\\VRChat.exe")
                {
                    general.AddMessageGroup(new MessageGroup(BuildANDTestBrokenError, MessageType.Error).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
                else if (savedVRCInstallPath == "\\VRChat.exe")
                {
                    general.AddMessageGroup(new MessageGroup(BuildANDTestForceNonVRError, MessageType.Warning).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
                else if (!File.Exists(savedVRCInstallPath))
                {
                    general.AddMessageGroup(new MessageGroup(BuildANDTestNoExecutableFound, MessageType.Error).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
#endif

                // Get spawn points for any possible problems
                if (sceneDescriptor.spawns != null && sceneDescriptor.spawns.Length > 0)
                {
                    var spawns = sceneDescriptor.spawns.Where(s => s != null).ToArray();

                    var spawnsLength = sceneDescriptor.spawns.Length;
                    var emptySpawns = spawnsLength != spawns.Length;

                    if (emptySpawns)
                    {
                        general.AddMessageGroup(new MessageGroup(NullSpawnPoint, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
                    }

                    var spawnUnderRespawnHeight = general.AddMessageGroup(new MessageGroup(SpawnUnderRespawnHeight, SpawnUnderRespawnHeightCombined, SpawnUnderRespawnHeightInfo, MessageType.Error));
                    var noColliderUnderSpawn = general.AddMessageGroup(new MessageGroup(NoColliderUnderSpawn, NoColliderUnderSpawnCombined, NoColliderUnderSpawnInfo, MessageType.Error));
                    var colliderUnderSpawnTrigger = general.AddMessageGroup(new MessageGroup(ColliderUnderSpawnIsTrigger, ColliderUnderSpawnIsTriggerCombined, ColliderUnderSpawnIsTriggerInfo, MessageType.Error));
                    var respawnHeightAboveCollider = general.AddMessageGroup(new MessageGroup(RespawnHeightAboveCollider, RespawnHeightAboveColliderCombined, RespawnHeightAboveColliderInfo, MessageType.Error));

                    for (var i = 0; i < sceneDescriptor.spawns.Length; i++)
                    {
                        if (sceneDescriptor.spawns[i] == null) continue;

                        var spawn = sceneDescriptor.spawns[i];

                        if (spawn.position.y < sceneDescriptor.RespawnHeightY)
                        {
                            spawnUnderRespawnHeight.AddSingleMessage(new SingleMessage(spawn.gameObject.name, Math.Abs(spawn.position.y - sceneDescriptor.RespawnHeightY).ToString(CultureInfo.InvariantCulture)).SetSelectObject(spawn.gameObject));
                        }

                        if (!Physics.Raycast(spawn.position + new Vector3(0, 0.01f, 0), Vector3.down, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (Physics.Raycast(spawn.position + new Vector3(0, 0.01f, 0), Vector3.down, out hit, Mathf.Infinity))
                            {
                                if (hit.collider.isTrigger)
                                {
                                    colliderUnderSpawnTrigger.AddSingleMessage(new SingleMessage(hit.collider.name, spawn.gameObject.name).SetSelectObject(spawn.gameObject));
                                }
                            }
                            else
                            {
                                noColliderUnderSpawn.AddSingleMessage(new SingleMessage(spawn.gameObject.name).SetSelectObject(spawn.gameObject));
                            }
                        }
                        // Round respawn height to 2 decimals to reflect in-game functionality
                        else if (Math.Round(hit.point.y, 2) <= Math.Round(sceneDescriptor.RespawnHeightY, 2))
                        {
                            respawnHeightAboveCollider.AddSingleMessage(new SingleMessage(hit.collider.gameObject.name, spawn.gameObject.name).SetSelectObject(spawn.gameObject).SetAutoFix(ChangeRespawnHeight(sceneDescriptor, hit.point.y - 100)));
                        }
                    }
                }
                else
                {
                    general.AddMessageGroup(new MessageGroup(NoSpawnPointSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
                }

#if VRC_SDK_VRCSDK2
                // Check if the world has playermods defined
                var playermods = FindObjectsOfType(typeof(VRC_PlayerMods)) as VRC_PlayerMods[];
                if (playermods.Length == 0)
                {
                    general.AddMessageGroup(new MessageGroup(NoPlayerMods, MessageType.Tips));
                }

                // Get triggers in the world
                var triggerScripts = (VRC_Trigger[]) FindObjectsOfType(typeof(VRC_Trigger));

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
                                    general.AddMessageGroup(new MessageGroup(TriggerTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
                                }
                                else if (trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC.SDKBase.VRC_Trigger.TriggerType.OnExitCollider)
                                {
                                    general.AddMessageGroup(new MessageGroup(ColliderTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
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
                    var triggerWrongLayerGroup = new MessageGroup(TriggerTriggerWrongLayer, TriggerTriggerWrongLayerCombined, TriggerTriggerWrongLayerInfo, MessageType.Warning);
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
                    optimization.AddMessageGroup(new MessageGroup(BakedOcclusionCulling, MessageType.GoodFPS));

                    var occlusionAreas = FindObjectsOfType<OcclusionArea>();

                    if (occlusionAreas.Length == 0)
                    {
                        optimization.AddMessageGroup(new MessageGroup(NoOcclusionAreas, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2019.4/Documentation/Manual/class-OcclusionArea.html"));
                    }
                    else
                    {
                        var disabledOcclusionAreasGroup = optimization.AddMessageGroup(new MessageGroup(DisabledOcclusionArea, DisabledOcclusionAreaCombined, DisabledOcclusionAreaInfo, MessageType.Warning));

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
                    optimization.AddMessageGroup(new MessageGroup(NoOcclusionCulling, MessageType.Tips).SetDocumentation("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Occlusion-Culling"));
                }

                OcclusionMessageCheck();

                // Check for possible camera problems
                var cameras = FindObjectsOfType<Camera>();

                if (cameras.Length > 0)
                {
                    var activeCamerasMessages = optimization.AddMessageGroup(new MessageGroup(ActiveCameraOutputtingToRenderTexture, ActiveCameraOutputtingToRenderTextureCombined, ActiveCameraOutputtingToRenderTextureInfo, MessageType.BadFPS));
                    var cameraDepthWarning = general.AddMessageGroup(new MessageGroup(ActiveCameraWithOverZeroDepth, ActiveCameraWithOverZeroDepthCombined, ActiveCameraWithOverZeroDepthInfo, MessageType.Error));

                    for (var i = 0; i < cameras.Length; i++)
                    {
                        var camera = cameras[i];

                        if (!camera.enabled) continue;

                        if (camera.targetTexture)
                        {
                            activeCamerasMessages.AddSingleMessage(new SingleMessage(camera.name).SetSelectObject(camera.gameObject));
                        }
                        else if (camera.depth > 0 && camera.targetDisplay == 0)
                        {
                            cameraDepthWarning.AddSingleMessage(new SingleMessage(camera.name).SetSelectObject(camera.gameObject));
                        }
                    }
                }

                // Get active mirrors in the world and complain about them
                var mirrors = FindObjectsOfType(typeof(VRC_MirrorReflection)) as VRC_MirrorReflection[];

                if (mirrors.Length > 0)
                {
                    var activeCamerasMessage = new MessageGroup(MirrorONByDefault, MirrorONByDefaultCombined, MirrorONByDefaultInfo, MessageType.BadFPS);
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
                        lighting.AddMessageGroup(new MessageGroup(AmbientModeSetToCustom, MessageType.Error).AddSingleMessage(new SingleMessage(SetAmbientMode(AmbientMode.Skybox))));
                        break;
                    case AmbientMode.Flat:
                        lighting.AddMessageGroup(new MessageGroup(SingleColorEnvironmentLighting, MessageType.Tips));
                        break;
                }

                if (Helper.GetBrightness(RenderSettings.ambientLight) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Flat) ||
                    Helper.GetBrightness(RenderSettings.ambientSkyColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight) ||
                    Helper.GetBrightness(RenderSettings.ambientEquatorColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight) ||
                    Helper.GetBrightness(RenderSettings.ambientGroundColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight))
                {
                    lighting.AddMessageGroup(new MessageGroup(DarkEnvironmentLighting, MessageType.Tips));
                }

                if (RenderSettings.defaultReflectionMode.Equals(DefaultReflectionMode.Custom) && !RenderSettings.customReflection)
                {
                    lighting.AddMessageGroup(new MessageGroup(CustomEnvironmentReflectionsNull, MessageType.Error).AddSingleMessage(new SingleMessage(SetEnviromentReflections(DefaultReflectionMode.Skybox))));
                }

                var bakedLighting = false;
                var xatlasUnwrapper = false;

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
                            lighting.AddMessageGroup(new MessageGroup(ShrnmDirectionalModeBakeryError, MessageType.Error).SetDocumentation("https://github.com/Merlin-san/VRC-Bakery-Adapter"));
                        }

                        break;
                }

                if (bakerySettings.renderSettingsUnwrapper == 1)
                {
                    xatlasUnwrapper = true;
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
                        var notEditorOnlyGroup = new MessageGroup(BakeryLightNotSetEditorOnly, BakeryLightNotSetEditorOnlyCombined, BakeryLightNotSetEditorOnlyInfo, MessageType.Warning);
                        foreach (var item in notEditorOnly)
                        {
                            notEditorOnlyGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(SetGameObjectTag(item, "EditorOnly")).SetSelectObject(item));
                        }

                        lighting.AddMessageGroup(notEditorOnlyGroup.SetGroupAutoFix(SetGameObjectTag(notEditorOnly.ToArray(), "EditorOnly")));
                    }

                    if (unityLightOnBakeryLight.Count > 0)
                    {
                        var unityLightGroup = new MessageGroup(BakeryLightUnityLight, BakeryLightUnityLightCombined, BakeryLightUnityLightInfo, MessageType.Warning);
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

                    if (androidBuildPlatform && EditorUserBuildSettings.androidBuildSubtarget == MobileTextureSubtarget.Generic)
                    {
                        var lightmaps = LightmapSettings.lightmaps;

                        var androidCompressionGroup = lighting.AddMessageGroup(new MessageGroup(QuestLightmapCompressionOverride, QuestLightmapCompressionOverrideCombined, QuestLightmapCompressionOverrideInfo, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2019.4/Documentation/Manual/class-TextureImporter.html"));

                        for (var i = 0; i < lightmaps.Length; i++)
                        {
                            Object lightmap = lightmaps[i].lightmapColor;

                            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lightmaps[i].lightmapColor)) as TextureImporter;

                            var platformSettings = textureImporter.GetPlatformTextureSettings("Android");

                            if (!platformSettings.overridden)
                            {
                                androidCompressionGroup.AddSingleMessage(new SingleMessage(lightmap.name).SetAssetPath(textureImporter.assetPath));
                            }
                        }
                    }
                }

                var probes = LightmapSettings.lightProbes;

                // If the scene has baked lights complain about stuff important to baked lighting missing
                if (bakedLighting)
                {
                    // Count lightmaps and suggest to use bigger lightmaps if needed
                    var lightMapSize = LightmapEditorSettings.maxAtlasSize;
                    if (lightMapSize < 2048 && LightmapSettings.lightmaps.Length >= 4)
                    {
                        if (LightmapSettings.lightmaps[0] != null)
                        {
                            var lightmap = LightmapSettings.lightmaps[0];

                            if (lightmap.lightmapColor != null && lightmap.lightmapColor.height != 4096)
                            {
                                lighting.AddMessageGroup(new MessageGroup(ConsiderLargerLightmaps, MessageType.Tips).AddSingleMessage(new SingleMessage(lightMapSize.ToString())));
                            }
                        }
                    }

                    if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU) && lightMapSize == 4096 && SystemInfo.graphicsMemorySize < 12000)
                    {
                        lighting.AddMessageGroup(new MessageGroup(ConsiderSmallerLightmaps, MessageType.Warning).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(2048))));
                    }

                    // Count how many light probes the scene has
                    long probeCounter = 0;
                    long bakedProbes = probes != null ? probes.count : 0;

                    var lightprobegroups = FindObjectsOfType<LightProbeGroup>();

                    var overlappingLightProbesGroup = new MessageGroup(OverlappingLightProbes, OverlappingLightProbesCombined, OverlappingLightProbesInfo, MessageType.Info);

                    for (var i = 0; i < lightprobegroups.Length; i++)
                    {
                        if (lightprobegroups[i].probePositions.GroupBy(p => p).Any(g => g.Count() > 1))
                        {
                            overlappingLightProbesGroup.AddSingleMessage(new SingleMessage(lightprobegroups[i].name, (lightprobegroups[i].probePositions.Length - lightprobegroups[i].probePositions.Distinct().ToArray().Length).ToString()).SetSelectObject(lightprobegroups[i].gameObject).SetAutoFix(RemoveOverlappingLightProbes(lightprobegroups[i])));
                        }

                        probeCounter += lightprobegroups[i].probePositions.Length;
                    }

                    if (probeCounter > 0)
                    {
                        if (probeCounter - bakedProbes < 0)
                        {
                            lighting.AddMessageGroup(new MessageGroup(LightProbesRemovedNotReBaked, MessageType.Warning).AddSingleMessage(new SingleMessage(bakedProbes.ToString(), probeCounter.ToString())));
                        }
                        else
                        {
                            if (bakedProbes - (0.9 * probeCounter) < 0)
                            {
                                lighting.AddMessageGroup(new MessageGroup(LightProbeCountNotBaked, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"), (probeCounter - bakedProbes).ToString("n0"))));
                            }
                            else
                            {
                                lighting.AddMessageGroup(new MessageGroup(LightProbeCount, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"))));
                            }
                        }
                    }

                    if (overlappingLightProbesGroup.GetTotalCount() > 0)
                    {
                        if (overlappingLightProbesGroup.GetTotalCount() > 1)
                        {
                            overlappingLightProbesGroup.SetGroupAutoFix(RemoveOverlappingLightProbes(lightprobegroups));
                        }

                        lighting.AddMessageGroup(overlappingLightProbesGroup);
                    }

                    // Since the scene has baked lights complain if there's no lightprobes
                    else if (probes == null && probeCounter == 0)
                    {
                        lighting.AddMessageGroup(new MessageGroup(NoLightProbes, MessageType.Info).SetDocumentation("https://docs.unity3d.com/2019.4/Documentation/Manual/LightProbes.html"));
                    }

                    // Check lighting data asset size if it exists
                    if (Lightmapping.lightingDataAsset != null)
                    {
                        var pathTo = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                        var length = new FileInfo(pathTo).Length;
                        lighting.AddMessageGroup(new MessageGroup(LightingDataAssetInfo, MessageType.Info).AddSingleMessage(new SingleMessage((length / 1024.0f / 1024.0f).ToString("F2"))));
                    }

                    if (nonBakedLights.Count != 0)
                    {
                        var nonBakedLightsGroup = new MessageGroup(NonBakedBakedLight, NonBakedBakedLightCombined, NonBakedBakedLightInfo, MessageType.Warning);
                        for (var i = 0; i < nonBakedLights.Count; i++)
                        {
                            nonBakedLightsGroup.AddSingleMessage(new SingleMessage(nonBakedLights[i].name).SetSelectObject(nonBakedLights[i].gameObject));
                        }

                        lighting.AddMessageGroup(nonBakedLightsGroup);
                    }
                }
                else
                {
                    lighting.AddMessageGroup(new MessageGroup(androidBuildPlatform ? QuestBakedLightingWarning : LightsNotBaked, androidBuildPlatform ? MessageType.Warning : MessageType.Tips)
                        .SetDocumentation("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Light-Baking"));
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
                    lighting.AddMessageGroup(new MessageGroup(NoReflectionProbes, MessageType.Tips).SetDocumentation("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Reflection-Probes"));
                }
                else if (reflectionProbeCount > 0)
                {
                    lighting.AddMessageGroup(new MessageGroup(ReflectionProbeCountText, MessageType.Info).AddSingleMessage(new SingleMessage(reflectionProbeCount.ToString())));

                    if (unbakedprobes.Count > 0)
                    {
                        var probesUnbakedGroup = new MessageGroup(ReflectionProbesSomeUnbaked, ReflectionProbesSomeUnbakedCombined, MessageType.Warning);

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
                if (sceneDescriptor.ReferenceCamera != null && sceneDescriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)))
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

                        postProcessing.AddMessageGroup(new MessageGroup(PostProcessingNoResourcesSet, MessageType.Error).AddSingleMessage(singleMessage));

                        var resources = (PostProcessResources) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("d82512f9c8e5d4a4d938b575d47f88d4"), typeof(PostProcessResources));

                        if (resources != null) singleMessage.SetAutoFix(SetPostProcessingLayerResources(mainPostProcessLayer, resources));
                    }
                }

                // If post processing is imported but no setup isn't detected show a message
                if (postProcessVolumes.Length == 0 && mainPostProcessLayer is null)
                {
                    postProcessing.AddMessageGroup(new MessageGroup(PostProcessingImportedButNotSetup, MessageType.Info));
                }
                else
                {
                    // Check the scene view for post processing effects being off
                    var sceneViewState = SceneView.lastActiveSceneView.sceneViewState;
                    if (!sceneViewState.showImageEffects)
                    {
                        postProcessing.AddMessageGroup(new MessageGroup(PostProcessingDisabledInSceneView, MessageType.Info).SetGroupAutoFix(SetPostProcessingInScene(sceneViewState, true)));
                    }

                    // Start by checking if reference camera has been set in the Scene Descriptor
                    if (!sceneDescriptor.ReferenceCamera)
                    {
                        var noReferenceCameraMessage = new SingleMessage(sceneDescriptor.gameObject);

                        if (Camera.main && Camera.main.GetComponent<PostProcessLayer>())
                        {
                            noReferenceCameraMessage.SetAutoFix(SetReferenceCamera(sceneDescriptor, Camera.main));
                        }

                        postProcessing.AddMessageGroup(new MessageGroup(NoReferenceCameraSetPp, MessageType.Warning).AddSingleMessage(noReferenceCameraMessage));
                    }
                    else
                    {
                        // Check for post process volumes in the scene
                        if (postProcessVolumes.Length == 0)
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingVolumes, MessageType.Info));
                        }
                        else
                        {
                            var postprocessLayer = sceneDescriptor.ReferenceCamera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                            if (postprocessLayer is null)
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(ReferenceCameraNoPostProcessingLayer, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.gameObject)));
                            }

                            if (postprocessLayer)
                            {
                                var volumeLayer = postprocessLayer.volumeLayer;
                                if (volumeLayer == 0)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(VolumeBlendingLayerNotSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.gameObject)));
                                }

                                // Check for usage of reserved layers since they break post processing
                                var numbersFromMask = Helper.GetAllLayerNumbersFromMask(volumeLayer);
                                if (numbersFromMask.Contains(19) | numbersFromMask.Contains(20) | numbersFromMask.Contains(21))
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(PostProcessLayerUsingReservedLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject.name).SetSelectObject(postprocessLayer.gameObject)));
                                }

                                var noProfileSet = postProcessing.AddMessageGroup(new MessageGroup(NoProfileSet, NoProfileSetCombined, MessageType.Error));
                                var volumeNoGlobalNoCollider = postProcessing.AddMessageGroup(new MessageGroup(PostProcessingVolumeNotGlobalNoCollider, PostProcessingVolumeNotGlobalNoColliderCombined, PostProcessingVolumeNotGlobalNoColliderInfo, MessageType.Error));
                                var matchingVolumes = new List<PostProcessVolume>();
                                foreach (var postProcessVolume in postProcessVolumes)
                                {
                                    // Check if the layer matches the cameras post processing layer
                                    if (volumeLayer != 0 && (postprocessLayer.volumeLayer == (postprocessLayer.volumeLayer | (1 << postProcessVolume.gameObject.layer))))
                                    {
                                        matchingVolumes.Add(postProcessVolume);
                                    }

                                    // Check if the volume has a profile set
                                    if (postProcessVolume.profile is null && postProcessVolume.sharedProfile is null)
                                    {
                                        noProfileSet.AddSingleMessage(new SingleMessage(postProcessVolume.gameObject.name).SetSelectObject(postProcessVolume.gameObject));
                                    }

                                    // Check if the collider is either global or has a collider on it
                                    if (!postProcessVolume.isGlobal && !postProcessVolume.GetComponent<Collider>())
                                    {
                                        volumeNoGlobalNoCollider.AddSingleMessage(new SingleMessage(postProcessVolume.name).SetSelectObject(postProcessVolume.gameObject));
                                    }
                                }

                                if (matchingVolumes.Count == 0)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(NoMatchingLayersFound, MessageType.Warning).AddSingleMessage(new SingleMessage(Helper.GetAllLayersFromMask(postprocessLayer.volumeLayer)).SetSelectObject(postprocessLayer.gameObject)));
                                }

                                var noTonemapper = true;

                                // Go trough the profile settings and see if any bad one's are used
                                foreach (var postProcessVolume in matchingVolumes)
                                {
                                    var postProcessProfile = postProcessVolume.profile ? postProcessVolume.profile : postProcessVolume.sharedProfile;

                                    if (postProcessProfile is null) continue;

                                    var ambientOcclusion = postProcessProfile.GetSetting<AmbientOcclusion>();
                                    if (ambientOcclusion && ambientOcclusion.enabled && ambientOcclusion.active)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(NoAmbientOcclusion, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.AmbientOcclusion)).SetSelectObject(postProcessVolume.gameObject)));
                                    }

                                    var screenSpaceReflections = postProcessProfile.GetSetting<ScreenSpaceReflections>();
                                    if (screenSpaceReflections && screenSpaceReflections.enabled && screenSpaceReflections.active)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(ScreenSpaceReflectionsWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.ScreenSpaceReflections)).SetSelectObject(postProcessVolume.gameObject)));
                                    }

                                    var vignette = postProcessProfile.GetSetting<Vignette>();
                                    if (vignette && vignette.enabled && vignette.active)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(VignetteWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }

                                    if (postProcessVolume.isGlobal)
                                    {
                                        var colorGrading = postProcessProfile.GetSetting<ColorGrading>();
                                        if (colorGrading && colorGrading.enabled && colorGrading.active)
                                        {
                                            if (colorGrading.tonemapper.overrideState && colorGrading.tonemapper.value != Tonemapper.None)
                                            {
                                                noTonemapper = false;
                                            }
                                        }

                                        var bloom = postProcessProfile.GetSetting<Bloom>();
                                        if (bloom && bloom.enabled && bloom.active)
                                        {
                                            if (bloom.intensity.overrideState && bloom.intensity.value > 0.3f)
                                            {
                                                postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomIntensity, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                            }

                                            if (bloom.threshold.overrideState && bloom.threshold.value > 1f)
                                            {
                                                postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomThreshold, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                            }

                                            if (bloom.dirtTexture.overrideState && bloom.dirtTexture.value || bloom.dirtIntensity.overrideState && bloom.dirtIntensity.value > 0)
                                            {
                                                postProcessing.AddMessageGroup(new MessageGroup(NoBloomDirtInVR, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePpEffect.BloomDirt)).SetSelectObject(postProcessVolume.gameObject)));
                                            }
                                        }

                                        var depthOfField = postProcessProfile.GetSetting<DepthOfField>();
                                        if (depthOfField && depthOfField.enabled && depthOfField.active)
                                        {
                                            postProcessing.AddMessageGroup(new MessageGroup(DepthOfFieldWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                        }
                                    }
                                }

                                if (noTonemapper)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(TonemapperMissing, MessageType.Tips).SetDocumentation("https://gitlab.com/s-ilent/SCSS/-/wikis/Other/Post-Processing#colour-grading"));
                                }
                            }
                        }
                    }

                    if (!postProcessing.HasMessages())
                    {
                        postProcessing.AddMessageGroup(new MessageGroup(NoProblemsFoundInPp, MessageType.Info));
                    }
                }
#else
                postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingImported, MessageType.Info));
#endif

                // GameObject checks

                var importers = new List<ModelImporter>();

                var unCrunchedTextures = new List<Texture>();
                var badShaders = 0;
                var textureCount = 0;

                var missingShaders = new List<Material>();
                var selectablesNotNone = new List<Selectable>();
                var scrollRectsScrollSensitivityNotZero = new List<ScrollRect>();
                var legacyBlendShapes = new List<ModelImporter>();

                var checkedMaterials = new List<Material>();
                var checkedShaders = new Dictionary<Shader, CheckedShaderProperties>();

                var mirrorsDefaultLayers = optimization.AddMessageGroup(new MessageGroup(MirrorWithDefaultLayers, MirrorWithDefaultLayersCombined, MirrorWithDefaultLayersInfo, MessageType.Tips));
                var legacyBlendShapeIssues = general.AddMessageGroup(new MessageGroup(LegacyBlendShapeIssues, LegacyBlendShapeIssuesCombined, LegacyBlendShapeIssuesInfo, MessageType.Warning));
                var grabPassShaders = general.AddMessageGroup(new MessageGroup(MaterialWithGrabPassShader, MaterialWithGrabPassShaderCombined, androidBuildPlatform ? MaterialWithGrabPassShaderInfoPC : MaterialWithGrabPassShaderInfoQuest, androidBuildPlatform ? MessageType.Error : MessageType.Info));
                var materialWithNonWhitelistedShader = general.AddMessageGroup(new MessageGroup(MaterialWithNonWhitelistedShader, MaterialWithNonWhitelistedShaderCombined, MaterialWithNonWhitelistedShaderInfo, MessageType.Warning).SetDocumentation("https://docs.vrchat.com/docs/quest-content-limitations#shaders"));
                var uiElementNavigation = general.AddMessageGroup(new MessageGroup(UIElementWithNavigationNotNone, UIElementWithNavigationNotNoneCombined, UIElementWithNavigationNotNoneInfo, MessageType.Tips));
                var scrollRectScrollSensitivity = general.AddMessageGroup(new MessageGroup(ScrollRectWithScrollSensitivityNotZero, ScrollRectWithScrollSensitivityNotZeroCombined, ScrollRectWithScrollSensitivityNotZeroInfo, MessageType.Tips));
                var nullTriggerReceivers = general.AddMessageGroup(new MessageGroup(NullTriggerReceiver, NullTriggerReceiverCombined, NullTriggerReceiverInfo, MessageType.Info));
                var textMeshStatic = general.AddMessageGroup(new MessageGroup(TextMeshLightmapStatic, TextMeshLightmapStaticCombined, TextMeshLightmapStaticInfo, MessageType.Warning));
                var unsupportedCompressionFormatQuest = general.AddMessageGroup(new MessageGroup(UnsupportedCompressionFormatQuest, UnsupportedCompressionFormatQuestCombined, UnsupportedCompressionFormatQuestInfo, MessageType.Error).SetDocumentation("https://docs.unity3d.com/2019.4/Documentation/Manual/class-TextureImporterOverride.html"));

                var allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                for (var i = 0; i < allGameObjects.Length; i++)
                {
                    var gameObject = allGameObjects[i] as GameObject;

                    if (gameObject.hideFlags != HideFlags.None || EditorUtility.IsPersistent(gameObject.transform.root.gameObject)) continue;

                    var staticEditorFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
                    var hasMeshRenderer = false;
                    var renderers = gameObject.GetComponents<Renderer>();

                    for (var k = 0; k < renderers.Length; k++)
                    {
                        var renderer = renderers[k];

                        if (renderer.GetType() == typeof(MeshRenderer))
                        {
                            hasMeshRenderer = true;

                            // If baked lighting in the scene check for lightmap uvs
                            if (bakedLighting && (staticEditorFlags & StaticEditorFlags.ContributeGI) != 0 && !xatlasUnwrapper)
                            {
                                var meshFilter = gameObject.GetComponent<MeshFilter>();

                                if (meshFilter != null)
                                {
                                    var sharedMesh = meshFilter.sharedMesh;

                                    if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sharedMesh)) != null)
                                    {
                                        var modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sharedMesh)) as ModelImporter;

                                        if (!importers.Contains(modelImporter))
                                        {
                                            if (modelImporter != null)
                                            {
                                                var so = new SerializedObject(renderer);

                                                if (!modelImporter.generateSecondaryUV && sharedMesh.uv2.Length == 0 && so.FindProperty("m_ScaleInLightmap").floatValue != 0)
                                                {
                                                    importers.Add(modelImporter);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                        {
                            var skinnedMesh = (SkinnedMeshRenderer) renderer;
                            var sharedMesh = skinnedMesh.sharedMesh;
                            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sharedMesh)) as ModelImporter;

                            if (importer != null)
                            {
                                if (sharedMesh.blendShapeCount > 0 && importer.importBlendShapeNormals == ModelImporterNormals.Calculate && !ModelImporterUtil.GetLegacyBlendShapeNormals(importer))
                                {
                                    legacyBlendShapes.Add(importer);
                                    legacyBlendShapeIssues.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(sharedMesh)), EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(sharedMesh))).SetAssetPath(importer.assetPath).SetAutoFix(SetLegacyBlendShapeNormals(importer)));
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

                            if (androidBuildPlatform && !Validation.WorldShaderWhiteList.Contains(shader.name))
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

                            if (!checkedShaders.ContainsKey(shader) && AssetDatabase.GetAssetPath(shader) != null)
                            {
                                var assetPath = AssetDatabase.GetAssetPath(shader);

                                if (File.Exists(assetPath))
                                {
                                    var checkedShaderProperties = new CheckedShaderProperties();

                                    // Read shader file to string
                                    var word = File.ReadAllText(assetPath);

                                    // Strip comments
                                    word = Regex.Replace(word, "(\\/\\/.*)|(\\/\\*)(.*)(\\*\\/)", "");

                                    // Match for GrabPass and check if it's active
                                    var grabPassMatch = Regex.Match(word, "GrabPass\\s*{[\\s\\S]*?}");
                                    if (grabPassMatch.Success)
                                    {
                                        checkedShaderProperties.IncludesGrabPass = true;
                                        var lightModeTags = Regex.Matches(grabPassMatch.Value, "[\"|']LightMode[\"|']\\s*=\\s*[\"|'](\\w*)[\"|']");

                                        if (lightModeTags.Count > 0)
                                        {
                                            for (var j = 0; j < lightModeTags.Count; j++)
                                            {
                                                checkedShaderProperties.GrabPassLightModeTags.Add(lightModeTags[j].Groups[1].Value);
                                            }
                                        }
                                    }

                                    checkedShaders.Add(shader, checkedShaderProperties);
                                }
                            }

                            if (checkedShaders.ContainsKey(shader))
                            {
                                var checkedShader = checkedShaders[shader];
                                if (checkedShader.IncludesGrabPass)
                                {
                                    var grabPassActive = false;
                                    if (checkedShader.GrabPassLightModeTags.Count > 0)
                                    {
                                        for (var j = 0; j < checkedShader.GrabPassLightModeTags.Count; j++)
                                        {
                                            if (material.GetShaderPassEnabled(checkedShader.GrabPassLightModeTags[j])) grabPassActive = true;
                                        }
                                    }
                                    else
                                    {
                                        grabPassActive = true;
                                    }

                                    if (grabPassActive) grabPassShaders.AddSingleMessage(new SingleMessage(material.name, shader.name).SetAssetPath(AssetDatabase.GetAssetPath(material)));
                                }
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
                                        var assetPath = AssetDatabase.GetAssetPath(texture);
                                        var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                                        if (textureImporter != null)
                                        {
                                            if (!unCrunchedTextures.Contains(texture))
                                            {
                                                textureCount++;
                                            }

                                            var platformTextureSettings = textureImporter.GetPlatformTextureSettings("Android");
                                            if (platformTextureSettings.overridden && Validation.UnsupportedCompressionFormatsQuest.Contains(platformTextureSettings.format))
                                            {
                                                unsupportedCompressionFormatQuest.AddSingleMessage(new SingleMessage(texture.name, platformTextureSettings.format.ToString()).SetAssetPath(assetPath));
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

                    if (hasMeshRenderer)
                    {
                        if ((staticEditorFlags & StaticEditorFlags.ContributeGI) != 0 && gameObject.GetComponent<TextMesh>())
                        {
                            textMeshStatic.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                        }

                        if (gameObject.GetComponent<VRC_MirrorReflection>())
                        {
                            var mirrorMask = gameObject.GetComponent<VRC_MirrorReflection>().m_ReflectLayers;

                            if (mirrorMask.value == -1025)
                            {
                                mirrorsDefaultLayers.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                            }
                        }
                    }

                    var selectable = gameObject.GetComponent<Selectable>();
                    if (selectable != null)
                    {
                        if (selectable.navigation.mode != Navigation.Mode.None)
                        {
                            uiElementNavigation.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject).SetAutoFix(SetSelectableNavigationMode(selectable, Navigation.Mode.None)));

                            selectablesNotNone.Add(selectable);
                        }
                    }

                    var scrollRect = gameObject.GetComponent<ScrollRect>();
                    if (scrollRect != null)
                    {
                        if (scrollRect.scrollSensitivity != 0)
                        {
                            scrollRectScrollSensitivity.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject).SetAutoFix(SetScrollRectScrollSensitivity(scrollRect, 0)));

                            scrollRectsScrollSensitivityNotZero.Add(scrollRect);
                        }
                    }

#if VRC_SDK_VRCSDK2
                    var trigger = gameObject.GetComponent<VRC_Trigger>();
                    if (trigger != null)
                    {
                        var missingFound = false;
                        for (var j = 0; j < trigger.Triggers.Count; j++)
                        {
                            var triggerScript = trigger.Triggers[j];
                            for (var k = 0; k < triggerScript.Events.Count; k++)
                            {
                                var parameterObjects = triggerScript.Events[k].ParameterObjects;

                                if (parameterObjects.Length == 0)
                                {
                                    nullTriggerReceivers.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                                    missingFound = true;
                                    break;
                                }

                                for (var l = 0; l < parameterObjects.Length; l++)
                                {
                                    if (parameterObjects[l].gameObject == null)
                                    {
                                        nullTriggerReceivers.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                                        missingFound = true;
                                        break;
                                    }
                                }
                            }

                            if (missingFound) break;
                        }
                    }
#endif
                }

                if (legacyBlendShapes.Count > 1)
                {
                    legacyBlendShapeIssues.SetGroupAutoFix(SetLegacyBlendShapeNormals(legacyBlendShapes.ToArray()));
                }

                if (selectablesNotNone.Count > 1)
                {
                    uiElementNavigation.SetGroupAutoFix(SetSelectableNavigationMode(selectablesNotNone.ToArray(), Navigation.Mode.None));
                }

                if (scrollRectsScrollSensitivityNotZero.Count > 1)
                {
                    scrollRectScrollSensitivity.SetGroupAutoFix(SetScrollRectScrollSensitivity(scrollRectsScrollSensitivityNotZero.ToArray(), 0));
                }

                // If more than 10% of shaders used in scene are toon shaders to leave room for people using them for avatar displays
                if (checkedMaterials.Count > 0)
                {
                    if (badShaders / checkedMaterials.Count * 100 > 10)
                    {
                        optimization.AddMessageGroup(new MessageGroup(NoToonShaders, MessageType.Warning));
                    }
                }

                // Suggest to crunch textures if there are any uncrunched textures found
                if (textureCount > 0)
                {
                    var percent = (int) ((float) unCrunchedTextures.Count / (float) textureCount * 100f);
                    if (percent > 20)
                    {
                        optimization.AddMessageGroup(new MessageGroup(NonCrunchedTextures, MessageType.Tips).AddSingleMessage(new SingleMessage(percent.ToString()).SetAutoFix(MassTextureImporter.ShowWindow)));
                    }
                }

                var modelsCount = importers.Count;
                if (modelsCount > 0)
                {
                    var noUVGroup = new MessageGroup(NoLightmapUV, NoLightmapUVCombined, NoLightmapUVInfo, MessageType.Warning);
                    for (var i = 0; i < modelsCount; i++)
                    {
                        var modelImporter = importers[i];

                        noUVGroup.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(modelImporter))).SetAutoFix(SetGenerateLightmapUV(modelImporter)).SetAssetPath(modelImporter.assetPath));
                    }

                    lighting.AddMessageGroup(noUVGroup.SetGroupAutoFix(SetGenerateLightmapUV(importers)).SetDocumentation("https://docs.unity3d.com/2019.4/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html"));
                }

                var missingShadersCount = missingShaders.Count;
                if (missingShadersCount > 0)
                {
                    var missingShadersGroup = new MessageGroup(MissingShaderWarning, MissingShaderWarningCombined, MissingShaderWarningInfo, MessageType.Error);
                    for (var i = 0; i < missingShaders.Count; i++)
                    {
                        missingShadersGroup.AddSingleMessage(new SingleMessage(missingShaders[i].name).SetAssetPath(AssetDatabase.GetAssetPath(missingShaders[i])).SetAutoFix(ChangeShader(missingShaders[i], "Standard")));
                    }

                    general.AddMessageGroup(missingShadersGroup.SetGroupAutoFix(ChangeShader(missingShaders.ToArray(), "Standard")));
                }
            }
            catch (Exception exception)
            {
                general.AddMessageGroup(new MessageGroup(HeyYouFoundABug, MessageType.Error)).AddSingleMessage(new SingleMessage(exception.Message.Replace("\n", " ").Replace("\r", ""), Regex.Matches(exception.StackTrace, "(?<=\\.cs:).*(?<=\\S)")[0].ToString()));
                Debug.LogError(exception);
                autoRecheck = false;
            }
        }

        private void OnFocus()
        {
            if (initDone)
            {
                RefreshBuild();
            }

            recheck = true;
        }

        private const string LastBuild = "Library/LastBuild.buildreport";

        private const string BuildReportDir = "Assets/_LastBuild/";

        private const string LastBuildReportPath = "Assets/_LastBuild/LastBuild.buildreport";
        private const string WindowsBuildReportPath = "Assets/_LastBuild/LastWindowsBuild.buildreport";
        private const string QuestBuildReportPath = "Assets/_LastBuild/LastQuestBuild.buildreport";

        [SerializeField] private BuildReport buildReportWindows;
        [SerializeField] private BuildReport buildReportQuest;

        [SerializeField] private TreeViewState treeViewState;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;

        private BuildReportTreeView buildReportTreeView;
        private SearchField searchField;

        private void RefreshBuild()
        {
#if VRWT_BENCHMARK
            CheckTime.Restart();
#endif
            if (!Directory.Exists(BuildReportDir))
                Directory.CreateDirectory(BuildReportDir);
            if (File.Exists(LastBuild) && (!File.Exists(LastBuildReportPath) || File.GetLastWriteTime(LastBuild) > File.GetLastWriteTime(LastBuildReportPath)))
            {
                File.Copy(LastBuild, LastBuildReportPath, true);
                AssetDatabase.ImportAsset(LastBuildReportPath);
            }

            var newBuildSet = false;
            if (File.Exists(LastBuildReportPath))
            {
                switch (AssetDatabase.LoadAssetAtPath<BuildReport>(LastBuildReportPath).summary.platform)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        if (File.GetLastWriteTime(LastBuildReportPath) > File.GetLastWriteTime(WindowsBuildReportPath))
                        {
                            AssetDatabase.CopyAsset(LastBuildReportPath, WindowsBuildReportPath);
                            buildReportWindows = (BuildReport) AssetDatabase.LoadAssetAtPath(WindowsBuildReportPath, typeof(BuildReport));
                            newBuildSet = true;
                        }

                        break;
                    case BuildTarget.Android:
                        if (File.GetLastWriteTime(LastBuildReportPath) > File.GetLastWriteTime(QuestBuildReportPath))
                        {
                            AssetDatabase.CopyAsset(LastBuildReportPath, QuestBuildReportPath);
                            buildReportQuest = (BuildReport) AssetDatabase.LoadAssetAtPath(QuestBuildReportPath, typeof(BuildReport));
                            newBuildSet = true;
                        }

                        break;
                }
            }

            if (buildReportWindows is null && File.Exists(WindowsBuildReportPath))
            {
                buildReportWindows = (BuildReport) AssetDatabase.LoadAssetAtPath(WindowsBuildReportPath, typeof(BuildReport));
            }

            if (buildReportQuest is null && File.Exists(QuestBuildReportPath))
            {
                buildReportQuest = (BuildReport) AssetDatabase.LoadAssetAtPath(QuestBuildReportPath, typeof(BuildReport));
            }

            if (buildReportInitDone)
            {
                BuildReport report = null;

                if (newBuildSet)
                {
                    switch (Helper.BuildPlatform())
                    {
                        case RuntimePlatform.WindowsPlayer:
                            report = buildReportWindows;
                            selectedBuildReport = 0;
                            break;
                        case RuntimePlatform.Android:
                            report = buildReportQuest;
                            selectedBuildReport = 1;
                            break;
                    }
                }
                else
                {
                    if (selectedBuildReport == 1 && buildReportQuest != null)
                    {
                        report = buildReportQuest;
                    }
                    else
                    {
                        selectedBuildReport = 0;
                        report = buildReportWindows;
                    }
                }

                buildReportTreeView.SetReport(report);
            }

#if VRWT_BENCHMARK
            CheckTime.Stop();
            Debug.Log($"Refreshed build reports in: {CheckTime.ElapsedMilliseconds} ms.");
#endif
        }

        [NonSerialized] private bool initDone;
        [NonSerialized] private bool buildReportInitDone;

        [SerializeField] private MessageCategoryList masterList;

        [SerializeField] private MessageCategory general;
        [SerializeField] private MessageCategory optimization;
        [SerializeField] private MessageCategory lighting;
        [SerializeField] private MessageCategory postProcessing;

        private void InitWhenNeeded()
        {
            if (!initDone)
            {
#if VRWT_BENCHMARK
                CheckTime.Restart();
#endif
                RefreshBuild();

                if (masterList is null)
                    masterList = new MessageCategoryList();

                general = masterList.CreateOrGetCategory("General");

                optimization = masterList.CreateOrGetCategory("Optimization");

                lighting = masterList.CreateOrGetCategory("Lighting");

                postProcessing = masterList.CreateOrGetCategory("Post Processing");

#if VRC_SDK_VRCSDK3 && UDON
                projectType = ProjectType.World;
#elif VRC_SDK_VRCSDK3 && !UDON
                projectType = ProjectType.Avatar;
#elif VRC_SDK_VRCSDK2
                projectType = FindObjectsOfType(typeof(VRC_AvatarDescriptor)) is VRC_AvatarDescriptor[] avatarDescriptors && avatarDescriptors.Length > 0 ? ProjectType.Avatar : ProjectType.World;
#else
                projectType = ProjectType.Generic;
#endif

                initDone = true;
#if VRWT_BENCHMARK
                CheckTime.Stop();
                Debug.Log($"Main initialization done in: {CheckTime.ElapsedMilliseconds} ms.");
#endif
            }

            if (!buildReportInitDone && tab == 1)
            {
#if VRWT_BENCHMARK
                CheckTime.Restart();
#endif
                var firstInit = multiColumnHeaderState == null;
                var headerState = BuildReportTreeView.CreateDefaultMultiColumnHeaderState(EditorGUIUtility.currentViewWidth - 121);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
                multiColumnHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                if (treeViewState is null)
                {
                    treeViewState = new TreeViewState();
                }

                BuildReport report;
                if (selectedBuildReport == 1 && buildReportQuest != null)
                {
                    report = buildReportQuest;
                }
                else
                {
                    selectedBuildReport = 0;
                    report = buildReportWindows;
                }

                buildReportTreeView = new BuildReportTreeView(treeViewState, multiColumnHeader, report);
                searchField = new SearchField();
                searchField.downOrUpArrowKeyPressed += buildReportTreeView.SetFocusAndEnsureSelectedItem;

                buildReportInitDone = true;
#if VRWT_BENCHMARK
                CheckTime.Stop();
                Debug.Log($"Build report initialization done in: {CheckTime.ElapsedMilliseconds} ms.");
#endif
            }
        }

        private static readonly Stopwatch CheckTime = new Stopwatch();

        private void Refresh()
        {
            if (!EditorApplication.isPlaying && recheck && autoRecheck && tab == 0)
            {
                // Check for bloat in occlusion cache
                if (occlusionCacheFiles == 0 && Directory.Exists("Library/Occlusion/"))
                {
                    Task.Run(CountOcclusionCacheFiles);
                }

                CheckTime.Restart();

                switch (projectType)
                {
                    case ProjectType.World:
                        CheckScene();
                        break;
                }

                CheckTime.Stop();

                if (CheckTime.ElapsedMilliseconds >= 500)
                {
                    autoRecheck = false;
                }

#if VRWT_BENCHMARK
                Debug.Log("Checks done in: " + CheckTime.ElapsedMilliseconds + " ms.");
#endif

                recheck = false;
            }
        }

        private enum BuildReportType
        {
            Windows = 0,
            Quest = 1
        }

        private static readonly string[] BuildReportToolbar =
        {
            "Windows", "Quest"
        };

        private static readonly string[] MainToolbar =
        {
            "Messages", "Build Report"
        };

        [SerializeField] private int selectedBuildReport;
        [SerializeField] private bool overallStatsFoldout;
        [SerializeField] private bool buildReportMessagesFoldout;

        private enum ProjectType
        {
            NotDetected,
            Generic,
            World,
            Avatar
        }

        private ProjectType projectType = ProjectType.NotDetected;

        private void OnGUI()
        {
            var current = Event.current;

            if (current.type == EventType.Layout)
            {
                InitWhenNeeded();
                Refresh();
            }

            DrawBuildReportOverviews(current);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            tab = GUILayout.Toolbar(tab, MainToolbar);

            switch (tab)
            {
                case 0:
                    MessagesTab();
                    break;
                case 1:
                    BuildReportTab();
                    break;
            }
        }

        private void DrawBuildReportOverviews(Event current)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (buildReportWindows)
                {
                    DrawOverview(buildReportWindows, "Windows");
                }

                if (buildReportQuest)
                {
                    DrawOverview(buildReportQuest, "Quest");
                }
            }

            void DrawOverview(BuildReport report, string platform)
            {
                using (var verticalScope = new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label($"Last found {platform} build:", EditorStyles.boldLabel);

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label("<b>Build size:</b> " + EditorUtility.FormatBytes((long) report.summary.totalSize), Styles.LabelRichText);

                        GUILayout.Label("<b>Build done:</b> " + report.summary.buildEndedAt.ToLocalTime(), Styles.LabelRichText);

                        GUILayout.Label("<b>Errors during build:</b> " + report.summary.totalErrors, Styles.LabelRichText);

                        GUILayout.Label("<b>Warnings during build:</b> " + report.summary.totalWarnings, Styles.LabelRichText);

                        GUILayout.Label("<b>Build result:</b> " + report.summary.result, Styles.LabelRichText);
                    }

                    if (current.type == EventType.ContextClick && verticalScope.rect.Contains(current.mousePosition))
                    {
                        var path = report.summary.outputPath;
                        var menu = new GenericMenu();

                        if (File.Exists(path))
                        {
                            menu.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(report.summary.outputPath));
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("Show in Explorer"));
                        }

                        menu.ShowAsContext();
                    }
                }
            }
        }

        private void MessagesTab()
        {
            switch (projectType)
            {
                case ProjectType.NotDetected:
                    ProjectTypeNotDetected();
                    break;
                case ProjectType.Generic:
                    ProjectTypeNotSupportedYet();
                    break;
                case ProjectType.World:
                    if (EditorApplication.isPlaying)
                    {
                        GUILayout.FlexibleSpace();

                        EditorGUILayout.LabelField("The editor is currently in play mode.", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
                        EditorGUILayout.LabelField("Stop it to see the messages.", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));

                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        if (!autoRecheck && GUILayout.Button("Refresh"))
                        {
                            recheck = true;
                            autoRecheck = true;
                        }

                        masterList.DrawTabSelector();

                        masterList.DrawMessages();
                    }

                    break;
                case ProjectType.Avatar:
                    ProjectTypeNotSupportedYet();
                    break;
            }
        }

        private void ProjectTypeNotDetected()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Current project type not detected.", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));

            GUILayout.FlexibleSpace();
        }

        private void ProjectTypeNotSupportedYet()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"{projectType} projects\nnot fully supported yet.", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));

            GUILayout.FlexibleSpace();
        }

        private void BuildReportTab()
        {
            if (buildReportInitDone)
            {
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (buildReportWindows && buildReportQuest)
                {
                    EditorGUI.BeginChangeCheck();

                    selectedBuildReport = GUILayout.Toolbar(selectedBuildReport, BuildReportToolbar, EditorStyles.toolbarButton);

                    if (EditorGUI.EndChangeCheck())
                    {
                        switch ((BuildReportType) selectedBuildReport)
                        {
                            case BuildReportType.Windows:
                                buildReportTreeView.SetReport(buildReportWindows);
                                break;
                            case BuildReportType.Quest:
                                buildReportTreeView.SetReport(buildReportQuest);
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
                    RefreshBuild();

                    if (buildReportTreeView.BuildSucceeded)
                    {
                        buildReportTreeView.Reload();
                    }
                    else
                    {
                        if (buildReportWindows != null)
                        {
                            buildReportTreeView.SetReport(buildReportWindows);
                        }
                        else if (buildReportQuest != null)
                        {
                            buildReportTreeView.SetReport(buildReportQuest);
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                buildReportTreeView.searchString = searchField.OnToolbarGUI(buildReportTreeView.searchString);

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                if (buildReportMessagesFoldout)
                {
                    buildReportTreeView.DrawMessages();
                }
                else
                {
                    if (overallStatsFoldout)
                    {
                        buildReportTreeView.DrawOverallStats();
                    }

                    var treeViewRect = EditorGUILayout.BeginVertical();

                    if (buildReportTreeView.BuildSucceeded)
                    {
                        buildReportTreeView.OnGUI(treeViewRect);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();

                        if (!buildReportTreeView.HasReport)
                        {
                            EditorGUILayout.LabelField($"No Last Build Found", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Last {BuildReportToolbar[selectedBuildReport]} Build Failed", Styles.CenteredLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                        }

                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
#endif