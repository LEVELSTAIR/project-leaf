using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arborvale.Online.Editor
{
    /// <summary>
    /// Aborts the build if the STEAM_BUILD define is absent for a Steam-profile
    /// build, or present for an itch.io demo build. Guards against human error when
    /// toggling defines manually.
    /// Set EditorPrefs key "ArbBuildTarget" to "steam" or "itch" before building.
    /// Default (key absent) assumes Steam build.
    /// </summary>
    public class BuildProfileValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1; // run before ItchDemoBuildPostprocessor

        public void OnPreprocessBuild(BuildReport report)
        {
            string target = EditorPrefs.GetString("ArbBuildTarget", "steam").ToLowerInvariant();

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            bool hasSteamDefine = defines.Contains("STEAM_BUILD");

            if (target == "itch" && hasSteamDefine)
            {
                throw new BuildFailedException(
                    "[BuildProfileValidator] ArbBuildTarget is 'itch' but STEAM_BUILD is defined. " +
                    "Remove STEAM_BUILD from Scripting Define Symbols before building the itch demo.");
            }

            if (target == "steam" && !hasSteamDefine)
            {
                throw new BuildFailedException(
                    "[BuildProfileValidator] ArbBuildTarget is 'steam' but STEAM_BUILD is not defined. " +
                    "Add STEAM_BUILD to Scripting Define Symbols before building the Steam release.");
            }

            Debug.Log($"[BuildProfileValidator] Build target '{target}' validated. STEAM_BUILD={hasSteamDefine}");
        }
    }
}
