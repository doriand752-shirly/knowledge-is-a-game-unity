#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kiag.Editor
{
    public static class KiagBuildMenu
    {
        const string ScenePath = "Assets/Scenes/ShowStage.unity";

        [MenuItem("Kiag/Create ShowStage Scene")]
        public static void CreateScene()
        {
            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Kiag] ShowStage scene ready at " + ScenePath);
        }

        [MenuItem("Kiag/Build Windows Host")]
        public static void BuildWindows()
        {
            EnsureScene();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/KnowledgeIsAGame.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Kiag] Windows build: " + report.summary.result);
        }

        public static void EnsureScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var boot = new GameObject("KiagBootstrap");
            boot.AddComponent<ShowBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
            };
            EditorBuildSettings.scenes = scenes;
        }

        // Batchmode: Unity.exe -batchmode -quit -projectPath ... -executeMethod Kiag.Editor.KiagBuildMenu.BuildWindows
    }
}
#endif
