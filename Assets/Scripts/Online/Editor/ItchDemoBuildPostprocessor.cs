using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Arborvale.Online.Editor
{
    /// <summary>
    /// Strips Steam binaries from the output folder after any build
    /// that does not include STEAM_BUILD in its scripting define symbols.
    /// This ensures the itch.io demo ships with no Steamworks dependency.
    /// </summary>
    public class ItchDemoBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);

            if (defines.Contains("STEAM_BUILD"))
                return;

            var outputDir = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(outputDir))
                return;

            string[] steamFiles =
            {
                "steam_api64.dll",
                "steam_api.dll",
                "libsteam_api.so",
                "libsteam_api.dylib",
                "steam_appid.txt",
            };

            foreach (var filename in steamFiles)
            {
                var path = Path.Combine(outputDir, filename);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    UnityEngine.Debug.Log($"[ItchDemo] Stripped Steam file: {filename}");
                }
            }
        }
    }
}
