#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
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
using VRC.Core;
using System.Threading;
using System.Threading.Tasks;

namespace VRWorldToolkit.WorldDebugger
{
    public class WorldDebugger : EditorWindow
    {
        private static Texture _BadFPS;
        private static Texture _GoodFPS;
        private static Texture _Tips;
        private static Texture _Info;
        private static Texture _Error;
        private static Texture _Warning;

        private static bool recheck = true;

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

        class SingleMessage
        {
            public string variable;
            public string variable2;
            public GameObject[] selectObjects;
            public System.Action autoFix;
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

            public SingleMessage(GameObject[] selectObjects)
            {
                this.selectObjects = selectObjects;
            }

            public SingleMessage(GameObject selectObjects)
            {
                this.selectObjects = new GameObject[] { selectObjects };
            }

            public SingleMessage(System.Action autoFix)
            {
                this.autoFix = autoFix;
            }

            public SingleMessage SetSelectObject(GameObject[] selectObjects)
            {
                this.selectObjects = selectObjects;
                return this;
            }

            public SingleMessage SetSelectObject(GameObject selectObjects)
            {
                this.selectObjects = new GameObject[] { selectObjects };
                return this;
            }

            public SingleMessage SetAutoFix(System.Action autoFix)
            {
                this.autoFix = autoFix;
                return this;
            }

            public SingleMessage SetAssetPath(string assetPath)
            {
                this.assetPath = assetPath;
                return this;
            }
        }

        class MessageGroup : IEquatable<MessageGroup>
        {
            public bool showAll = false;
            public string message;
            public string combinedMessage;
            public string additionalInfo;

            public MessageType messageType;

            public string documentation;

            public System.Action groupAutoFix;

            public List<SingleMessage> messageList = new List<SingleMessage>();

            public MessageGroup(string message, MessageType messageType)
            {
                this.message = message;
                this.messageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, MessageType messageType)
            {
                this.message = message;
                this.combinedMessage = combinedMessage;
                this.messageType = messageType;
            }

            public MessageGroup(string message, string combinedMessage, string additionalInfo, MessageType messageType)
            {
                this.message = message;
                this.combinedMessage = combinedMessage;
                this.additionalInfo = additionalInfo;
                this.messageType = messageType;
            }

            public MessageGroup SetGroupAutoFix(System.Action groupAutoFix)
            {
                this.groupAutoFix = groupAutoFix;
                return this;
            }

            public MessageGroup SetDocumentation(string documentation)
            {
                this.documentation = documentation;
                return this;
            }

            public MessageGroup AddSingleMessage(SingleMessage message)
            {
                messageList.Add(message);
                return this;
            }

            public MessageGroup SetMessageList(List<SingleMessage> messageList)
            {
                this.messageList = messageList;
                return this;
            }

            public int GetTotalCount()
            {
                int count = 0;

                if (messageList != null)
                {
                    foreach (var item in messageList)
                    {
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
                }

                if (count == 0)
                {
                    return messageList.Count;
                }

                return count;
            }

            public GameObject[] GetSelectObjects()
            {
                List<GameObject> objs = new List<GameObject>();
                foreach (var item in messageList.Where(o => o.selectObjects != null))
                {
                    objs.AddRange(item.selectObjects);
                }
                return objs.ToArray();
            }

            public System.Action[] GetSeparateActions()
            {
                return messageList.Where(m => m.autoFix != null).Select(m => m.autoFix).ToArray();
            }

            public bool Buttons()
            {
                bool buttons = false;
                if (GetSelectObjects().Any() || groupAutoFix != null || GetSeparateActions().Any() || groupAutoFix != null || documentation != null)
                {
                    buttons = true;
                }
                return buttons;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MessageGroup);
            }

            public bool Equals(MessageGroup other)
            {
                return other != null &&
                       message == other.message &&
                       combinedMessage == other.combinedMessage &&
                       additionalInfo == other.additionalInfo &&
                       messageType == other.messageType;
            }

            public override int GetHashCode()
            {
                var hashCode = 842570769;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(message);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(combinedMessage);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(additionalInfo);
                hashCode = hashCode * -1521134295 + messageType.GetHashCode();
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

        class MessageCategory
        {
            public List<MessageGroup> messageGroups = new List<MessageGroup>();

            Dictionary<int, bool> expandedGroups = new Dictionary<int, bool>();

            public string listName;
            public bool enabled;

            public MessageCategory(string listName)
            {
                this.listName = listName;
                enabled = false;
            }

            public void AddMessageGroup(MessageGroup debuggerMessage)
            {
                messageGroups.Add(debuggerMessage);
            }

            public void ClearMessages()
            {
                messageGroups.Clear();
            }

            public bool IsExpanded(MessageGroup mg)
            {
                int hash = mg.GetHashCode();
                if (expandedGroups.ContainsKey(hash))
                {
                    return expandedGroups[hash];
                }
                return false;
            }

            public void SetExpanded(MessageGroup mg, bool expanded)
            {
                int hash = mg.GetHashCode();
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

        class MessageCategoryList
        {
            public List<MessageCategory> messageCategory = new List<MessageCategory>();

            public MessageCategory AddMessageCategory(string name)
            {
                MessageCategory newMessageCategory = new MessageCategory(name);
                messageCategory.Add(newMessageCategory);
                return newMessageCategory;
            }

            public void DrawTabSelector()
            {
                EditorGUILayout.BeginHorizontal();

                foreach (var item in messageCategory)
                {
                    string button = "miniButtonMid";
                    if (messageCategory.First() == item)
                    {
                        button = "miniButtonLeft";
                    }
                    else if (messageCategory.Last() == item)
                    {
                        button = "miniButtonRight";
                    }
                    bool currentState = item.enabled;
                    item.enabled = GUILayout.Toggle(item.enabled, item.listName, button);
                }

                EditorGUILayout.EndHorizontal();
            }

            public void ClearCategories()
            {
                messageCategory.ForEach(m => m.ClearMessages());
            }

            private bool AllDisabled()
            {
                return messageCategory.All(m => !m.enabled);
            }

            private readonly static GUIStyle boxStyle = new GUIStyle("HelpBox");

            public void DrawMessages()
            {
                foreach (var group in messageCategory)
                {
                    if (group.enabled || AllDisabled())
                    {
                        GUILayout.Label(group.listName, EditorStyles.boldLabel);

                        var buttonWidth = 80;
                        var buttonHeight = 20;

                        if (group.messageGroups.Count == 0)
                        {
                            DrawMessage("No messages found for " + group.listName + ".", MessageType.Info);
                        }

                        foreach (var messageGroup in group.messageGroups)
                        {
                            bool hasButtons = messageGroup.Buttons();

                            if (messageGroup.messageList.Count > 0)
                            {
                                if (messageGroup.combinedMessage != null && messageGroup.messageList.Count != 1)
                                {
                                    EditorGUILayout.BeginHorizontal();

                                    string finalMessage = string.Format(messageGroup.combinedMessage, messageGroup.GetTotalCount().ToString());

                                    if (messageGroup.additionalInfo != null)
                                    {
                                        finalMessage += " " + messageGroup.additionalInfo;
                                    }

                                    if (hasButtons)
                                    {
                                        GUIContent Box = new GUIContent(finalMessage, GetDebuggerIcon(messageGroup.messageType));
                                        GUILayout.Box(Box, boxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                        EditorGUILayout.BeginVertical();

                                        if (messageGroup.documentation != null)
                                        {
                                            if (GUILayout.Button("Info", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                if (messageGroup.documentation != null)
                                                {
                                                    Application.OpenURL(messageGroup.documentation);
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

                                        EditorGUI.BeginDisabledGroup(messageGroup.groupAutoFix == null);

                                        if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                        {
                                            messageGroup.groupAutoFix();
                                            recheck = true;
                                        }

                                        EditorGUI.EndDisabledGroup();

                                        EditorGUILayout.EndVertical();
                                    }
                                    else
                                    {
                                        DrawMessage(finalMessage, messageGroup.messageType);
                                    }

                                    EditorGUILayout.EndHorizontal();

                                    bool expanded = group.IsExpanded(messageGroup);

                                    expanded = EditorGUILayout.Foldout(expanded, "Show separate messages");

                                    group.SetExpanded(messageGroup, expanded);

                                    if (expanded)
                                    {
                                        GUIStyle boxStylePadded = new GUIStyle("HelpBox")
                                        {
                                            margin = new RectOffset(18, 4, 4, 4),
                                            alignment = TextAnchor.MiddleLeft
                                        };

                                        foreach (var message in messageGroup.messageList)
                                        {
                                            EditorGUILayout.BeginHorizontal();

                                            string finalSingleMessage = string.Format(messageGroup.message, message.variable, message.variable2);

                                            if (hasButtons)
                                            {
                                                GUIContent Box = new GUIContent(finalSingleMessage);
                                                GUILayout.Box(Box, boxStylePadded, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 121));

                                                EditorGUILayout.BeginVertical();

                                                EditorGUI.BeginDisabledGroup(!(message.selectObjects != null || message.assetPath != null));

                                                if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    if (message.assetPath != null)
                                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(message.assetPath));

                                                    else
                                                        Selection.objects = message.selectObjects;
                                                }

                                                EditorGUI.EndDisabledGroup();

                                                EditorGUI.BeginDisabledGroup(message.autoFix == null);

                                                if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                                {
                                                    message.autoFix();
                                                    recheck = true;
                                                }

                                                EditorGUI.EndDisabledGroup();

                                                EditorGUILayout.EndVertical();
                                            }
                                            else
                                            {
                                                DrawMessage(finalSingleMessage, messageGroup.messageType);
                                            }

                                            EditorGUILayout.EndHorizontal();
                                        }

                                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                                    }
                                }
                                else
                                {
                                    foreach (var message in messageGroup.messageList)
                                    {
                                        EditorGUILayout.BeginHorizontal();

                                        string finalMessage = string.Format(messageGroup.message, message.variable, message.variable2);

                                        if (messageGroup.additionalInfo != null)
                                        {
                                            finalMessage = string.Concat(finalMessage, " ", messageGroup.additionalInfo);
                                        }

                                        if (hasButtons)
                                        {
                                            GUIContent Box = new GUIContent(finalMessage, GetDebuggerIcon(messageGroup.messageType));
                                            GUILayout.Box(Box, boxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                            EditorGUILayout.BeginVertical();

                                            EditorGUI.BeginDisabledGroup(!(message.selectObjects != null || message.assetPath != null));

                                            if (GUILayout.Button("Select", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                if (message.assetPath != null)
                                                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(message.assetPath));

                                                else
                                                    Selection.objects = message.selectObjects;
                                            }

                                            EditorGUI.EndDisabledGroup();

                                            EditorGUI.BeginDisabledGroup(message.autoFix == null);

                                            if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                            {
                                                message.autoFix();
                                                recheck = true;
                                            }

                                            EditorGUI.EndDisabledGroup();

                                            EditorGUILayout.EndVertical();
                                        }
                                        else
                                        {
                                            DrawMessage(finalMessage, messageGroup.messageType);
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

                                    GUIContent Box = new GUIContent(messageGroup.message, GetDebuggerIcon(messageGroup.messageType));
                                    GUILayout.Box(Box, boxStyle, GUILayout.MinHeight(42), GUILayout.MinWidth(EditorGUIUtility.currentViewWidth - 107));

                                    EditorGUILayout.BeginVertical();

                                    EditorGUI.BeginDisabledGroup(messageGroup.documentation == null);

                                    if (GUILayout.Button("Info", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                    {
                                        if (messageGroup.documentation != null)
                                        {
                                            Application.OpenURL(messageGroup.documentation);
                                        }
                                    }

                                    EditorGUI.EndDisabledGroup();

                                    EditorGUI.BeginDisabledGroup(messageGroup.groupAutoFix == null);

                                    if (GUILayout.Button("Auto Fix", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                                    {
                                        if (messageGroup.groupAutoFix != null)
                                        {
                                            messageGroup.groupAutoFix();
                                            recheck = true;
                                        }
                                    }

                                    EditorGUI.EndDisabledGroup();

                                    EditorGUILayout.EndVertical();

                                    EditorGUILayout.EndHorizontal();
                                }
                                else
                                {
                                    DrawMessage(messageGroup.message, messageGroup.messageType);
                                }
                            }
                        }
                    }
                }
            }

            public void DrawMessage(string messageText, MessageType type)
            {
                GUIContent Box = new GUIContent(messageText, GetDebuggerIcon(type));
                GUILayout.Box(Box, boxStyle, GUILayout.MinHeight(42), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 18));
            }
        }

        Vector2 scrollPos;

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
                        GameObject camera = new GameObject("Main Camera");
                        camera.AddComponent<Camera>();
                        camera.AddComponent<AudioListener>();
                        camera.tag = "MainCamera";
                    }
                    descriptor.ReferenceCamera = Camera.main.gameObject;
                    if (!Camera.main.gameObject.GetComponent<PostProcessLayer>())
                    {
                        descriptor.ReferenceCamera.gameObject.AddComponent(typeof(PostProcessLayer));
                        PostProcessLayer postprocess_layer = descriptor.ReferenceCamera.gameObject.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        postprocess_layer.volumeLayer = LayerMask.GetMask("Water");
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
                    string path = AssetDatabase.GUIDToAssetPath("eaac6f7291834264f97854154e89bf76");
                    if (path != null)
                    {
                        AssetDatabase.CopyAsset(path, "Assets/Post Processing/SilentProfile.asset");
                    }
                }

                //Set up the post process volume
                PostProcessVolume volume = GameObject.Instantiate(PostProcessManager.instance.QuickVolume(16, 100f));
                if (File.Exists("Assets/Post Processing/SilentProfile.asset"))
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

        public static System.Action RemovePostProcessSetting(PostProcessProfile postprocess_profile, RemovePSEffect effect)
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
        #endregion

        #region Texts
        private static readonly string noSceneDescriptor = "Current scene has no Scene Descriptor. Please add one yourself, or drag the VRCWorld prefab to your scene.";
        private static readonly string tooManySceneDescriptors = "Multiple Scene Descriptors found, you can only have one Scene Descriptor in a scene.";
        private static readonly string tooManyPipelineManagers = "Current scene has multiple Pipeline Managers in it this can break the world upload process.";
        private static readonly string worldDescriptorFar = "Scene Descriptor is {0} units far from the the zero point in Unity. Having your world center out this far will cause some noticable jittering on models. You should move your world closer to the zero point of your scene.";
        private static readonly string worldDescriptorOff = "Scene Descriptor is {0} units far from the the zero point in Unity. It's usually good practice to try to keep it as close as possible to the absolute zero point to avoid floating point errors.";
        private static readonly string noSpawnPointSet = "There are no spawn points set in your Scene Descriptor. Spawning into a world with no spawn point will cause you to get thrown back to your home world.";
        private static readonly string nullSpawnPoint = "There is a null spawn point set in your Scene Descriptor. Spawning into a null spawn point will cause you to get thrown back to your home world.";
        private static readonly string colliderUnderSpawnIsTrigger = "The collider \"{0}\" under your spawn point {1} has been set as Is Trigger! Spawning into a world with nothing to stand on will cause the players to fall forever.";
        private static readonly string noColliderUnderSpawn = "Spawn point \"{0}\" doesn't have anything underneath it. Spawning into a world with nothing to stand on will cause the players to fall forever";
        private static readonly string noPlayerMods = "No Player Mods found in the scene. Player mods are used for adding jumping and changing walking speed.";
        private static readonly string triggerTriggerNoCollider = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that doesn't have a Collider on it.";
        private static readonly string colliderTriggerNoCollider = "You have an OnEnterCollider or OnExitCollider Trigger \"{0}\" that doesn't have a Collider on it.";
        private static readonly string triggerTriggerWrongLayer = "You have an OnEnterTrigger or OnExitTrigger Trigger \"{0}\" that is not on the MirrorReflection layer.";
        private static readonly string combinedTriggerTriggerWrongLayer = "You have {0} OnEnterTrigger or OnExitTrigger Triggers that are not on the MirrorReflection layer.";
        private static readonly string triggerTriggerWrongLayerInfo = "This can stop raycasts from working properly breaking buttons, UI Menus and pickups for example.";
        private static readonly string mirrorOnByDefault = "The mirror \"{0}\" is on by default.";
        private static readonly string combinedMirrorsOnByDefault = "The scene has {0} mirrors on by default.";
        private static readonly string mirrorsOnByDefaultInfo = "This is a very bad practice and you should disable any mirrors in your world by default.";
        private static readonly string mirrorWithDefaultLayers = "The mirror \"{0}\" has the default Reflect Layers set.";
        private static readonly string combinedMirrorWithDefaultLayers = "You have {0} mirrors that have the default Reflect Layers set.";
        private static readonly string mirrorWithDefaultLayersInfo = "Only having the layers you need enabled in mirrors can save a lot of frames especially in populated instances.";
        private static readonly string bakedOcclusionCulling = "Baked Occlusion Culling found.";
        private static readonly string noOcclusionCulling = "Current scene doesn't have baked Occlusion Culling. Occlusion culling gives you a lot more performance in your world, especially in larger worlds that have multiple rooms or areas.";
        private static readonly string occlusionCullingCacheWarning = "Current projects occlusion culling cache has {0} files. When the occlusion culling cache grows too big baking occlusion culling can take much longer than intended. It can be cleared with no negative effects.";
        private static readonly string activeCameraOutputtingToRenderTexture = "Current scene has an active camera \"{0}\" outputting to a render texture.";
        private static readonly string combinedActiveCamerasOutputtingToRenderTextures = "Current scene has {0} active cameras outputting to render textures.";
        private static readonly string activeCamerasOutputtingToRenderTextureInfo = "This will affect performance negatively by causing more drawcalls to happen. Ideally you would only have them enabled when needed.";
        private static readonly string noToonShaders = "You shouldn't use toon shaders for world building, as they're missing crucial things for making worlds. For world building the most recommended shader is Standard.";
        private static readonly string nonCrunchedTextures = "{0}% of the textures used in your scene haven't been crunch compressed. Crunch compression can greatly reduce the size of your world download. It can be accessed from the texture's import settings.";
        private static readonly string switchToProgressive = "Current scene is using the Enlighten lightmapper, which has been deprecated in newer versions of Unity. You should consider switching to Progressive for improved fidelity and performance.";
        private static readonly string singleColorEnviromentLighting = "Consider changing your Enviroment Lighting to Gradient from Flat.";
        private static readonly string darkEnviromentLighting = "Using dark colours for Environment Lighting can cause avatars to look weird. Only use dark Environment Lighting if your world has dark lighting.";
        private static readonly string customEnviromentReflectionsNull = "Your Enviroment Reflections have been set to custom, but you haven't defined a custom cubemap!";
        private static readonly string noLightmapUV = "Model found in the scene \"{0}\" is set to be lightmapped but doesn't have Lightmap UVs.";
        private static readonly string combineNoLightmapUV = "Current scene has {0} models set to be lightmapped that don't have Lightmap UVs.";
        private static readonly string noLightmapUVInfo = "This causes issues when baking lighting. You can enable generating Lightmap UV's in the model's import settings.";
        private static readonly string lightsNotBaked = "Current scenes lighting is not baked. Consider baking your lights for improved performance.";
        private static readonly string considerLargerLightmaps = "Consider increasing your Lightmap Size from {0} to 4096. This allows for more stuff to fit on a single lightmap, leaving less textures that need to be sampled.";
        private static readonly string considerSmallerLightmaps = "Baking lightmaps at 4096 with Progressive GPU will silently fall back to CPU Progressive because it needs more than 12GB GPU Memory to be able to bake with GPU Progressive.";
        private static readonly string nonBakedBakedLights = "The light {0} is set to be baked/mixed but it hasn't been baked yet!";
        private static readonly string combinedNonBakedBakedLights = "The scene contains {0} baked/mixed lights that haven't been baked!";
        private static readonly string nonBakedBakedLightsInfo = "Baked lights that haven't been baked yet function as realtime lights ingame.";
        private static readonly string lightingDataAssetInfo = "Your lighting data asset takes up {0} MB of your world's size. This contains your scene's light probe data and realtime GI data.";
        private static readonly string noLightProbes = "No light probes found in the current scene, which means your baked lights won't affect dynamic objects such as players and pickups.";
        private static readonly string lightProbeCountNotBaked = "Current scene contains {0} light probes, but {1} of them haven't been baked yet.";
        private static readonly string lightProbesRemovedNotReBaked = "Some light probes have been removed after the last bake, bake them again to update your scene's lighting data. The lighting data contains {0} baked light probes and the current scene has {1} light probes.";
        private static readonly string lightProbeCount = "Current scene contains {0} baked light probes.";
        private static readonly string overlappingLightProbes = "Light Probe Group \"{0}\" has {1} overlapping light probes.";
        private static readonly string combinedOverlappingLightProbes = "{0} Light Probe Groups with overlapping light probes found.";
        private static readonly string overlappingLightProbesInfo = "These can cause a slowdown in the editor and won't get baked because Unity will skip any extra overlapping probes.";
        private static readonly string noReflectionProbes = "Current scene has no active reflection probes. Reflection probes are needed to have proper reflections on reflective materials.";
        private static readonly string reflectionProbesSomeUnbaked = "The reflection probe \"{0}\" is unbaked.";
        private static readonly string combinedReflectionProbesSomeUnbaked = "Current scene has {0} unbaked reflection probes.";
        private static readonly string reflectionProbeCountText = "Current scene has {0} baked reflection probes.";
        private static readonly string postProcessingImportedButNotSetup = "Current project has Post Processing imported, but you haven't set it up yet.";
        private static readonly string noReferenceCameraSet = "Current scenes Scene Descriptor has no Reference Camera set. Without a Reference Camera set, you won't be able to see Post Processing ingame.";
        private static readonly string noPostProcessingVolumes = "You don't have any Post Processing Volumes in your scene. A Post Processing Volume is needed to apply effects to the camera's Post Processing Layer.";
        private static readonly string referenceCameraNoPostProcessingLayer = "Your Reference Camera doesn't have a Post Processing Layer on it. A Post Processing Layer is needed for the Post Processing Volume to affect the camera.";
        private static readonly string volumeBlendingLayerNotSet = "You don't have a Volume Blending Layer set in your Post Process Layer, so post processing won't work. Using the Water layer is recommended.";
        private static readonly string postProcessingVolumeNotGlobalNoCollider = "The Post Processing Volume \"{0}\" isn't marked as Global and doesn't have a collider. It won't affect the camera without one of these set on it.";
        private static readonly string noProfileSet = "You don't have a profile set in the Post Processing Volume \"{0}\"";
        private static readonly string volumeOnWrongLayer = "Your Post Processing Volume \"{0}\" is not on one of the layers set in your cameras Post Processing Layer setting. (Currently: {1})";
        private static readonly string dontUseNoneForTonemapping = "Use either Neutral or ACES for Color Grading tonemapping, selecting None for Tonemapping is essentially the same as leaving Tonemapping unchecked.";
        private static readonly string tooHighBloomIntensity = "Don't raise the Bloom intensity too high! You should use a low Bloom intensity, between 0.01 to 0.3.";
        private static readonly string tooHighBloomThreshold = "You should avoid having your Bloom threshold set high. It might cause unexpected problems with avatars. Ideally you should keep it at 0, but always below 1.0.";
        private static readonly string noBloomDirtInVR = "Don't use Bloom Dirt, it looks really bad in VR!";
        private static readonly string noAmbientOcclusion = "Don't use Ambient Occlusion in VRChat! VRchat is using Forward rendering, so it gets applied on top of everything else, which is bad! It also has a super high rendering cost in VR.";
        private static readonly string depthOfFieldWarning = "Depth of field has a high performance cost, and is very disorientating in VR. If you really want to use depth of field, have it be disabled by default.";
        private static readonly string screenSpaceReflectionsWarning = "Screen Space Reflections only works when using deferred rendering. VRchat isn't using deferred rendering, so this will have no effect on the main camera.";
        private static readonly string vignetteWarning = "Only use vignette in very small amounts. A powerful vignette can cause sickness in VR.";
        private static readonly string noPostProcessingImported = "Post Processing package not found in the project.";
        private static readonly string questBakedLightingWarning = "You should bake lights for content build for Quest.";
        private static readonly string ambientModeSetToCustom = "Your Environment Lighting setting is broken. This will override all light probes in the scene with black ambient light. Please change it to something else.";
        private static readonly string noProblemsFoundInPP = "No problems found in your post processing setup. In some cases where post processing is working in editor but not in game it's possible some imported asset is causing it not to function properly.";
        private static readonly string bakeryLightNotSetEditorOnly = "Your Bakery light named \"{0}\" is not set to be EditorOnly this causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";
        private static readonly string combinedBakeryLightNotSetEditorOnly = "You have {0} Bakery lights are not set to be EditorOnly.";
        private static readonly string bakeryLightNotSetEditorOnlyInfo = "This causes unnecessary errors in the output log loading into a world in VRChat because external scripts get removed in the upload process.";
        private static readonly string bakeryLightUnityLight = "Your Bakery light named \"{0}\" has an active Unity Light component on it.";
        private static readonly string combinedBakeryLightUnityLight = "You have {0} Bakery lights that have an active Unity Light component on it.";
        private static readonly string bakeryLightUnityLightInfo = "These will not get baked with Bakery and will keep acting as real time lights even if set to baked.";
        private static readonly string missingShaderWarning = "The material \"{0}\" found in your scene has a missing or broken shader.";
        private static readonly string combinedMissingShaderWarning = "You have {0} materials found in your scene that have missing or broken shaders.";
        private static readonly string missingShaderWarningInfo = "These will fallback to the pink error shader.";
        private static readonly string errorPauseWarning = "You have Error Pause enabled in your console this can cause your world upload to fail by interrupting the build process.";
        #endregion

        static long occlusionCacheFiles = 0;

        //TODO: Better check threading
        private static void CountOcclusionCacheFiles()
        {
            occlusionCacheFiles = Directory.EnumerateFiles("Library/Occlusion/").Count();
        }

        private static MessageCategory general;
        private static MessageCategory optimization;
        private static MessageCategory lighting;
        private static MessageCategory postProcessing;

        private void CheckScene()
        {
            masterList.ClearCategories();

            if (general == null)
                general = masterList.AddMessageCategory("General");

            if (optimization == null)
                optimization = masterList.AddMessageCategory("Optimization");

            if (lighting == null)
                lighting = masterList.AddMessageCategory("Lighting");

            if (postProcessing == null)
                postProcessing = masterList.AddMessageCategory("Post Processing");

            //General Checks

            //Get Descriptors
            VRC_SceneDescriptor[] descriptors = FindObjectsOfType(typeof(VRC_SceneDescriptor)) as VRC_SceneDescriptor[];
            long descriptorCount = descriptors.Length;
            VRC_SceneDescriptor sceneDescriptor;
            PipelineManager[] pipelines = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];

            //Check if a descriptor exists
            if (descriptorCount == 0)
            {
                general.AddMessageGroup(new MessageGroup(noSceneDescriptor, MessageType.Error));
                return;
            }
            else
            {
                sceneDescriptor = descriptors[0];

                //Make sure only one descriptor exists
                if (descriptorCount > 1)
                {
                    general.AddMessageGroup(new MessageGroup(tooManySceneDescriptors, MessageType.Info).AddSingleMessage(new SingleMessage(Array.ConvertAll(descriptors, s => s.gameObject))));
                    return;
                }
                else if (pipelines.Length > 1)
                {
                    general.AddMessageGroup(new MessageGroup(tooManyPipelineManagers, MessageType.Error).AddSingleMessage(new SingleMessage(Array.ConvertAll(pipelines.ToArray(), s => s.gameObject))));
                }

                //Check how far the descriptor is from zero point for floating point errors
                int descriptorRemoteness = (int)Vector3.Distance(sceneDescriptor.transform.position, new Vector3(0.0f, 0.0f, 0.0f));

                if (descriptorRemoteness > 1000)
                {
                    general.AddMessageGroup(new MessageGroup(worldDescriptorFar, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
                else if (descriptorRemoteness > 250)
                {
                    general.AddMessageGroup(new MessageGroup(worldDescriptorOff, MessageType.Error).AddSingleMessage(new SingleMessage(descriptorRemoteness.ToString()).SetSelectObject(Array.ConvertAll(descriptors, s => s.gameObject))));
                }
            }

            //Check if console has error pause on
            if (ConsoleFlagUtil.GetConsoleErrorPause())
            {
                general.AddMessageGroup(new MessageGroup(errorPauseWarning, MessageType.Error).AddSingleMessage(new SingleMessage(SetErrorPause(false))));
            }

            //Get spawn points for any possible problems
            Transform[] spawns = sceneDescriptor.spawns.Where(s => s != null).ToArray();

            int spawnsLength = sceneDescriptor.spawns.Length;
            bool emptySpawns = false;

            if (spawnsLength != spawns.Length)
            {
                emptySpawns = true;
            }

            if (spawns.Length == 0)
            {
                general.AddMessageGroup(new MessageGroup(noSpawnPointSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
            }
            else
            {
                if (emptySpawns)
                {
                    general.AddMessageGroup(new MessageGroup(nullSpawnPoint, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.gameObject).SetAutoFix(FixSpawns(sceneDescriptor))));
                }

                foreach (var item in sceneDescriptor.spawns)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    if (!Physics.Raycast(item.position + new Vector3(0, 0.01f, 0), Vector3.down, out RaycastHit hit, Mathf.Infinity, 0, QueryTriggerInteraction.Ignore))
                    {
                        if (Physics.Raycast(item.position + new Vector3(0, 0.01f, 0), Vector3.down, out hit, Mathf.Infinity))
                        {
                            if (hit.collider.isTrigger)
                            {
                                general.AddMessageGroup(new MessageGroup(colliderUnderSpawnIsTrigger, MessageType.Error).AddSingleMessage(new SingleMessage(hit.collider.name, item.gameObject.name).SetSelectObject(item.gameObject)));
                            }
                        }
                        else
                        {
                            general.AddMessageGroup(new MessageGroup(noColliderUnderSpawn, MessageType.Error).AddSingleMessage(new SingleMessage(item.gameObject.name).SetSelectObject(item.gameObject)));
                        }
                    }
                }
            }

#if VRC_SDK_VRCSDK2
            //Check if the world has playermods defined
            VRC_PlayerMods[] playermods = FindObjectsOfType(typeof(VRC_PlayerMods)) as VRC_PlayerMods[];
            if (playermods.Length == 0)
            {
                general.AddMessageGroup(new MessageGroup(noPlayerMods, MessageType.Tips));
            }

            //Get triggers in the world
            VRC_Trigger[] trigger_scripts = (VRC_Trigger[])VRC_Trigger.FindObjectsOfType(typeof(VRC_Trigger));

            List<GameObject> triggerWrongLayer = new List<GameObject>();

            //Check for OnEnterTriggers to make sure they are on mirrorreflection layer
            foreach (var trigger_script in trigger_scripts)
            {
                foreach (var trigger in trigger_script.Triggers)
                {
                    if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitCollider)
                    {
                        if (!trigger_script.gameObject.GetComponent<Collider>())
                        {
                            if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterTrigger || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitTrigger)
                            {
                                general.AddMessageGroup(new MessageGroup(triggerTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(trigger_script.name).SetSelectObject(trigger_script.gameObject)));
                            }
                            else if (trigger.TriggerType == VRC_Trigger.TriggerType.OnEnterCollider || trigger.TriggerType == VRC_Trigger.TriggerType.OnExitCollider)
                            {
                                general.AddMessageGroup(new MessageGroup(colliderTriggerNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(trigger_script.name).SetSelectObject(trigger_script.gameObject)));
                            }
                        }
                        if ((trigger.TriggerType.ToString() == "OnEnterTrigger" || trigger.TriggerType.ToString() == "OnExitTrigger") && trigger_script.gameObject.layer != LayerMask.NameToLayer("MirrorReflection"))
                        {
                            triggerWrongLayer.Add(trigger_script.gameObject);
                        }
                    }
                }
            }

            if (triggerWrongLayer.Count > 0)
            {
                MessageGroup triggerWrongLayerGroup = new MessageGroup(triggerTriggerWrongLayer, combinedTriggerTriggerWrongLayer, triggerTriggerWrongLayerInfo, MessageType.Warning);
                foreach (var item in triggerWrongLayer)
                {
                    triggerWrongLayerGroup.AddSingleMessage(new SingleMessage(item.name).SetSelectObject(item.gameObject).SetAutoFix(SetObjectLayer(item.gameObject, "MirrorReflection")));
                }
                general.AddMessageGroup(triggerWrongLayerGroup.SetGroupAutoFix(SetObjectLayer(triggerWrongLayerGroup.GetSelectObjects(), "MirrorReflection")));
            }
#endif

            //Optimization Checks

            //Check for occlusion culling
            if (StaticOcclusionCulling.umbraDataSize > 0)
            {
                optimization.AddMessageGroup(new MessageGroup(bakedOcclusionCulling, MessageType.GoodFPS));
            }
            else
            {
                optimization.AddMessageGroup(new MessageGroup(noOcclusionCulling, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/occlusion-culling-getting-started.html"));
            }

            if (occlusionCacheFiles > 0)
            {
                //Set the message type depending on how many files found
                MessageType cacheWarningType = MessageType.Info;
                if (occlusionCacheFiles > 50000)
                {
                    cacheWarningType = MessageType.Error;
                }
                else if (occlusionCacheFiles > 5000)
                {
                    cacheWarningType = MessageType.Warning;
                }

                optimization.AddMessageGroup(new MessageGroup(occlusionCullingCacheWarning, cacheWarningType).AddSingleMessage(new SingleMessage(occlusionCacheFiles.ToString()).SetAutoFix(ClearOcclusionCache(occlusionCacheFiles))));
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

            if (cameraCount > 0 && cameraCount != 1)
            {
                MessageGroup activeCamerasMessages = new MessageGroup(activeCameraOutputtingToRenderTexture, combinedActiveCamerasOutputtingToRenderTextures, activeCamerasOutputtingToRenderTextureInfo, MessageType.BadFPS);
                foreach (var camera in activeCameras)
                {
                    activeCamerasMessages.AddSingleMessage(new SingleMessage(cameraCount.ToString()).SetSelectObject(camera.gameObject));
                }
                optimization.AddMessageGroup(activeCamerasMessages);
            }

            //Get active mirrors in the world and complain about them
            VRC_MirrorReflection[] mirrors = FindObjectsOfType(typeof(VRC_MirrorReflection)) as VRC_MirrorReflection[];
            if (mirrors.Length > 0)
            {
                MessageGroup activeCamerasMessage = new MessageGroup(mirrorOnByDefault, combinedMirrorsOnByDefault, mirrorsOnByDefaultInfo, MessageType.BadFPS);
                foreach (var mirror in mirrors)
                {
                    activeCamerasMessage.AddSingleMessage(new SingleMessage(mirror.name).SetSelectObject(mirror.gameObject));
                }
                optimization.AddMessageGroup(activeCamerasMessage);
            }

            //Lighting Checks

            if (RenderSettings.ambientMode.Equals(AmbientMode.Custom))
            {
                lighting.AddMessageGroup(new MessageGroup(ambientModeSetToCustom, MessageType.Error).AddSingleMessage(new SingleMessage(SetAmbientMode(AmbientMode.Skybox))));
            }

            if (RenderSettings.ambientMode.Equals(AmbientMode.Flat))
            {
                lighting.AddMessageGroup(new MessageGroup(singleColorEnviromentLighting, MessageType.Tips));
            }

            if ((Helper.GetBrightness(RenderSettings.ambientLight) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Flat)) || (Helper.GetBrightness(RenderSettings.ambientSkyColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientEquatorColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)) || (Helper.GetBrightness(RenderSettings.ambientGroundColor) < 0.1f && RenderSettings.ambientMode.Equals(AmbientMode.Trilight)))
            {
                lighting.AddMessageGroup(new MessageGroup(darkEnviromentLighting, MessageType.Tips));
            }

            if (RenderSettings.defaultReflectionMode.Equals(DefaultReflectionMode.Custom) && !RenderSettings.customReflection)
            {
                lighting.AddMessageGroup(new MessageGroup(customEnviromentReflectionsNull, MessageType.Error).AddSingleMessage(new SingleMessage(SetEnviromentReflections(DefaultReflectionMode.Skybox))));
            }

            bool bakedLighting = false;

#if BAKERY_INCLUDED
            List<GameObject> bakeryLights = new List<GameObject>();
            //TODO: Investigate whether or not these should be included
            //bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryDirectLight)) as BakeryDirectLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakeryPointLight)) as BakeryPointLight[], s => s.gameObject));
            bakeryLights.AddRange(Array.ConvertAll(FindObjectsOfType(typeof(BakerySkyLight)) as BakerySkyLight[], s => s.gameObject));

            if (bakeryLights.Count > 0)
            {
                List<GameObject> notEditorOnly = new List<GameObject>();
                List<GameObject> unityLightOnBakeryLight = new List<GameObject>();

                bakedLighting = true;

                foreach (var obj in bakeryLights)
                {
                    if (obj.tag != "EditorOnly")
                    {
                        notEditorOnly.Add(obj);
                    }

                    if (obj.GetComponent<Light>())
                    {
                        Light light = obj.GetComponent<Light>();
                        if (!light.bakingOutput.isBaked && light.enabled)
                        {
                            unityLightOnBakeryLight.Add(obj);
                        }
                    }
                }

                if (notEditorOnly.Count > 0)
                {
                    MessageGroup notEditorOnlyGroup = new MessageGroup(bakeryLightNotSetEditorOnly, combinedBakeryLightNotSetEditorOnly, bakeryLightNotSetEditorOnlyInfo, MessageType.Warning);
                    foreach (var item in notEditorOnly)
                    {
                        notEditorOnlyGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(SetGameObjectTag(item, "EditorOnly")).SetSelectObject(item));
                    }
                    lighting.AddMessageGroup(notEditorOnlyGroup.SetGroupAutoFix(SetGameObjectTag(notEditorOnly.ToArray(), "EditorOnly")));
                }

                if (unityLightOnBakeryLight.Count > 0)
                {
                    MessageGroup unityLightGroup = new MessageGroup(bakeryLightUnityLight, combinedBakeryLightUnityLight, bakeryLightUnityLightInfo, MessageType.Warning);
                    foreach (var item in unityLightOnBakeryLight)
                    {
                        unityLightGroup.AddSingleMessage(new SingleMessage(item.name).SetAutoFix(DisableComponent(item.GetComponent<Light>())).SetSelectObject(item));
                    }
                    lighting.AddMessageGroup(unityLightGroup.SetGroupAutoFix(DisableComponent(Array.ConvertAll(unityLightOnBakeryLight.ToArray(), s => s.GetComponent<Light>()))));
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
                    if (!light.bakingOutput.isBaked && light.GetComponent<Light>().enabled)
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
                //Count lightmaps and suggest to use bigger lightmaps if needed
                int lightMapSize = 0;
                lightMapSize = LightmapEditorSettings.maxAtlasSize;
                if (lightMapSize != 4096 && LightmapSettings.lightmaps.Length > 1 && !LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU))
                {
                    if (LightmapSettings.lightmaps[0] != null)
                    {
                        if (LightmapSettings.lightmaps[0].lightmapColor.height != 4096)
                        {
                            lighting.AddMessageGroup(new MessageGroup(considerLargerLightmaps, MessageType.Tips).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(4096))));
                        }
                    }
                }

                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.ProgressiveGPU) && lightMapSize == 4096 && SystemInfo.graphicsMemorySize < 12000)
                {
                    lighting.AddMessageGroup(new MessageGroup(considerSmallerLightmaps, MessageType.Warning).AddSingleMessage(new SingleMessage(lightMapSize.ToString()).SetAutoFix(SetLightmapSize(2048))));
                }

                //Count how many light probes the scene has
                long probeCounter = 0;
                long bakedProbes = 0;

                if (probes != null)
                {
                    bakedProbes = probes.count;
                }

                LightProbeGroup[] lightprobegroups = GameObject.FindObjectsOfType<LightProbeGroup>();

                MessageGroup overlappingLightProbesGroup = new MessageGroup(overlappingLightProbes, combinedOverlappingLightProbes, overlappingLightProbesInfo, MessageType.Info);

                foreach (LightProbeGroup lightprobegroup in lightprobegroups)
                {
                    if (lightprobegroup.probePositions.GroupBy(p => p).Any(g => g.Count() > 1))
                    {
                        overlappingLightProbesGroup.AddSingleMessage(new SingleMessage(lightprobegroup.name, (lightprobegroup.probePositions.Length - lightprobegroup.probePositions.Distinct().ToArray().Length).ToString()).SetSelectObject(lightprobegroup.gameObject).SetAutoFix(RemoveOverlappingLightprobes(lightprobegroup)));
                    }

                    probeCounter += lightprobegroup.probePositions.Length;
                }

                if (probeCounter > 0)
                {
                    if ((probeCounter - bakedProbes) < 0)
                    {
                        lighting.AddMessageGroup(new MessageGroup(lightProbesRemovedNotReBaked, MessageType.Warning).AddSingleMessage(new SingleMessage(bakedProbes.ToString(), probeCounter.ToString())));
                    }
                    else
                    {
                        if ((bakedProbes - (0.9 * probeCounter)) < 0)
                        {
                            lighting.AddMessageGroup(new MessageGroup(lightProbeCountNotBaked, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"), (probeCounter - bakedProbes).ToString("n0"))));
                        }
                        else
                        {
                            lighting.AddMessageGroup(new MessageGroup(lightProbeCount, MessageType.Info).AddSingleMessage(new SingleMessage(probeCounter.ToString("n0"))));
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

                //Since the scene has baked lights complain if there's no lightprobes
                else if (probes == null && probeCounter == 0)
                {
                    lighting.AddMessageGroup(new MessageGroup(noLightProbes, MessageType.Info).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightProbes.html"));
                }

                //Check lighting data asset size if it exists
                long length = 0;
                if (Lightmapping.lightingDataAsset != null)
                {
                    string lmdName = Lightmapping.lightingDataAsset.name;
                    string pathTo = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    length = new System.IO.FileInfo(pathTo).Length;
                    lighting.AddMessageGroup(new MessageGroup(lightingDataAssetInfo, MessageType.Info).AddSingleMessage(new SingleMessage((length / 1024.0f / 1024.0f).ToString("F2"))));
                }

#if !BAKERY_INCLUDED
                if (LightmapEditorSettings.lightmapper.Equals(LightmapEditorSettings.Lightmapper.Enlighten))
                {
                    lighting.AddMessageGroup(new MessageGroup(switchToProgressive, MessageType.Tips));
                }
#endif

                if (nonBakedLights.Count != 0)
                {
                    MessageGroup nonBakedLightsGroup = new MessageGroup(nonBakedBakedLights, combinedNonBakedBakedLights, nonBakedBakedLightsInfo, MessageType.Warning);
                    foreach (var item in nonBakedLights)
                    {
                        nonBakedLightsGroup.AddSingleMessage(new SingleMessage(item.name).SetSelectObject(item.gameObject));
                    }
                    lighting.AddMessageGroup(nonBakedLightsGroup);
                }
            }
            else
            {
#if UNITY_ANDROID
                lighting.AddMessageGroup(new MessageGroup(questBakedLightingWarning, MessageType.BadFPS));
#else
                lighting.AddMessageGroup(new MessageGroup(lightsNotBaked, MessageType.Tips).AddSingleMessage(new SingleMessage(nonBakedLights.ToArray())));
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
                lighting.AddMessageGroup(new MessageGroup(noReflectionProbes, MessageType.Tips).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/class-ReflectionProbe.html"));
            }
            else if (reflectionProbesUnbaked > 0)
            {
                MessageGroup probesUnbakedGroup = new MessageGroup(reflectionProbesSomeUnbaked, combinedReflectionProbesSomeUnbaked, MessageType.Warning);
                foreach (var item in unbakedprobes)
                {
                    probesUnbakedGroup.AddSingleMessage(new SingleMessage(item.name).SetSelectObject(item));
                }
                lighting.AddMessageGroup(probesUnbakedGroup);
            }
            else
            {
                lighting.AddMessageGroup(new MessageGroup(reflectionProbeCountText, MessageType.Info).AddSingleMessage(new SingleMessage(reflectionProbeCount.ToString())));
            }

            //Post Processing Checks

#if UNITY_POST_PROCESSING_STACK_V2
            PostProcessVolume[] PostProcessVolumes = FindObjectsOfType(typeof(PostProcessVolume)) as PostProcessVolume[];
            bool postProcessLayerExists = false;

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

            if (!sceneDescriptor.ReferenceCamera && PostProcessVolumes.Length == 0 && !postProcessLayerExists)
            {
                postProcessing.AddMessageGroup(new MessageGroup(postProcessingImportedButNotSetup, MessageType.Info));
            }
            else
            {
                //Start by checking if reference camera has been set in the Scene Descriptor
                if (!sceneDescriptor.ReferenceCamera)
                {
                    postProcessing.AddMessageGroup(new MessageGroup(noReferenceCameraSet, MessageType.Info).AddSingleMessage(new SingleMessage(SetReferenceCamera(sceneDescriptor)).SetSelectObject(sceneDescriptor.gameObject)));
                }
                else
                {
                    //Check for post process volumes in the scene
                    if (PostProcessVolumes.Length == 0)
                    {
                        postProcessing.AddMessageGroup(new MessageGroup(noPostProcessingVolumes, MessageType.Info).AddSingleMessage(new SingleMessage(AddDefaultPPVolume())));
                    }
                    else
                    {
                        PostProcessLayer postprocess_layer = sceneDescriptor.ReferenceCamera.GetComponent(typeof(PostProcessLayer)) as PostProcessLayer;
                        if (postprocess_layer == null)
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(referenceCameraNoPostProcessingLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                        }

                        LayerMask volume_layer = postprocess_layer.volumeLayer;
                        if (volume_layer == LayerMask.GetMask("Nothing"))
                        {
                            postProcessing.AddMessageGroup(new MessageGroup(volumeBlendingLayerNotSet, MessageType.Error).AddSingleMessage(new SingleMessage(sceneDescriptor.ReferenceCamera.gameObject)));
                        }

                        foreach (PostProcessVolume postprocess_volume in PostProcessVolumes)
                        {
                            //Check if the layer matches the cameras post processing layer
                            if (postprocess_layer.volumeLayer != (postprocess_layer.volumeLayer | (1 << postprocess_volume.gameObject.layer)))
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(volumeOnWrongLayer, MessageType.Error).AddSingleMessage(new SingleMessage(postprocess_volume.gameObject.name, Helper.GetAllLayersFromMask(postprocess_layer.volumeLayer)).SetSelectObject(postprocess_volume.gameObject)));
                            }

                            //Check if the volume has a profile set
                            if (!postprocess_volume.profile && !postprocess_volume.sharedProfile)
                            {
                                postProcessing.AddMessageGroup(new MessageGroup(noProfileSet, MessageType.Error).AddSingleMessage(new SingleMessage(postprocess_volume.gameObject.name)));
                                continue;
                            }

                            if (!postprocess_volume.isGlobal)
                            {
                                //Check if the collider is either global or has a collider on it
                                if (!postprocess_volume.GetComponent<Collider>())
                                {
                                    GameObject[] objs = { postprocess_volume.gameObject };
                                    postProcessing.AddMessageGroup(new MessageGroup(postProcessingVolumeNotGlobalNoCollider, MessageType.Error).AddSingleMessage(new SingleMessage(postprocess_volume.name).SetSelectObject(postprocess_volume.gameObject)));
                                }
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
                                        postProcessing.AddMessageGroup(new MessageGroup(dontUseNoneForTonemapping, MessageType.Error).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                                    }
                                }

                                if (postprocess_profile.GetSetting<Bloom>())
                                {
                                    if (postprocess_profile.GetSetting<Bloom>().intensity.value > 0.3f)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(tooHighBloomIntensity, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                                    }

                                    if (postprocess_profile.GetSetting<Bloom>().threshold.value > 1f)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(tooHighBloomThreshold, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                                    }

                                    if (postprocess_profile.GetSetting<Bloom>().dirtTexture.value || postprocess_profile.GetSetting<Bloom>().dirtIntensity.value != 0)
                                    {
                                        postProcessing.AddMessageGroup(new MessageGroup(noBloomDirtInVR, MessageType.Error).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.BloomDirt)).SetSelectObject(postprocess_layer.gameObject)));
                                    }
                                }
                                if (postprocess_profile.GetSetting<AmbientOcclusion>())
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(noAmbientOcclusion, MessageType.Error).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.AmbientOcclusion)).SetSelectObject(postprocess_layer.gameObject)));
                                }

                                if (postprocess_profile.GetSetting<DepthOfField>() && postprocess_volume.isGlobal)
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(depthOfFieldWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                                }

                                if (postprocess_profile.GetSetting<ScreenSpaceReflections>())
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(screenSpaceReflectionsWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(RemovePostProcessSetting(postprocess_profile, RemovePSEffect.ScreenSpaceReflections)).SetSelectObject(postprocess_layer.gameObject)));
                                }

                                if (postprocess_profile.GetSetting<Vignette>())
                                {
                                    postProcessing.AddMessageGroup(new MessageGroup(vignetteWarning, MessageType.Warning).AddSingleMessage(new SingleMessage(postprocess_layer.gameObject)));
                                }
                            }
                        }
                    }
                }

                if (postProcessing.messageGroups.Count == 0)
                {
                    postProcessing.AddMessageGroup(new MessageGroup(noProblemsFoundInPP, MessageType.Info));
                }
            }
#else
            postProcessing.AddMessageGroup(new MessageGroup(noPostProcessingImported, MessageType.Info));
#endif

            //Gameobject checks

            List<ModelImporter> importers = new List<ModelImporter>();
            List<string> meshName = new List<string>();

            List<Texture> unCrunchedTextures = new List<Texture>();
            int badShaders = 0;
            int textureCount = 0;

            List<Material> missingShaders = new List<Material>();

            List<Material> checkedMaterials = new List<Material>();

            MessageGroup mirrorsDefaultLayers = new MessageGroup(mirrorWithDefaultLayers, combinedMirrorWithDefaultLayers, mirrorWithDefaultLayersInfo, MessageType.Tips);

            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
            {
                if (EditorUtility.IsPersistent(gameObject.transform.root.gameObject) && !(gameObject.hideFlags == HideFlags.NotEditable || gameObject.hideFlags == HideFlags.HideAndDontSave))
                    continue;

                if (gameObject.GetComponent<Renderer>())
                {
                    // If baked lighting in the scene check for lightmap uvs
                    if (bakedLighting)
                    {
                        if (GameObjectUtility.AreStaticEditorFlagsSet(gameObject, StaticEditorFlags.LightmapStatic) && gameObject.GetComponent<MeshRenderer>())
                        {
                            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                            if (meshFilter == null)
                            {
                                continue;
                            }
                            Mesh _sharedMesh = meshFilter.sharedMesh;
                            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_sharedMesh)) != null)
                            {
                                ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_sharedMesh)) as ModelImporter;
                                if (!importers.Contains(modelImporter))
                                {
                                    if (modelImporter != null)
                                    {
                                        if (!modelImporter.generateSecondaryUV && _sharedMesh.uv2.Length == 0)
                                        {
                                            importers.Add(modelImporter);
                                            meshName.Add(_sharedMesh.name);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (gameObject.GetComponent<VRC_MirrorReflection>())
                    {
                        LayerMask mirrorMask = gameObject.GetComponent<VRC_MirrorReflection>().m_ReflectLayers;
                        if (mirrorMask.value == -1025)
                        {
                            mirrorsDefaultLayers.AddSingleMessage(new SingleMessage(gameObject.name).SetSelectObject(gameObject));
                        }
                    }

                    // Check materials for problems
                    Renderer meshRenderer = gameObject.GetComponent<Renderer>();

                    foreach (var material in meshRenderer.sharedMaterials)
                    {
                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        Shader shader = material.shader;
                        if (shader.name == "Hidden/InternalErrorShader" && !missingShaders.Contains(material))
                            missingShaders.Add(material);

                        if (shader.name.StartsWith(".poiyomi") || shader.name.StartsWith("poiyomi") || shader.name.StartsWith("arktoon") || shader.name.StartsWith("Cubedparadox") || shader.name.StartsWith("Silent's Cel Shading") || shader.name.StartsWith("Xiexe"))
                            badShaders++;

                        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                Texture texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                                if (AssetDatabase.GetAssetPath(texture) != "" && !unCrunchedTextures.Contains(texture))
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
                }
            }

            if (mirrorsDefaultLayers.messageList.Count > 0)
            {
                optimization.AddMessageGroup(mirrorsDefaultLayers);
            }

            //If more than 10% of shaders used in scene are toon shaders to leave room for people using them for avatar displays
            if (checkedMaterials.Count > 0)
            {
                if ((badShaders / checkedMaterials.Count * 100) > 10)
                {
                    optimization.AddMessageGroup(new MessageGroup(noToonShaders, MessageType.Warning));
                }
            }

            //Suggest to crunch textures if there are any uncrunched textures found
            if (textureCount > 0)
            {
                int percent = (int)((float)unCrunchedTextures.Count / (float)textureCount * 100f);
                if (percent > 20)
                {
                    optimization.AddMessageGroup(new MessageGroup(nonCrunchedTextures, MessageType.Tips).AddSingleMessage(new SingleMessage(percent.ToString())));
                }
            }


            var modelsCount = importers.Count;
            if (modelsCount > 0)
            {
                MessageGroup noUVGroup = new MessageGroup(noLightmapUV, combineNoLightmapUV, noLightmapUVInfo, MessageType.Warning);
                for (int i = 0; i < modelsCount; i++)
                {
                    string modelName = meshName[i];
                    ModelImporter modelImporter = importers[i];
                    noUVGroup.AddSingleMessage(new SingleMessage(modelName).SetAutoFix(SetGenerateLightmapUV(modelImporter)).SetAssetPath(modelImporter.assetPath));
                }
                lighting.AddMessageGroup(noUVGroup.SetGroupAutoFix(SetGenerateLightmapUV(importers)).SetDocumentation("https://docs.unity3d.com/2018.4/Documentation/Manual/LightingGiUvs-GeneratingLightmappingUVs.html"));
            }

            var missingShadersCount = missingShaders.Count;
            if (missingShadersCount > 0)
            {
                MessageGroup missingsShadersGroup = new MessageGroup(missingShaderWarning, combinedMissingShaderWarning, missingShaderWarningInfo, MessageType.Error);
                foreach (var material in missingShaders)
                {
                    missingsShadersGroup.AddSingleMessage(new SingleMessage(material.name).SetAssetPath(AssetDatabase.GetAssetPath(material)).SetAutoFix(ChangeShader(material, "Standard")));
                }
                general.AddMessageGroup(missingsShadersGroup.SetGroupAutoFix(ChangeShader(missingShaders.ToArray(), "Standard")));
            }
        }

        static MessageCategoryList masterList;

        void Awake()
        {
            RefreshBuild();
        }

        void OnFocus()
        {
            recheck = true;
            RefreshBuild();
            buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
        }

        private static readonly string lastBuild = "Library/LastBuild.buildreport";

        private static readonly string buildReportDir = "Assets/_LastBuild/";

        private static readonly string assetPath = "Assets/_LastBuild/LastBuild.buildreport";

        private static DateTime timeNow;

        private static BuildReport buildReport;

        static void RefreshBuild()
        {
            timeNow = DateTime.Now.ToUniversalTime();

            if (!Directory.Exists(buildReportDir))
                Directory.CreateDirectory(buildReportDir);

            if (File.Exists(lastBuild))
            {
                if (!File.Exists(assetPath) || File.GetLastWriteTime(lastBuild) > File.GetLastWriteTime(assetPath))
                {
                    File.Copy(lastBuild, assetPath, true);
                    AssetDatabase.ImportAsset(assetPath);
                    buildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
                }
            }
        }

        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                richText = true
            };

            if (masterList == null)
            {
                masterList = new MessageCategoryList();
            }

            if (recheck)
            {
#if VRWTOOLKIT_BENCHMARK_MODE
                System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
#endif
                //Check for bloat in occlusion cache
                if (occlusionCacheFiles == 0 && Directory.Exists("Library/Occlusion/"))
                {
                    Task task = Task.Run(CountOcclusionCacheFiles);
                }

                recheck = false;
                CheckScene();
#if VRWTOOLKIT_BENCHMARK_MODE
                watch.Stop();
                Debug.Log("Scene checked in: " + watch.ElapsedMilliseconds + " ms.");
#endif
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (buildReport != null)
            {
                GUILayout.Label("<b>Last build size:</b> " + Helper.FormatSize(buildReport.summary.totalSize), style);

                GUILayout.Label("<b>Last build was done:</b> " + Helper.FormatTime(timeNow.Subtract(buildReport.summary.buildEndedAt)), style);

                GUILayout.Label("<b>Errors last build:</b> " + buildReport.summary.totalErrors.ToString(), style);

                GUILayout.Label("<b>Warnings last build:</b> " + buildReport.summary.totalWarnings.ToString(), style);
            }
            else
            {
                GUILayout.Label("No build found");
            }

            GUILayout.EndVertical();

            masterList.DrawTabSelector();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.BeginHorizontal();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            masterList.DrawMessages();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}