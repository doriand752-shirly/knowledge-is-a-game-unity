using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Clean TV plate grab: hide overlay chrome, wait a frame, ScreenCapture, optional quit.
    /// Launch: KnowledgeIsAGame.exe -kiagCapturePlate [outPath]
    /// UI proof: KnowledgeIsAGame.exe -kiagCaptureUi [outPath]  (lobby buttons visible)
    /// </summary>
    public class PlateCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeArm()
        {
            var args = Environment.GetCommandLineArgs();
            int plateIdx = Array.FindIndex(args, a => string.Equals(a, "-kiagCapturePlate", StringComparison.OrdinalIgnoreCase));
            int uiIdx = Array.FindIndex(args, a => string.Equals(a, "-kiagCaptureUi", StringComparison.OrdinalIgnoreCase));
            if (plateIdx < 0 && uiIdx < 0) return;

            var go = new GameObject("PlateCapture");
            DontDestroyOnLoad(go);
            var cap = go.AddComponent<PlateCapture>();
            cap._uiMode = uiIdx >= 0;
            int idx = cap._uiMode ? uiIdx : plateIdx;
            if (idx + 1 < args.Length && !args[idx + 1].StartsWith("-"))
                cap._path = args[idx + 1];
        }

        string _path;
        bool _uiMode;

        IEnumerator Start()
        {
            // Wait for stage bootstrap
            for (int i = 0; i < 90 && FindFirstObjectByType<ShowStageBuilder>() == null; i++)
                yield return null;

            var stage = FindFirstObjectByType<ShowStageBuilder>();
            if (stage != null)
            {
                stage.BuildNow();
                stage.SetOverlayVisible(_uiMode);
                if (stage.PadsRoot) stage.PadsRoot.gameObject.SetActive(true);
                if (stage.BroadcastCam)
                {
                    foreach (var c in Camera.allCameras)
                        if (c) c.enabled = false;
                    stage.BroadcastCam.enabled = true;
                    var al = stage.BroadcastCam.GetComponent<AudioListener>();
                    if (al) al.enabled = true;
                }

                if (_uiMode && stage.ShowCanvas != null)
                {
                    // ScreenSpaceOverlay is invisible to cam.Render — bind UI to broadcast cam.
                    // Kill bloom/DOF so labels stay sharp (postFX would wash cream/dark ink).
                    if (stage.BroadcastCam != null)
                    {
                        var fx = stage.BroadcastCam.GetComponent<ClayPostFx>();
                        if (fx) fx.enabled = false;
                    }
                    var canvas = stage.ShowCanvas.GetComponent<Canvas>();
                    if (canvas != null && stage.BroadcastCam != null)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = stage.BroadcastCam;
                        canvas.planeDistance = 1.05f;
                    }
                    if (stage.RoomCodeText) stage.RoomCodeText.text = "Salle DEMO";
                    if (stage.PhaseText) stage.PhaseText.text = "lobby";
                    if (stage.HostLineText) stage.HostLineText.text = "Bienvenue sur le plateau !";
                    if (stage.BrandText) stage.BrandText.text = KiagConfig.Brand;
                }
            }

            // Settle lighting / postFX / TMP mesh
            yield return new WaitForSeconds(0.7f);
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            yield return null;

            if (string.IsNullOrEmpty(_path))
            {
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _path = Path.Combine(dir, _uiMode ? "host-ui-buttons.png" : "clay-plate.png");
            }
            else
            {
                _path = Path.GetFullPath(_path);
                var parent = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            }

            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Camera.allCameras)
                    if (c && c.enabled) { cam = c; break; }
            }
            if (cam == null)
            {
                Debug.LogError("[Kiag] Plate capture: no camera");
                QuitNow();
                yield break;
            }

            // Fixed 16:9 TV plate — ignore window aspect (narrow windows were side-cropping cast).
            const int w = 1920;
            const int h = 1080;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.aspect = w / (float)h;
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            File.WriteAllBytes(_path, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
            Debug.Log("[Kiag] " + (_uiMode ? "UI" : "Plate") + " capture → " + _path + " (" + w + "x" + h + ")");

            yield return null;
            QuitNow();
        }

        static void QuitNow()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            Application.Quit(0);
#endif
        }
    }
}
