using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Clean TV plate grab: hide overlay chrome, wait a frame, ScreenCapture, optional quit.
    /// Launch: KnowledgeIsAGame.exe -kiagCapturePlate [outPath]
    /// </summary>
    public class PlateCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeArm()
        {
            var args = Environment.GetCommandLineArgs();
            int idx = Array.FindIndex(args, a => string.Equals(a, "-kiagCapturePlate", StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return;
            var go = new GameObject("PlateCapture");
            DontDestroyOnLoad(go);
            var cap = go.AddComponent<PlateCapture>();
            if (idx + 1 < args.Length && !args[idx + 1].StartsWith("-"))
                cap._path = args[idx + 1];
        }

        string _path;

        IEnumerator Start()
        {
            // Wait for stage bootstrap
            for (int i = 0; i < 90 && FindFirstObjectByType<ShowStageBuilder>() == null; i++)
                yield return null;

            var stage = FindFirstObjectByType<ShowStageBuilder>();
            if (stage != null)
            {
                stage.BuildNow();
                stage.SetOverlayVisible(false);
                if (stage.PadsRoot) stage.PadsRoot.gameObject.SetActive(true);
                if (stage.BroadcastCam)
                {
                    foreach (var c in Camera.allCameras)
                        if (c) c.enabled = false;
                    stage.BroadcastCam.enabled = true;
                    var al = stage.BroadcastCam.GetComponent<AudioListener>();
                    if (al) al.enabled = true;
                }
            }

            // Settle lighting / postFX
            yield return new WaitForSeconds(0.6f);
            yield return new WaitForEndOfFrame();

            if (string.IsNullOrEmpty(_path))
            {
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _path = Path.Combine(dir, "clay-plate.png");
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

            int w = Mathf.Max(1280, Screen.width);
            int h = Mathf.Max(720, Screen.height);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
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
            Debug.Log("[Kiag] Plate capture → " + _path + " (" + w + "x" + h + ")");

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
