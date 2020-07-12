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

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit.WorldDebugger
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

        private class SingleMessage
        {
            public readonly string Variable;
            public readonly string Variable2;
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
            public readonly string Message;
            public readonly string CombinedMessage;
            public readonly string AdditionalInfo;

            public readonly MessageType MessageType;

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

            public System.Action[] GetSeparateActions()
            {
                return MessageList.Where(m => m.AutoFix != null).Select(m => m.AutoFix).ToArray();
            }

            public bool Buttons()
            {
                return GetSelectObjects().Any() || GroupAutoFix != null || GetSeparateActions().Any() || GroupAutoFix != null || Documentation != null;
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

        private class MessageCategory
        {
            public readonly List<MessageGroup> MessageGroups = new List<MessageGroup>();

            private readonly Dictionary<int, bool> _expandedGroups = new Dictionary<int, bool>();

            public readonly string ListName;
            public bool Enabled;

            public MessageCategory(string listName)
            {
                this.ListName = listName;
                Enabled = false;
            }

            public void AddMessageGroup(MessageGroup debuggerMessage)
            {
                MessageGroups.Add(debuggerMessage);
            }

            public void ClearMessages()
            {
                MessageGroups.Clear();
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

        private class MessageCategoryList
        {
            private readonly List<MessageCategory> _messageCategory = new List<MessageCategory>();

            public MessageCategory AddMessageCategory(string name)
            {
                var newMessageCategory = new MessageCategory(name);
                _messageCategory.Add(newMessageCategory);
                return newMessageCategory;
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
                    var currentState = item.Enabled;
                    item.Enabled = GUILayout.Toggle(item.Enabled, item.ListName, button);
                }

                EditorGUILayout.EndHorizontal();
            }

            public void ClearCategories()
            {
                _messageCategory.ForEach(m => m.ClearMessages());
            }

            private bool AllDisabled()
            {
                return _messageCategory.All(m => !m.Enabled);
            }

            private static readonly GUIStyle BoxStyle = new GUIStyle("HelpBox");

            public void DrawMessages()
            {
                var drawList = _messageCategory;

                for (int i = 0; i < drawList.Count; i++)
                {
                    MessageCategory group = drawList[i];
                    if (group.Enabled || AllDisabled())
                    {
                        GUILayout.Label(group.ListName, EditorStyles.boldLabel);

                        var buttonWidth = 80;
                        var buttonHeight = 20;

                        if (group.MessageGroups.Count == 0)
                        {
                            DrawMessage("No messages found for " + group.ListName + ".", MessageType.Info);
                        }

                        for (int l = 0; l < group.MessageGroups.Count; l++)
                        {
                            MessageGroup messageGroup = group.MessageGroups[l];
                            var hasButtons = messageGroup.Buttons();

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
                                        GUILayout.Box(box, BoxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

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
                                        var boxStylePadded = new GUIStyle("HelpBox")
                                        {
                                            margin = new RectOffset(18, 4, 4, 4),
                                            alignment = TextAnchor.MiddleLeft
                                        };

                                        for (int j = 0; j < messageGroup.MessageList.Count; j++)
                                        {
                                            SingleMessage message = messageGroup.MessageList[j];

                                            EditorGUILayout.BeginHorizontal();

                                            var finalSingleMessage = string.Format(messageGroup.Message, message.Variable, message.Variable2);

                                            if (hasButtons)
                                            {
                                                var box = new GUIContent(finalSingleMessage);
                                                GUILayout.Box(box, boxStylePadded, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 121));

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
                                            GUILayout.Box(Box, BoxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

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

                                    GUIContent Box = new GUIContent(messageGroup.Message, GetDebuggerIcon(messageGroup.MessageType));
                                    GUILayout.Box(Box, BoxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

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
                GUILayout.Box(Box, BoxStyle, GUILayout.MinHeight(42), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 18));
            }
        }

        private Vector2 _scrollPos;

        [MenuItem("VRWorld Toolkit/Open World Debugger", false, 0)]
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
                if (EditorUtility.DisplayDialog("Enable lightmap UV generation?", "This operation will enable the lightmap UV generation on the mesh " + importer.name + ". Do you want to continue?", "Yes", "Cancel"))
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
                if (EditorUtility.DisplayDialog("Enable lightmap UV generation?", "This operation will enable the lightmap UV generation on " + importers.Count + " meshes. Do you want to continue?", "Yes", "Cancel"))
                {
                    importers.ForEach(i => { i.generateSecondaryUV = true; i.SaveAndReimport(); });
                }
            };
        }

        public static System.Action DisableComponent(Behaviour behaviour)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviour.GetType() + " on the GameObject \"" + behaviour.gameObject.name + "\". Do you want to continue?", "Yes", "Cancel"))
                {
                    behaviour.enabled = false;
                }
            };
        }

        public static System.Action DisableComponent(Behaviour[] behaviours)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Disable component?", "This operation will disable the " + behaviours[0].GetType() + " component on " + behaviours.Count().ToString() + " GameObjects. Do you want to continue?", "Yes", "Cancel"))
                {
                    behaviours.ToList().ForEach(b => b.enabled = false);
                }
            };
        }

        public static System.Action SetObjectLayer(GameObject obj, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + obj.name + " layer to " + layer + ". Do you want to continue?", "Yes", "Cancel"))
                {
                    obj.layer = LayerMask.NameToLayer(layer);
                }
            };
        }

        public static System.Action SetObjectLayer(GameObject[] objs, string layer)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change layer?", "This operation will change " + objs.Length + " GameObjects layer to " + layer + ". Do you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.layer = LayerMask.NameToLayer(layer));
                }
            };
        }

        public static System.Action SetLightmapSize(int newSize)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change lightmap size?", "This operation will change your lightmap size from " + LightmapEditorSettings.maxAtlasSize + " to " + newSize + ". Do you want to continue?", "Yes", "Cancel"))
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
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + obj.name + " tag to " + tag + ". Do you want to continue?", "Yes", "Cancel"))
                {
                    obj.tag = tag;
                }
            };
        }

        public static System.Action SetGameObjectTag(GameObject[] objs, string tag)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change tag?", "This operation will change " + objs.Length + " GameObjects tag to " + tag + ". Do you want to continue?", "Yes", "Cancel"))
                {
                    objs.ToList().ForEach(o => o.tag = tag);
                }
            };
        }

        public static System.Action ChangeShader(Material material, String shader)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Change shader?", "This operation will change the shader of the material " + material.name + " to " + shader + ". Do you want to continue?", "Yes", "Cancel"))
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
                if (EditorUtility.DisplayDialog("Change shader?", "This operation will change the shader of " + materials.Length + " materials to " + shader + ". Do you want to continue?", "Yes", "Cancel"))
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
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes in the group \"" + lpg.gameObject.name + "\". Do you want to continue?", "Yes", "Cancel"))
                {
                    lpg.probePositions = lpg.probePositions.Distinct().ToArray();
                }
            };
        }

        public static System.Action RemoveOverlappingLightprobes(LightProbeGroup[] lpgs)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Remove overlapping light probes?", "This operation will remove any overlapping light probes found in the current scene. Do you want to continue?", "Yes", "Cancel"))
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
                    if (EditorUtility.DisplayDialog("Remove redundant light probes?", "This operation will attempt to remove any redundant light probes in the current scene. Bake your lighting before this operation to avoid any correct light probes getting removed. Do you want to continue?", "Yes", "Cancel"))
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
                if (EditorUtility.DisplayDialog("Clear Occlusion Cache?", "This will clear your occlusion culling cache. Which has " + fileCount + " files currently. Deleting a massive amount of files can take a while. Do you want to continue?", "Yes", "Cancel"))
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

#if UNITY_POST_PROCESSING_STACK_V2
        public static System.Action SetReferenceCamera(VRC_SceneDescriptor descriptor)
        {
            return () =>
            {
                if (EditorUtility.DisplayDialog("Setup reference camera?", "This operation will try to set the reference camera in your scene descriptor. Do you want to continue?", "Yes", "Cancel"))
                {
                    if (Camera.main == null)
                    {
                        var camera = new GameObject("Main Camera");
                        camera.AddComponent<Camera>();
                        camera.AddComponent<AudioListener>();
                        camera.tag = "MainCamera";
                    }
                    descriptor.ReferenceCamera = Camera.main.gameObject;
                    if (!Camera.main.gameObject.GetComponent<PostProcessLayer>())
                    {
                        descriptor.ReferenceCamera.gameObject.AddComponent(typeof(PostProcessLayer));
                        var postprocessLayer = descriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        postprocessLayer.volumeLayer = LayerMask.GetMask("Water");
                    }
                }
            };
        }

        public static System.Action AddDefaultPPVolume()
        {
            return () =>
            {
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
                volume.gameObject.layer = LayerMask.NameToLayer("Water");
            };
        }

        public enum RemovePPEffect
        {
            AmbientOcclusion = 0,
            ScreenSpaceReflections = 1,
            BloomDirt = 2
        }

        public static System.Action RemovePostProcessSetting(PostProcessProfile postprocessProfile, RemovePPEffect effect)
        {
            return () =>
            {
                switch (effect)
                {
                    case RemovePPEffect.AmbientOcclusion:
                        postprocessProfile.RemoveSettings<AmbientOcclusion>();
                        break;
                    case RemovePPEffect.ScreenSpaceReflections:
                        postprocessProfile.RemoveSettings<ScreenSpaceReflections>();
                        break;
                    case RemovePPEffect.BloomDirt:
                        postprocessProfile.GetSetting<Bloom>().dirtTexture.value = null;
                        postprocessProfile.GetSetting<Bloom>().dirtIntensity.value = 0;
                        break;
                    default:
                        break;
                }
            };
        }
#endif
        #endregion

        #region Texts

        private const string NoSceneDescriptor = "Current scene has no Scene Descriptor. Please add one yourself, or drag the VRCWorld prefab to your scene.";
        private const string TooManySceneDescriptors = "Multiple Scene Descriptors found, you can only have one Scene Descriptor in a scene.";
        private const string TooManyPipelineManagers = "Current scene has multiple Pipeline Managers in it this can break the world upload process.";
        private const string WorldDescriptorFar = "Scene Descriptor is {0} units far from the the zero point in Unity. Having your world center out this far will cause some noticable jittering on models. You should move your world closer to the zero point of your scene.";
        private const string WorldDescriptorOff = "Scene Descriptor is {0} units far from the the zero point in Unity. It's usually good practice to try to keep it as close as possible to the absolute zero point to avoid floating point errors.";
        private const string NoSpawnPointSet = "There are no spawn points set in your Scene Descriptor. Spawning into a world with no spawn point will cause you to get thrown back to your home world.";
        private const string NullSpawnPoint = "There is a null spawn point set in your Scene Descriptor. Spawning into a null spawn point will cause you to get thrown back to your home world.";
        private const string ColliderUnderSpawnIsTrigger = "The collider \"{0}\" under your spawn point {1} has been set as Is Trigger! Spawning into a world with nothing to stand on will cause the players to fall forever.";
        private const string NoColliderUnderSpawn = "Spawn point \"{0}\" doesn't have anything underneath it. Spawning into a world with nothing to stand on will cause the players to fall forever";
        private const string NoPlayerMods = "No Player Mods found in the scene. Player mods are used for adding jumping and changing walking speed.";
        private const string TriggerTriggerNoCollider = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that doesn't have a Collider on it.";
        private const string ColliderTriggerNoCollider = "You have an OnEnterCollider or OnExitCollider Trigger \"{0}\" that doesn't have a Collider on it.";
        private const string TriggerTriggerWrongLayer = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that is not on the MirrorReflection layer.";
        private const string CombinedTriggerTriggerWrongLayer = "You have {0} OnEnterTrigger or OnExitTrigger Triggers that are not on the MirrorReflection layer.";
        private const string TriggerTriggerWrongLayerInfo = "This can stop raycasts from working properly breaking buttons, UI Menus and pickups for example.";
        private const string MirrorOnByDefault = "The mirror \"{0}\" is on by default.";
        private const string CombinedMirrorsOnByDefault = "The scene has {0} mirrors on by default.";
        private const string MirrorsOnByDefaultInfo = "This is a very bad practice and you should disable any mirrors in your world by default.";
        private const string MirrorWithDefaultLayers = "The mirror \"{0}\" has the default Reflect Layers set.";
        private const string CombinedMirrorWithDefaultLayers = "You have {0} mirrors that have the default Reflect Layers set.";
        private const string MirrorWithDefaultLayersInfo = "Only having the layers you need enabled in mirrors can save a lot of frames especially in populated instances.";
        private const string BakedOcclusionCulling = "Baked Occlusion Culling found.";
        private const string NoOcclusionCulling = "Current scene doesn't have baked Occlusion Culling. Occlusion culling gives you a lot more performance in your world, especially in larger worlds that have multiple rooms or areas.";
        private const string OcclusionCullingCacheWarning = "Current projects occlusion culling cache has {0} files. When the occlusion culling cache grows too big baking occlusion culling can take much longer than intended. It can be cleared with no negative effects.";
        private const string ActiveCameraOutputtingToRenderTexture = "Current scene has an active camera \"{0}\" outputting to a render texture.";
        private const string CombinedActiveCamerasOutputtingToRenderTextures = "Current scene has {0} active cameras outputting to render textures.";
        private const string ActiveCamerasOutputtingToRenderTextureInfo = "This will affect performance negatively by causing more drawcalls to happen. Ideally you would only have them enabled when needed.";
        private const string NoToonShaders = "You shouldn't use toon shaders for world building, as they're missing crucial things for making worlds. For world building the most recommended shader is Standard.";
        private const string NonCrunchedTextures = "{0}% of the textures used in your scene haven't been crunch compressed. Crunch compression can greatly reduce the size of your world download. It can be accessed from the texture's import settings.";
        private const string SwitchToProgressive = "Current scene is using the Enlighten lightmapper, which has been deprecated in newer versions of Unity. You should consider switching to Progressive for improved fidelity and performance.";
        private const string SingleColorEnvironmentLighting = "Consider changing your Enviroment Lighting to Gradient from Flat.";
        private const string DarkEnvironmentLighting = "Using dark colours for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if your world has dark lighting.";
        private const string CustomEnvironmentReflectionsNull = "Your Enviroment Reflections have been set to custom, but you haven't defined a custom cubemap!";
        private const string NoLightmapUV = "Model found in the scene \"{0}\" is set to be lightmapped but doesn't have Lightmap UVs.";
        private const string CombineNoLightmapUV = "Current scene has {0} models set to be lightmapped that don't have Lightmap UVs.";
        private const string NoLightmapUVInfo = "This causes issues when baking lighting. You can enable generating Lightmap UV's in the model's import settings.";
        private const string LightsNotBaked = "Current scenes lighting is not baked. Consider baking your lights for improved performance.";
        private const string ConsiderLargerLightmaps = "Consider increasing your Lightmap Size from {0} to 4096. This allows for more stuff to fit on a single lightmap, leaving less textures that need to be sampled.";
        private const string ConsiderSmallerLightmaps = "Baking lightmaps at 4096 with Progressive GPU will silently fall back to CPU Progressive because it needs more than 12GB GPU Memory to be able to bake with GPU Progressive.";
        private const string NonBakedBakedLights = "The light {0} is set to be baked/mixed but it hasn't been baked yet!";
        private const string CombinedNonBakedBakedLights = "The scene contains {0} baked/mixed lights that haven't been baked!";
        private const string NonBakedBakedLightsInfo = "Baked lights that haven't been baked yet function as realtime lights ingame.";
        private const string LightingDataAssetInfo = "Your lighting data asset takes up {0} MB of your world's size. This contains your scene's light probe data and realtime GI data.";
        private const string NoLightProbes = "No light probes found in the current scene, which means your baked lights won't affect dynamic objects such as players and pickups.";
        private const string LightProbeCountNotBaked = "Current scene contains {0} light probes, but {1} of them haven't been baked yet.";
        private const string LightProbesRemovedNotReBaked = "Some light probes have been removed after the last bake, bake them again to update your scene's lighting data. The lighting data contains {0} baked light probes and the current scene has {1} light probes.";
        private const string LightProbeCount = "Current scene contains {0} baked light probes.";
        private const string OverlappingLightProbes = "Light Probe Group \"{0}\" has {1} overlapping light probes.";
        private const string CombinedOverlappingLightProbes = "{0} Light Probe Groups with overlapping light probes found.";
        private const string OverlappingLightProbesInfo = "These can cause a slowdown in the editor and won't get baked because Unity will skip any extra overlapping probes.";
        private const string NoReflectionProbes = "Current scene has no active reflection probes. Reflection probes are needed to have proper reflections on reflective materials.";
        private const string ReflectionProbesSomeUnbaked = "The reflection probe \"{0}\" is unbaked.";
        private const string CombinedReflectionProbesSomeUnbaked = "Current scene has {0} unbaked reflection probes.";
        private const string ReflectionProbeCountText = "Current scene has {0} reflection probes.";
        private const string PostProcessingImportedButNotSetup = "Current project has Post Processing imported, but you haven't set it up yet.";
        private const string NoReferenceCameraSet = "Current scenes Scene Descriptor has no Reference Camera set. Without a Reference Camera set, you won't be able to see Post Processing ingame.";
        private const string NoPostProcessingVolumes = "You don't have any Post Processing Volumes in your scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";
        private const string ReferenceCameraNoPostProcessingLayer = "Your Reference Camera doesn't have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";
        private const string VolumeBlendingLayerNotSet = "You don't have a Volume Blending Layer set in your Post Process Layer, so post processing won't work. Using the Water layer is recommended.";
        private const string PostProcessingVolumeNotGlobalNoCollider = "The Post Processing Volume \"{0}\" isn't marked as Global and doesn't have a collider. It won't affect the camera without one of these set on it.";
        private const string NoProfileSet = "You don't have a profile set in the Post Processing Volume \"{0}\"";
        private const string VolumeOnWrongLayer = "Your Post Processing Volume \"{0}\" is not on one of the layers set in your cameras Post Processing Layer setting. (Currently: {1})";
        private const string DontUseNoneForTonemapping = "Use either Neutral or ACES for Color Grading tonemapping, selecting None for Tonemapping is essentially the same as leaving Tonemapping unchecked.";
        private const string TooHighBloomIntensity = "Don't raise the Bloom intensity too high! You should use a low Bloom intensity, between 0.01 to 0.3.";
        private const string TooHighBloomThreshold = "You should avoid having your Bloom threshold set high. It might cause unexpected problems with avatars. Ideally you should keep it at 0, but always below 1.0.";
        private const string NoBloomDirtInVr = "Don't use Bloom Dirt, it looks really bad in VR!";
        private const string NoAmbientOcclusion = "Don't use Ambient Occlusion in VRChat! VRchat is using Forward rendering, so it gets applied on top of everything else, which is bad! It also has a super high rendering cost in VR.";
        private const string DepthOfFieldWarning = "Depth of field has a high performance cost, and is very disorientating in VR. If you really want to use depth of field, have it be disabled by default.";
        private const string ScreenSpaceReflectionsWarning = "Screen Space Reflections only works when using deferred rendering. VRchat isn't using deferred rendering, so this will have no effect on the main camera.";
        private const string VignetteWarning = "Only use vignette in very small amounts. A powerful vignette can cause sickness in VR.";
        private const string NoPostProcessingImported = "Post Processing package not found in the project.";
        private const string QuestBakedLightingWarning = "You should bake lights for content build for Quest.";
        private const string AmbientModeSetToCustom = "Your Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";
        private const string NoProblemsFoundInPP = "No problems found in your post processing setup. In some cases where post processing is working in editor but not in game it's possible some imported asset is causing it not to function properly.";
        private const string BakeryLightNotSetEditorOnly = "Your Bakery light named \"{0}\" is not set to be EditorOnly this causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";
        private const string CombinedBakeryLightNotSetEditorOnly = "You have {0} Bakery lights are not set to be EditorOnly.";
        private const string BakeryLightNotSetEditorOnlyInfo = "This causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";
        private const string BakeryLightUnityLight = "Your Bakery light named \"{0}\" has an active Unity Light component on it.";
        private const string CombinedBakeryLightUnityLight = "You have {0} Bakery lights that have an active Unity Light component on it.";
        private const string BakeryLightUnityLightInfo = "These will not get baked with Bakery and will keep acting as real time lights even if set to baked.";
        private const string MissingShaderWarning = "The material \"{0}\" found in your scene has a missing or broken shader.";
        private const string CombinedMissingShaderWarning = "You have {0} materials found in your scene that have missing or broken shaders.";
        private const string MissingShaderWarningInfo = "These will fallback to the pink error shader.";
        private const string ErrorPauseWarning = "You have Error Pause enabled in your console this can cause your world upload to fail by interrupting the build process.";
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

        private static MessageCategory _general;
        private static MessageCategory _optimization;
        private static MessageCategory _lighting;
        private static MessageCategory _postProcessing;

        private void CheckScene()
        {
            _masterList.ClearCategories();

            if (_general == null)
                _general = _masterList.AddMessageCategory("General");

            if (_optimization == null)
                _optimization = _masterList.AddMessageCategory("Optimization");

            if (_lighting == null)
                _lighting = _masterList.AddMessageCategory("Lighting");

            if (_postProcessing == null)
                _postProcessing = _masterList.AddMessageCategory("Post Processing");

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
                    _general.AddMessageGroup(new MessageGroup(TooManyPipelineManagers, MessageType.Error).AddSingleMessage(new SingleMessage(Array.ConvertAll(pipelines.ToArray(), s => s.gameObject))));
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

            //Check if console has error pause on
            if (ConsoleFlagUtil.GetConsoleErrorPause())
            {
                _general.AddMessageGroup(new MessageGroup(ErrorPauseWarning, MessageType.Error).AddSingleMessage(new SingleMessage(SetErrorPause(false))));
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
                var triggerWrongLayerGroup = new MessageGroup(TriggerTriggerWrongLayer, CombinedTriggerTriggerWrongLayer, TriggerTriggerWrongLayerInfo, MessageType.Warning);
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
                if (!cameras[i].targetTexture) continue;

                cameraCount++;
                activeCameras.Add(cameras[i].gameObject);
            }

            if (cameraCount > 0)
            {
                var activeCamerasMessages = new MessageGroup(ActiveCameraOutputtingToRenderTexture, CombinedActiveCamerasOutputtingToRenderTextures, ActiveCamerasOutputtingToRenderTextureInfo, MessageType.BadFPS);
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
                var activeCamerasMessage = new MessageGroup(MirrorOnByDefault, CombinedMirrorsOnByDefault, MirrorsOnByDefaultInfo, MessageType.BadFPS);
                for (int i = 0; i < mirrors.Length; i++)
                {
                    activeCamerasMessage.AddSingleMessage(new SingleMessage(mirrors[i].name).SetSelectObject(mirrors[i].gameObject));
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
                    var notEditorOnlyGroup = new MessageGroup(BakeryLightNotSetEditorOnly, CombinedBakeryLightNotSetEditorOnly, BakeryLightNotSetEditorOnlyInfo, MessageType.Warning);
                    foreach (var item in notEditorOnly)
                    {
                        notEditorOnlyGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(SetGameObjectTag(item, "EditorOnly")).SetSelectObject(item));
                    }
                    _lighting.AddMessageGroup(notEditorOnlyGroup.SetGroupAutoFix(SetGameObjectTag(notEditorOnly.ToArray(), "EditorOnly")));
                }

                if (unityLightOnBakeryLight.Count > 0)
                {
                    var unityLightGroup = new MessageGroup(BakeryLightUnityLight, CombinedBakeryLightUnityLight, BakeryLightUnityLightInfo, MessageType.Warning);
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
                if (lights[i].lightmapBakeType != LightmapBakeType.Baked &&
                    lights[i].lightmapBakeType != LightmapBakeType.Mixed) continue;

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
                long bakedProbes = 0;

                if (probes != null)
                {
                    bakedProbes = probes.count;
                }

                var lightprobegroups = GameObject.FindObjectsOfType<LightProbeGroup>();

                var overlappingLightProbesGroup = new MessageGroup(OverlappingLightProbes, CombinedOverlappingLightProbes, OverlappingLightProbesInfo, MessageType.Info);

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
                    var nonBakedLightsGroup = new MessageGroup(NonBakedBakedLights, CombinedNonBakedBakedLights, NonBakedBakedLightsInfo, MessageType.Warning);
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
                _lighting.AddMessageGroup(new MessageGroup(QuestBakedLightingWarning, MessageType.BadFPS));
#else
                _lighting.AddMessageGroup(new MessageGroup(LightsNotBaked, MessageType.Tips).AddSingleMessage(new SingleMessage(nonBakedLights.ToArray())));
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
                    var probesUnbakedGroup = new MessageGroup(ReflectionProbesSomeUnbaked, CombinedReflectionProbesSomeUnbaked, MessageType.Warning);

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
                //Start by checking if reference camera has been set in the Scene Descriptor
                if (!sceneDescriptor.ReferenceCamera)
                {
                    _postProcessing.AddMessageGroup(new MessageGroup(NoReferenceCameraSet, MessageType.Info).AddSingleMessage(new SingleMessage(SetReferenceCamera(sceneDescriptor)).SetSelectObject(sceneDescriptor.gameObject)));
                }
                else
                {
                    //Check for post process volumes in the scene
                    if (postProcessVolumes.Length == 0)
                    {
                        _postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingVolumes, MessageType.Info).AddSingleMessage(new SingleMessage(AddDefaultPPVolume())));
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

                        foreach (var postprocessVolume in postProcessVolumes)
                        {
                            //Check if the layer matches the cameras post processing layer
                            if (postprocessLayer.volumeLayer != (postprocessLayer.volumeLayer | (1 << postprocessVolume.gameObject.layer)))
                            {
                                _postProcessing.AddMessageGroup(new MessageGroup(VolumeOnWrongLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessVolume.gameObject.name, Helper.GetAllLayersFromMask(postprocessLayer.volumeLayer)).SetSelectObject(postprocessVolume.gameObject)));
                            }

                            //Check if the volume has a profile set
                            if (!postprocessVolume.profile && !postprocessVolume.sharedProfile)
                            {
                                _postProcessing.AddMessageGroup(new MessageGroup(NoProfileSet, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessVolume.gameObject.name)));
                                continue;
                            }

                            if (!postprocessVolume.isGlobal)
                            {
                                //Check if the collider is either global or has a collider on it
                                if (!postprocessVolume.GetComponent<Collider>())
                                {
                                    GameObject[] objs = { postprocessVolume.gameObject };
                                    _postProcessing.AddMessageGroup(new MessageGroup(PostProcessingVolumeNotGlobalNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessVolume.name).SetSelectObject(postprocessVolume.gameObject)));
                                }
                            }
                            else
                            {
                                //Go trough the profile settings and see if any bad one's are used
                                PostProcessProfile postprocessProfile;

                                if (postprocessVolume.profile)
                                    postprocessProfile = postprocessVolume.profile as PostProcessProfile;
                                else
                                    postprocessProfile = postprocessVolume.sharedProfile as PostProcessProfile;

                                if (postprocessProfile.GetSetting<ColorGrading>())
                                {
                                    if (postprocessProfile.GetSetting<ColorGrading>().tonemapper.value.ToString() == "None")
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(DontUseNoneForTonemapping, MessageType.Error).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                                    }
                                }

                                if (postprocessProfile.GetSetting<Bloom>())
                                {
                                    if (postprocessProfile.GetSetting<Bloom>().intensity.value > 0.3f)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomIntensity, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                                    }

                                    if (postprocessProfile.GetSetting<Bloom>().threshold.value > 1f)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(TooHighBloomThreshold, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                                    }

                                    if (postprocessProfile.GetSetting<Bloom>().dirtTexture.value || postprocessProfile.GetSetting<Bloom>().dirtIntensity.value != 0)
                                    {
                                        _postProcessing.AddMessageGroup(new MessageGroup(NoBloomDirtInVr, MessageType.Error).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocessProfile, RemovePPEffect.BloomDirt)).SetSelectObject(postprocessLayer.gameObject)));
                                    }
                                }
                                if (postprocessProfile.GetSetting<AmbientOcclusion>())
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(NoAmbientOcclusion, MessageType.Error).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocessProfile, RemovePPEffect.AmbientOcclusion)).SetSelectObject(postprocessLayer.gameObject)));
                                }

                                if (postprocessProfile.GetSetting<DepthOfField>() && postprocessVolume.isGlobal)
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(DepthOfFieldWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                                }

                                if (postprocessProfile.GetSetting<ScreenSpaceReflections>())
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(ScreenSpaceReflectionsWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocessProfile, RemovePPEffect.ScreenSpaceReflections)).SetSelectObject(postprocessLayer.gameObject)));
                                }

                                if (postprocessProfile.GetSetting<Vignette>())
                                {
                                    _postProcessing.AddMessageGroup(new MessageGroup(VignetteWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocessLayer.gameObject)));
                                }
                            }
                        }
                    }
                }

                if (_postProcessing.MessageGroups.Count == 0)
                {
                    _postProcessing.AddMessageGroup(new MessageGroup(NoProblemsFoundInPP, MessageType.Info));
                }
            }
#else
            _postProcessing.AddMessageGroup(new MessageGroup(NoPostProcessingImported, MessageType.Info));
#endif

            //Gameobject checks

            var importers = new List<ModelImporter>();
            var meshName = new List<string>();

            var unCrunchedTextures = new List<Texture>();
            var badShaders = 0;
            var textureCount = 0;

            var missingShaders = new List<Material>();

            var checkedMaterials = new List<Material>();

            var mirrorsDefaultLayers = new MessageGroup(MirrorWithDefaultLayers, CombinedMirrorWithDefaultLayers, MirrorWithDefaultLayersInfo, MessageType.Tips);

            UnityEngine.Object[] allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < allGameObjects.Length; i++)
            {
                GameObject gameObject = allGameObjects[i] as GameObject;

                if (EditorUtility.IsPersistent(gameObject.transform.root.gameObject) && !(gameObject.hideFlags == HideFlags.NotEditable || gameObject.hideFlags == HideFlags.HideAndDontSave))
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
                                            meshName.Add(sharedMesh.name);
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

                    // Check materials for problems
                    var meshRenderer = gameObject.GetComponent<Renderer>();

                    for (int l = 0; l < meshRenderer.sharedMaterials.Length; l++)
                    {
                        Material material = meshRenderer.sharedMaterials[l];
                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        var shader = material.shader;
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
            }

            if (mirrorsDefaultLayers.MessageList.Count > 0)
            {
                _optimization.AddMessageGroup(mirrorsDefaultLayers);
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
                var noUVGroup = new MessageGroup(NoLightmapUV, CombineNoLightmapUV, NoLightmapUVInfo, MessageType.Warning);
                for (int i = 0; i < modelsCount; i++)
                {
                    var modelName = meshName[i];
                    var modelImporter = importers[i];
                    noUVGroup.AddSingleMessage(new SingleMessage(modelName).SetAutoFix(SetGenerateLightmapUV(modelImporter)).SetAssetPath(modelImporter.assetPath));
                }
                _lighting.AddMessageGroup(noUVGroup.SetGroupAutoFix(SetGenerateLightmapUV(importers)).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html"));
            }

            var missingShadersCount = missingShaders.Count;
            if (missingShadersCount > 0)
            {
                var missingShadersGroup = new MessageGroup(MissingShaderWarning, CombinedMissingShaderWarning, MissingShaderWarningInfo, MessageType.Error);
                for (int i = 0; i < missingShaders.Count; i++)
                {
                    missingShadersGroup.AddSingleMessage(new SingleMessage(missingShaders[i].name).SetAssetPath(AssetDatabase.GetAssetPath(missingShaders[i])).SetAutoFix(ChangeShader(missingShaders[i], "Standard")));
                }
                _general.AddMessageGroup(missingShadersGroup.SetGroupAutoFix(ChangeShader(missingShaders.ToArray(), "Standard")));
            }
        }

        private static MessageCategoryList _masterList;

        private void Awake()
        {
            RefreshBuild();
        }

        private void OnFocus()
        {
            _recheck = true;
            RefreshBuild();
            _buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(AssetPath);
        }

        private const string LastBuild = "Library/LastBuild.buildreport";

        private const string BuildReportDir = "Assets/_LastBuild/";

        private const string AssetPath = "Assets/_LastBuild/LastBuild.buildreport";

        private static DateTime _timeNow;

        private static BuildReport _buildReport;

        private static void RefreshBuild()
        {
            _timeNow = DateTime.Now.ToUniversalTime();

            if (!Directory.Exists(BuildReportDir))
                Directory.CreateDirectory(BuildReportDir);

            if (File.Exists(LastBuild))
            {
                if (!File.Exists(AssetPath) || File.GetLastWriteTime(LastBuild) > File.GetLastWriteTime(AssetPath))
                {
                    File.Copy(LastBuild, AssetPath, true);
                    AssetDatabase.ImportAsset(AssetPath);
                    _buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(AssetPath);
                }
            }
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                richText = true
            };

            if (_masterList == null)
            {
                _masterList = new MessageCategoryList();
            }

            if (_recheck)
            {
#if VRWTOOLKIT_BENCHMARK_MODE
                var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
                //Check for bloat in occlusion cache
                if (_occlusionCacheFiles == 0 && Directory.Exists("Library/Occlusion/"))
                {
                    var task = Task.Run(CountOcclusionCacheFiles);
                }

                _recheck = false;
                CheckScene();
#if VRWTOOLKIT_BENCHMARK_MODE
                watch.Stop();
                Debug.Log("Scene checked in: " + watch.ElapsedMilliseconds + " ms.");
#endif
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (_buildReport != null)
            {
                GUILayout.Label("<b>Last build size:</b> " + Helper.FormatSize(_buildReport.summary.totalSize), style);

                GUILayout.Label("<b>Last build was done:</b> " + Helper.FormatTime(_timeNow.Subtract(_buildReport.summary.buildEndedAt)), style);

                GUILayout.Label("<b>Errors last build:</b> " + _buildReport.summary.totalErrors.ToString(), style);

                GUILayout.Label("<b>Warnings last build:</b> " + _buildReport.summary.totalWarnings.ToString(), style);
            }
            else
            {
                GUILayout.Label("No build found");
            }

            GUILayout.EndVertical();

            _masterList.DrawTabSelector();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.BeginHorizontal();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            _masterList.DrawMessages();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
#endif