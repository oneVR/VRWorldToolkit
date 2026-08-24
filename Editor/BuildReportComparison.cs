using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class BuildReportComparison : EditorWindow
    {
        [MenuItem("VRWorld Toolkit/Build Report Comparison", false, 26)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(BuildReportComparison));
            window.titleContent = new GUIContent("Build Report Comparison");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private const string WindowsBuildReportPath = "Assets/_LastBuild/LastWindowsBuild.buildreport";
        private const string AndroidBuildReportPath = "Assets/_LastBuild/LastAndroidBuild.buildreport";
        private const string iOSBuildReportPath = "Assets/_LastBuild/LastiOSBuild.buildreport";

        private const string ArchivedReportsPath = "Assets/_LastBuild/ArchivedBuilds/";

        [NonSerialized] private bool initDone;
        
        [SerializeField] private int tab;

        [SerializeField] private Vector2 scrollPosOrigin;
        [SerializeField] private Vector2 scrollPosTarget;
        
        [SerializeField] private string selectedLeft = "Latest";
        [SerializeField] private string selectedRight = "Windows";

        [SerializeField] private BuildReport latestWindowsBuild;
        [SerializeField] private BuildReport latestAndroidBuild;
        [SerializeField] private BuildReport latestiOSBuild;
        
        private Dictionary<string, List<BuildReport>> archivedBuilds = new();

        private SearchField searchField;
        private BuildReportTreeView comparisonOriginTreeView;
        private BuildReportTreeView comparisonTargetTreeView;

        [SerializeField] private MultiColumnHeaderState multiColumnHeaderStateOrigin;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderStateTarget;
        [SerializeField] private TreeViewState treeViewStateOrigin;
        [SerializeField] private TreeViewState treeViewStateTarget;
        
        [SerializeField] private BuildReport comparisonOriginBuildReport;
        [SerializeField] private BuildReport comparisonTargetBuildReport;
        [SerializeField] private bool overallStatsFoldout;
        [SerializeField] private bool buildReportMessagesFoldout;

        private void Init()
        {
            if (!initDone)
            {
                if (File.Exists(WindowsBuildReportPath))
                {
                    latestWindowsBuild = AssetDatabase.LoadAssetAtPath<BuildReport>(WindowsBuildReportPath);
                }
                if (File.Exists(AndroidBuildReportPath))
                {
                    latestAndroidBuild = AssetDatabase.LoadAssetAtPath<BuildReport>(AndroidBuildReportPath);
                }
                if (File.Exists(iOSBuildReportPath))
                {
                    latestiOSBuild = AssetDatabase.LoadAssetAtPath<BuildReport>(iOSBuildReportPath);
                }
                
                RefreshArchivedBuilds();
                
                initDone = true;

                CheckIfSelected();
            }
        }

        private void RefreshArchivedBuilds()
        {
            archivedBuilds.Clear();

            if (AssetDatabase.IsValidFolder(ArchivedReportsPath))
            {
                var platformDirectories = Directory.GetDirectories(ArchivedReportsPath);

                foreach (var directory in platformDirectories)
                {
                    var guids = AssetDatabase.FindAssets("t:BuildReport", new[] { directory });
                    var directoryName = new DirectoryInfo(directory).Name;
                        
                    var list = new List<BuildReport>();

                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var report = AssetDatabase.LoadAssetAtPath<BuildReport>(path);
                        if (report is null) continue;
                        list.Add(report);
                    }
                        
                    archivedBuilds.Add(directoryName, list.OrderByDescending(b => b.summary.buildEndedAt).ToList());
                }

                if (platformDirectories.Length > 0 && !archivedBuilds.ContainsKey(selectedRight))
                {
                    selectedRight = archivedBuilds.Keys.First();
                }
            }
            else
            {
                selectedRight = "Latest";
            }
        }

        private void OnGUI()
        {
            var current = Event.current;

            if (current.type == EventType.Layout)
            {
                Init();
            }

            switch (tab)
            {
                case 0:
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawSelectionSide(ref selectedLeft, ref comparisonOriginBuildReport, ref scrollPosOrigin);
                        }
                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawSelectionSide(ref selectedRight, ref comparisonTargetBuildReport, ref scrollPosTarget);
                        }
                    }
                    break;
                case 1:
                    if (comparisonOriginTreeView == null || comparisonTargetTreeView == null)
                    {
                        tab = 0;
                        break;
                    }
                    
                    if (searchField == null) searchField = new SearchField();

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                    {
                        if (GUILayout.Button("Back", EditorStyles.toolbarButton))
                        {
                            tab = 0;
                            comparisonOriginBuildReport = null;
                            comparisonTargetBuildReport = null;
                            GUIUtility.ExitGUI();
                        }

                        GUILayout.FlexibleSpace();

                        overallStatsFoldout = GUILayout.Toggle(overallStatsFoldout, "Stats", EditorStyles.toolbarButton);

                        buildReportMessagesFoldout = GUILayout.Toggle(buildReportMessagesFoldout, "Messages", EditorStyles.toolbarButton);

                        GUILayout.Space(5);

                        var currentSearch = comparisonOriginTreeView.searchString;
                        var newSearch = searchField.OnToolbarGUI(currentSearch, GUILayout.Width(250));

                        if (newSearch != currentSearch)
                        {
                            comparisonOriginTreeView.searchString = newSearch;
                            comparisonTargetTreeView.searchString = newSearch;
                        }
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawSide(comparisonOriginBuildReport, comparisonOriginTreeView);
                        }
                        using (new EditorGUILayout.VerticalScope())
                        {
                            DrawSide(comparisonTargetBuildReport, comparisonTargetTreeView);
                        }
                    }
                    break;
            }
            
            void DrawSelectionSide(ref string selected, ref BuildReport report, ref Vector2 scrollPos)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    DrawPlatformButtons(ref selected);

                    GUILayout.FlexibleSpace();
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = scrollView.scrollPosition;
                    DrawBuilds(selected, ref report);
                }
            }

            void DrawBuilds(string selected, ref BuildReport report)
            {
                if (selected == "Latest")
                {
                    GUILayout.FlexibleSpace();
                    if (latestWindowsBuild != null) DrawBuildReportButton(latestWindowsBuild, ref report, current);
                    if (latestAndroidBuild != null) DrawBuildReportButton(latestAndroidBuild, ref report, current);
                    if (latestiOSBuild != null) DrawBuildReportButton(latestiOSBuild, ref report, current);
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.FlexibleSpace();
                    
                    if (archivedBuilds.Count > 0)
                    {
                        if (archivedBuilds.TryGetValue(selected, out var buildReports))
                        {
                            if (buildReports.Count > 0)
                            {
                                foreach (var item in buildReports)
                                {
                                    DrawBuildReportButton(item, ref report, current);
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField($"No past archived builds stored for platform yet.", Styles.CenteredNoticeLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                                RefreshButton();
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"No past archived builds stored yet.", Styles.CenteredNoticeLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                        RefreshButton();
                    }

                    GUILayout.FlexibleSpace();

                    void RefreshButton()
                    {
                        if (GUILayout.Button("Refresh")) RefreshArchivedBuilds();
                    }
                }
            }

            void DrawPlatformButtons(ref string selected)
            {
                if (GUILayout.Toggle(selected == "Latest", "Latest", EditorStyles.toolbarButton))
                {
                    selected = "Latest";
                }

                foreach (var key in archivedBuilds.Keys)
                {
                    if (GUILayout.Toggle(selected == key, key, EditorStyles.toolbarButton))
                    {
                        selected = key;
                    }
                }
            }

            void DrawSide(BuildReport report, BuildReportTreeView reportTreeView)
            {
                DrawOverview(report);

                if (buildReportMessagesFoldout)
                {
                    // TODO: Fix layout when one side doesn't have messages
                    reportTreeView.DrawMessages();
                }
                else
                {
                    if (overallStatsFoldout)
                    {
                        reportTreeView.DrawOverallStats();
                    }

                    var treeViewRect = EditorGUILayout.BeginVertical();

                    if (reportTreeView.BuildSucceeded)
                    {
                        reportTreeView.OnGUI(treeViewRect);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();

                        if (!reportTreeView.HasReport)
                        {
                            EditorGUILayout.LabelField($"No Build Report Found", Styles.CenteredNoticeLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"Selected Build Failed", Styles.CenteredNoticeLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.Height(40));
                        }

                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawBuildReportButton(BuildReport report, ref BuildReport reportTarget, Event current)
        {
            using (var verticalScope = new EditorGUILayout.VerticalScope())
            {
                var prev = GUI.backgroundColor;
                if (report == reportTarget) GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
                DrawOverview(report);
                GUI.backgroundColor = prev;

                if (current.type == EventType.MouseUp && verticalScope.rect.Contains(current.mousePosition))
                {
                    reportTarget = report;

                    CheckIfSelected();
                    GUIUtility.ExitGUI();
                }

                EditorGUIUtility.AddCursorRect(verticalScope.rect, MouseCursor.Link);
            }
        }

        private void CheckIfSelected()
        {
            if (comparisonOriginBuildReport != null && comparisonTargetBuildReport != null)
            {
                RebuildTreeViews();
                tab = 1;
            }
        }

        private void RebuildTreeViews()
        {
            comparisonOriginTreeView = InitTreeView(comparisonOriginBuildReport, ref treeViewStateOrigin, ref multiColumnHeaderStateOrigin);
            comparisonTargetTreeView = InitTreeView(comparisonTargetBuildReport, ref treeViewStateTarget, ref multiColumnHeaderStateTarget);
            
            BuildReportTreeView InitTreeView(BuildReport report, ref TreeViewState treeViewState, ref MultiColumnHeaderState multiColumnHeaderState)
            {
                var firstInit = multiColumnHeaderState == null;
                var headerState = BuildReportTreeView.CreateDefaultMultiColumnHeaderState(EditorGUIUtility.currentViewWidth);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
                multiColumnHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(headerState);
                if (firstInit) multiColumnHeader.ResizeToFit();

                treeViewState ??= new TreeViewState();

                return new BuildReportTreeView(treeViewState, multiColumnHeader, report);
            }
        }

        private static void DrawOverview(BuildReport report)
        {
            if (report == null) return;
            var summary = report.summary;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var currentCulture = CultureInfo.CurrentCulture;
                var dateTimeFormat = currentCulture.DateTimeFormat;

                GUILayout.Label("<b>" + Helper.GetReadableBuildTargetName(summary.platformGroup) + " build</b>", Styles.LabelTitle);

                GUILayout.Label("<b>Build time:</b> " + summary.buildEndedAt.ToLocalTime().ToString("g", dateTimeFormat), Styles.LabelRichText);

                GUILayout.Label("<b>Build size:</b> " + EditorUtility.FormatBytes((long)summary.totalSize), Styles.LabelRichText);

                GUILayout.Label("<b>Build duration:</b> " + (summary.buildEndedAt - summary.buildStartedAt).ToString(@"hh\:mm\:ss"), Styles.LabelRichText);
                
                GUILayout.Label("<b>Errors during build:</b> " + summary.totalErrors, Styles.LabelRichText);

                GUILayout.Label("<b>Warnings during build:</b> " + summary.totalWarnings, Styles.LabelRichText);

                GUILayout.Label("<b>Build result:</b> " + summary.result, Styles.LabelRichText);
            }
        }
        
        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}