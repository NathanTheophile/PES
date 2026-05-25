#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Editor
#endregion

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TacticalPort.EditorTools
{
    public static class CloudCodeDeployMenu
    {
        #region _____________________________/ VALUES

        private const string DeployMenuPath = "Project/Deploy/Edgegap Cloud Code...";
        private const string DryRunMenuPath = "Project/Deploy/Edgegap Cloud Code Dry Run";
        private const string BuildOnlyMenuPath = "Project/Deploy/Edgegap Cloud Code Build Only";
        private const string LauncherRelativePath = "Tools/DeployEdgegapCloudCode.cmd";
        private const string AllocatorSourceRelativePath = "CloudCodeModules/EdgegapAllocator/Project/EdgegapAllocator.cs";

        #endregion

        #region _____________________________| MENU

        [MenuItem(DeployMenuPath)]
        private static void DeployEdgegapCloudCode()
        {
            string lVersionName = ResolveVersionName();
            bool lConfirmed = EditorUtility.DisplayDialog(
                "Deploy Edgegap Cloud Code",
                $"This will build and deploy EdgegapAllocator to the active UGS project/environment.\n\nVersionName: {lVersionName}\n\nContinue?",
                "Deploy",
                "Cancel");

            if (!lConfirmed)
                return;

            RunLauncher(string.Empty);
        }

        [MenuItem(DryRunMenuPath)]
        private static void DryRunEdgegapCloudCode() => RunLauncher("-DryRunOnly");

        [MenuItem(BuildOnlyMenuPath)]
        private static void BuildOnlyEdgegapCloudCode() => RunLauncher("-BuildOnly");

        [MenuItem(DeployMenuPath, true)]
        [MenuItem(DryRunMenuPath, true)]
        [MenuItem(BuildOnlyMenuPath, true)]
        private static bool CanRunEdgegapCloudCode() => File.Exists(ResolveLauncherPath());

        #endregion

        #region _____________________________| HELPERS

        private static void RunLauncher(string pArguments)
        {
            string lLauncherPath = ResolveLauncherPath();
            if (!File.Exists(lLauncherPath))
            {
                EditorUtility.DisplayDialog("Deploy Edgegap Cloud Code", $"Launcher not found:\n{lLauncherPath}", "OK");
                return;
            }

            ProcessStartInfo lStartInfo = new ProcessStartInfo
            {
                FileName = lLauncherPath,
                Arguments = pArguments,
                WorkingDirectory = ResolveProjectRoot(),
                UseShellExecute = true
            };

            try
            {
                Process.Start(lStartInfo);
                Debug.Log($"Started Edgegap Cloud Code deployment launcher: {lLauncherPath} {pArguments}");
            }
            catch (System.Exception lException)
            {
                Debug.LogError($"Failed to start Edgegap Cloud Code deployment launcher: {lException}");
                EditorUtility.DisplayDialog("Deploy Edgegap Cloud Code", lException.Message, "OK");
            }
        }

        private static string ResolveVersionName()
        {
            string lAllocatorPath = Path.Combine(ResolveProjectRoot(), AllocatorSourceRelativePath);
            if (!File.Exists(lAllocatorPath))
                return "<not found>";

            string lSource = File.ReadAllText(lAllocatorPath);
            Match lMatch = Regex.Match(lSource, "private\\s+const\\s+string\\s+VersionName\\s*=\\s*\"([^\"]+)\"");
            return lMatch.Success ? lMatch.Groups[1].Value : "<not found>";
        }

        private static string ResolveLauncherPath() =>
            Path.Combine(ResolveProjectRoot(), LauncherRelativePath);

        private static string ResolveProjectRoot() =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        #endregion
    }
}
