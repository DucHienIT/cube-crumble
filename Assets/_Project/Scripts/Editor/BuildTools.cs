using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CubeBurst.EditorTools
{
    /// <summary>
    /// WebGL build pipeline for Cube Burst: a top-level "Build" menu.
    ///
    /// Two flavours, each with its own output folder so a dev build never
    /// overwrites the shippable one:
    ///   Release     → Builds/WebGL      (Gzip + JS decompression fallback, hashed
    ///                                    file names, minimal exceptions, no debug
    ///                                    symbols) — runs from any static host:
    ///                                    itch.io / GitHub Pages / any CDN.
    ///   Development → Builds/WebGL-Dev  (full stack traces, profiler, diagnostics
    ///                                    overlay, uncompressed → fast iteration).
    ///
    /// "Zip Release For Upload" packs Builds/WebGL with index.html at the archive
    /// root, which is exactly what itch.io expects.
    ///
    /// CI / one-click scripts use <see cref="BuildWebGLCI"/>:
    ///   Unity.exe -quit -batchmode -nographics -projectPath &lt;proj&gt; \
    ///             -executeMethod CubeBurst.EditorTools.BuildTools.BuildWebGLCI \
    ///             [-devBuild] [-zip] [-outputPath Builds/WebGL] -logFile -
    /// It exits 0 on success, 1 on failure.
    /// </summary>
    public static class BuildTools
    {
        // Shown in the browser tab / index.html title of the build. Lock this in
        // before release: WebGL keys its IndexedDB store (where PlayerPrefs, i.e.
        // SaveSystem's progress, lives) off company+product name, so renaming later
        // makes every existing player look like a fresh install.
        const string ProductName = "Cube Burst";

        const string ReleaseDir = "Builds/WebGL";
        const string DevDir = "Builds/WebGL-Dev";
        const string ZipDir = "Builds";

        // IL2CPP Master squeezes the wasm down but roughly doubles build time.
        // Flip to false while iterating on release settings.
        const bool MasterConfigForRelease = true;

        enum Flavor { Release, Development }

        // ---------------------------------------------------------------- menu

        [MenuItem("Build/WebGL Release", priority = 0)]
        public static void BuildRelease()
        {
            Run(Flavor.Release, ReleaseDir, BuildOptions.None);
        }

        [MenuItem("Build/WebGL Release + Run", priority = 1)]
        public static void BuildReleaseAndRun()
        {
            Run(Flavor.Release, ReleaseDir, BuildOptions.AutoRunPlayer);
        }

        [MenuItem("Build/WebGL Development + Run", priority = 20)]
        public static void BuildDevelopmentAndRun()
        {
            Run(Flavor.Development, DevDir, BuildOptions.AutoRunPlayer);
        }

        [MenuItem("Build/Zip Release For Upload", priority = 40)]
        public static void ZipReleaseMenu()
        {
            if (!IsWebGLBuild(ReleaseDir))
            {
                Fail("No release build", $"{ReleaseDir} does not contain a WebGL build — run Build/WebGL Release first.");
                return;
            }
            var zip = ZipRelease();
            if (zip != null && !Application.isBatchMode) EditorUtility.RevealInFinder(zip);
        }

        [MenuItem("Build/Open Build Folder", priority = 41)]
        public static void OpenBuildFolder()
        {
            var dir = Directory.Exists(ReleaseDir) ? ReleaseDir : DevDir;
            if (!Directory.Exists(dir))
            {
                Fail("No build yet", $"{ReleaseDir} does not exist — run Build/WebGL Release first.");
                return;
            }
            EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(dir, "index.html")));
        }

        [MenuItem("Build/Apply WebGL Player Settings (Release)", priority = 60)]
        public static void ApplyReleaseSettingsMenu()
        {
            ApplyWebGLSettings(Flavor.Release);
            AssetDatabase.SaveAssets();
            Debug.Log("[CubeBurst] Release WebGL player settings applied (see Project Settings → Player).");
        }

        // ----------------------------------------------------------------- CI

        /// <summary>Batchmode entry point. See the class summary for the command line.</summary>
        public static void BuildWebGLCI()
        {
            var args = Environment.GetCommandLineArgs();
            var flavor = args.Contains("-devBuild") ? Flavor.Development : Flavor.Release;
            var outputDir = ArgValue(args, "-outputPath") ?? (flavor == Flavor.Development ? DevDir : ReleaseDir);

            var ok = Run(flavor, outputDir, BuildOptions.None);
            if (ok && args.Contains("-zip")) ok = ZipRelease(outputDir) != null;

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static string ArgValue(string[] args, string flag)
        {
            var i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        // -------------------------------------------------------------- build

        static bool Run(Flavor flavor, string outputDir, BuildOptions extraOptions)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Fail("WebGL module missing",
                     "WebGL Build Support is not installed for this editor version.\n" +
                     "Install it via Unity Hub → Installs → Add modules → WebGL.");
                return false;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Fail("No scenes in build",
                     "Build Settings has no enabled scenes. Add Assets/_Project/Scenes/Main.unity first.");
                return false;
            }

            if (!PrepareOutputDir(outputDir)) return false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL))
            {
                Fail("Cannot switch platform", "Switching the active build target to WebGL failed — see the console.");
                return false;
            }

            ApplyWebGLSettings(flavor);

            var options = extraOptions;
            if (flavor == Flavor.Development)
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging;

            var report = BuildPipeline.BuildPlayer(scenes, outputDir, BuildTarget.WebGL, options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[CubeBurst] WebGL {flavor} build {summary.result}: " +
                               $"{summary.totalErrors} error(s) — see console above.");
                return false;
            }

            // With decompressionFallback on, the payload ships pre-gzipped as .unityweb,
            // so what's on disk is what the browser downloads.
            var payloadMB = DirectorySizeMB(Path.Combine(outputDir, "Build"));
            Debug.Log($"[CubeBurst] WebGL {flavor} build OK → {Path.GetFullPath(outputDir)}\n" +
                      $"  {DirectorySizeMB(outputDir):F1} MB total ({payloadMB:F1} MB payload), " +
                      $"built in {summary.totalTime.TotalMinutes:F1} min.");
            return true;
        }

        /// <summary>
        /// Wipes a previous build so removed/renamed files (hashed names change every
        /// build) don't linger. Refuses to touch a folder that isn't a Unity WebGL build.
        /// </summary>
        static bool PrepareOutputDir(string dir)
        {
            if (!Directory.Exists(dir)) return true;

            if (IsEmptyDir(dir) || IsWebGLBuild(dir))
            {
                Directory.Delete(dir, true);
                return true;
            }

            Fail("Output folder is not a build",
                 $"{Path.GetFullPath(dir)} exists but does not look like a Unity WebGL build " +
                 "(no index.html + Build/ folder). Refusing to delete it — move it aside or pick another path.");
            return false;
        }

        static bool IsWebGLBuild(string dir) =>
            File.Exists(Path.Combine(dir, "index.html")) && Directory.Exists(Path.Combine(dir, "Build"));

        static bool IsEmptyDir(string dir) =>
            !Directory.EnumerateFileSystemEntries(dir).Any();

        // ------------------------------------------------------------ settings

        /// <summary>WebGL player settings suited to a small portrait puzzle game.</summary>
        static void ApplyWebGLSettings(Flavor flavor)
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 540;   // portrait canvas like a phone
            PlayerSettings.defaultWebScreenHeight = 960;

            // Gzip + decompression fallback: decodes in JS when the host doesn't send
            // Content-Encoding headers, so the build runs on itch.io/GitHub Pages/any CDN.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;

            // Managed stripping trims IL2CPP output; Assets/link.xml keeps the physics
            // types that are only ever AddComponent'd at runtime (see SharedSlotView).
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);

            var isRelease = flavor == Flavor.Release;
            PlayerSettings.WebGL.exceptionSupport = isRelease
                ? WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly
                : WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.debugSymbolMode = isRelease
                ? WebGLDebugSymbolMode.Off
                : WebGLDebugSymbolMode.Embedded;
            PlayerSettings.WebGL.showDiagnostics = !isRelease;
            // Hashed names let a CDN cache each file forever; only useful for a release.
            PlayerSettings.WebGL.nameFilesAsHashes = isRelease;
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL,
                isRelease && MasterConfigForRelease
                    ? Il2CppCompilerConfiguration.Master
                    : Il2CppCompilerConfiguration.Release);
        }

        // ----------------------------------------------------------------- zip

        /// <summary>Packs a finished build with index.html at the archive root (itch.io layout).</summary>
        static string ZipRelease(string sourceDir = ReleaseDir)
        {
            var name = $"{ProductName.Replace(" ", "")}-WebGL-v{PlayerSettings.bundleVersion}.zip";
            var zipPath = Path.GetFullPath(Path.Combine(ZipDir, name));

            try
            {
                Directory.CreateDirectory(ZipDir);
                if (File.Exists(zipPath)) File.Delete(zipPath);
                // Fastest: the payload is already gzipped, so deflate has nothing left to win.
                ZipFile.CreateFromDirectory(sourceDir, zipPath, System.IO.Compression.CompressionLevel.Fastest, false);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CubeBurst] Zipping {sourceDir} failed: {e.Message}");
                return null;
            }

            Debug.Log($"[CubeBurst] Zipped → {zipPath} ({new FileInfo(zipPath).Length / (1024f * 1024f):F1} MB)");
            return zipPath;
        }

        // --------------------------------------------------------------- utils

        static float DirectorySizeMB(string dir) =>
            Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                     .Sum(f => (float)new FileInfo(f).Length) / (1024f * 1024f);

        static void Fail(string title, string message)
        {
            Debug.LogError($"[CubeBurst] {title}: {message}");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog(title, message, "OK");
        }
    }
}
