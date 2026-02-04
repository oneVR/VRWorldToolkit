using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace VRWorldToolkit.Editor
{

    public class TextureTreeViewItem : TreeViewItem
    {
        public Texture Texture { get; }
        public TextureImporter Importer { get; }
        public TextureImporterType TextureType { get; }
        public TextureImporterShape TextureShape { get; }
        public TextureImporterFormat Format { get; } 
        public int MaxTextureSize { get; }
        public bool CrunchedCompression { get; }
        public int CompressionQuality { get; }
        public TextureCompressionMode Compression { get; }
        public string AssetPath { get; }
        public string FileName { get; }
        public long StorageSize { get; }
        public int TextureWidth { get; }
        public int TextureHeight { get; }

        private readonly Dictionary<string, TextureImporterPlatformSettings> _platformSettings = new();

        public TextureTreeViewItem(int id, Texture texture, TextureImporter importer) : base(id, 0)
        {
            Texture = texture;
            Importer = importer;
            AssetPath = importer.assetPath;
            FileName = Path.GetFileName(AssetPath);
            StorageSize = EditorTextureUtil.GetStorageMemorySize(texture);
            Format = importer.GetDefaultPlatformTextureSettings().format;
            TextureWidth = texture.width;
            TextureHeight = texture.height;
            TextureType = importer.textureType;
            TextureShape = importer.textureShape;
            MaxTextureSize = importer.maxTextureSize;
            CrunchedCompression = importer.crunchedCompression;
            CompressionQuality = importer.compressionQuality;
            Compression = importer.textureCompression.ToTextureCompressionMode();
            displayName = FileName;
            CachePlatformSettings("Standalone");
            CachePlatformSettings("Android");
            CachePlatformSettings("iPhone");

            void CachePlatformSettings(string platform)
            {
                _platformSettings[platform] = Importer.GetPlatformTextureSettings(platform);
            }
        }

        public TextureImporterPlatformSettings GetPlatformSettings(string platform)
        {
            return _platformSettings.TryGetValue(platform, out var settings) ? settings : null;
        }

        public int GetPlatformMaxSize(string platform)
        {
            var settings = GetPlatformSettings(platform);
            return settings.overridden ? settings.maxTextureSize : MaxTextureSize;
        }

        public TextureImporterFormat GetPlatformFormat(string platform)
        {
            return GetPlatformSettings(platform).format;
        }

        public bool IsPlatformOverridden(string platform)
        {
            return GetPlatformSettings(platform).overridden;
        }
    }

    public class TextureTreeView : TreeView
    {
        public enum TreeColumns
        {
            Icon,
            StorageSize,
            Name,
            TextureSize,
            TextureType,
            TextureShape,
            MaxSize,
            Format,
            Compression,
            Crunched,
            CrunchQuality,
            MaxSizeWindows,
            FormatWindows,
            MaxSizeAndroid,
            FormatAndroid,
            MaxSizeiOS,
            FormatiOS,
        }

        private Dictionary<Texture, TextureImporter> _textures;
        private ImporterSettingsManager _settingsManager;
        private List<TreeViewItem> _rows = new();

        private TreeColumns _sortedColumn = TreeColumns.Name;
        private bool _sortAscending = true;

        private readonly MultiColumnHeader.HeaderCallback _visibleColumnsChangedHandler;

        public TextureTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, Dictionary<Texture, TextureImporter> textures, ImporterSettingsManager settingsManager) : base(state, multiColumnHeader)
        {
            _textures = textures;
            _settingsManager = settingsManager;

            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            _visibleColumnsChangedHandler = _ => Reload();

            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged += _visibleColumnsChangedHandler;

            Reload();
        }

        public void Cleanup()
        {
            multiColumnHeader.sortingChanged -= OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged -= _visibleColumnsChangedHandler;
        }

        public void SetTextures(Dictionary<Texture, TextureImporter> textures)
        {
            _textures = textures;
            Reload();
        }

        public void SetSettingsManager(ImporterSettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Reload();
        }

        public void RefreshItems()
        {
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            int id = 1;
            foreach (var texture in _textures)
            {
                if (texture.Value != null)
                {
                    var item = new TextureTreeViewItem(id++, texture.Key, texture.Value);
                    root.AddChild(item);
                }
            }

            if (!root.hasChildren)
            {
                root.children = new List<TreeViewItem>();
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            _rows.Clear();

            if (!root.hasChildren) return _rows;

            var filteredItems = root.children
                .Cast<TextureTreeViewItem>()
                .Where(item => _settingsManager.MatchesFilters(item.Importer));

            if (hasSearch)
            {
                filteredItems = filteredItems
                    .Where(item => DoesItemMatchSearch(item, searchString));
            }

            _rows.AddRange(filteredItems);

            _rows.Sort((a, b) =>
            {
                if (a is not TextureTreeViewItem itemA || b is not TextureTreeViewItem itemB) return 0;

                int result = _sortedColumn switch
                {
                    TreeColumns.StorageSize => itemA.StorageSize.CompareTo(itemB.StorageSize),
                    TreeColumns.Name => string.Compare(itemA.FileName, itemB.FileName, StringComparison.OrdinalIgnoreCase),
                    TreeColumns.TextureSize => (itemA.TextureWidth * itemA.TextureHeight).CompareTo(itemB.TextureWidth * itemB.TextureHeight),
                    TreeColumns.TextureType => itemA.TextureType.CompareTo(itemB.TextureType),
                    TreeColumns.TextureShape => itemA.TextureShape.CompareTo(itemB.TextureShape),
                    TreeColumns.MaxSize => itemA.MaxTextureSize.CompareTo(itemB.MaxTextureSize),
                    TreeColumns.Compression => itemA.Compression.CompareTo(itemB.Compression),
                    TreeColumns.Crunched => itemA.CrunchedCompression.CompareTo(itemB.CrunchedCompression),
                    TreeColumns.CrunchQuality => itemA.CompressionQuality.CompareTo(itemB.CompressionQuality),
                    TreeColumns.MaxSizeWindows => itemA.GetPlatformMaxSize("Standalone").CompareTo(itemB.GetPlatformMaxSize("Standalone")),
                    TreeColumns.FormatWindows => itemA.GetPlatformFormat("Standalone").CompareTo(itemB.GetPlatformFormat("Standalone")),
                    TreeColumns.MaxSizeAndroid => itemA.GetPlatformMaxSize("Android").CompareTo(itemB.GetPlatformMaxSize("Android")),
                    TreeColumns.FormatAndroid => itemA.GetPlatformFormat("Android").CompareTo(itemB.GetPlatformFormat("Android")),
                    TreeColumns.MaxSizeiOS => itemA.GetPlatformMaxSize("iPhone").CompareTo(itemB.GetPlatformMaxSize("iPhone")),
                    TreeColumns.FormatiOS => itemA.GetPlatformFormat("iPhone").CompareTo(itemB.GetPlatformFormat("iPhone")),
                    _ => 0
                };

                return _sortAscending ? result : -result;
            });

            return _rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as TextureTreeViewItem;
            if (item == null) return;

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                var rect = args.GetCellRect(i);
                var columnIndex = args.GetColumn(i);

                CenterRectUsingSingleLineHeight(ref rect);
                DrawCell(rect, item, (TreeColumns)columnIndex, args.selected);
            }
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item is not TextureTreeViewItem textureItem) return false;

            return textureItem.FileName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   textureItem.AssetPath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);

            if (FindItem(id, rootItem) is TextureTreeViewItem item)
            {
                EditorGUIUtility.PingObject(item.Texture);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            if (FindItem(id, rootItem) is TextureTreeViewItem item)
            {
                Selection.activeObject = item.Texture;
                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            }
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);

            if (FindItem(id, rootItem) is not TextureTreeViewItem item) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Name"), false, ReplaceClipboard, Path.GetFileName(item.AssetPath));
            menu.AddItem(new GUIContent("Copy Path"), false, ReplaceClipboard, item.AssetPath);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Reveal in Explorer"), false, () => EditorUtility.RevealInFinder(item.AssetPath));
            menu.AddItem(new GUIContent("Select in Assets"), false, SelectAssetsInProjectWindow);
            menu.ShowAsContext();

            void ReplaceClipboard(object input)
            {
                EditorGUIUtility.systemCopyBuffer = (string)input;
            }
        }

        /// <summary>
        /// Selects assets in the Project window based on the currently selected BuildReportItems.
        /// This is useful for quickly selecting a batch of assets to modify their import settings or other properties in bulk.
        /// Original PR for BuildReportTreeView by @akira0245, see here: https://github.com/oneVR/VRWorldToolkit/pull/26
        /// </summary>
        private void SelectAssetsInProjectWindow()
        {
            // Retrieve the IDs of currently selected items
            var selectedItems = GetSelection();
            var assetPaths = new List<string>();

            // Iterate over each selected item and collect their asset paths
            foreach (var itemId in selectedItems)
            {
                var item = FindItem(itemId, rootItem) as TextureTreeViewItem;
                if (item != null && !string.IsNullOrEmpty(item.AssetPath))
                {
                    assetPaths.Add(item.AssetPath);
                }
            }

            // Load and select the assets in the Project window
            var assets = assetPaths.Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>).ToArray();
            Selection.objects = assets;

            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        private void DrawCell(Rect rect, TextureTreeViewItem item, TreeColumns column, bool selected)
        {
            var labelStyle = selected ? Styles.TreeViewLabelSelected : Styles.TreeViewLabel;
            var labelStyleRight = selected ? Styles.TreeViewLabelSelectedRight : Styles.TreeViewLabelRight;

            switch (column)
            {
                case TreeColumns.Icon:
                    var iconRect = new Rect(rect.x + 4, rect.y + 1, 16, 16);
                    var preview = AssetPreview.GetMiniThumbnail(item.Texture);
                    if (preview != null)
                    {
                        GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
                    }
                    break;

                case TreeColumns.Name:
                    GUI.Label(rect, item.FileName, labelStyle);
                    break;

                case TreeColumns.TextureSize:
                    GUI.Label(rect, $"{item.TextureWidth}x{item.TextureHeight}", labelStyleRight);
                    break;

                case TreeColumns.TextureType:
                    GUI.Label(rect, item.TextureType.GetDisplayName(), labelStyle);
                    break;

                case TreeColumns.TextureShape:
                    GUI.Label(rect, item.TextureShape.GetDisplayName(), labelStyle);
                    break;

                case TreeColumns.MaxSize:
                    DrawMaxSizeCell(rect, item, labelStyleRight);
                    break;

                case TreeColumns.Format:
                    // Using ToString here returns CompressedAutomatic instead of Automatic so manually overriding it here
                    if (item.Format == TextureImporterFormat.Automatic)
                    {
                        GUI.Label(rect, "Automatic", labelStyle);
                    }
                    else
                    {
                        GUI.Label(rect, item.Format.ToString(), labelStyle);
                    }
                    break;

                case TreeColumns.Compression:
                    DrawCompression(rect, item, labelStyle);
                    break;

                case TreeColumns.Crunched:
                    DrawCrunchedCell(rect, item);
                    break;

                case TreeColumns.CrunchQuality:
                    DrawCrunchQualityCell(rect, item, labelStyleRight);
                    break;

                case TreeColumns.StorageSize:
                    GUI.Label(rect, EditorUtility.FormatBytes(item.StorageSize), labelStyleRight);
                    break;

                case TreeColumns.MaxSizeWindows:
                    DrawPlatformMaxSize(rect, item, "Standalone", labelStyleRight);
                    break;

                case TreeColumns.FormatWindows:
                    DrawPlatformFormat(rect, item, "Standalone", labelStyle);
                    break;

                case TreeColumns.MaxSizeAndroid:
                    DrawPlatformMaxSize(rect, item, "Android", labelStyleRight);
                    break;

                case TreeColumns.FormatAndroid:
                    DrawPlatformFormat(rect, item, "Android", labelStyle);
                    break;

                case TreeColumns.MaxSizeiOS:
                    DrawPlatformMaxSize(rect, item, "iPhone", labelStyleRight);
                    break;

                case TreeColumns.FormatiOS:
                    DrawPlatformFormat(rect, item, "iPhone", labelStyle);
                    break;
            }
        }

        private void DrawCompression(Rect rect, TextureTreeViewItem item, GUIStyle style)
        {
            var currentValue = item.Compression;
            string currentText = ObjectNames.NicifyVariableName(currentValue.ToString());

            if (_settingsManager.DefaultSettings.ChangeCompression)
            {
                if (_settingsManager.DefaultSettings.IgnoreUncompressed && currentValue == TextureCompressionMode.None)
                {
                    GUI.Label(rect, currentText, style);
                    return;
                }

                var newValue = _settingsManager.DefaultSettings.Compression;

                if (currentValue != newValue)
                {
                    string tooltip = $"Current: {currentText}";
                    string text = ObjectNames.NicifyVariableName(newValue.ToString());

                    if (MemorySize(newValue) < MemorySize(currentValue))
                    {
                        GUI.Label(rect, new GUIContent($"↓ {text}", tooltip), Styles.TreeViewLabelPositive);
                    }
                    else
                    {
                        GUI.Label(rect, new GUIContent($"↑ {text}", tooltip), Styles.TreeViewLabelNegative);
                    }
                    return;

                    int MemorySize(TextureCompressionMode mode) => mode switch
                    {
                        TextureCompressionMode.LowQuality => 1,
                        TextureCompressionMode.NormalQuality => 2,
                        TextureCompressionMode.HighQuality => 3,
                        TextureCompressionMode.None => 4,
                        _ => 2
                    };
                }
            }

            GUI.Label(rect, currentText, style);
        }

        private void DrawCrunchedCell(Rect rect, TextureTreeViewItem item)
        {
            var hasCompression = _settingsManager.DefaultSettings.ChangeCompression ? _settingsManager.DefaultSettings.Compression : item.Compression;

            if (hasCompression == TextureCompressionMode.None || _settingsManager.DefaultSettings.IgnoreUncompressed && item.Compression == TextureCompressionMode.None)
            {
                GUI.Label(rect, "―", Styles.TreeViewLabelCenter);
                return;
            }

            bool currentValue = item.CrunchedCompression;

            if (_settingsManager.DefaultSettings.ChangeCrunch)
            {
                bool skip = _settingsManager.DefaultSettings.SkipCrunchWhen switch
                {
                    DontOverrideWhen.AlreadyDisabled => !currentValue,
                    DontOverrideWhen.AlreadyEnabled => currentValue,
                    _ => false
                };

                if (!skip)
                {
                    bool newValue = _settingsManager.DefaultSettings.UseCrunch;

                    if (currentValue != newValue)
                    {
                        string tooltip = $"Current: {(currentValue ? "Yes" : "No")}";
                        string text = newValue ? "✓" : "―";
                        var style = newValue ? Styles.TreeViewLabelPositiveCenter : Styles.TreeViewLabelNegativeCenter;

                        GUI.Label(rect, new GUIContent(text, tooltip), style);
                        return;
                    }
                }
            }

            GUI.Label(rect, currentValue ? "✓" : "―", Styles.TreeViewLabelCenter);
        }

        private void DrawCrunchQualityCell(Rect rect, TextureTreeViewItem item, GUIStyle style)
        {
            var hasCompression = _settingsManager.DefaultSettings.ChangeCompression ? _settingsManager.DefaultSettings.Compression : item.Compression;

            if (hasCompression == TextureCompressionMode.None || (_settingsManager.DefaultSettings.IgnoreUncompressed && item.Compression == TextureCompressionMode.None))
            {
                GUI.Label(rect, "―", style);
                return;
            }

            bool currentCrunch = item.CrunchedCompression;
            int currentQuality = item.CompressionQuality;

            bool effectiveCrunch = currentCrunch;

            if (_settingsManager.DefaultSettings.ChangeCrunch)
            {
                bool skip = _settingsManager.DefaultSettings.SkipCrunchWhen switch
                {
                    DontOverrideWhen.AlreadyDisabled => !currentCrunch,
                    DontOverrideWhen.AlreadyEnabled => currentCrunch,
                    _ => false
                };

                if (!skip)
                {
                    effectiveCrunch = _settingsManager.DefaultSettings.UseCrunch;
                }
            }

            if (!effectiveCrunch)
            {
                GUI.Label(rect, "―", style);
                return;
            }

            int displayQuality = currentQuality;
            bool changes = false;

            if (_settingsManager.DefaultSettings.ChangeCrunch)
            {
                bool skip = _settingsManager.DefaultSettings.SkipCrunchWhen switch
                {
                    DontOverrideWhen.AlreadyDisabled => !currentCrunch,
                    DontOverrideWhen.AlreadyEnabled => currentCrunch,
                    _ => false
                };

                if (!skip && _settingsManager.DefaultSettings.UseCrunch)
                {
                    int newQuality = _settingsManager.DefaultSettings.CrunchQuality;

                    changes = _settingsManager.DefaultSettings.CrunchQualityCondition switch
                    {
                        OverrideCondition.Always => currentQuality != newQuality,
                        OverrideCondition.Smaller => currentQuality < newQuality,
                        OverrideCondition.Bigger => currentQuality > newQuality,
                        _ => false
                    };

                    if (changes)
                    {
                        displayQuality = newQuality;
                    }
                }
            }

            if (changes)
            {
                string tooltip = $"Current: {currentQuality}";

                if (displayQuality > currentQuality)
                {
                    GUI.Label(rect, new GUIContent($"↑ {displayQuality}", tooltip), Styles.TreeViewLabelNegativeRight);
                }
                else
                {
                    GUI.Label(rect, new GUIContent($"↓ {displayQuality}", tooltip), Styles.TreeViewLabelPositiveRight);
                }
                return;
            }

            GUI.Label(rect, currentQuality.ToString(), style);
        }

        private void DrawMaxSizeCell(Rect rect, TextureTreeViewItem item, GUIStyle style)
        {
            int currentSize = item.MaxTextureSize;
            string text = currentSize.ToString();

            if (_settingsManager.DefaultSettings.ChangeMaxSize)
            {
                int newSize = _settingsManager.MaxTextureSize;

                bool changes = _settingsManager.DefaultSettings.MaxSizeCondition switch
                {
                    OverrideCondition.Always => currentSize != newSize,
                    OverrideCondition.Smaller => currentSize < newSize,
                    OverrideCondition.Bigger => currentSize > newSize,
                    _ => false
                };

                if (changes)
                {
                    var tooltip = $"Current: {currentSize}";

                    if (newSize < currentSize)
                    {
                        text = $"↓ {newSize}";
                        GUI.Label(rect, new GUIContent(text, tooltip), Styles.TreeViewLabelPositiveRight);
                    }
                    else
                    {
                        text = $"↑ {newSize}";
                        GUI.Label(rect, new GUIContent(text, tooltip), Styles.TreeViewLabelNegativeRight);
                    }
                    return;
                }
            }

            GUI.Label(rect, text, style);
        }

        private void DrawPlatformMaxSize(Rect rect, TextureTreeViewItem item, string platform, GUIStyle style)
        {
            int currentSize = item.GetPlatformMaxSize(platform);
            var platformSettings = _settingsManager.GetPlatformSettings(platform);
            bool isOverridden = item.IsPlatformOverridden(platform) || platformSettings.Enabled;

            if (platformSettings.Enabled)
            {
                int newSize = platformSettings.MaxTextureSize;

                bool changes = platformSettings.MaxSizeCondition switch
                {
                    OverrideCondition.Always => currentSize != newSize,
                    OverrideCondition.Smaller => currentSize < newSize,
                    OverrideCondition.Bigger => currentSize > newSize,
                    _ => false
                };

                if (changes)
                {
                    string tooltip = $"Current: {currentSize}";

                    if (newSize < currentSize)
                    {
                        GUI.Label(rect, new GUIContent($"↓ {newSize}", tooltip), Styles.TreeViewLabelPositiveRight);
                    }
                    else
                    {
                        GUI.Label(rect, new GUIContent($"↑ {newSize}", tooltip), Styles.TreeViewLabelNegativeRight);
                    }
                    return;
                }
            }

            string displayText = isOverridden ? currentSize.ToString() : $"({currentSize})";
            GUI.Label(rect, displayText, isOverridden ? style : Styles.TreeViewLabelRightDimmed);
        }

        private void DrawPlatformFormat(Rect rect, TextureTreeViewItem item, string platform, GUIStyle style)
        {
            var platformSettings = _settingsManager.GetPlatformSettings(platform);
            if (platformSettings.Enabled)
            {
                GUI.Label(rect, ((TextureImporterFormat)platformSettings.Format).ToString(), Styles.TreeViewLabelPositive);
                return;
            }
            if (!item.IsPlatformOverridden(platform))
            {
                GUI.Label(rect, "(Automatic)", Styles.TreeViewLabelDimmed);
                return;
            }
            GUI.Label(rect, item.GetPlatformFormat(platform).ToString(), style);
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            if (header.sortedColumnIndex < 0)
                return;

            _sortedColumn = (TreeColumns)header.sortedColumnIndex;
            _sortAscending = header.IsSortedAscending(header.sortedColumnIndex);
            Reload();
        }

        private const int WidthSize = 60;
        private const int WidthFormat = 85;
        private const int MaxWidthFormat = 85;
        private const int MinWidthFormat = 60;

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.IconContent("Texture Icon"),
                    contextMenuText = "Preview",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 25,
                    minWidth = 25,
                    maxWidth = 25,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Storage Size", "Storage size on disk"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 80,
                    minWidth = 80,
                    maxWidth = 80,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 80,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Original Size", "Original texture dimensions"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 80,
                    minWidth = 60,
                    maxWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 80,
                    minWidth = 60,
                    maxWidth = 200,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Shape"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 80,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Max Size"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthSize,
                    minWidth = WidthSize,
                    maxWidth = WidthSize,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Format", "Format Default"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = WidthFormat,
                    minWidth = MinWidthFormat,
                    maxWidth = MaxWidthFormat,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Compression"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 100,
                    minWidth = 80,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Crunched"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 70,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Quality"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 50,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Windows"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthSize,
                    minWidth = WidthSize,
                    maxWidth = WidthSize,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Format", "Format Windows"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = WidthFormat,
                    minWidth = MinWidthFormat,
                    maxWidth = MaxWidthFormat,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Android"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthSize,
                    minWidth = WidthSize,
                    maxWidth = WidthSize,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Format", "Format Android"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthFormat,
                    minWidth = MinWidthFormat,
                    maxWidth = MaxWidthFormat,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("iOS"),
                    headerTextAlignment = TextAlignment.Right,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthSize,
                    minWidth = WidthSize,
                    maxWidth = WidthSize,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Format", "Format iOS"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = WidthFormat,
                    minWidth = MinWidthFormat,
                    maxWidth = MaxWidthFormat,
                    autoResize = false,
                    allowToggleVisibility = true
                },
            };

            var state = new MultiColumnHeaderState(columns);

            // var state = new MultiColumnHeaderState(columns)
            // {
            //     visibleColumns = new[]
            //     {
            //         (int)TreeColumns.Icon,
            //         (int)TreeColumns.StorageSize,
            //         (int)TreeColumns.Name,
            //         (int)TreeColumns.TextureSize,
            //         (int)TreeColumns.MaxSize,
            //         (int)TreeColumns.Compression,
            //         (int)TreeColumns.Crunched,
            //         (int)TreeColumns.MaxSizeWindows,
            //         (int)TreeColumns.MaxSizeAndroid,
            //         (int)TreeColumns.MaxSizeiOS,
            //     }
            // };

            return state;
        }
    }
}
