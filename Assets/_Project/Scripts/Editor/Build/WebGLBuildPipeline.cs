using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SayHeyDash.EditorTools.Build
{
    /// <summary>
    /// Reproducible WebGL build: applies the agreed player settings in code (so they are
    /// reviewable in version control) and builds every enabled scene in Build Settings.
    ///
    /// Menu:      Say Hey Dash > Build > ...
    /// Batchmode: Unity.exe -batchmode -quit -projectPath &lt;path&gt; -buildTarget WebGL
    ///            -executeMethod SayHeyDash.EditorTools.Build.WebGLBuildPipeline.BuildFromCommandLine
    ///            [-shdDevelopment]
    /// </summary>
    public static class WebGLBuildPipeline
    {
        public const string OutputDirectory = "Builds/WebGL";
        public const string TemplateName = "PROJECT:SayHeyDash";
        private const string DevelopmentArg = "-shdDevelopment";

        [MenuItem("Say Hey Dash/Build/WebGL (Release)", priority = 1)]
        public static void BuildRelease() => Build(development: false);

        [MenuItem("Say Hey Dash/Build/WebGL (Development)", priority = 2)]
        public static void BuildDevelopment() => Build(development: true);

        [MenuItem("Say Hey Dash/Build/Apply WebGL Player Settings", priority = 20)]
        public static void ApplyReleaseSettings() => ApplyPlayerSettings(development: false);

        public static void BuildFromCommandLine()
        {
            bool development = Environment.GetCommandLineArgs().Contains(DevelopmentArg);
            bool ok = Build(development);
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void ApplyPlayerSettings(bool development)
        {
            var target = NamedBuildTarget.WebGL;

            // Names, icon and splash screen (client logo) - Say Hey Dash > Branding (Milestone 5).
            Branding.BrandingBuilder.ApplyPlayerSettings(saveAssets: false);
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;

            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            // Brotli = smallest download. Decompression fallback lets the build run on hosts that
            // do not send Content-Encoding headers (local tests, simple static hosts).
            // Milestone 5: switch fallback off once the production host serves .br correctly.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = development
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);

            AssetDatabase.SaveAssets();
        }

        public static bool Build(bool development)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebGLBuildPipeline] No enabled scenes in Build Settings.");
                return false;
            }

            ApplyPlayerSettings(development);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;

            long downloadBytes = ok ? MeasureDownloadSize() : 0;
            string message =
                $"[WebGLBuildPipeline] {(development ? "Development" : "Release")} build {summary.result} " +
                $"in {summary.totalTime:mm\\:ss}. Scenes: {string.Join(", ", scenes)}. " +
                $"Compressed download (Build/ folder): {downloadBytes / 1048576f:0.00} MB. Output: {OutputDirectory}";

            if (ok) Debug.Log(message);
            else Debug.LogError(message);
            return ok;
        }

        private static long MeasureDownloadSize()
        {
            string buildDir = Path.Combine(OutputDirectory, "Build");
            return Directory.Exists(buildDir)
                ? new DirectoryInfo(buildDir).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
                : 0;
        }
    }
}
