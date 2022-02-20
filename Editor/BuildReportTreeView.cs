using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRWorldToolkit.DataStructures;

namespace VRWorldToolkit
{
    public class BuildReportTreeView : TreeView
    {
        private BuildReport report;
        public bool HasReport { get; private set; }
        public bool BuildSucceeded { get; private set; }

        private enum TreeColumns
        {
            Type,
            Size,
            Name,
            Extension,
            Percentage,
        }

        public BuildReportTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, BuildReport report) : base(state, multiColumnHeader)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;

            SetReport(report);
        }

        private class BuildListAsset
        {
            public string AssetType { get; set; }
            public string FullPath { get; set; }
            public int Size { get; set; }
            public double Percentage { get; set; }

            public BuildListAsset()
            {
            }

            public BuildListAsset(string assetType, string fullPath, int size)
            {
                AssetType = assetType;
                FullPath = fullPath;
                Size = size;
            }
        }

        private sealed class BuildReportItem : TreeViewItem
        {
            public Texture previewIcon { get; set; }
            public string assetType { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
            public int size { get; set; }
            public double percentage { get; set; }

            public BuildReportItem(int id, int depth, Texture previewIcon, string assetType, string displayName, string path, string extension, int size, double percentage) : base(id, depth, displayName)
            {
                this.previewIcon = previewIcon;
                this.assetType = assetType;
                this.displayName = displayName;
                this.path = path;
                this.extension = extension;
                this.size = size;
                this.percentage = percentage;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = -1, depth = -1};

            var serializedReport = new SerializedObject(report);

            var bl = new List<BuildListAsset>();

            var appendices = serializedReport.FindProperty("m_Appendices");

            for (var i = 0; i < appendices.arraySize; i++)
            {
                var appendix = appendices.GetArrayElementAtIndex(i);

                if (appendix.objectReferenceValue.GetType() != typeof(PackedAssets)) continue;

                var serializedAppendix = new SerializedObject(appendix.objectReferenceValue);

                if (serializedAppendix.FindProperty("m_ShortPath") is null) continue;

                var contents = serializedAppendix.FindProperty("m_Contents");

                for (var j = 0; j < contents.arraySize; j++)
                {
                    var entry = contents.GetArrayElementAtIndex(j);

                    var fullPath = entry.FindPropertyRelative("buildTimeAssetPath").stringValue;

                    var assetImporter = AssetImporter.GetAtPath(fullPath);

                    var type = assetImporter != null ? assetImporter.GetType().Name : "Unknown";

                    if (type.EndsWith("Importer"))
                    {
                        type = type.Remove(type.Length - 8);
                    }

                    var byteSize = entry.FindPropertyRelative("packedSize").intValue;

                    var asset = new BuildListAsset(type, fullPath, byteSize);

                    bl.Add(asset);
                }
            }

            var results = bl
                .GroupBy(x => x.FullPath)
                .Select(cx => new BuildListAsset()
                {
                    AssetType = cx.First().AssetType,
                    FullPath = cx.First().FullPath,
                    Size = cx.Sum(x => x.Size),
                })
                .OrderByDescending(x => x.Size)
                .ToList();

            var totalSize = results.Sum(x => (long) x.Size);

            for (var i = 0; i < results.Count; i++)
            {
                results[i].Percentage = (double) results[i].Size / totalSize;
            }

            for (var i = 0; i < results.Count; i++)
            {
                var asset = results[i];

                root.AddChild(new BuildReportItem(i,
                    0,
                    AssetDatabase.GetCachedIcon(asset.FullPath),
                    asset.AssetType,
                    asset.FullPath == "" ? "Unknown" : Path.GetFileName(asset.FullPath),
                    asset.FullPath,
                    Path.GetExtension(asset.FullPath),
                    asset.Size,
                    asset.Percentage)
                );
            }

            return root;
        }

        public void SetReport(BuildReport newReport)
        {
            report = newReport;
            HasReport = report != null;
            BuildSucceeded = HasReport && report.summary.result == BuildResult.Succeeded;

            if (HasReport && BuildSucceeded)
            {
                Reload();
            }
        }

        private bool HasMessages()
        {
            return report.summary.totalErrors > 0 || report.summary.totalWarnings > 0;
        }

        private struct CategoryStats
        {
            public string Name;
            public int Size;
        }

        /// <summary>
        /// Draw overall stats view of the current build report
        /// </summary>
        public void DrawOverallStats()
        {
            if (HasReport && BuildSucceeded)
            {
                var stats = base.GetRows().Cast<BuildReportItem>().ToList();

                var totalSize = stats.Sum(x => x.size);

                var grouped = stats
                    .GroupBy(x => x.assetType)
                    .Select(cx => new CategoryStats()
                    {
                        Name = cx.First().assetType,
                        Size = cx.Sum(x => x.size),
                    }).OrderByDescending(x => x.Size)
                    .ToArray();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                for (var i = 0; i < grouped.Length; i++)
                {
                    var item = grouped[i];

                    string name;

                    switch (item.Name)
                    {
                        case "Mono":
                            name = "Scripts";
                            break;
                        case "Model":
                        case "Texture":
                        case "Shader":
                        case "Asset":
                        case "TrueTypeFont":
                        case "Plugin":
                        case "Prefab":
                            name = item.Name + "s";
                            break;
                        default:
                            name = item.Name;
                            break;
                    }

                    if (GUILayout.Button(name + " -  " + EditorUtility.FormatBytes(item.Size) + " - " + ((double) item.Size / totalSize).ToString("P"), EditorStyles.label))
                    {
                        searchString = item.Name;
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private Vector2 scrollPosMessages;

        public void DrawMessages()
        {
            if (HasReport && HasMessages())
            {
                EditorGUILayout.BeginVertical();
                scrollPosMessages = EditorGUILayout.BeginScrollView(scrollPosMessages);

                var steps = report.steps;

                for (var i = 0; i < steps.Length; i++)
                {
                    var step = steps[i];

                    if (step.messages.Length > 0)
                    {
                        GUILayout.Label(step.name, Styles.BoldWrap);

                        for (var j = 0; j < step.messages.Length; j++)
                        {
                            var message = step.messages[j];

                            var messageType = MessageType.Info;

                            switch (message.type)
                            {
                                case LogType.Error:
                                case LogType.Exception:
                                    messageType = MessageType.Error;
                                    break;
                                case LogType.Assert:
                                case LogType.Warning:
                                    messageType = MessageType.Warning;
                                    break;
                            }

                            EditorGUILayout.HelpBox(message.content, messageType);
                        }

                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No messages to show.", MessageType.Info);
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.IconContent("FilterByType"),
                    contextMenuText = "Preview",
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    width = 20,
                    minWidth = 20,
                    maxWidth = 20,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size", "Uncompressed size of asset"),
                    contextMenuText = "Size",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 75,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 250,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "File type"),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("%", "Percentage out of all assets"),
                    contextMenuText = "Percentage",
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 60,
                    maxWidth = 70,
                    autoResize = false,
                    allowToggleVisibility = true
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var buildReportItem = (BuildReportItem) args.item;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect;
                // Get the current cell rect and index
                if (visibleColumnIndex == 2)
                {
                    var rectOne = args.GetCellRect(visibleColumnIndex);
                    var rectTwo = args.GetCellRect(3);

                    rect = new Rect(rectOne.position, new Vector2(rectOne.width + rectTwo.width, rectOne.height));
                }
                else
                {
                    rect = args.GetCellRect(visibleColumnIndex);
                }

                var columnIndex = (TreeColumns) args.GetColumn(visibleColumnIndex);

                //Set label style to white if cell is selected otherwise to normal
                var labelStyle = args.selected ? Styles.TreeViewLabelSelected : Styles.TreeViewLabel;

                //Handle drawing of the columns
                switch (columnIndex)
                {
                    case TreeColumns.Type:
                        GUI.Label(rect, buildReportItem.previewIcon, Styles.Center);
                        break;
                    case TreeColumns.Name:
                        if (args.selected && buildReportItem.path != "")
                        {
                            EditorGUI.LabelField(rect, buildReportItem.path, labelStyle);
                        }
                        else
                        {
                            EditorGUI.LabelField(rect, buildReportItem.displayName, labelStyle);
                        }

                        break;
                    case TreeColumns.Extension:
                        //EditorGUI.LabelField(rect, buildReportItem.extension, labelStyle);
                        break;
                    case TreeColumns.Size:
                        EditorGUI.LabelField(rect, EditorUtility.FormatBytes(buildReportItem.size), labelStyle);
                        break;
                    case TreeColumns.Percentage:
                        EditorGUI.LabelField(rect, buildReportItem.percentage.ToString("P"), labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }

        /// <summary>
        /// Handle double clicks inside the TreeView
        /// </summary>
        /// <param name="id"></param>
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            // Get the clicked item
            var clickedItem = (BuildReportItem) FindItem(id, rootItem);

            //Ping clicked asset in project window
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(clickedItem.path));
        }

        /// <summary>
        /// Handle context clicks inside the TreeView
        /// </summary>
        /// <param name="id">ID of the clicked TreeView item</param>
        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);

            // Get the clicked item
            var clickedItem = (BuildReportItem) FindItem(id, rootItem);

            //base.SetSelection(new IList<int>());

            // Create new
            var menu = new GenericMenu();

            // Create the menu items
            menu.AddItem(new GUIContent("Copy Name"), false, ReplaceClipboard, clickedItem.displayName + clickedItem.extension);
            menu.AddItem(new GUIContent("Copy Path"), false, ReplaceClipboard, clickedItem.path);

            // Show the menu
            menu.ShowAsContext();

            // Function to replace clipboard contents
            void ReplaceClipboard(object input)
            {
                EditorGUIUtility.systemCopyBuffer = (string) input;
            }
        }

        /// <summary>
        /// Check if current item matches the search string
        /// </summary>
        /// <param name="item">Item to match</param>
        /// <param name="search">Search string</param>
        /// <returns>Returns true if the search term matches name or asset type</returns>
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            // Cast match item for parameter access
            var textureTreeViewItem = (BuildReportItem) item;

            // Try to match the search string to item name or asset type and return true if it does
            return textureTreeViewItem.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   textureTreeViewItem.assetType.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Handle TreeView columns sorting changes
        /// </summary>
        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            if (!(multiColumnHeader.sortedColumnIndex > -1)) return;

            // Get TreeView items
            var items = rootItem.children.Cast<BuildReportItem>();

            // Sort items by sorted column
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 2:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 3:
                    items = items.OrderBy(x => x.extension);
                    break;
                case 1:
                case 4:
                    items = items.OrderBy(x => x.size);
                    break;
            }

            // Reverse list if not sorted ascending
            if (!multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex))
            {
                items = items.Reverse();
            }

            // Cast collection back to a list
            rootItem.children = items.Cast<TreeViewItem>().ToList();

            // Build rows again with the new sorting
            BuildRows(rootItem);
        }
    }
}