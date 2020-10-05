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

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    public class WorldDebugger : EditorWindow
    {
        private static Texture _badFPS;
        private static Texture _goodFPS;
        private static Texture _tips;
        private static Texture _info;
        private static Texture _error;
        private static Texture _warning;

        private static bool _recheck = true;

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
            if (!_badFPS)
                _badFPS = Resources.Load<Texture>("DebuggerIcons/Bad_FPS_Icon");
            if (!_goodFPS)
                _goodFPS = Resources.Load<Texture>("DebuggerIcons/Good_FPS_Icon");
            if (!_tips)
                _tips = Resources.Load<Texture>("DebuggerIcons/Performance_Tips");
            if (!_info)
                _info = Resources.Load<Texture>("DebuggerIcons/Performance_Info");
            if (!_error)
                _error = Resources.Load<Texture>("DebuggerIcons/Error_Icon");
            if (!_warning)
                _warning = Resources.Load<Texture>("DebuggerIcons/Warning_Icon");

            switch (infoType)
            {
                case MessageType.BadFPS:
                    return _badFPS;
                case MessageType.GoodFPS:
                    return _goodFPS;
                case MessageType.Tips:
                    return _tips;
                case MessageType.Info:
                    return _info;
                case MessageType.Error:
                    return _error;
                case MessageType.Warning:
                    return _warning;
            }

            return _info;
        }

        [Serializable]
        private class SingleMessage
        {
            public string Variable;
            public string Variable2;
            public GameObject[] SelectObjects;
            public System.Action AutoFix;
            public string AssetPath;

            public SingleMessage(string variable)
            {
                this.Variable = variable;
            }

            public SingleMessage(string variable, string variable2)
            {
                this.Variable = variable;
                this.Variable2 = variable2;
            }

            public SingleMessage(GameObject[] selectObjects)
            {
                this.SelectObjects = selectObjects;
            }

            public SingleMessage(GameObject selectObjects)
            {
                this.SelectObjects = new GameObject[] { selectObjects };
            }

            public SingleMessage(System.Action autoFix)
            {
                this.AutoFix = autoFix;
            }

            public SingleMessage SetSelectObject(GameObject[] selectObjects)
            {
                this.SelectObjects = selectObjects;
                return this;
            }

            public SingleMessage SetSelectObject(GameObject selectObjects)
            {
                this.SelectObjects = new GameObject[] { selectObjects };
                return this;
            }

            public SingleMessage SetAutoFix(System.Action autoFix)
            {
                this.AutoFix = autoFix;
                return this;
            }

            public SingleMessage SetAssetPath(string assetPath)
            {
                this.AssetPath = assetPath;
                return this;
            }
        }

        private class MessageGroup : IEquatable<MessageGroup>
        {
            public string Message;
            public string CombinedMessage;
            public string AdditionalInfo;

            public MessageType MessageType;

            public string Documentation;

            public System.Action GroupAutoFix;

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

            public MessageGroup SetGroupAutoFix(System.Action groupAutoFix)
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

            public MessageGroup SetMessageList(List<SingleMessage> messageList)
            {
                this.MessageList = messageList;
                return this;
            }

            public int GetTotalCount()
            {
                var count = 0;

                if (MessageList == null) return count;

                for (int i = 0; i < MessageList.Count; i++)
                {
                    SingleMessage item = MessageList[i];
                    if (item.SelectObjects != null)
                    {
                        count += item.SelectObjects.Count();
                    }
                    else
                    {
                        if (item.AssetPath != null)
                        {
                            count++;
                        }
                    }
                }

                return count == 0 ? MessageList.Count : count;
            }

            public GameObject[] GetSelectObjects()
            {
                var objs = new List<GameObject>();
                foreach (var item in MessageList.Where(o => o.SelectObjects != null))
                {
                    objs.AddRange(item.SelectObjects);
                }
                return objs.ToArray();
            }

            public string[] GetAssetPaths()
            {
                var paths = new List<string>();
                foreach (var item in MessageList.Where(a => a.AssetPath != null))
                {
                    paths.Add(item.AssetPath);
                }
                return paths.ToArray();
            }

            public System.Action[] GetSeparateActions()
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

            [SerializeField] private Dictionary<int, bool> _expandedGroups;

            public string ListName;
            public bool Disabled = false;

            public MessageCategory()
            {
                MessageGroups = new List<MessageGroup>();
                _expandedGroups = new Dictionary<int, bool>();
            }

            public MessageCategory(string listName)
            {
                MessageGroups = new List<MessageGroup>();
                _expandedGroups = new Dictionary<int, bool>();

                this.ListName = listName;
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
                if (MessageGroups is null || MessageGroups.Count == 0) return false;

                return true;
            }

            public bool IsExpanded(MessageGroup mg)
            {
                var hash = mg.GetHashCode();
                return _expandedGroups.ContainsKey(hash) && _expandedGroups[hash];
            }

            public void SetExpanded(MessageGroup mg, bool expanded)
            {
                var hash = mg.GetHashCode();
                if (_expandedGroups.ContainsKey(hash))
                {
                    _expandedGroups[hash] = expanded;
                }
                else
                {
                    _expandedGroups.Add(hash, expanded);
                }
            }
        }

        [Serializable]
        private class MessageCategoryList
        {
            public List<MessageCategory> _messageCategory = new List<MessageCategory>();

            public MessageCategory AddOrGetCategory(string listName)
            {
                var newMessageCategory = new MessageCategory(listName);

                var oldMessageCategory = _messageCategory.Find(x => x.ListName == listName);

                if (oldMessageCategory is null)
                {
                    _messageCategory.Add(newMessageCategory);

                    return newMessageCategory;
                }

                return oldMessageCategory;
            }

            public void DrawTabSelector()
            {
                EditorGUILayout.BeginHorizontal();

                for (int i = 0; i < _messageCategory.Count; i++)
                {
                    MessageCategory item = _messageCategory[i];

                    var button = "miniButtonMid";

                    if (_messageCategory.First() == item)
                    {
                        button = "miniButtonLeft";
                    }
                    else if (_messageCategory.Last() == item)
                    {
                        button = "miniButtonRight";
                    }

                    var currentState = item.Disabled;

                    item.Disabled = GUILayout.Toggle(item.Disabled, item.ListName, button);
                }

                EditorGUILayout.EndHorizontal();
            }

            public bool HasCategories()
            {
                if (_messageCategory.Count > 0) return true;

                return false;
            }

            public void ClearCategories()
            {
                _messageCategory.ForEach(m => m.ClearMessages());
            }

            public void DrawMessages()
            {
                var drawList = _messageCategory;

                for (int i = 0; i < drawList.Count; i++)
                {
                    MessageCategory group = drawList[i];
                    if (!group.Disabled)
                    {
                        GUILayout.Label(group.ListName, EditorStyles.boldLabel);

                        var buttonWidth = 80;
                        var buttonHeight = 20;

                        if (!group.HasMessages())
                        {
                            DrawMessage("No messages found for " + group.ListName + ".", MessageType.Info);

                            continue;
                        }

                        for (int l = 0; l < group.MessageGroups.Count; l++)
                        {
                            MessageGroup messageGroup = group.MessageGroups[l];
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
                                            EditorGUI.BeginDisabledGroup(messageGroup.GetSelectObjects().Length == 0);

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

                                            _recheck = true;
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
                                        for (int j = 0; j < messageGroup.MessageList.Count; j++)
                                        {
                                            SingleMessage message = messageGroup.MessageList[j];

                                            EditorGUILayout.BeginHorizontal();

                                            var finalSingleMessage = string.Format(messageGroup.Message, message.Variable, message.Variable2);

                                            if (hasButtons)
                                            {
                                                var box = new GUIContent(finalSingleMessage);
                                                GUILayout.Box(box, Styles.HelpBoxPadded, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 121));

                                                EditorGUILayout.BeginVertical();

                                                EditorGUI.BeginDisabledGroup(!(message.SelectObjects != null || message.AssetPath != null));

                                                if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    if (message.AssetPath != null)
                                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(message.AssetPath));

                                                    else
                                                        Selection.objects = message.SelectObjects;
                                                }

                                                EditorGUI.EndDisabledGroup();

                                                EditorGUI.BeginDisabledGroup(message.AutoFix == null);

                                                if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    message.AutoFix();

                                                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                                                    _recheck = true;
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
                                    for (int j = 0; j < messageGroup.MessageList.Count; j++)
                                    {
                                        SingleMessage message = messageGroup.MessageList[j];
                                        EditorGUILayout.BeginHorizontal();

                                        var finalMessage = string.Format(messageGroup.Message, message.Variable, message.Variable2);

                                        if (messageGroup.AdditionalInfo != null)
                                        {
                                            finalMessage = string.Concat(finalMessage, " ", messageGroup.AdditionalInfo);
                                        }

                                        if (hasButtons)
                                        {
                                            GUIContent Box = new GUIContent(finalMessage, GetDebuggerIcon(messageGroup.MessageType));
                                            GUILayout.Box(Box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                            EditorGUILayout.BeginVertical();

                                            EditorGUI.BeginDisabledGroup(!(message.SelectObjects != null || message.AssetPath != null));

                                            if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                if (message.AssetPath != null)
                                                {
                                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(message.AssetPath));
                                                }
                                                else
                                                {
                                                    Selection.objects = message.SelectObjects;
                                                }
                                            }

                                            EditorGUI.EndDisabledGroup();

                                            EditorGUI.BeginDisabledGroup(message.AutoFix == null);

                                            if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                message.AutoFix();

                                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                                                _recheck = true;
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

                                    GUIContent box = new GUIContent(messageGroup.Message, GetDebuggerIcon(messageGroup.MessageType));
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
                                            _recheck = true;
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
                var Box = new GUIContent(messageText, GetDebuggerIcon(type));
                GUILayout.Box(Box, Styles.HelpBoxRichText, GUILayout.MinHeight(42), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 18));
            }
        }

        private Vector2 _scrollPos;

        [SerializeField] private int tab;

        [MenuItem("VRWorld Toolkit/World Debugger", false, 0)]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(WorldDebugger));
            window.titleContent = new GUIContent("World Debugger");
            window.minSize = new Vector2(530, 600);
            window.Show();
        }

        #region Actions
        public static System.Action SelectAsset(GameObject obj)
        {
            return () =>
            {
                Selection.activeObject = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
            };
        }

        public static System.Action SetGenerateLightmapUV(ModelImporter importer)
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

        public static System.Action SetGenerateLightmapUV(List<ModelImporter> importers)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Enable lightmap UV generation?", "This operation will enable the lightmap UV generation on " + importers.Count + " meshes.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    importers.ForEach(i => { i.generateSecondaryUV = true; i.SaveAndReimport(); });
                }
            };
        }

        public static System.Action RemoveBadPipelineManagers(PipelineManager[] pipelineManagers)
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

        public static System.Action SetLegacyBlendShapeNormals(ModelImporter importer)
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

        public static System.Action DisableComponent(Behaviour behaviour)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviour.GetType() + " on the GameObject \"" + behaviour.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    behaviour.enabled = false;
                }
            };
        }

        public static System.Action DisableComponent(Behaviour[] behaviours)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviours[0].GetType() + " component on " + behaviours.Count().ToString() + " GameObjects.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    behaviours.ToList().ForEach(b => b.enabled = false);
                }
            };
        }

        public static System.Action SetObjectLayer(GameObject obj, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change the layer of " + obj.name + " to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    obj.layer = LayerMask.NameToLayer(layer);
                }
            };
        }

        public static System.Action SetObjectLayer(GameObject[] objs, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change layer?", "This operation will change " + objs.Length + " GameObjects layer to " + layer + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.layer = LayerMask.NameToLayer(layer));
                }
            };
        }

        public static System.Action SetLightmapSize(int newSize)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change lightmap size?", "This operation will change your lightmap size from " + LightmapEditorSettings.maxAtlasSize + " to " + newSize + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    LightmapEditorSettings.maxAtlasSize = newSize;
                }
            };
        }

        public static System.Action SetEnviromentReflections(DefaultReflectionMode reflections)
        {
            return () =>
            {
                RenderSettings.defaultReflectionMode = reflections;
            };
        }

        public static System.Action SetAmbientMode(AmbientMode ambientMode)
        {
            return () =>
            {
                RenderSettings.ambientMode = ambientMode;
            };
        }

        public static System.Action SetGameObjectTag(GameObject obj, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change the Tag of " + obj.name + " to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    obj.tag = tag;
                }
            };
        }

        public static System.Action SetGameObjectTag(GameObject[] objs, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + objs.Length + " GameObjects tag to " + tag + ".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.tag = tag);
                }
            };
        }

        public static System.Action ChangeShader(Material material, String shader)
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

        public static System.Action ChangeShader(Material[] materials, String shader)
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

        public static System.Action RemoveOverlappingLightprobes(LightProbeGroup lpg)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes in the group \"" + lpg.gameObject.name + "\".\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    lpg.probePositions = lpg.probePositions.Distinct().ToArray();
                }
            };
        }

        public static System.Action RemoveOverlappingLightprobes(LightProbeGroup[] lpgs)
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

        public static System.Action RemoveRedundantLightProbes(LightProbeGroup[] lpgs)
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

        public static System.Action ClearOcclusionCache(long fileCount)
        {
            return async () =>
            {
                if (EditorUtility.DisplayDialog("Clear Occlusion Cache?", "This will clear your occlusion culling cache. Which has " + fileCount + " files currently. Deleting a massive amount of files can take a while.\n\nDo you want to continue?", "Yes", "Cancel"))
                {
                    long deleteCount = 0;

                    CancellationTokenSource tokenSource = new CancellationTokenSource();

                    var deleteFiles = new Progress<string>(fileName =>
                    {
                        deleteCount++;
                        if (EditorUtility.DisplayCancelableProgressBar("Clearing Occlusion Cache", fileName, (float)deleteCount / (float)fileCount))
                        {
                            tokenSource.Cancel();
                        }
                    });

                    var token = tokenSource.Token;

                    await Task.Run(() => DeleteFiles(deleteFiles, token));
                    EditorUtility.ClearProgressBar();

                    _occlusionCacheFiles = 0;
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

        public static System.Action FixSpawns(VRC_SceneDescriptor descriptor)
        {
            return () =>
            {
                descriptor.spawns = descriptor.spawns.Where(c => c != null).ToArray();
                if (descriptor.spawns.Length == 0)
                {
                    descriptor.spawns = new Transform[] { descriptor.gameObject.transform };
                }
            };
        }

        public static System.Action SetErrorPause(bool enabled)
        {
            return () =>
            {
                ConsoleFlagUtil.SetConsoleErrorPause(enabled);
            };
        }

        public static System.Action SetVRChatLayers()
        {
            return () =>
            {
                UpdateLayers.SetupEditorLayers();
            };
        }

        public static System.Action SetVRChatCollisionMatrix()
        {
            return () =>
            {
                UpdateLayers.SetupCollisionLayerMatrix();
            };
        }

        public static System.Action SetReferenceCamera(VRC_SceneDescriptor descriptor, Camera camera)
        {
            return () =>
            {
                descriptor.ReferenceCamera = camera.gameObject;
            };
        }

        public static System.Action SetVRCInstallPath()
        {
            return () =>
            {
                var clientPath = Helper.GetSteamVRCExecutablePath();

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
        public enum RemovePPEffect
        {
            AmbientOcclusion = 0,
            ScreenSpaceReflections = 1,
            BloomDirt = 2
        }

        public static System.Action DisablePostProcessEffect(PostProcessProfile postprocessProfile, RemovePPEffect effect)
        {
            return () =>
            {
                switch (effect)
                {
                    case RemovePPEffect.AmbientOcclusion:
                        postprocessProfile.GetSetting<AmbientOcclusion>().active = false;
                        break;
                    case RemovePPEffect.ScreenSpaceReflections:
                        postprocessProfile.GetSetting<ScreenSpaceReflections>().active = false;
                        break;
                    case RemovePPEffect.BloomDirt:
                        postprocessProfile.GetSetting<Bloom>().dirtTexture.overrideState = false;
                        postprocessProfile.GetSetting<Bloom>().dirtIntensity.overrideState = false;
                        break;
                    default:
                        break;
                }
            };
        }

        public static System.Action SetPostProcessingInScene(SceneView.SceneViewState sceneViewState, bool isActive)
        {
            return () =>
            {
                sceneViewState.showImageEffects = isActive;
            };
        }
#endif
        #endregion

        #region Texts
        private const string NoSceneDescriptor = "The current scene has no Scene Descriptor. Please add one, or drag the VRCWorld prefab to the scene.";

        private const string TooManySceneDescriptors = "Multiple Scene Descriptors were found. Only one scene descriptor can exist in a single scene.";

        private const string TooManyPipelineManagers = "The current scene has multiple Pipeline Managers in it. This can break the world upload process and cause you not to be able to load into the world.";

        private const string WorldDescriptorFar = "Scene Descriptor is {0} units far from the zero point in Unity. Having your world center out this far will cause some noticeable jittering on models. You should move your world closer to the zero point of your scene.";

        private const string WorldDescriptorOff = "Scene Descriptor is {0} units far from the zero point in Unity. It is usually good practice to keep it as close as possible to the absolute zero point to avoid floating-point errors.";

        private const string NoSpawnPointSet = "There are no spawn points set in your Scene Descriptor. Spawning into a world with no spawn point will cause you to get thrown back to your homeworld.";

        private const string NullSpawnPoint = "Null spawn point set Scene Descriptor. Spawning into a null spawn point will cause you to get thrown back to your homeworld.";

        private const string ColliderUnderSpawnIsTrigger = "The collider \"{0}\" under your spawn point {1} has been set as Is Trigger! Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string NoColliderUnderSpawn = "Spawn point \"{0}\" does not have anything underneath it. Spawning into a world with nothing to stand on will cause the players to fall forever.";

        private const string NoPlayerMods = "No Player Mods were found in the scene. Player mods are needed for adding jumping and changing walking speed.";

        private const string TriggerTriggerNoCollider = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that does not have a Collider on it.";
        private const string ColliderTriggerNoCollider = "You have an OnEnterCollider or OnExitCollider Trigger \"{0}\" that does not have a Collider on it.";

        private const string TriggerTriggerWrongLayer = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that is not on the MirrorReflection layer.";
        private const string TriggerTriggerWrongLayerCombined = "You have {0} OnEnterTrigger or OnExitTrigger Triggers that are not on the MirrorReflection layer.";
        private const string TriggerTriggerWrongLayerInfo = "This can stop raycasts from working correctly, making you unable to interact with objects and UI Buttons.";

        private const string MirrorOnByDefault = "The mirror \"{0}\" is on by default.";
        private const string MirrorOnByDefaultCombined = "The scene has {0} mirrors on by default.";
        private const string MirrorOnByDefaultInfo = "This is an awful practice. Any mirrors in worlds should be disabled by default.";

        private const string MirrorWithDefaultLayers = "The mirror \"{0}\" has the default Reflect Layers set.";
        private const string MirrorWithDefaultLayersCombined = "You have {0} mirrors that have the default Reflect Layers set.";
        private const string MirrorWithDefaultLayersInfo = "Only having the layers needed to have enabled in mirrors can save a lot of frames, especially in populated instances.";

        private const string LegacyBlendShapeIssues = "Skinned mesh renderer found with model {0} ({1}) without Legacy Blend Shape Normals enabled.";
        private const string LegacyBlendShapeIssuesCombined = "Found {0} models without Legacy Blend Shape Normals enabled.";
        private const string LegacyBlendShapeIssuesInfo = "This can significantly increase the size of the world.";

        private const string BakedOcclusionCulling = "Baked Occlusion Culling found.";

        private const string NoOcclusionAreas = "No occlusion areas were found. Occlusion Areas are recommended to help generate higher precision data where the camera is likely to be. If no set, the area is created automatically containing all Occluders and Occludees.";

        private const string DisabledOcclusionArea = "Occlusion Area {0} found with Is View Volume disabled.";
        private const string DisabledOcclusionAreaCombined = "Occlusion Areas found with Is View Volume disabled.";
        private const string DisabledOcclusionAreaInfo = "Without this enabled, the Occlusion Area does not get used for the occlusion bake.";

        private const string NoOcclusionCulling = "The current scene does not have baked Occlusion Culling. Occlusion culling gives a lot more performance for the world, especially in more massive worlds with multiple rooms or areas.";

        private const string OcclusionCullingCacheWarning = "The current project's occlusion culling cache has {0} files. When the occlusion culling cache grows too big, baking occlusion culling can take much longer than intended. It can be cleared with no adverse effects.";

        private const string ActiveCameraOutputtingToRenderTexture = "The current scene has an active camera \"{0}\" outputting to a render texture.";
        private const string ActiveCameraOutputtingToRenderTextureCombined = "The current scene has {0} active cameras outputting to render textures.";
        private const string ActiveCameraOutputtingToRenderTextureInfo = "This will affect performance negatively by causing more draw calls to happen. They should only be enabled when needed.";

        private const string NoToonShaders = "Toon shaders should be avoided for world-building, as they are missing crucial things for making worlds. For world-building, the most recommended shader is Standard.";

        private const string NonCrunchedTextures = "{0}% of the textures used in the scene have not been crunch compressed. Crunch compression can significantly reduce the size of the world download. It can be found from the texture's import settings.";

        private const string SwitchToProgressive = "The current scene is using the Enlighten lightmapper, which has been deprecated in newer versions of Unity. It would be best to consider switching to Progressive for improved fidelity and performance.";

        private const string SingleColorEnvironmentLighting = "Consider changing the Environment Lighting to Gradient from Flat.";

        private const string DarkEnvironmentLighting = "Using dark colors for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if the world has dark lighting.";

        private const string CustomEnvironmentReflectionsNull = "The current scenes Environment Reflections have been set to custom, but a custom cubemap has not been defined.";

        private const string NoLightmapUV = "The model found in the scene \"{0}\" is set to be lightmapped but does not have Lightmap UVs.";
        private const string NoLightmapUVCombined = "The current scene has {0} models set to be lightmapped that do not have Lightmap UVs.";
        private const string NoLightmapUVInfo = "This can cause issues when baking lighting. You can enable generating Lightmap UV's in the model's import settings.";

        private const string LightsNotBaked = "The current scene is using realtime lighting. Consider baked lighting for improved performance.";

        private const string ConsiderLargerLightmaps = "Consider increasing Lightmap Size from {0} to 2048 or 4096. This allows for more stuff to fit on a single lightmap, leaving fewer textures that need to be sampled.";

        private const string ConsiderSmallerLightmaps = "Baking lightmaps at 4096 with Progressive GPU will silently fall back to CPU Progressive. More than 12GB GPU Memory is needed to bake 4k lightmaps with GPU Progressive.";

        private const string NonBakedBakedLight = "The light {0} is set to be baked/mixed, but it has not been baked yet!";
        private const string NonBakedBakedLightCombined = "The scene contains {0} baked/mixed lights that have not been baked!";
        private const string NonBakedBakedLightInfo = "Baked lights that have not been baked yet function as realtime lights in-game.";

        private const string LightingDataAssetInfo = "The current scene's lighting data asset takes up {0} MB of the world's size. This contains the scene's light probe data and realtime GI data.";

        private const string NoLightProbes = "No light probes found in the current scene. Without baked light probes baked lights are not able to affect dynamic objects such as players and pickups.";

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

        private const string NoReferenceCameraSet = "The current scenes Scene Descriptor has no Reference Camera set. Without a Reference Camera set Post Processing will not be visible in-game.";

        private const string NoPostProcessingVolumes = "No enabled Post Processing Volumes found in the scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";

        private const string ReferenceCameraNoPostProcessingLayer = "The current Reference Camera does not have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";

        private const string VolumeBlendingLayerNotSet = "You don't have a Volume Blending Layer set in the Post Process Layer, so post-processing will not work. Using the Water or PostProcessing layer is recommended.";

        private const string PostProcessingVolumeNotGlobalNoCollider = "Post Processing Volume \"{0}\" is not marked as Global and does not have a collider. It will not affect the camera without one of these set on it.";

        private const string NoProfileSet = "Post Processing Volume \"{0}\" does not have a profile set.";

        private const string VolumeOnWrongLayer = "Post Processing Volume \"{0}\" is not on one of the layers set in the cameras Post Processing Layer setting. (Currently: {1})";

        private const string DontUseNoneForTonemapping = "Use either Neutral or ACES for Color Grading Tonemapping. Selecting None for Tonemapping is essentially the same as leaving Tonemapping unchecked.";

        private const string TooHighBloomIntensity = "Do not raise the Bloom intensity too high! It is best to use a low Bloom intensity, between 0.01 to 0.3.";

        private const string TooHighBloomThreshold = "You should avoid having the Bloom Threshold set high. It might cause unexpected problems with avatars. Ideally, it should be kept at 0, but always below 1.0.";

        private const string NoBloomDirtInVr = "Avoid using Bloom Dirt. It looks terrible in VR!";

        private const string NoAmbientOcclusion = "Do not use Ambient Occlusion in VRChat! VRchat uses Forward rendering, so it gets applied on top of everything else, which is bad! It also has a super high rendering cost in VR.";

        private const string DepthOfFieldWarning = "Depth of field has a high-performance cost and is very disorientating in VR. If you want to use depth of field, it should be disabled by default.";

        private const string ScreenSpaceReflectionsWarning = "Screen-space Reflections only works when using deferred rendering. Because VRchat is not using deferred rendering, so this should not be used.";

        private const string VignetteWarning = "Only use Post Processing vignette in minimal amounts. A powerful vignette can cause sickness in VR.";

        private const string NoPostProcessingImported = "Post Processing package not found in the project.";

        private const string QuestBakedLightingWarning = "Realtime lighting for Quest content should be avoided and instead have a properly baked lighting setup for performance.";

        private const string AmbientModeSetToCustom = "The current scenes Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";

        private const string NoProblemsFoundInPP = "No problems were found in your post-processing setup. In some cases where post-processing is working in the editor but not in-game, some imported assets may be causing it not to function correctly.";

        private const string BakeryLightNotSetEditorOnly = "Your Bakery light named \"{0}\" is not set to be EditorOnly.";
        private const string BakeryLightNotSetEditorOnlyCombined = "You have {0} Bakery lights are not set to be EditorOnly.";
        private const string BakeryLightNotSetEditorOnlyInfo = "This causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";

        private const string BakeryLightUnityLight = "Your Bakery light named \"{0}\" has an active Unity Light component on it.";
        private const string BakeryLightUnityLightCombined = "You have {0} Bakery lights that have an active Unity Light component on it.";
        private const string BakeryLightUnityLightInfo = "These will not get baked with Bakery and will keep acting as realtime lights even if set to baked.";

        private const string MissingShaderWarning = "The material \"{0}\" found in the scene has a missing or broken shader.";
        private const string MissingShaderWarningCombined = "Found {0} materials in the current scene that have missing or broken shaders.";
        private const string MissingShaderWarningInfo = "These will fallback to the pink error shader.";

        private const string ErrorPauseWarning = "You have Error Pause enabled in your console. This can cause your world upload to fail by interrupting the build process.";

        private const string MultipleScenesLoaded = "Multiple scenes loaded, this is not supported by VRChat and can cause the world upload to fail. Only one scene should be used for world creation at a time.";

        private const string LayersNotSetup = "Project layers are not set up for VRChat yet.";

        private const string CollisionMatrixNotSetup = "The projects Collision Matrix is not set up for VRChat yet.";

        private const string MaterialWithGrabPassShader = "A material ({0}) in the scene is using a GrabPass due to shader \"{1}\".";
        private const string MaterialWithGrabPassShaderCombined = "Found {0} materials in the scene using a GrabPass.";
        private const string MaterialWithGrabPassShaderInfoPC = "A GrabPass will halt the rendering to copy the screen's contents into a texture for the shader to read. This has a notable effect on performance.";
        private const string MaterialWithGrabPassShaderInfoQuest = "Please change the shader for this material. When a shader uses a GrabPass on Quest, it will cause painful visual artifacts to occur, as they are not supported.";

        private const string DisabledPortalsWarning = "Portal \"{0}\" disabled by default.";
        private const string DisabledPortalsWarningCombined = "Found {0} portals disabled by default.";
        private const string DisabledPortalsWarningInfo = "Having a portal disabled by default will cause players that are entering to end up in different instances.";

        private const string SHRNMDirectionalModeBakeryError = "SH or RNM directional mode detected in Bakery. Using SH directional mode is not supported in VRChat by default. It requires the usage of VRC Bakery Adapter by Merlin for it to function in-game.";

        private const string BuildAndTestBrokenError = "VRChat link association has not been set up, and the VRChat client path has not been set in the VRCSDK settings. Without one of these settings set Build & Test will not function.";

        private const string BuildAndTestForceNonVRError = "VRChat client path has not been set to point directly to the VRChat executable in the VRCSDK settings. This will cause Force Non-VR setting for Build & Test not to work.";
        #endregion

        private static long _occlusionCacheFiles = 0;

        //TODO: Better check threading
        private static void CountOcclusionCacheFiles()
        {
            _occlusionCacheFiles = Directory.EnumerateFiles("Library/Occlusion/").Count();

            if (_occlusionCacheFiles > 0)
            {
                _recheck = true;
            }
        }

        private void CheckScene()
        {
            _masterList.ClearCategories();

            //General Checks

            //Get Descriptors
            var descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            long descriptorCount = descriptors.Length;
            VRC_SceneDescriptor sceneDescriptor;
            var pipelines = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];

            //Check if a descriptor exists
            if (descriptorCount == 0)
            {
                _general.AddMessageGroup(new MessageGroup(NoSceneDescriptor, MessageType.Error));
                return;
            }
            else
            {
                sceneDescriptor = descriptors[0];

                //Make sure only one descriptor exists
                if (descriptorCount > 1)
                {
                    _general.AddMessageGroup(new MessageGroup(TooManySceneDescriptors, MessageType.Info).AddSingleMessage(new SingleMessage(Array.ConvertAll(descriptors, s => s.gameObject))));
                    return;
                }
                else if (pipelines.Length > 1)
                {
                    _general.AddMessageGroup(new MessageGroup(TooManyPipelineManagers, MessageType.Error).AddSingleMessage(new SingleMessage(Array.ConvertAll(pipelines, s => s.gameObject)).SetAutoFix(RemoveBadPipelineManagers(pipelines))));
                }

                //Check how far the descriptor is from zero point for floating point errors
                int descriptorRemoteness = (int)Vector3.Distance(sceneDescriptor.transform.position, new Vector3(0.0f, 0.0f, 0.0f));

                if (descriptorRemoteness > 1000)
                {
                    _general.AddMessageGroup(new MessageGroup(WorldDescriptorFar, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
                else if (descriptorRemoteness > 250)
                {
                    _general.AddMessageGroup(new MessageGroup(WorldDescriptorOff, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
            }

            if (!UpdateLayers.AreLayersSetup())
            {
                _general.AddMessageGroup(new MessageGroup(LayersNotSetup, MessageType.Warning).SetGroupAutoFix(SetVRChatLayers()));
            }

            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                _general.AddMessageGroup(new MessageGroup(CollisionMatrixNotSetup, MessageType.Warning).SetGroupAutoFix(SetVRChatCollisionMatrix()));
            }

            //Check if multiple scenes loaded
            if (SceneManager.sceneCount > 1)
            {
                _general.AddMessageGroup(new MessageGroup(MultipleScenesLoaded, MessageType.Error));
            }

            //Check if console has error pause on
            if (ConsoleFlagUtil.GetConsoleErrorPause())
            {
                _general.AddMessageGroup(new MessageGroup(ErrorPauseWarning, MessageType.Error).AddSingleMessage(new SingleMessage(SetErrorPause(false))));
            }

            //Check for problems with Build & Test
            if (SDKClientUtilities.GetSavedVRCInstallPath() == "\\VRChat.exe" || SDKClientUtilities.GetSavedVRCInstallPath() == "")
            {
                if (Registry.ClassesRoot.OpenSubKey(@"VRChat\shell\open\command") is null)
                {
                    _general.AddMessageGroup(new MessageGroup(BuildAndTestBrokenError, MessageType.Error).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
                else
                {
                    _general.AddMessageGroup(new MessageGroup(BuildAndTestForceNonVRError, MessageType.Warning).AddSingleMessage(new SingleMessage(SetVRCInstallPath())));
                }
            }

            //Get spawn points for any possible problems
            var spawns = sceneDescriptor.spawns.Where(s => s != null).ToArray();

            var spawnsLength = sceneDescriptor.spawns.Length;
            var emptySpawns = spawnsLength != spawns.Length;

            if (spawns.Length == 0)
            {
                _general.AddMessageGroup(new MessageGroup(NoSpawnPointSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
            }
            else
            {
                if (emptySpawns)
                {
                    _general.AddMessageGroup(new MessageGroup(NullSpawnPoint, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
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
                                _general.AddMessageGroup(new MessageGroup(ColliderUnderSpawnIsTrigger, MessageType.Error).AddSingleMessage(new SingleMessage(hit.collider.name, sceneDescriptor.spawns[i].gameObject.name).SetSelectObject(sceneDescriptor.spawns[i].gameObject)));
                            }
                        }
                        else
                        {
                            _general.AddMessageGroup(new MessageGroup(NoColliderUnderSpawn, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.spawns[i].gameObject.name).SetSelectObject(sceneDescriptor.spawns[i].gameObject)));
                        }
                    }
                }
            }

#if VRC_SDK_VRCSDK2
            //Check if the world has playermods defined
            var playermods = FindObjectsOfType(typeof(VRC_PlayerMods)) as VRC_PlayerMods[];
            if (playermods.Length == 0)
            {
                _general.AddMessageGroup(new MessageGroup(NoPlayerMods, MessageType.Tips));
            }

            //Get triggers in the world
            var triggerScripts = (VRC_Trigger[])VRC_Trigger.FindObjectsOfType(typeof(VRC_Trigger));

            var triggerWrongLayer = new List<GameObject>();

            //Check for OnEnterTriggers to make sure they are on mirrorreflection layer
            foreach (var triggerScript in triggerScripts)
            {
                foreach (var trigger in triggerScript.Triggers)
                {
                    if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitCollider)
                    {
                        if (!triggerScript.gameObject.GetComponent<Collider>())
                        {
                            if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitTrigger)
                            {
                                _general.AddMessageGroup(new MessageGroup(TriggerTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
                            }
                            else if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitCollider)
                            {
                                _general.AddMessageGroup(new MessageGroup(ColliderTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(triggerScript.name).SetSelectObject(triggerScript.gameObject)));
                            }
                        }
                        if ((trigger.TriggerType.ToString() == "OnEnterTrigger" || trigger.TriggerType.ToString() == "OnExitTrigger") && triggerScript.gameObject.layer != LayerMask.NameToLayer("MirrorReflection"))
                        {
                            triggerWrongLayer.Add(triggerScript.gameObject);
                        }
                    }
                }
            }

            if (triggerWrongLayer.Count > 0)
            {
                var triggerWrongLayerGroup = new MessageGroup(TriggerTriggerWrongLayer, TriggerTriggerWrongLayerCombined, TriggerTriggerWrongLayerInfo, MessageType.Warning);
                for (int i = 0; i < triggerWrongLayer.Count; i++)
                {
                    triggerWrongLayerGroup.AddSingleMessage(new SingleMessage(triggerWrongLayer[i].name).SetSelectObject(triggerWrongLayer[i].gameObject).SetAutoFix(SetObjectLayer(triggerWrongLayer[i].gameObject, "MirrorReflection")));
                }
                _general.AddMessageGroup(triggerWrongLayerGroup.SetGroupAutoFix(SetObjectLayer(triggerWrongLayerGroup.GetSelectObjects(), "MirrorReflection")));
            }
#endif

            //Optimization Checks

            //Check for occlusion culling
            if (StaticOcclusionCulling.umbraDataSize > 0)
            {
                _optimization.AddMessageGroup(new MessageGroup(BakedOcclusionCulling, MessageType.GoodFPS));

                var occlusionAreas = GameObject.FindObjectsOfType<OcclusionArea>();

                if (occlusionAreas.Length == 0)
                {
                    _optimization.AddMessageGroup(new MessageGroup(NoOcclusionAreas, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/class-OcclusionArea.html"));
                }
                else
                {
                    var disabledOcclusionAreasGroup = _optimization.AddMessageGroup(new MessageGroup(DisabledOcclusionArea, DisabledOcclusionAreaCombined, DisabledOcclusionAreaInfo, MessageType.Warning));

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
                _optimization.AddMessageGroup(new MessageGroup(NoOcclusionCulling, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/occlusion-culling-getting-started.html"));
            }

            if (_occlusionCacheFiles > 0)
            {
                //Set the message type depending on how many files found
                var cacheWarningType = MessageType.Info;
                if (_occlusionCacheFiles > 50000)
                {
                    cacheWarningType = MessageType.Error;
                }
                else if (_occlusionCacheFiles > 5000)
                {
                    cacheWarningType = MessageType.Warning;
                }

                _optimization.AddMessageGroup(new MessageGroup(OcclusionCullingCacheWarning, cacheWarningType).AddSingleMessage(new SingleMessage(_occlusionCacheFiles.ToString()).SetAutoFix(ClearOcclusionCache(_occlusionCacheFiles))));
            }

            //Check if there's any active cameras outputting to render textures
            var activeCameras = new List<GameObject>();
            var cameraCount = 0;
            var cameras = GameObject.FindObjectsOfType<Camera>();

            for (int i = 0; i < cameras.Length; i++)
            {
                if (!cameras[i].enabled || (!cameras[i].targetTexture || cameras[i].name == "VRCCam")) continue;

                cameraCount++;
                activeCameras.Add(cameras[i].gameObject);
            }

            if (cameraCount > 0)
            {
                var activeCamerasMessages = new MessageGroup(ActiveCameraOutputtingToRenderTexture, ActiveCameraOutputtingToRenderTextureCombined, ActiveCameraOutputtingToRenderTextureInfo, MessageType.BadFPS);
                for (int i = 0; i < activeCameras.Count; i++)
                {
                    activeCamerasMessages.AddSingleMessage(new SingleMessage(activeCameras[i].name).SetSelectObject(activeCameras[i].gameObject));
                }
                _optimization.AddMessageGroup(activeCamerasMessages);
            }

            //Get active mirrors in the world and complain about them
            var mirrors = FindObjectsOfType(typeof(VRC_MirrorReflection)) as VRC_MirrorReflection[];

            if (mirrors.Length > 0)
            {
                var activeCamerasMessage = new MessageGroup(MirrorOnByDefault, MirrorOnByDefaultCombined, MirrorOnByDefaultInfo, MessageType.BadFPS);
                for (int i = 0; i < mirrors.Length; i++)
                {
                    if (mirrors[i].enabled)
                    {
                        activeCamerasMessage.AddSingleMessage(new SingleMessage(mirrors[i].name).SetSelectObject(mirrors[i].gameObject));
                    }
                }
                _optimization.AddMessageGroup(activeCamerasMessage);
            }

            //Lighting Checks

            switch (RenderSettings.ambientMode)
            {
                case AmbientMode.Custom:
                    _lighting.AddMessageGroup(new MessageGroup(AmbientModeSetToCustom, MessageType.Error).AddSingleMessage(new SingleMessage(SetAmbientMode(AmbientMode.Skybox))));
                    break;
                case AmbientMode.Flat:
                    _lighting.AddMessageGroup(new MessageGroup(SingleColorEnvironmentLighting, MessageType.Tips));
                    break;
            }

            if ((Helper.GetBrightness(RenderSettings.ambientLight) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Flat)) || (Helper.GetBrightness(RenderSettings.ambientSkyColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientEquatorColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientGroundColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)))
            {
                _lighting.AddMessageGroup(new MessageGroup(DarkEnvironmentLighting, MessageType.Tips));
            }

            if (RenderSettings.defaultReflectionMode.Equals(DefaultReflectionMode.Custom) && !RenderSettings.customReflection)
            {
                _lighting.AddMessageGroup(new MessageGroup(CustomEnvironmentReflectionsNull, MessageType.Error).AddSingleMessage(new SingleMessage(SetEnviromentReflections(DefaultReflectionMode.Skybox))));
            }

            var bakedLighting = false;

#if BAKERY_INCLUDED
            var bakeryLights = new List<GameObject>();
            //TODO: Investigate whether or not these should be included
            //bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryDirectLight)) as BakeryDirectLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryPointLight)) as BakeryPointLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakerySkyLight)) as BakerySkyLight[], s => s.gameObject));

            var bakerySettings = ftRenderLightmap.FindRenderSettingsStorage();

            switch ((ftRenderLightmap.RenderDirMode)bakerySettings.renderSettingsRenderDirMode)
            {
                case ftRenderLightmap.RenderDirMode.RNM:
                case ftRenderLightmap.RenderDirMode.SH:
                    string className = "Merlin.VRCBakeryAdapter";

                    if (Helper.GetTypeFromName(className) is null)
                    {
                        _lighting.AddMessageGroup(new MessageGroup(SHRNMDirectionalModeBakeryError, MessageType.Error).SetDocumentation("https://github.com/Merlin-san/VRC-Bakery-Adapter"));
                    }
                    break;
                default:
                    break;
            }

            if (bakeryLights.Count > 0)
            {
                var notEditorOnly = new List<GameObject>();
                var unityLightOnBakeryLight = new List<GameObject>();

                bakedLighting = true;

                for (int i = 0; i < bakeryLights.Count; i++)
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
                    _lighting.AddMessageGroup(notEditorOnlyGroup.SetGroupAutoFix(SetGameObjectTag(notEditorOnly.ToArray(), "EditorOnly")));
                }

                if (unityLightOnBakeryLight.Count > 0)
                {
                    var unityLightGroup = new MessageGroup(BakeryLightUnityLight, BakeryLightUnityLightCombined, BakeryLightUnityLightInfo, MessageType.Warning);
                    foreach (var item in unityLightOnBakeryLight)
                    {
                        unityLightGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(DisableComponent(item.GetComponent<Light>())).SetSelectObject(item));
                    }
                    _lighting.AddMessageGroup(unityLightGroup.SetGroupAutoFix(DisableComponent(Array.ConvertAll(unityLightOnBakeryLight.ToArray(), s => s.GetComponent<Light>()))));
                }
            }
#endif

            //Get lights in scene
            var lights = FindObjectsOfType<Light>();

            var nonBakedLights = new List<GameObject>();

            //Go trough the lights to check if the scene contains lights set to be baked
            for (int i = 0; i < lights.Length; i++)
            {
                //Skip checking realtime lights
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
            }

            var probes = LightmapSettings.lightProbes;

            //If the scene has baked lights complain about stuff important to baked lighting missing
            if (bakedLighting)
            {
                //Count lightmaps and suggest to use bigger lightmaps if needed
                var lightMapSize = 0;
                lightMapSize = LightmapEditorSettings.maxAtlasSize;
                if (lightMapSize != 4096 && LightmapSettings.lightmaps.Length > 1 && !LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU))
                {
                    if (LightmapSettings.lightmaps[0] != null)
                    {
                        if (LightmapSettings.lightmaps[0].lightmapColor.height != 4096)
                        {
                            _lighting.AddMessageGroup(new MessageGroup(ConsiderLargerLightmaps, MessageType.Tips).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(4096))));
                        }
                    }
                }

                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU) && lightMapSize == 4096 && SystemInfo.graphicsMemorySize < 12000)
                {
                    _lighting.AddMessageGroup(new MessageGroup(ConsiderSmallerLightmaps, MessageType.Warning).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(2048))));
                }

                //Count how many light probes the scene has
                long probeCounter = 0;
                long bakedProbes = probes != null ? probes.count : 0;

                var lightprobegroups = GameObject.FindObjectsOfType<LightProbeGroup>();

                var overlappingLightProbesGroup = new MessageGroup(OverlappingLightProbes, OverlappingLightProbesCombined, OverlappingLightProbesInfo, MessageType.Info);

                for (int i = 0; i < lightprobegroups.Length; i++)
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
                        _lighting.AddMessageGroup(new MessageGroup(LightProbesRemovedNotReBaked, MessageType.Warning).AddSingleMessage(new SingleMessage(bakedProbes.ToString(), probeCounter.ToString())));
                    }
                    else
                    {
                        if (bakedProbes - (0.9 * probeCounter) < 0)
                        {
                            _lighting.AddMessageGroup(new MessageGroup(LightProbeCountNotBaked, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"), (probeCounter - bakedProbes).ToString("n0"))));
                        }
                        else
                        {
                            _lighting.AddMessageGroup(new MessageGroup(LightProbeCount, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"))));
                        }
                    }
                }

                if (overlappingLightProbesGroup.GetTotalCount() > 0)
                {
                    if (overlappingLightProbesGroup.GetTotalCount() > 1)
                    {
                        overlappingLightProbesGroup.SetGroupAutoFix(RemoveOverlappingLightprobes(lightprobegroups));
                    }

                    _lighting.AddMessageGroup(overlappingLightProbesGroup);
                }

                //Since the scene has baked lights complain if there's no lightprobes
                else if (probes == null && probeCounter == 0)
                {
                    _lighting.AddMessageGroup(new MessageGroup(NoLightProbes, MessageType.Info).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightProbes.html"));
                }

                //Check lighting data asset size if it exists
                if (Lightmapping.lightingDataAsset != null)
                {
                    var lmdName = Lightmapping.lightingDataAsset.name;
                    var pathTo = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    var length = new System.IO.FileInfo(pathTo).Length;
                    _lighting.AddMessageGroup(new MessageGroup(LightingDataAssetInfo, MessageType.Info).AddSingleMessage(new SingleMessage((length / 1024.0f / 1024.0f).ToString("F2"))));
                }

#if !BAKERY_INCLUDED
                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.Enlighten))
                {
                    _lighting.AddMessageGroup(new MessageGroup(SwitchToProgressive, MessageType.Tips));
                }
#endif

                if (nonBakedLights.Count != 0)
                {
                    var nonBakedLightsGroup = new MessageGroup(NonBakedBakedLight, NonBakedBakedLightCombined, NonBakedBakedLightInfo, MessageType.Warning);
                    for (int i = 0; i < nonBakedLights.Count; i++)
                    {
                        nonBakedLightsGroup.AddSingleMessage(new SingleMessage(nonBakedLights[i].name).SetSelectObject(nonBakedLights[i].gameObject));
                    }
                    _lighting.AddMessageGroup(nonBakedLightsGroup);
                }
            }
            else
            {
#if UNITY_ANDROID
                _lighting.AddMessageGroup(new MessageGroup(QuestBakedLightingWarning, MessageType.BadFPS).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/Lightmapping.html"));
#else
                _lighting.AddMessageGroup(new MessageGroup(LightsNotBaked, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/Lightmapping.html"));
#endif
            }

            //ReflectionProbes
            var reflectionprobes = GameObject.FindObjectsOfType<ReflectionProbe>();
            var unbakedprobes = new List<GameObject>();
            var reflectionProbeCount = reflectionprobes.Count();
            for (int i = 0; i < reflectionprobes.Length; i++)
            {
                if (!reflectionprobes[i].bakedTexture && reflectionprobes[i].mode == ReflectionProbeMode.Baked)
                {
                    unbakedprobes.Add(reflectionprobes[i].gameObject);
                }
            }

            if (reflectionProbeCount == 0)
            {
                _lighting.AddMessageGroup(new MessageGroup(NoReflectionProbes, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/class-ReflectionProbe.html"));
            }
            else if (reflectionProbeCount > 0)
            {
                _lighting.AddMessageGroup(new MessageGroup(ReflectionProbeCountText, MessageType.Info).AddSingleMessage(new SingleMessage(reflectionProbeCount.ToString())));

                if (unbakedprobes.Count > 0)
                {
                    var probesUnbakedGroup = new MessageGroup(ReflectionProbesSomeUnbaked, ReflectionProbesSomeUnbakedCombined, MessageType.Warning);

                    foreach (var item in unbakedprobes)
                    {
                        probesUnbakedGroup.AddSingleMessage(new SingleMessage(item.name).SetSelectObject(item));
                    }

                    _lighting.AddMessageGroup(probesUnbakedGroup);
                }
            }

            //Post Processing Checks

#if UNITY_POST_PROCESSING_STACK_V2
            var postProcessVolumes = FindObjectsOfType(typeof(PostProcessVolume)) as PostProcessVolume[];
            var postProcessLayerExists = false;

            if (Camera.main == null)
            {
                if (sceneDescriptor.ReferenceCamera)
                {
                    if (sceneDescriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)))
                    {
                        postProcessLayerExists = true;
                    }
                }
            }
            else
            {
                if (Camera.main.gameObject.GetComponent(typeof(PostProcessLayer)))
                {
                    postProcessLayerExists = true;
                }
            }

            if (!sceneDescriptor.ReferenceCamera && postProcessVolumes.Length == 0 && !postProcessLayerExists)
            {
                _postProcessing.AddMessageGroup(new MessageGroup(PostProcessingImportedButNotSetup, MessageType.Info));
            }
            else
            {
                var sceneViewState = SceneView.lastActiveSceneView.sceneViewState;

                if (!sceneViewState.showImageEffects)
                {
                    _postProcessing.AddMessageGroup(new MessageGroup(PostProcessingDisabledInSceneView, MessageType.Info).SetGroupAutoFix(SetPostProcessingInScene(sceneViewState, true)));
                }

                //Start by checking if reference camera has been set in the Scene Descriptor
                if (!sceneDescriptor.ReferenceCamera)
                {
                    SingleMessage noReferenceCameraMessage = new SingleMessage(sceneDescriptor.gameObject);

                    if (Camera.main && Camera.main.GetComponent<PostProcessLayer>())
                    {
                        noReferenceCameraMessage.SetAutoFix(SetReferenceCamera(sceneDescriptor, Camera.main));
                    }

                    _postProcessing.AddMessageGroup(new MessageGroup(NoReferenceCameraSet, MessageType.Warning).AddSingleMessage(noReferenceCameraMessage));
                }
                else
                {
                    //Check for post process volumes in the scene
                    if (postProcessVolumes.Length == 0)
                    {
                        _postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingVolumes, MessageType.Info));
                    }
                    else
                    {
                        PostProcessLayer postprocessLayer = sceneDescriptor.ReferenceCamera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        if (postprocessLayer == null)
                        {
                            _postProcessing.AddMessageGroup(new MessageGroup(ReferenceCameraNoPostProcessingLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                        }

                        var volumeLayer = postprocessLayer.volumeLayer;
                        if (volumeLayer == LayerMask.GetMask("Nothing"))
                        {
                            _postProcessing.AddMessageGroup(new MessageGroup(VolumeBlendingLayerNotSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.gameObject)));
                        }

                        foreach (var postProcessVolume in postProcessVolumes)
                        {
                            //Check if the layer matches the cameras post processing layer
                            if (postprocessLayer.volumeLayer != (postprocessLayer.volumeLayer | (1 << postProcessVolume.gameObject.layer)))
                            {
                                _postProcessing.AddMessageGroup(new MessageGroup(VolumeOnWrongLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject.name, Helper.GetAllLayersFromMask(postprocessLayer.volumeLayer)).SetSelectObject(postProcessVolume.gameObject)));
                            }

                            //Check if the volume has a profile set
                            if (!postProcessVolume.profile && !postProcessVolume.sharedProfile)
                            {
                                _postProcessing.AddMessageGroup(new MessageGroup(NoProfileSet, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject.name)));
                                continue;
                            }

                            //Check if the collider is either global or has a collider on it
                            if (!postProcessVolume.isGlobal && !postProcessVolume.GetComponent<Collider>())
                            {
                                _postProcessing.AddMessageGroup(new MessageGroup(PostProcessingVolumeNotGlobalNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.name).SetSelectObject(postProcessVolume.gameObject)));
                            }
                            else
                            {
                                //Go trough the profile settings and see if any bad one's are used
                                PostProcessProfile postProcessProfile;

                                if (postProcessVolume.profile)
                                {
                                    postProcessProfile = postProcessVolume.profile as PostProcessProfile;
                                }
                                else
                                {
                                    postProcessProfile = postProcessVolume.sharedProfile as PostProcessProfile;
                                }

                                if (postProcessProfile.GetSetting<ColorGrading>() && postProcessProfile.GetSetting<ColorGrading>().enabled && postProcessProfile.GetSetting<ColorGrading>().active)
                                {
                                    if (postProcessProfile.GetSetting<ColorGrading>().tonemapper.value.ToString() == "None")
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(DontUseNoneForTonemapping, MessageType.Error).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }
                                }

                                if (postProcessProfile.GetSetting<Bloom>() && postProcessProfile.GetSetting<Bloom>().enabled && postProcessProfile.GetSetting<Bloom>().active)
                                {
                                    var bloom = postProcessProfile.GetSetting<Bloom>();

                                    if (bloom.intensity.overrideState && bloom.intensity.value > 0.3f)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomIntensity, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }

                                    if (bloom.threshold.overrideState && bloom.threshold.value > 1f)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomThreshold, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                    }

                                    if (bloom.dirtTexture.overrideState && bloom.dirtTexture.value || bloom.dirtIntensity.overrideState && bloom.dirtIntensity.value > 0)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(NoBloomDirtInVr, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePPEffect.BloomDirt)).SetSelectObject(postProcessVolume.gameObject)));
                                    }
                                }

                                if (postProcessProfile.GetSetting<AmbientOcclusion>() && postProcessProfile.GetSetting<AmbientOcclusion>().enabled && postProcessProfile.GetSetting<AmbientOcclusion>().active)
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(NoAmbientOcclusion, MessageType.Error).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePPEffect.AmbientOcclusion)).SetSelectObject(postProcessVolume.gameObject)));
                                }

                                if (postProcessVolume.isGlobal && postProcessProfile.GetSetting<DepthOfField>() && postProcessProfile.GetSetting<DepthOfField>().enabled && postProcessProfile.GetSetting<DepthOfField>().active)
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(DepthOfFieldWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                }

                                if (postProcessProfile.GetSetting<ScreenSpaceReflections>() && postProcessProfile.GetSetting<ScreenSpaceReflections>().enabled && postProcessProfile.GetSetting<ScreenSpaceReflections>().active)
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(ScreenSpaceReflectionsWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(DisablePostProcessEffect(postProcessProfile, RemovePPEffect.ScreenSpaceReflections)).SetSelectObject(postProcessVolume.gameObject)));
                                }

                                if (postProcessProfile.GetSetting<Vignette>() && postProcessProfile.GetSetting<Vignette>().enabled && postProcessProfile.GetSetting<Vignette>().active)
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(VignetteWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postProcessVolume.gameObject)));
                                }
                            }
                        }
                    }
                }

                if (!_postProcessing.HasMessages())
                {
                    _postProcessing.AddMessageGroup(new MessageGroup(NoProblemsFoundInPP, MessageType.Info));
                }
            }
#else
            _postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingImported, MessageType.Info));
#endif

            //Gameobject checks

            var importers = new List<ModelImporter>();

            var unCrunchedTextures = new List<Texture>();
            var badShaders = 0;
            var textureCount = 0;

            var missingShaders = new List<Material>();

            var checkedMaterials = new List<Material>();
            var checkedShaders = new List<Shader>();

            var mirrorsDefaultLayers = _optimization.AddMessageGroup(new MessageGroup(MirrorWithDefaultLayers, MirrorWithDefaultLayersCombined, MirrorWithDefaultLayersInfo, MessageType.Tips));
            var legacyBlendShapeIssues = _general.AddMessageGroup(new MessageGroup(LegacyBlendShapeIssues, LegacyBlendShapeIssuesCombined, LegacyBlendShapeIssuesInfo, MessageType.Warning));
            var grabPassShaders = _general.AddMessageGroup(new MessageGroup(MaterialWithGrabPassShader, MaterialWithGrabPassShaderCombined, Helper.BuildPlatform() == RuntimePlatform.WindowsPlayer ? MaterialWithGrabPassShaderInfoPC : MaterialWithGrabPassShaderInfoQuest, Helper.BuildPlatform() == RuntimePlatform.Android ? MessageType.Error : MessageType.Info));
            var disabledPortals = _general.AddMessageGroup(new MessageGroup(DisabledPortalsWarning, DisabledPortalsWarningCombined, DisabledPortalsWarningInfo, MessageType.Warning));

            UnityEngine.Object[] allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < allGameObjects.Length; i++)
            {
                GameObject gameObject = allGameObjects[i] as GameObject;

                if (!(gameObject.hideFlags == HideFlags.NotEditable || gameObject.hideFlags == HideFlags.HideAndDontSave) && EditorUtility.IsPersistent(gameObject.transform.root.gameObject))
                    continue;

                if (gameObject.GetComponent<Renderer>())
                {
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
                                        if (!modelImporter.generateSecondaryUV && sharedMesh.uv2.Length == 0)
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
                            ModelImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mesh)) as ModelImporter;

                            if (mesh.blendShapeCount > 0 && (importer.importBlendShapeNormals == ModelImporterNormals.Calculate && !ModelImporterUtil.GetLegacyBlendShapeNormals(importer)))
                            {
                                legacyBlendShapeIssues.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(mesh)), EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(mesh))).SetAssetPath(importer.assetPath).SetAutoFix(SetLegacyBlendShapeNormals(importer)));
                            }
                        }
                    }

                    // Check materials for problems
                    var meshRenderer = gameObject.GetComponent<Renderer>();

                    for (int l = 0; l < meshRenderer.sharedMaterials.Length; l++)
                    {
                        Material material = meshRenderer.sharedMaterials[l];

                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        var shader = material.shader;

                        if (AssetDatabase.GetAssetPath(shader) != null)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(shader);

                            if (File.Exists(assetPath))
                            {
                                //Read shader file to string
                                string word = File.ReadAllText(assetPath);

                                //Strip comments
                                word = Regex.Replace(word, "(\\/\\/.*)|(\\/\\*)(.*)(\\*\\/)", "");

                                //Match for GrabPass
                                if (Regex.IsMatch(word, "GrabPass\\s*{"))
                                {
                                    grabPassShaders.AddSingleMessage(new SingleMessage(material.name, shader.name).SetAssetPath(AssetDatabase.GetAssetPath(material)));
                                }
                            }
                        }

                        checkedShaders.Add(shader);

                        if (shader.name == "Hidden/InternalErrorShader" && !missingShaders.Contains(material))
                            missingShaders.Add(material);

                        if (shader.name.StartsWith(".poiyomi") || shader.name.StartsWith("poiyomi") || shader.name.StartsWith("arktoon") || shader.name.StartsWith("Cubedparadox") || shader.name.StartsWith("Silent's Cel Shading") || shader.name.StartsWith("Xiexe"))
                            badShaders++;

                        for (int j = 0; j < ShaderUtil.GetPropertyCount(shader); j++)
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

                if (gameObject.activeInHierarchy == false)
                {
                    if (gameObject.GetComponent<VRC_PortalMarker>())
                    {
                        disabledPortals.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                    }
                }
            }

            //If more than 10% of shaders used in scene are toon shaders to leave room for people using them for avatar displays
            if (checkedMaterials.Count > 0)
            {
                if (badShaders / checkedMaterials.Count * 100 > 10)
                {
                    _optimization.AddMessageGroup(new MessageGroup(NoToonShaders, MessageType.Warning));
                }
            }

            //Suggest to crunch textures if there are any uncrunched textures found
            if (textureCount > 0)
            {
                var percent = (int)((float)unCrunchedTextures.Count / (float)textureCount * 100f);
                if (percent > 20)
                {
                    _optimization.AddMessageGroup(new MessageGroup(NonCrunchedTextures, MessageType.Tips).AddSingleMessage(new SingleMessage(percent.ToString())));
                }
            }


            var modelsCount = importers.Count;
            if (modelsCount > 0)
            {
                var noUVGroup = new MessageGroup(NoLightmapUV, NoLightmapUVCombined, NoLightmapUVInfo, MessageType.Warning);
                for (int i = 0; i < modelsCount; i++)
                {
                    var modelImporter = importers[i];

                    noUVGroup.AddSingleMessage(new SingleMessage(Path.GetFileName(AssetDatabase.GetAssetPath(modelImporter))).SetAutoFix(SetGenerateLightmapUV(modelImporter)).SetAssetPath(modelImporter.assetPath));
                }
                _lighting.AddMessageGroup(noUVGroup.SetGroupAutoFix(SetGenerateLightmapUV(importers)).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html"));
            }

            var missingShadersCount = missingShaders.Count;
            if (missingShadersCount > 0)
            {
                var missingShadersGroup = new MessageGroup(MissingShaderWarning, MissingShaderWarningCombined, MissingShaderWarningInfo, MessageType.Error);
                for (int i = 0; i < missingShaders.Count; i++)
                {
                    missingShadersGroup.AddSingleMessage(new SingleMessage(missingShaders[i].name).SetAssetPath(AssetDatabase.GetAssetPath(missingShaders[i])).SetAutoFix(ChangeShader(missingShaders[i], "Standard")));
                }
                _general.AddMessageGroup(missingShadersGroup.SetGroupAutoFix(ChangeShader(missingShaders.ToArray(), "Standard")));
            }
        }

        private void OnFocus()
        {
            _recheck = true;
        }

        private const string LastBuild = "Library/LastBuild.buildreport";

        private const string BuildReportDir = "Assets/_LastBuild/";

        private const string LastBuildReportPath = "Assets/_LastBuild/LastBuild.buildreport";
        private const string WindowsBuildReportPath = "Assets/_LastBuild/LastWindowsBuild.buildreport";
        private const string QuestBuildReportPath = "Assets/_LastBuild/LastQuestBuild.buildreport";

        [SerializeField] private BuildReport BuildReportWindows;
        [SerializeField] private BuildReport BuildReportQuest;

        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        private BuildReportTreeView m_TreeView;
        private SearchField m_SearchField;

        private void RefreshBuild()
        {
            if (!Directory.Exists(BuildReportDir))
                Directory.CreateDirectory(BuildReportDir);

            if (File.Exists(LastBuild) && (!File.Exists(LastBuildReportPath) || File.GetLastWriteTime(LastBuild) > File.GetLastWriteTime(LastBuildReportPath)))
            {
                File.Copy(LastBuild, LastBuildReportPath, true);
                AssetDatabase.ImportAsset(LastBuildReportPath);
            }

            if (File.Exists(LastBuildReportPath))
            {
                switch (AssetDatabase.LoadAssetAtPath<BuildReport>(LastBuildReportPath).summary.platform)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        if (File.GetLastWriteTime(LastBuildReportPath) > File.GetLastWriteTime(WindowsBuildReportPath))
                        {
                            AssetDatabase.CopyAsset(LastBuildReportPath, WindowsBuildReportPath);
                            BuildReportWindows = (BuildReport)AssetDatabase.LoadAssetAtPath(WindowsBuildReportPath, typeof(BuildReport));
                        }
                        break;
                    case BuildTarget.Android:
                        if (File.GetLastWriteTime(LastBuildReportPath) > File.GetLastWriteTime(QuestBuildReportPath))
                        {
                            AssetDatabase.CopyAsset(LastBuildReportPath, QuestBuildReportPath);
                            BuildReportQuest = (BuildReport)AssetDatabase.LoadAssetAtPath(QuestBuildReportPath, typeof(BuildReport));
                        }
                        break;
                    default:
                        break;
                }
            }

            if (BuildReportWindows is null && File.Exists(WindowsBuildReportPath))
            {
                BuildReportWindows = (BuildReport)AssetDatabase.LoadAssetAtPath(WindowsBuildReportPath, typeof(BuildReport));
            }

            if (BuildReportQuest is null && File.Exists(QuestBuildReportPath))
            {
                BuildReportQuest = (BuildReport)AssetDatabase.LoadAssetAtPath(QuestBuildReportPath, typeof(BuildReport));
            }

            if (!m_TreeView.HasReport())
            {
                if (BuildReportWindows != null)
                {
                    m_TreeView.SetReport(BuildReportWindows);
                }
                else if (BuildReportQuest != null)
                {
                    m_TreeView.SetReport(BuildReportWindows);
                }
            }
        }

        private void DrawBuildSummary(BuildReport report)
        {
            var richText = Styles.RichText;

            if (EditorGUIUtility.isProSkin)
            {
                richText.normal.textColor = Color.white;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (report != null)
            {
                GUILayout.Label("<b>Build size:</b> " + EditorUtility.FormatBytes((long)report.summary.totalSize), richText);

                GUILayout.Label("<b>Build done:</b> " + report.summary.buildEndedAt.ToLocalTime(), richText);

                GUILayout.Label("<b>Errors during build:</b> " + report.summary.totalErrors.ToString(), richText);

                GUILayout.Label("<b>Warnings during build:</b> " + report.summary.totalWarnings.ToString(), richText);

                GUILayout.Label("<b>Build result:</b> " + report.summary.result, richText);
            }

            GUILayout.EndVertical();
        }

        [NonSerialized] private bool initDone = false;

        [SerializeField] private MessageCategoryList _masterList;

        private MessageCategory _general;
        private MessageCategory _optimization;
        private MessageCategory _lighting;
        private MessageCategory _postProcessing;

        private void InitWhenNeeded()
        {
            if (!initDone)
            {
                if (_masterList is null)
                    _masterList = new MessageCategoryList();

                _general = _masterList.AddOrGetCategory("General");

                _optimization = _masterList.AddOrGetCategory("Optimization");

                _lighting = _masterList.AddOrGetCategory("Lighting");

                _postProcessing = _masterList.AddOrGetCategory("Post Processing");

                bool firstInit = m_MultiColumnHeaderState == null;
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

                if (BuildReportWindows is null && File.Exists(WindowsBuildReportPath))
                {
                    BuildReportWindows = (BuildReport)AssetDatabase.LoadAssetAtPath(WindowsBuildReportPath, typeof(BuildReport));
                }

                if (BuildReportQuest is null && File.Exists(QuestBuildReportPath))
                {
                    BuildReportQuest = (BuildReport)AssetDatabase.LoadAssetAtPath(QuestBuildReportPath, typeof(BuildReport));
                }

                var report = BuildReportWindows != null ? BuildReportWindows : BuildReportQuest;

                m_TreeView = new BuildReportTreeView(m_TreeViewState, multiColumnHeader, report);
                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                initDone = true;
            }
        }

        private void Refresh()
        {
            if (_recheck)
            {
                RefreshBuild();

                //Check for bloat in occlusion cache
                if (_occlusionCacheFiles == 0 && Directory.Exists("Library/Occlusion/"))
                {
                    var task = Task.Run(CountOcclusionCacheFiles);
                }

#if VRWT_BENCHMARK
                var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
                CheckScene();
#if VRWT_BENCHMARK
                watch.Stop();
                Debug.Log("Scene checked in: " + watch.ElapsedMilliseconds + " ms.");
#endif

                _recheck = false;
            }
        }

        enum BuildReportType
        {
            Windows = 0,
            Quest = 1
        }
        private string[] buildReportToolbar = { "Windows", "Quest" };

        [SerializeField] int selectedBuildReport = 0;
        [SerializeField] bool overallStatsFoldout = false;

        private void OnGUI()
        {
            InitWhenNeeded();
            Refresh();

            GUILayout.BeginHorizontal();

            if (BuildReportWindows)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Last found Windows build:", EditorStyles.boldLabel);

                DrawBuildSummary(BuildReportWindows);

                GUILayout.EndVertical();
            }

            if (BuildReportQuest)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Last found Quest build:", EditorStyles.boldLabel);

                DrawBuildSummary(BuildReportQuest);

                GUILayout.EndVertical();

            }

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            tab = GUILayout.Toolbar(tab, new string[] { "Messages", "Build Report" });

            switch (tab)
            {
                case 0:
                    _masterList.DrawTabSelector();

                    EditorGUILayout.BeginVertical();
                    _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                    _masterList.DrawMessages();

                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                    break;
                case 1:
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);

                    if (BuildReportWindows != null && BuildReportQuest != null)
                    {
                        EditorGUI.BeginChangeCheck();

                        selectedBuildReport = GUILayout.Toolbar(selectedBuildReport, buildReportToolbar, EditorStyles.toolbarButton);

                        if (EditorGUI.EndChangeCheck())
                        {
                            switch ((BuildReportType)selectedBuildReport)
                            {
                                case BuildReportType.Windows:
                                    m_TreeView.SetReport(BuildReportWindows);
                                    break;
                                case BuildReportType.Quest:
                                    m_TreeView.SetReport(BuildReportQuest);
                                    break;
                                default:
                                    break;
                            }
                        }

                        GUILayout.Space(10);
                    }

                    overallStatsFoldout = GUILayout.Toggle(overallStatsFoldout, "Stats", EditorStyles.toolbarButton);

                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                    {
                        if (m_TreeView.HasReport())
                        {
                            m_TreeView.Reload();
                        }
                        else
                        {
                            if (BuildReportWindows != null)
                            {
                                m_TreeView.SetReport(BuildReportWindows);
                            }
                            else if (BuildReportQuest != null)
                            {
                                m_TreeView.SetReport(BuildReportQuest);
                            }
                        }
                    }

                    GUILayout.Space(10);

                    GUILayout.FlexibleSpace();

                    m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();

                    if (overallStatsFoldout)
                    {
                        m_TreeView.DrawOverallStats();
                    }

                    Rect treeViewRect = EditorGUILayout.BeginVertical();

                    if (m_TreeView.HasReport())
                    {
                        m_TreeView.OnGUI(treeViewRect);
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndVertical();
                    break;
                default:
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