#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EcoShark.Editor
{
    [InitializeOnLoad]
    public static class EcoSharkPublishHelper
    {
        static EcoSharkPublishHelper()
        {
            EditorApplication.delayCall += CheckTriggerAndBuild;
        }

        private static void CheckTriggerAndBuild()
        {
            string triggerPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "trigger_webgl_build.txt");
            if (File.Exists(triggerPath))
            {
                try
                {
                    File.Delete(triggerPath);
                }
                catch { }

                Debug.Log("<color=#38bdf8>[ECO SHARK]</color> Gatillo de compilación detectado. Iniciando compilación WebGL...");
                BuildWebGLForPlay();
            }
        }

        [MenuItem("EcoShark/1. Abrir Ventana de Publicacion (Unity Play)", false, 100)]
        public static void OpenUnityPlayWindow()
        {
            try
            {
                EditorApplication.ExecuteMenuItem("Publish/WebGL Project");
                Debug.Log("<color=#38bdf8>[ECO SHARK]</color> Ventana de Unity Play abierta con exito.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ECO SHARK] No se pudo abrir desde el menu directo: " + ex.Message);
            }
        }

        [MenuItem("EcoShark/2. Compilar Build WebGL para Unity Play", false, 101)]
        public static void BuildWebGLForPlay()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string buildFolder = Path.Combine(projectRoot, "Builds", "WebGL");
            string zipOutput = Path.Combine(projectRoot, "Builds", "EcoShark_WebGL.zip");

            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string[] scenes = new string[] { "Assets/EcoShark/Scenes/EcoShark_MVP.unity" };

            // Ensure WebGL target settings
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            // Configure WebGL player settings for optimal web compatibility
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildFolder,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("<color=#38bdf8>[ECO SHARK]</color> Compilando WebGL hacia " + buildFolder + "...");
            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#4ade80>[ECO SHARK] Build WebGL completada con exito!</color> Tamano: {report.summary.totalSize / (1024 * 1024)} MB");

                // Create Zip for Unity Play / Itch.io upload
                try
                {
                    if (File.Exists(zipOutput)) File.Delete(zipOutput);
                    ZipFile.CreateFromDirectory(buildFolder, zipOutput);
                    Debug.Log($"<color=#a855f7>[ECO SHARK] Archivo ZIP generado en:</color> {zipOutput}");
                    
                    // Write status file for agent
                    File.WriteAllText(Path.Combine(projectRoot, "Builds", "build_status.txt"), "SUCCESS: " + zipOutput);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ECO SHARK] No se pudo comprimir en zip: " + e.Message);
                    File.WriteAllText(Path.Combine(projectRoot, "Builds", "build_status.txt"), "ZIP_ERROR: " + e.Message);
                }
            }
            else
            {
                Debug.LogError($"[ECO SHARK] Error al compilar WebGL: {report.summary.result}");
                File.WriteAllText(Path.Combine(projectRoot, "Builds", "build_status.txt"), "BUILD_FAILED: " + report.summary.result);
            }
        }
    }
}
#endif
