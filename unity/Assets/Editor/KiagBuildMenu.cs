#if UNITY_EDITOR
using System.IO;
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

        /// <summary>
        /// Offline RenderTexture capture of CamBroadcast after denser set build.
        /// Batchmode: -executeMethod Kiag.Editor.KiagBuildMenu.CaptureBroadcastFrame
        /// </summary>
        [MenuItem("Kiag/Capture Broadcast Frame")]
        public static void CaptureBroadcastFrame()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("CaptureStage");
            var builder = root.AddComponent<ShowStageBuilder>();
            builder.BuildNow(); // edit-mode safe (Awake may not run)
            var cam = builder.BroadcastCam;
            if (cam == null)
            {
                Debug.LogError("[Kiag] Capture failed: BroadcastCam missing");
                return;
            }

            // Show pads + demo cast badges for figurine plate
            if (builder.PadsRoot) builder.PadsRoot.gameObject.SetActive(true);
            if (builder.CastRoot)
            {
                for (int i = 0; i < 4; i++)
                {
                    var go = new GameObject("DemoCast_" + i);
                    go.transform.SetParent(builder.CastRoot, false);
                    go.transform.position = new Vector3(-3.6f + i * 2.4f, 0, 0.5f);
                    var view = go.AddComponent<CastAvatarView>();
                    view.Setup(new PlayerSnap
                    {
                        sessionId = "demo-" + i,
                        name = new[] { "Alex", "Sam", "Lou", "Max" }[i],
                        score = 100 * (i + 1),
                        characterId = ClayPalette.CastIds[i % ClayPalette.CastIds.Length],
                        color = "",
                        anim = "idle",
                        selfieDataUrl = "",
                    });
                }
            }
            // Broadcast lobby plate (cast mid-frame) — hide overlay chrome
            builder.SetOverlayVisible(false);
            if (builder.PadsRoot) builder.PadsRoot.gameObject.SetActive(true);
            cam = builder.BroadcastCam;

            // -nographics batchmode crashes on soft shadow casters — disable for capture.
            var prevShadows = QualitySettings.shadows;
            QualitySettings.shadows = ShadowQuality.Disable;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                l.shadows = LightShadows.None;
            // Skip postFX blit path in headless (can be unstable without GPU)
            var fx = cam.GetComponent<ClayPostFx>();
            if (fx) fx.enabled = false;

            const int w = 1920;
            const int h = 1080;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);
            QualitySettings.shadows = prevShadows;

            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "broadcast-frame.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Kiag] Broadcast frame saved: " + path);

            // Keep scene file intact for builds
            EnsureScene();
            AssetDatabase.SaveAssets();
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
