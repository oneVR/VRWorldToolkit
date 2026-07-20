using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using VRC.SDK3.Editor;

namespace VRWorldToolkit.Editor
{
    public static class BuildReportManager
    {
#if VRC_SDK_VRCSDK3
        [InitializeOnLoadMethod]
        private static void RegisterSDKCallback()
        {
            VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
        }
#if UDON
        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder))
            {
                builder.OnSdkBuildFinish -= OnBuildFinished;
                builder.OnSdkBuildFinish += OnBuildFinished;
            }
        }
#else
        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
            {
                builder.OnSdkBuildFinish -= OnBuildFinished;
                builder.OnSdkBuildFinish += OnBuildFinished;
            }
        }
#endif
        private static void OnBuildFinished(object sender, string path)
        {
            if (VRWorldToolkitSettings.Instance.alwaysCaptureBuildReport)
            {
                CheckForNewBuild();
            }
        }
#endif

        private const string LastBuild = "Library/LastBuild.buildreport";
        private const string BuildReportDir = "Assets/_LastBuild";
        private const string LastBuildReportPath = "Assets/_LastBuild/LastBuild.buildreport";

        private static readonly Dictionary<BuildTargetGroup, string> PlatformNames = new()
        {
            { BuildTargetGroup.Standalone, "Windows"},
            { BuildTargetGroup.Android, "Android"},
            { BuildTargetGroup.iOS, "iOS"},
        };

        private static string LatestPathFor(string platform) => $"{BuildReportDir}/Last{platform}Build.buildreport";

        private static string ArchivePathFor(string platform, DateTime time) =>
            $"{BuildReportDir}/ArchivedBuilds/{platform}/build_report_{time:yyyy-MM-dd_HH-mm-ss}.buildreport";

        public static bool CheckForNewBuild()
        {
            if (!File.Exists(LastBuild)) return false;
            
            if (File.Exists(LastBuildReportPath) && File.GetLastWriteTime(LastBuild) <= File.GetLastWriteTime(LastBuildReportPath)) return false;

            Directory.CreateDirectory(BuildReportDir);
            File.Copy(LastBuild, LastBuildReportPath, true);
            AssetDatabase.ImportAsset(LastBuildReportPath);
            
            var report = (BuildReport)AssetDatabase.LoadAssetAtPath(LastBuildReportPath, typeof(BuildReport));
            if (report == null) return false;
            if (!PlatformNames.TryGetValue(report.summary.platformGroup, out var platform)) return false;
            
            var latest = LatestPathFor(platform);

            if (File.Exists(latest))
            {
                if (File.GetLastWriteTime(LastBuildReportPath) < File.GetLastWriteTime(latest)) return false;
                
                var latestReport = (BuildReport)AssetDatabase.LoadAssetAtPath(latest, typeof(BuildReport));
                if (latestReport == null) return false;

                if (VRWorldToolkitSettings.Instance.archiveBuildReports)
                {
                    var archivePath = ArchivePathFor(platform, latestReport.summary.buildEndedAt);
                    var archivePathDirectory = Path.GetDirectoryName(archivePath);
                    if (archivePathDirectory != null)
                    {
                        Directory.CreateDirectory(archivePathDirectory);
                        AssetDatabase.Refresh();
                        AssetDatabase.CopyAsset(latest, archivePath);
                        if (VRWorldToolkitSettings.Instance.rotateArchivedBuildReports)
                        {
                            var files = Directory.GetFiles(archivePathDirectory, "*.buildreport");
                            Array.Sort(files);
                            if (files.Length > VRWorldToolkitSettings.Instance.maxArchivedBuildReports)
                            {
                                var count = files.Length - VRWorldToolkitSettings.Instance.maxArchivedBuildReports;
                                for (var i = 0; i < count; i++)
                                {
                                    AssetDatabase.DeleteAsset(files[i]);
                                }
                            }
                        }
                    }
                }
            }
            
            AssetDatabase.CopyAsset(LastBuildReportPath, LatestPathFor(platform));
            return true;
        }
    }
}