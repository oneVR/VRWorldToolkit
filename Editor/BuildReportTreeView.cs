using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VRWorldToolkit.Editor
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
            public ulong Size { get; set; }
            public double Percentage { get; set; }

            public BuildListAsset()
            {
            }

            public BuildListAsset(Type assetType, string fullPath, ulong size)
            {
                AssetType = assetType.Name;
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
            public ulong size { get; set; }
            public double percentage { get; set; }

            public BuildReportItem(int id, int depth, Texture previewIcon, string assetType, string displayName, string path, string extension, ulong size, double percentage) : base(id, depth, displayName)
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

            var packedAssets = report.packedAssets;

            var bl = new List<BuildListAsset>();

            for (var i = 0; i < packedAssets.Length; i++)
            {
                var packedAssetInfos = packedAssets[i].contents;
                for (int j = 0; j < packedAssetInfos.Length; j++)
                {
                    var packedAssetInfo = packedAssetInfos[j];
                    
                    var asset = new BuildListAsset(packedAssetInfo.type, packedAssetInfo.sourceAssetPath, packedAssetInfo.packedSize);

                    bl.Add(asset);
                }
            }

            var results = bl
                .GroupBy(x => x.FullPath)
                .Select(cx => new BuildListAsset()
                {
                    AssetType = cx.First().AssetType,
                    FullPath = cx.First().FullPath,
                    Size = cx.Aggregate(0UL, (total, x) => total + x.Size),
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
            public ulong Size;
        }

        /// <summary>
        /// Draw overall stats view of the current build report
        /// </summary>
        public void DrawOverallStats()
        {
            if (HasReport && BuildSucceeded)
            {
                var stats = base.GetRows().Cast<BuildReportItem>().ToList();

                var totalSize = stats.Aggregate(0UL, (total, x) => total + x.size);

                var grouped = stats
                    .GroupBy(x => x.assetType)
                    .Select(cx => new CategoryStats()
                    {
                        Name = cx.First().assetType,
                        Size = cx.Aggregate(0UL, (total, x) => total + x.size),
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

                    var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
                    var barGraphRect = rect;

                    barGraphRect.width *= (float)((double)item.Size / totalSize);
                    EditorGUI.DrawRect(barGraphRect, new Color(0.28f, 0.37f, 0.51f, 0.6f));
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    
                    GUI.Label(rect , name + " - " + EditorUtility.FormatBytes((long)item.Size), EditorStyles.label);
                    GUI.Label(rect, ((double)item.Size / totalSize).ToString("P"), Styles.BuildReportStatsLabel);

                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
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
                        EditorGUI.LabelField(rect, EditorUtility.FormatBytes((long)buildReportItem.size), labelStyle);
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
            menu.AddItem(new GUIContent("Select in Assets"), false, SelectAssetsInProjectWindow);

            // Show the menu
            menu.ShowAsContext();

            // Function to replace clipboard contents
            void ReplaceClipboard(object input)
            {
                EditorGUIUtility.systemCopyBuffer = (string) input;
            }
        }
        
        /// <summary>
        /// Selects assets in the Project window based on the currently selected BuildReportItems.
        /// This is useful for quickly selecting a batch of assets to modify their import settings or other properties in bulk.
        /// </summary>
        private void SelectAssetsInProjectWindow()
        {
            // Retrieve the IDs of currently selected items
            var selectedItems = GetSelection();
            var assetPaths = new List<string>();

            // Iterate over each selected item and collect their asset paths
            foreach (var itemId in selectedItems)
            {
                var item = FindItem(itemId, rootItem) as BuildReportItem;
                if (item != null && !string.IsNullOrEmpty(item.path))
                {
                    assetPaths.Add(item.path);
                }
            }

            // Load and select the assets in the Project window
            var assets = assetPaths.Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>).ToArray();
            Selection.objects = assets;
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
