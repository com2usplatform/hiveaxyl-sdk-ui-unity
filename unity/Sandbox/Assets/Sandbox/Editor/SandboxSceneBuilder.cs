// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hive.Axyl.UIKit.Sandbox.Editor
{
    /// <summary>
    /// Builds the sandbox scene — a camera, an EventSystem, and the controller that owns
    /// everything else — importing the TMP Essential Resources first when the project does
    /// not have them yet (the Kit's TMP text needs the project-side TMP Settings asset, and
    /// a fresh checkout has none). The scene asset is committed; rebuild it here after
    /// changing what the scene must contain. The rewrite is not byte-stable, so a rebuild
    /// that changed nothing on purpose leaves a scene diff to discard.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        private const string k_ScenePath = "Assets/Sandbox/SandboxScene.unity";
        private const string k_TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Axyl/UI Kit Sandbox/Build Scene")]
        public static void Build()
        {
            if (File.Exists(k_TmpSettingsPath))
            {
                BuildScene();
                ExitIfBatch(0);
                return;
            }

            // The import lands asynchronously, so the scene is built from its completion
            // callback — in batch mode the editor must keep running until then (no -quit).
            AssetDatabase.importPackageCompleted += OnTmpImported;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;
            TMPro.TMP_PackageResourceImporter.ImportResources(
                importEssentials: true, importExamples: false, interactive: false);
        }

        private static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            Debug.Log($"[SandboxSceneBuilder] Imported '{packageName}'.");
            BuildScene();
            ExitIfBatch(0);
        }

        private static void OnTmpImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            Debug.LogError($"[SandboxSceneBuilder] Importing '{packageName}' failed: {error}");
            ExitIfBatch(1);
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.12f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            new GameObject("Sandbox", typeof(SandboxController));

            EditorSceneManager.SaveScene(scene, k_ScenePath);
            Debug.Log($"[SandboxSceneBuilder] Saved {k_ScenePath}");
        }

        /// <summary>Builds a macOS player of the sandbox scene into Build/macOS — the
        /// quickest way to look at the screens outside the editor. Output is gitignored.</summary>
        [MenuItem("Axyl/UI Kit Sandbox/Build macOS Player")]
        public static void BuildMacOSPlayer()
        {
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                new[] { k_ScenePath },
                "Build/macOS/UIKitSandbox.app",
                BuildTarget.StandaloneOSX,
                BuildOptions.None);
            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[SandboxSceneBuilder] macOS player: {report.summary.result}");
            ExitIfBatch(ok ? 0 : 1);
        }

        /// <summary>Builds an iOS Xcode project of the sandbox scene into Build/iOS, for a
        /// look at the screens on a device. Set your bundle identifier and signing team in
        /// Player Settings first; archiving, signing and installing stay outside.</summary>
        [MenuItem("Axyl/UI Kit Sandbox/Build iOS Project")]
        public static void BuildIOSProject()
        {
            // Set your bundle identifier in Player Settings, or here in code:
            // PlayerSettings.SetApplicationIdentifier(
            //     UnityEditor.Build.NamedBuildTarget.iOS, "com.example.uikitsandbox");
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                new[] { k_ScenePath },
                "Build/iOS",
                BuildTarget.iOS,
                BuildOptions.None);
            var ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log($"[SandboxSceneBuilder] iOS project: {report.summary.result}");
            ExitIfBatch(ok ? 0 : 1);
        }

        private static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
