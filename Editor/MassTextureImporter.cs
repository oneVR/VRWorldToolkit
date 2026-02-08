using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public enum OverrideCondition
    {
        Always,
        Smaller,
        Bigger
    }

    public enum DontOverrideWhen
    {
        Never,
        AlreadyDisabled,
        AlreadyEnabled
    }

    [Flags]
    public enum TextureTypeFilter
    {
        [InspectorName("Default")] Default = 1 << 0,
        [InspectorName("Normal map")] NormalMap = 1 << 1,
        [InspectorName("Editor GUI and Legacy GUI")] GUI = 1 << 2,
        [InspectorName("Cookie")] Cookie = 1 << 4,
        [InspectorName("Lightmap")] Lightmap = 1 << 6,
        [InspectorName("Cursor")] Cursor = 1 << 7,
        [InspectorName("Sprite (2D and UI)")] Sprite = 1 << 8,
        [InspectorName("Single Channel")] SingleChannel = 1 << 10,
        [InspectorName("Shadowmask")] Shadowmask = 1 << 11,
        [InspectorName("Directional Lightmap")] DirectionalLightmap = 1 << 12,
    }

    [Flags]
    public enum TextureShapeFilter
    {
        [InspectorName("2D")] Texture2D = 1 << 1,
        [InspectorName("Cube")] TextureCube = 1 << 2,
        [InspectorName("2D Array")] Texture2DArray = 1 << 3,
        [InspectorName("3D")] Texture3D = 1 << 4,
    }

    public enum TextureCompressionMode
    {
        None = 0,
        [InspectorName("Low Quality")] LowQuality = 1,
        [InspectorName("Normal Quality")] NormalQuality = 2,
        [InspectorName("High Quality")] HighQuality = 3
    }

    public static class MassTextureImporterExtensions
    {
        public static TextureCompressionMode ToTextureCompressionMode(this TextureImporterCompression compression)
        {
            return compression switch
            {
                TextureImporterCompression.Uncompressed => TextureCompressionMode.None,
                TextureImporterCompression.CompressedLQ => TextureCompressionMode.LowQuality,
                TextureImporterCompression.Compressed => TextureCompressionMode.NormalQuality,
                TextureImporterCompression.CompressedHQ => TextureCompressionMode.HighQuality,
                _ => TextureCompressionMode.NormalQuality
            };
        }

        public static TextureImporterCompression ToTextureImporterCompression(this TextureCompressionMode mode)
        {
            return mode switch
            {
                TextureCompressionMode.None => TextureImporterCompression.Uncompressed,
                TextureCompressionMode.LowQuality => TextureImporterCompression.CompressedLQ,
                TextureCompressionMode.NormalQuality => TextureImporterCompression.Compressed,
                TextureCompressionMode.HighQuality => TextureImporterCompression.CompressedHQ,
                _ => TextureImporterCompression.Compressed
            };
        }

        public static string GetDisplayName(this TextureImporterType type)
        {
            return type switch
            {
                TextureImporterType.Default => "Default",
                TextureImporterType.NormalMap => "Normal map",
                TextureImporterType.GUI => "Editor GUI and Legacy GUI",
                TextureImporterType.Cookie => "Cookie",
                TextureImporterType.Lightmap => "Lightmap",
                TextureImporterType.Cursor => "Cursor",
                TextureImporterType.Sprite => "Sprite (2D and UI)",
                TextureImporterType.SingleChannel => "Single Channel",
                TextureImporterType.Shadowmask => "Shadowmask",
                TextureImporterType.DirectionalLightmap => "Directional Lightmap",
                _ => type.ToString()
            };
        }

        public static string GetDisplayName(this TextureImporterShape shape)
        {
            return shape switch
            {
                TextureImporterShape.Texture2D => "2D",
                TextureImporterShape.TextureCube => "Cube",
                TextureImporterShape.Texture2DArray => "2D Array",
                TextureImporterShape.Texture3D => "3D",
                _ => shape.ToString()
            };
        }
    }

    public abstract class PlatformConfig
    {
        public abstract string DisplayName { get; }
        public abstract string PlatformKey { get; }
        public abstract string IconName { get; }
        public abstract int DefaultFormat { get; }
        public abstract int DrawFormatSelector(int currentFormat);
        public abstract bool SupportsCrunch(int format);
        public abstract bool SupportsCompressorQuality(int format);
    }

    public class StandalonePlatformConfig : PlatformConfig
    {
        public override string DisplayName => "Windows, Mac, Linux";
        public override string PlatformKey => "Standalone";
        public override string IconName => "BuildSettings.Standalone";
        public override int DefaultFormat => (int)TextureImporterFormat.DXT5Crunched;

        public override int DrawFormatSelector(int currentFormat)
            => Selectors.WindowsFormatIntPopup(currentFormat);

        public override bool SupportsCrunch(int format)
            => format == (int)TextureImporterFormat.DXT1Crunched ||
                format == (int)TextureImporterFormat.DXT5Crunched;

        public override bool SupportsCompressorQuality(int format)
            => format == (int)TextureImporterFormat.BC7 ||
                format == (int)TextureImporterFormat.BC6H;
    }

    public class AndroidPlatformConfig : PlatformConfig
    {
        public override string DisplayName => "Android";
        public override string PlatformKey => "Android";
        public override string IconName => "BuildSettings.Android";
        public override int DefaultFormat => (int)TextureImporterFormat.ASTC_6x6;

        public override int DrawFormatSelector(int currentFormat)
            => Selectors.MobileFormatIntPopup(currentFormat);

        public override bool SupportsCrunch(int format)
            => format == (int)TextureImporterFormat.ETC_RGB4Crunched ||
                format == (int)TextureImporterFormat.ETC2_RGBA8Crunched;

        public override bool SupportsCompressorQuality(int format)
            => format == (int)TextureImporterFormat.ASTC_12x12 ||
                format == (int)TextureImporterFormat.ASTC_10x10 ||
                format == (int)TextureImporterFormat.ASTC_8x8 ||
                format == (int)TextureImporterFormat.ASTC_6x6 ||
                format == (int)TextureImporterFormat.ASTC_5x5 ||
                format == (int)TextureImporterFormat.ASTC_4x4 ||
                format == (int)TextureImporterFormat.ETC2_RGBA8 ||
                format == (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA ||
                format == (int)TextureImporterFormat.ETC2_RGB4;
    }

    public class iOSPlatformConfig : PlatformConfig
    {
        public override string DisplayName => "iOS";
        public override string PlatformKey => "iPhone";
        public override string IconName => "BuildSettings.iPhone";
        public override int DefaultFormat => (int)TextureImporterFormat.ASTC_6x6;

        public override int DrawFormatSelector(int currentFormat)
            => Selectors.MobileFormatIntPopup(currentFormat);

        public override bool SupportsCrunch(int format)
            => format == (int)TextureImporterFormat.ETC_RGB4Crunched ||
                format == (int)TextureImporterFormat.ETC2_RGBA8Crunched;

        public override bool SupportsCompressorQuality(int format)
            => format == (int)TextureImporterFormat.ASTC_12x12 ||
                format == (int)TextureImporterFormat.ASTC_10x10 ||
                format == (int)TextureImporterFormat.ASTC_8x8 ||
                format == (int)TextureImporterFormat.ASTC_6x6 ||
                format == (int)TextureImporterFormat.ASTC_5x5 ||
                format == (int)TextureImporterFormat.ASTC_4x4 ||
                format == (int)TextureImporterFormat.ETC2_RGBA8 ||
                format == (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA ||
                format == (int)TextureImporterFormat.ETC2_RGB4;
    }

    public class PlatformTabManager
    {
        private int _selectedIndex;
        private readonly List<Tab> _tabs = new();
        private bool _iconsInitialized;

        private struct Tab
        {
            public string Label;
            public string IconName;
            public GUIContent Icon;
            public Action DrawContent;
        }

        public int SelectedIndex => _selectedIndex;

        public void AddTab(string label, string iconName, Action drawContent)
        {
            _tabs.Add(new Tab
            {
                Label = label,
                IconName = iconName,
                Icon = null,
                DrawContent = drawContent
            });
        }

        public void AddDefaultTab(Action drawContent)
        {
            _tabs.Insert(0, new Tab
            {
                Label = "Default",
                IconName = null,
                Icon = null,
                DrawContent = drawContent
            });
        }

        private void InitializeIconsIfNeeded()
        {
            if (_iconsInitialized) return;

            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];

                if (string.IsNullOrEmpty(tab.IconName))
                {
                    tab.Icon = new GUIContent(tab.Label);
                }
                else
                {
                    var iconContent = EditorGUIUtility.IconContent(tab.IconName + ".Small");
                    tab.Icon = iconContent?.image != null
                        ? new GUIContent(iconContent.image, tab.Label)
                        : new GUIContent(tab.Label);
                }

                _tabs[i] = tab;
            }

            _iconsInitialized = true;
        }

        public void DrawTabs(Func<int, bool> isHighlighted = null)
        {
            InitializeIconsIfNeeded();

            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _tabs.Count; i++)
            {
                DrawTab(i, isHighlighted?.Invoke(i) ?? false);
            }

            EditorGUILayout.EndHorizontal();

            _tabs[_selectedIndex].DrawContent?.Invoke();
        }

        private void DrawTab(int index, bool highlight)
        {
            bool isSelected = _selectedIndex == index;

            if (isSelected)
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);

            if (GUILayout.Toggle(isSelected, _tabs[index].Icon, Styles.PlatformSelector))
                _selectedIndex = index;

            if (highlight)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.width = 3;
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.6f, 1f, 1f));
            }

            GUI.backgroundColor = Color.white;
        }
    }

    [Serializable]
    public class PlatformOverrideSettings
    {
        public bool Enabled;
        public int MaxTextureSize = 2048;
        public OverrideCondition MaxSizeCondition = OverrideCondition.Bigger;
        public int Format;
        public bool UseCrunchCompression = true;
        public int CrunchQuality = 80;
        public TextureCompressionQuality TextureCompressionQuality = TextureCompressionQuality.Normal;

        private readonly PlatformConfig _config;

        public PlatformConfig Config => _config;
        public string DisplayName => _config.DisplayName;
        public string PlatformKey => _config.PlatformKey;
        public bool IsCrunchedFormat => _config.SupportsCrunch(Format);
        public bool HasCompressionQuality => _config.SupportsCompressorQuality(Format);

        public PlatformOverrideSettings(PlatformConfig config)
        {
            _config = config;
            Format = config.DefaultFormat;
        }

        public void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Enabled = EditorGUILayout.ToggleLeft($"Override for {DisplayName}", Enabled);

                using (new EditorGUI.DisabledScope(!Enabled))
                using (new EditorGUI.IndentLevelScope())
                {
                    MaxTextureSize = Selectors.MaxSizeIntPopup(MaxTextureSize);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        MaxSizeCondition = (OverrideCondition)EditorGUILayout.EnumPopup("Override When", MaxSizeCondition);
                    }

                    Format = _config.DrawFormatSelector(Format);

                    if (IsCrunchedFormat)
                    {
                        UseCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", UseCrunchCompression);
                        CrunchQuality = EditorGUILayout.IntSlider("Compressor Quality", CrunchQuality, 1, 100);
                    }

                    if (HasCompressionQuality)
                    {
                        TextureCompressionQuality = (TextureCompressionQuality)EditorGUILayout.EnumPopup("Compressor Quality", TextureCompressionQuality);
                    }
                }
            }
        }

        public bool ApplyTo(TextureImporter importer)
        {
            if (!Enabled) return false;

            var settings = importer.GetPlatformTextureSettings(PlatformKey) ?? new TextureImporterPlatformSettings();
            bool changed = false;

            if (!settings.overridden)
            {
                settings.overridden = true;
                changed = true;
            }

            settings.name = PlatformKey;

            if (settings.format != (TextureImporterFormat)Format)
            {
                settings.format = (TextureImporterFormat)Format;
                changed = true;
            }

            int newMaxSize = MaxSizeCondition switch
            {
                OverrideCondition.Always => MaxTextureSize,
                OverrideCondition.Smaller when settings.maxTextureSize < MaxTextureSize => MaxTextureSize,
                OverrideCondition.Bigger when settings.maxTextureSize > MaxTextureSize => MaxTextureSize,
                _ => settings.maxTextureSize
            };

            if (settings.maxTextureSize != newMaxSize)
            {
                settings.maxTextureSize = newMaxSize;
                changed = true;
            }

            if (IsCrunchedFormat)
            {
                if (settings.crunchedCompression != UseCrunchCompression)
                {
                    settings.crunchedCompression = UseCrunchCompression;
                    changed = true;
                }

                if (UseCrunchCompression && settings.compressionQuality != CrunchQuality)
                {
                    settings.compressionQuality = CrunchQuality;
                    changed = true;
                }
            }

            if (HasCompressionQuality && settings.compressionQuality != (int)TextureCompressionQuality)
            {
                settings.compressionQuality = (int)TextureCompressionQuality;
                changed = true;
            }

            if (changed)
            {
                importer.SetPlatformTextureSettings(settings);
            }

            return changed;
        }
    }

    [Serializable]
    public class DefaultPlatformSettings
    {
        public bool ChangeMipMaps = true;
        public bool GenerateMipMaps = true;
        public bool StreamingMipMaps = true;

        public bool ChangeAniso;
        public int AnisoLevel = 1;
        public OverrideCondition AnisoCondition = OverrideCondition.Always;

        public bool ChangeMaxSize = true;
        public int MaxTextureSize = 2048;
        public OverrideCondition MaxSizeCondition = OverrideCondition.Bigger;

        public bool ChangeCompression = true;
        public TextureCompressionMode Compression = TextureCompressionMode.NormalQuality;
        public bool IgnoreUncompressed = true;

        public bool ChangeCrunch = true;
        public bool UseCrunch = true;
        public int CrunchQuality = 80;
        public OverrideCondition CrunchQualityCondition = OverrideCondition.Bigger;
        public DontOverrideWhen SkipCrunchWhen = DontOverrideWhen.AlreadyEnabled;

        public void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                ChangeMaxSize = EditorGUILayout.Toggle("Change Max Size", ChangeMaxSize);
                using (new EditorGUI.DisabledScope(!ChangeMaxSize))
                using (new EditorGUI.IndentLevelScope())
                {
                    MaxTextureSize = Selectors.MaxSizeIntPopup(MaxTextureSize);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        MaxSizeCondition = (OverrideCondition)EditorGUILayout.EnumPopup("Override When", MaxSizeCondition);
                    }
                }

                EditorGUILayout.Space();

                ChangeCompression = EditorGUILayout.Toggle("Change Compression", ChangeCompression);
                using (new EditorGUI.DisabledScope(!ChangeCompression))
                using (new EditorGUI.IndentLevelScope())
                {
                    Compression = (TextureCompressionMode)EditorGUILayout.EnumPopup("Compression", Compression);
                    IgnoreUncompressed = EditorGUILayout.Toggle("Ignore Uncompressed", IgnoreUncompressed);
                }

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(Compression == TextureCompressionMode.None))
                {
                    ChangeCrunch = EditorGUILayout.Toggle("Change Crunch Compression", ChangeCrunch);
                    using (new EditorGUI.DisabledScope(!ChangeCrunch))
                    using (new EditorGUI.IndentLevelScope())
                    {
                        UseCrunch = EditorGUILayout.Toggle("Use Crunch Compression", UseCrunch);
                        CrunchQuality = EditorGUILayout.IntSlider("Quality", CrunchQuality, 1, 100);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            CrunchQualityCondition = (OverrideCondition)EditorGUILayout.EnumPopup("Override When", CrunchQualityCondition);
                        }
                        SkipCrunchWhen = (DontOverrideWhen)EditorGUILayout.EnumPopup("Don't Override When", SkipCrunchWhen);
                    }
                }
            }
        }

        public bool ApplyTo(TextureImporter importer)
        {
            bool changed = false;

            if (ChangeMipMaps)
            {
                if (importer.mipmapEnabled != GenerateMipMaps)
                {
                    importer.mipmapEnabled = GenerateMipMaps;
                    changed = true;
                }

                if (GenerateMipMaps && importer.streamingMipmaps != StreamingMipMaps)
                {
                    importer.streamingMipmaps = StreamingMipMaps;
                    changed = true;
                }
            }

            if (ChangeAniso)
            {
                int newAniso = AnisoCondition switch
                {
                    OverrideCondition.Always => AnisoLevel,
                    OverrideCondition.Smaller when importer.anisoLevel < AnisoLevel => AnisoLevel,
                    OverrideCondition.Bigger when importer.anisoLevel > AnisoLevel => AnisoLevel,
                    _ => importer.anisoLevel
                };

                if (importer.anisoLevel != newAniso)
                {
                    importer.anisoLevel = newAniso;
                    changed = true;
                }
            }

            if (ChangeMaxSize)
            {
                int newMaxSize = MaxSizeCondition switch
                {
                    OverrideCondition.Always => MaxTextureSize,
                    OverrideCondition.Smaller when importer.maxTextureSize < MaxTextureSize => MaxTextureSize,
                    OverrideCondition.Bigger when importer.maxTextureSize > MaxTextureSize => MaxTextureSize,
                    _ => importer.maxTextureSize
                };

                if (importer.maxTextureSize != newMaxSize)
                {
                    importer.maxTextureSize = newMaxSize;
                    changed = true;
                }
            }

            if (ChangeCompression)
            {
                if (!(IgnoreUncompressed && importer.textureCompression == TextureImporterCompression.Uncompressed))
                {
                    var newCompression = Compression.ToTextureImporterCompression();
                    if (importer.textureCompression != newCompression)
                    {
                        importer.textureCompression = newCompression;
                        changed = true;
                    }
                }
            }

            if (ChangeCrunch && importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                bool shouldSkip = SkipCrunchWhen switch
                {
                    DontOverrideWhen.AlreadyDisabled => !importer.crunchedCompression,
                    DontOverrideWhen.AlreadyEnabled => importer.crunchedCompression,
                    _ => false
                };

                if (!shouldSkip)
                {
                    if (importer.crunchedCompression != UseCrunch)
                    {
                        importer.crunchedCompression = UseCrunch;
                        changed = true;
                    }

                    if (UseCrunch)
                    {
                        int targetQuality = CrunchQualityCondition switch
                        {
                            OverrideCondition.Always => CrunchQuality,
                            OverrideCondition.Smaller when importer.compressionQuality < CrunchQuality => CrunchQuality,
                            OverrideCondition.Bigger when importer.compressionQuality > CrunchQuality => CrunchQuality,
                            _ => importer.compressionQuality
                        };

                        if (importer.compressionQuality != targetQuality)
                        {
                            importer.compressionQuality = targetQuality;
                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }
    }

    public class TextureDetails
    {
        public readonly Dictionary<Texture, TextureImporter> TextureList = new();

        public IEnumerable<TextureImporter> GetImporters => TextureList.Values;

        public void AddTexture(TextureImporter textureImporter, Texture texture)
        {
            if (texture != null && textureImporter != null && !TextureList.ContainsKey(texture))
            {
                TextureList.Add(texture, textureImporter);
            }
        }
    }

    public class ImporterSettingsManager
    {
        public TextureTypeFilter TypeFilter = TextureTypeFilter.Default;
        public TextureShapeFilter ShapeFilter = TextureShapeFilter.Texture2D;

        private string _nameFilter;
        private Regex _nameRegex;
        private bool _nameRegexValid = true;

        private string _pathFilter;
        private Regex _pathRegex;
        private bool _pathRegexValid = true;

        public DefaultPlatformSettings DefaultSettings { get; } = new();

        private readonly Dictionary<string, PlatformOverrideSettings> _platformSettings;
        private readonly PlatformTabManager _tabController;

        public int MaxTextureSize => DefaultSettings.MaxTextureSize;
        public OverrideCondition OverrideMaxTextureSizeWhen => DefaultSettings.MaxSizeCondition;

        public ImporterSettingsManager()
        {
            _platformSettings = new Dictionary<string, PlatformOverrideSettings>
            {
                ["Standalone"] = new PlatformOverrideSettings(new StandalonePlatformConfig()),
                ["Android"] = new PlatformOverrideSettings(new AndroidPlatformConfig()),
                ["iPhone"] = new PlatformOverrideSettings(new iOSPlatformConfig())
            };

            _tabController = new PlatformTabManager();
            _tabController.AddDefaultTab(() => DefaultSettings.DrawSettings());

            foreach (var item in _platformSettings)
            {
                var settings = item.Value;
                _tabController.AddTab(
                    settings.DisplayName,
                    settings.Config.IconName,
                    () => settings.DrawSettings()
                );
            }
        }

        public PlatformOverrideSettings GetPlatformSettings(string platformKey)
            => _platformSettings.TryGetValue(platformKey, out var settings) ? settings : null;

        public static TextureTypeFilter ToTypeFilter(TextureImporterType type)
            => (TextureTypeFilter)(1 << (int)type);

        public static TextureShapeFilter ToShapeFilter(TextureImporterShape shape)
            => (TextureShapeFilter)(1 << (int)shape);

        public bool MatchesFilters(TextureImporter importer)
        {
            if (!string.IsNullOrEmpty(_nameFilter) && _nameRegex != null)
            {
                if (!_nameRegex.IsMatch(Path.GetFileName(importer.assetPath)))
                    return false;
            }

            if (!string.IsNullOrEmpty(_pathFilter) && _pathRegex != null)
            {
                if (!_pathRegex.IsMatch(importer.assetPath))
                    return false;
            }

            if ((TypeFilter & ToTypeFilter(importer.textureType)) == 0)
                return false;

            if ((ShapeFilter & ToShapeFilter(importer.textureShape)) == 0)
                return false;

            return true;
        }

        private void CompileNameRegex()
        {
            if (string.IsNullOrEmpty(_nameFilter))
            {
                _nameRegex = null;
                _nameRegexValid = true;
                return;
            }

            try
            {
                _nameRegex = new Regex(_nameFilter, RegexOptions.IgnoreCase);
                _nameRegexValid = true;
            }
            catch (ArgumentException)
            {
                _nameRegex = null;
                _nameRegexValid = false;
            }
        }

        private void CompilePathRegex()
        {
            if (string.IsNullOrEmpty(_pathFilter))
            {
                _pathRegex = null;
                _pathRegexValid = true;
                return;
            }

            try
            {
                _pathRegex = new Regex(_pathFilter, RegexOptions.IgnoreCase);
                _pathRegexValid = true;
            }
            catch (ArgumentException)
            {
                _pathRegex = null;
                _pathRegexValid = false;
            }
        }

        public void DrawSettings()
        {
            EditorGUIUtility.labelWidth = 150f;

            DrawFilters();
            EditorGUILayout.Space();
            DrawMipMapSettings();
            EditorGUILayout.Space();
            DrawAnisoSettings();
            EditorGUILayout.Space();
            DrawPlatformTabs();
        }

        private void DrawFilters()
        {
            GUILayout.Label("Filters", Styles.BoldWrap);
            using (new EditorGUI.IndentLevelScope())
            {
                TypeFilter = (TextureTypeFilter)EditorGUILayout.EnumFlagsField("Texture Type", TypeFilter);
                ShapeFilter = (TextureShapeFilter)EditorGUILayout.EnumFlagsField("Texture Shape", ShapeFilter);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    _nameFilter = EditorGUILayout.TextField("Name Regex:", _nameFilter);
                    if (check.changed) CompileNameRegex();
                }

                if (!_nameRegexValid && !string.IsNullOrEmpty(_nameFilter))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Invalid regex pattern for name", MessageType.Error);
                    EditorGUILayout.Space();
                }

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    _pathFilter = EditorGUILayout.TextField("Path Regex:", _pathFilter);
                    if (check.changed) CompilePathRegex();
                }

                if (!_pathRegexValid && !string.IsNullOrEmpty(_pathFilter))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Invalid regex pattern for path", MessageType.Error);
                    EditorGUILayout.Space();
                }
            }
        }

        private void DrawMipMapSettings()
        {
            DefaultSettings.ChangeMipMaps = EditorGUILayout.Toggle("Change Mip Maps", DefaultSettings.ChangeMipMaps);
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!DefaultSettings.ChangeMipMaps))
            {
                DefaultSettings.GenerateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", DefaultSettings.GenerateMipMaps);
                using (new EditorGUI.DisabledScope(!DefaultSettings.GenerateMipMaps))
                {
                    DefaultSettings.StreamingMipMaps = EditorGUILayout.Toggle("Streaming Mip Maps", DefaultSettings.StreamingMipMaps);
                }
            }
        }

        private void DrawAnisoSettings()
        {
            DefaultSettings.ChangeAniso = EditorGUILayout.Toggle("Change Aniso Level", DefaultSettings.ChangeAniso);
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUI.DisabledScope(!DefaultSettings.ChangeAniso))
            {
                DefaultSettings.AnisoLevel = EditorGUILayout.IntSlider("Aniso Level", DefaultSettings.AnisoLevel, 0, 16);
                using (new EditorGUI.IndentLevelScope())
                {
                    DefaultSettings.AnisoCondition = (OverrideCondition)EditorGUILayout.EnumPopup("Override When", DefaultSettings.AnisoCondition);
                }
            }
        }

        private void DrawPlatformTabs()
        {
            EditorGUIUtility.labelWidth = 180f;
            _tabController.DrawTabs(index =>
            {
                if (index == 0) return false;

                string platformKey = index switch
                {
                    1 => "Standalone",
                    2 => "Android",
                    3 => "iPhone",
                    _ => null
                };

                return platformKey != null && _platformSettings.TryGetValue(platformKey, out var settings) && settings.Enabled;
            });
        }

        public void ProcessTextures(TextureDetails details)
        {
            int changedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                var importers = details.GetImporters.ToList();
                int count = importers.Count;

                for (int i = 0; i < count; i++)
                {
                    var importer = importers[i];
                    if (importer == null) continue;

                    EditorUtility.DisplayProgressBar("Applying New Settings", importer.assetPath, (float)i / count);

                    if (!MatchesFilters(importer)) continue;

                    bool changed = DefaultSettings.ApplyTo(importer);

                    foreach (var platform in _platformSettings.Values)
                    {
                        changed |= platform.ApplyTo(importer);
                    }

                    if (changed)
                    {
                        changedCount++;
                        importer.SaveAndReimport();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                EditorUtility.DisplayDialog("Changes Applied", $"Changes were made to {changedCount} Texture Importers based on the settings.", "Ok");
            }
        }
    }

    public class TextureStats
    {
        private readonly Dictionary<Texture, TextureImporter> _textures;
        private readonly Func<TextureImporter, bool> _filterPredicate;

        private long? _totalSize;
        private long? _filteredSize;
        private int? _uncrunchedCount;
        private int? _filteredUncrunchedCount;
        private int? _filteredCount;

        public TextureStats(Dictionary<Texture, TextureImporter> textures, Func<TextureImporter, bool> filterPredicate)
        {
            _textures = textures;
            _filterPredicate = filterPredicate;
        }

        public int TotalCount => _textures.Count;
        public int FilteredCount => _filteredCount ??= _textures.Count(x => _filterPredicate(x.Value));
        public long TotalSize => _totalSize ??= _textures.Sum(x => EditorTextureUtil.GetStorageMemorySize(x.Key));

        public long FilteredSize => _filteredSize ??= _textures
            .Where(x => _filterPredicate(x.Value))
            .Sum(x => EditorTextureUtil.GetStorageMemorySize(x.Key));

        public int UncrunchedCount => _uncrunchedCount ??=
            _textures.Count(x => !x.Value.crunchedCompression || x.Value.textureCompression == TextureImporterCompression.Uncompressed);

        public int FilteredUncrunchedCount => _filteredUncrunchedCount ??=
            _textures.Count(x => (!x.Value.crunchedCompression || x.Value.textureCompression == TextureImporterCompression.Uncompressed) && _filterPredicate(x.Value));

        public void InvalidateFiltered()
        {
            _filteredSize = null;
            _filteredCount = null;
            _filteredUncrunchedCount = null;
        }

        public void InvalidateAll()
        {
            _totalSize = null;
            _uncrunchedCount = null;
            InvalidateFiltered();
        }
    }

    public class MassTextureImporter : EditorWindow
    {
        private TextureStats _stats;
        private TextureDetails _details;
        private ImporterSettingsManager _settingsManager;
        private bool _settingsChanged;
        private Vector2 _scrollPos;

        private TextureTreeView _textureTreeView;
        private SearchField _searchField;

        [NonSerialized] private bool initDone;
        [SerializeField] private TreeViewState _treeViewState;
        [SerializeField] private MultiColumnHeaderState _multiColumnHeaderState;
        [SerializeField] private MultiColumnHeader _multiColumnHeader;

        private const float SettingsPanelWidth = 450f;
        private const float MinWindowWidth = 1650f;
        private const float MinWindowHeight = 650f;

        [MenuItem("VRWorld Toolkit/Mass Texture Importer", false, 26)]
        public static void ShowWindow()
        {
            var window = GetWindow<MassTextureImporter>();
            window.titleContent = new GUIContent("Mass Texture Importer");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        private void OnEnable()
        {
            _settingsManager = new ImporterSettingsManager();
            _details = GetAllTexturesFromScene();
            _stats = new TextureStats(_details.TextureList, _settingsManager.MatchesFilters);
        }

        private void OnDisable()
        {
            if (_searchField != null && _textureTreeView != null)
            {
                _searchField.downOrUpArrowKeyPressed -= _textureTreeView.SetFocusAndEnsureSelectedItem;
            }

            _textureTreeView?.Cleanup();
            _details?.TextureList.Clear();
            _details = null;
            _stats = null;
            _textureTreeView = null;
        }

        private void OnGUI()
        {
            InitIfNeeded();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSettingsPanel();
                DrawTextureTreeView();
            }
        }

        private void InitIfNeeded()
        {
            if (initDone) return;

            bool firstInit = _multiColumnHeaderState == null;
            var headerState = TextureTreeView.CreateDefaultMultiColumnHeaderState(EditorGUIUtility.currentViewWidth - 121);

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_multiColumnHeaderState, headerState))
            {
                MultiColumnHeaderState.OverwriteSerializedFields(_multiColumnHeaderState, headerState);
            }

            _multiColumnHeaderState = headerState;
            _multiColumnHeader = new MultiColumnHeader(headerState);

            if (firstInit)
            {
                _multiColumnHeader.ResizeToFit();
            }

            _treeViewState ??= new TreeViewState();

            _searchField = new SearchField();
            _textureTreeView = new TextureTreeView(_treeViewState, _multiColumnHeader, _details.TextureList, _settingsManager);
            _searchField.downOrUpArrowKeyPressed += _textureTreeView.SetFocusAndEnsureSelectedItem;

            initDone = true;
        }

        private void DrawSettingsPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SettingsPanelWidth)))
            {
                DrawStats();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos))
                {
                    _scrollPos = scrollView.scrollPosition;

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        _settingsManager.DrawSettings();

                        if (check.changed)
                        {
                            _settingsChanged = true;
                            _textureTreeView.Reload();
                            _stats.InvalidateFiltered();
                        }
                    }
                }

                DrawButtons();
            }
        }

        private void DrawStats()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("All Textures:", EditorStyles.boldLabel);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label($"<b>Texture Count:</b> {_stats.TotalCount}", Styles.LabelRichText);
                        GUILayout.Label($"<b>Uncrunched Count:</b> {_stats.UncrunchedCount}", Styles.LabelRichText);
                        GUILayout.Label($"<b>Storage Size:</b> {EditorUtility.FormatBytes(_stats.TotalSize)}", Styles.LabelRichText);
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Filtered Textures:", EditorStyles.boldLabel);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label($"<b>Texture Count:</b> {_stats.FilteredCount}", Styles.LabelRichText);
                        GUILayout.Label($"<b>Uncrunched Count:</b> {_stats.FilteredUncrunchedCount}", Styles.LabelRichText);
                        GUILayout.Label($"<b>Storage Size:</b> {EditorUtility.FormatBytes(_stats.FilteredSize)}", Styles.LabelRichText);
                    }
                }
            }
        }

        private void DrawButtons()
        {
            GUILayout.Label("Get textures from:");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scene", GUILayout.Width(70), GUILayout.Height(20)))
                {
                    _details = GetAllTexturesFromScene();
                    _textureTreeView.SetTextures(_details.TextureList);
                    _stats = new TextureStats(_details.TextureList, _settingsManager.MatchesFilters);
                }

                if (GUILayout.Button("Assets", GUILayout.Width(70), GUILayout.Height(20)))
                {
                    _details = GetAllTexturesFromAssets();
                    _textureTreeView.SetTextures(_details.TextureList);
                    _stats = new TextureStats(_details.TextureList, _settingsManager.MatchesFilters);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!_settingsChanged))
                {
                    if (GUILayout.Button("Default", GUILayout.Width(70), GUILayout.Height(20)))
                    {
                        _settingsManager = new ImporterSettingsManager();
                        _settingsChanged = false;
                        _textureTreeView.SetSettingsManager(_settingsManager);
                    }
                }

                if (GUILayout.Button("Apply", GUILayout.Width(70), GUILayout.Height(20)))
                {
                    bool confirmed = EditorUtility.DisplayDialog("Process Importers?", $"About to process Texture Importer settings on {_stats.FilteredCount} textures. This can take a while depending on the number of textures with changes and their original size.\n\nDo you want to continue?", "Ok", "Cancel");

                    if (confirmed)
                    {
                        _settingsManager.ProcessTextures(_details);
                        _stats.InvalidateAll();
                        _textureTreeView.RefreshItems();
                    }
                }
            }
            EditorGUILayout.Space();
        }

        private void DrawTextureTreeView()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                    {
                        _textureTreeView.RefreshItems();
                    }

                    GUILayout.FlexibleSpace();

                    _textureTreeView.searchString = _searchField.OnToolbarGUI(_textureTreeView.searchString, GUILayout.Width(400));
                }

                var treeViewRect = EditorGUILayout.BeginVertical();
                _textureTreeView.OnGUI(treeViewRect);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }
        }

        private static TextureDetails GetAllTexturesFromScene()
        {
            var details = new TextureDetails();
            var checkedMaterials = new HashSet<Material>();

            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int totalCount = allGameObjects.Length;

            for (int i = 0; i < totalCount; i++)
            {
                var gameObject = allGameObjects[i];

                if (gameObject.hideFlags != HideFlags.None ||
                    EditorUtility.IsPersistent(gameObject.transform.root.gameObject))
                {
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Getting All Textures from the Scene", gameObject.name, (float)i / totalCount))
                {
                    break;
                }

                var renderers = gameObject.GetComponents<Renderer>();

                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || checkedMaterials.Contains(material))
                            continue;

                        checkedMaterials.Add(material);

                        var shader = material.shader;
                        int propertyCount = ShaderUtil.GetPropertyCount(shader);

                        for (int j = 0; j < propertyCount; j++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, j) != ShaderUtil.ShaderPropertyType.TexEnv)
                                continue;

                            var texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                            if (texture == null) continue;

                            var path = AssetDatabase.GetAssetPath(texture);
                            if (!IsValidTexturePath(path)) continue;

                            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                            if (textureImporter != null)
                            {
                                details.AddTexture(textureImporter, texture);
                            }
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            return details;
        }

        private static bool IsValidTexturePath(string path)
        {
            if (string.IsNullOrEmpty(path) ||
                path.StartsWith("Resources/unity_builtin") ||
                path.StartsWith("Library/") ||
                 !path.StartsWith("Assets/")) return false;
            return true;
        }

        private static TextureDetails GetAllTexturesFromAssets()
        {
            var details = new TextureDetails();
            var assetGuids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets" });
            int totalCount = assetGuids.Length;

            for (int i = 0; i < totalCount; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);

                if (EditorUtility.DisplayCancelableProgressBar("Getting All Textures from Assets", Path.GetFileName(path), (float)i / totalCount))
                {
                    break;
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                if (textureImporter != null)
                {
                    details.AddTexture(textureImporter, texture);
                }
            }

            EditorUtility.ClearProgressBar();
            return details;
        }
    }
}
