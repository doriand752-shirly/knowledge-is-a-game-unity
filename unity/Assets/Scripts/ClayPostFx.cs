using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Built-in RP figurine look: subtle bloom + soft far DOF/bokeh.
    /// Attach to each show camera (no Post Processing package required).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class ClayPostFx : MonoBehaviour
    {
        [Range(0.2f, 1.2f)] public float bloomThreshold = 0.62f;
        [Range(0f, 1.5f)] public float bloomIntensity = 0.38f;
        [Range(0.5f, 3f)] public float blurSize = 1.35f;
        public float focusDistance = 7.2f;
        public float focusRange = 5.5f;

        Material _mat;
        Camera _cam;

        void OnEnable()
        {
            _cam = GetComponent<Camera>();
            _cam.depthTextureMode |= DepthTextureMode.Depth;
            _cam.allowHDR = true;
            EnsureMat();
        }

        void OnDisable()
        {
            if (_mat) DestroyImmediate(_mat);
            _mat = null;
        }

        void EnsureMat()
        {
            if (_mat) return;
            // Resources mat keeps Hidden shader from being stripped in player builds.
            var prefab = Resources.Load<Material>("KiagClayPostFx");
            if (prefab != null)
            {
                _mat = new Material(prefab) { hideFlags = HideFlags.HideAndDontSave };
                return;
            }
            var shader = Shader.Find("Hidden/Kiag/ClayPostFx")
                         ?? Shader.Find("Kiag/ClayPostFx");
            if (!shader)
            {
                Debug.LogWarning("[Kiag] ClayPostFx shader missing (add Resources/KiagClayPostFx)");
                return;
            }
            _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            EnsureMat();
            if (_mat == null)
            {
                Graphics.Blit(src, dst);
                return;
            }

            _mat.SetFloat("_Threshold", bloomThreshold);
            _mat.SetFloat("_Intensity", bloomIntensity);
            _mat.SetFloat("_BlurSize", blurSize);
            _mat.SetFloat("_FocusDistance", focusDistance);
            _mat.SetFloat("_FocusRange", focusRange);

            int w = Mathf.Max(8, src.width / 2);
            int h = Mathf.Max(8, src.height / 2);
            var bright = RenderTexture.GetTemporary(w, h, 0, src.format);
            var bloomA = RenderTexture.GetTemporary(w, h, 0, src.format);
            var bloomB = RenderTexture.GetTemporary(w, h, 0, src.format);
            var dofA = RenderTexture.GetTemporary(w, h, 0, src.format);
            var dofB = RenderTexture.GetTemporary(w, h, 0, src.format);

            Graphics.Blit(src, bright, _mat, 0);
            Graphics.Blit(bright, bloomA, _mat, 1);
            Graphics.Blit(bloomA, bloomB, _mat, 2);

            Graphics.Blit(src, dofA, _mat, 1);
            Graphics.Blit(dofA, dofB, _mat, 2);

            _mat.SetTexture("_BloomTex", bloomB);
            _mat.SetTexture("_BlurredTex", dofB);
            Graphics.Blit(src, dst, _mat, 3);

            RenderTexture.ReleaseTemporary(bright);
            RenderTexture.ReleaseTemporary(bloomA);
            RenderTexture.ReleaseTemporary(bloomB);
            RenderTexture.ReleaseTemporary(dofA);
            RenderTexture.ReleaseTemporary(dofB);
        }

        /// <summary>Tune DOF focus for a given shot distance (meters).</summary>
        public void SetFocus(float distance, float range = -1f)
        {
            focusDistance = distance;
            if (range > 0) focusRange = range;
        }
    }
}
