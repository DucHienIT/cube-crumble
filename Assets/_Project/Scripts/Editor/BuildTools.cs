using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Toolbars;
using UnityEngine;

namespace CubeBurst.EditorTools
{
    /// <summary>
    /// WebGL build pipeline for Cube Burst: a top-level "Build" menu plus a
    /// "Build WebGL" button on the editor's main toolbar (Unity 6.3
    /// MainToolbarElement API). Every build first applies the project's WebGL
    /// player settings so the output runs from any static web server
    /// (Gzip + JS decompression fallback → itch.io / GitHub Pages / any CDN).
    /// </summary>
    public static class BuildTools
    {
        const string OutputDir = "Builds/WebGL";

        [MainToolbarElement("CubeBurst/BuildWebGL", defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarButton CreateToolbarButton()
        {
            return new MainToolbarButton(new MainToolbarContent("Build WebGL"), BuildWebGL);
        }

        [MenuItem("Build/Build WebGL", priority = 0)]
        public static void BuildWebGL()
        {
            Build(BuildOptions.None);
        }

        [MenuItem("Build/Build + Run WebGL", priority = 1)]
        public static void BuildAndRunWebGL()
        {
            Build(BuildOptions.AutoRunPlayer);
        }

        [MenuItem("Build/Open Build Folder", priority = 20)]
        public static void OpenBuildFolder()
        {
            if (!Directory.Exists(OutputDir))
            {
                EditorUtility.DisplayDialog("No build yet",
                    $"{OutputDir} does not exist — run Build WebGL first.", "OK");
                return;
            }
            EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(OutputDir, "index.html")));
        }

        static void Build(BuildOptions options)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("WebGL module missing",
                    "WebGL Build Support is not installed for this editor version.\n" +
                    "Install it via Unity Hub → Installs → Add modules → WebGL.", "OK");
                return;
            }

            ApplyWebGLSettings();

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("No scenes in build",
                    "Build Settings has no enabled scenes. Add Assets/_Project/Scenes/Main.unity first.", "OK");
                return;
            }

            var report = BuildPipeline.BuildPlayer(scenes, OutputDir, BuildTarget.WebGL, options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[CubeBurst] WebGL build OK → {Path.GetFullPath(OutputDir)} " +
                          $"({summary.totalSize / (1024f * 1024f):F1} MB in {summary.totalTime.TotalMinutes:F1} min)");
            }
            else
            {
                Debug.LogError($"[CubeBurst] WebGL build {summary.result}: {summary.totalErrors} error(s) — see console above.");
            }
        }

        /// <summary>WebGL player settings suited to a small portrait puzzle game.</summary>
        static void ApplyWebGLSettings()
        {
            // Gzip + decompression fallback: decodes in JS when the host doesn't send
            // Content-Encoding headers, so the build runs on itch.io/GitHub Pages/any CDN.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 540;   // portrait canvas like a phone
            PlayerSettings.defaultWebScreenHeight = 960;
        }
    }
}
