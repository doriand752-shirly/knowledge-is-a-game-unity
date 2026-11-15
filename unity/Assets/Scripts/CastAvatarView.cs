using System;
using UnityEngine;
using TMPro;

namespace Kiag
{
    /// <summary>3D clay puppet with optional selfie face texture.</summary>
    public class CastAvatarView : MonoBehaviour
    {
        TextMeshPro _name;
        Transform _body;
        Transform _head;
        Renderer _bodyRend;
        Renderer _headRend;
        string _characterId;
        Vector3 _target;
        bool _hasTarget;
        string _anim = "idle";
        float _stepClock;
        Texture2D _selfieTex;

        public string SessionId { get; private set; }
        public string CharacterId => _characterId;

        public void Setup(PlayerSnap p)
        {
            SessionId = p.sessionId;
            BuildClayBody(p);
            Refresh(p);
        }

        void BuildClayBody(PlayerSnap p)
        {
            var color = ClayPalette.ForCast(p.characterId);
            if (!string.IsNullOrEmpty(p.color) && ColorUtility.TryParseHtmlString(p.color, out var c))
                color = c;

            _body = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            _body.name = "Body";
            _body.SetParent(transform, false);
            _body.localPosition = new Vector3(0, 0.9f, 0);
            _body.localScale = new Vector3(0.7f, 0.75f, 0.55f);
            _bodyRend = _body.GetComponent<Renderer>();
            _bodyRend.sharedMaterial = ClayPalette.MakeClayMat(color);
            DestroyCollider(_body.gameObject);

            _head = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _head.name = "Head";
            _head.SetParent(transform, false);
            _head.localPosition = new Vector3(0, 1.85f, 0);
            _head.localScale = Vector3.one * 0.7f;
            _headRend = _head.GetComponent<Renderer>();
            _headRend.sharedMaterial = ClayPalette.MakeClayMat(Color.Lerp(color, Color.white, 0.35f));
            DestroyCollider(_head.gameObject);

            // Soft hat / silhouette accent per cast
            var accent = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            accent.name = "Accent";
            accent.SetParent(transform, false);
            accent.localPosition = new Vector3(0, 2.2f, 0);
            accent.localScale = new Vector3(0.55f, 0.25f, 0.55f);
            accent.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(Color.Lerp(color, Color.black, 0.25f));
            DestroyCollider(accent.gameObject);

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(transform, false);
            nameGo.transform.localPosition = new Vector3(0, 2.55f, 0);
            _name = nameGo.AddComponent<TextMeshPro>();
            _name.fontSize = 3.2f;
            _name.alignment = TextAlignmentOptions.Center;
            _name.color = Color.white;
            _name.rectTransform.sizeDelta = new Vector2(3f, 1f);
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
        }

        public void Refresh(PlayerSnap p)
        {
            SessionId = p.sessionId;
            if (_name) _name.text = $"{p.name}\n{p.score}";
            _characterId = p.characterId;
            _anim = p.anim ?? "idle";

            var color = ClayPalette.ForCast(p.characterId);
            if (!string.IsNullOrEmpty(p.color) && ColorUtility.TryParseHtmlString(p.color, out var parsed))
                color = parsed;
            if (_bodyRend) _bodyRend.sharedMaterial = ClayPalette.MakeClayMat(color);

            ApplySelfie(p.selfieDataUrl);
        }

        void ApplySelfie(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith("data:image") || _headRend == null)
                return;
            try
            {
                var b64 = dataUrl.Substring(dataUrl.IndexOf(',') + 1);
                var bytes = Convert.FromBase64String(b64);
                if (_selfieTex == null) _selfieTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _selfieTex.LoadImage(bytes);
                var mat = ClayPalette.MakeClayMat(Color.white, 0.25f);
                mat.mainTexture = _selfieTex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", _selfieTex);
                _headRend.sharedMaterial = mat;
            }
            catch
            {
                /* ignore bad selfie */
            }
        }

        public void MoveTo(Transform slot, string anim)
        {
            _anim = anim ?? "walk";
            if (slot == null) return;
            _target = slot.position;
            _hasTarget = true;
        }

        public void MoveToWorld(Vector3 world, string anim)
        {
            _anim = anim ?? "walk";
            _target = world;
            _hasTarget = true;
        }

        public void SetPyramidStep(int step, Transform pyramidRoot)
        {
            if (pyramidRoot == null) return;
            step = Mathf.Clamp(step, 0, Mathf.Max(0, pyramidRoot.childCount - 1));
            var stepT = pyramidRoot.GetChild(step);
            var slot = stepT.Find("Slot") ?? stepT;
            // Offset sideways by session hash so climbers don't stack
            var lateral = (SessionId?.GetHashCode() ?? 0) % 5 - 2;
            MoveToWorld(slot.position + new Vector3(lateral * 0.45f, 0, 0), "climb");
        }

        void Update()
        {
            _stepClock += Time.deltaTime;
            bool tick = Mathf.FloorToInt(_stepClock * 12f) != Mathf.FloorToInt((_stepClock - Time.deltaTime) * 12f);

            if (_hasTarget)
            {
                transform.position = Vector3.Lerp(transform.position, _target, Time.deltaTime * 5.5f);
                var flat = _target - transform.position;
                flat.y = 0;
                if (flat.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), Time.deltaTime * 8f);
            }

            if (!tick) return;

            switch (_anim)
            {
                case "correct":
                case "celebrate":
                    transform.localScale = Vector3.one * (1f + 0.1f * Mathf.Abs(Mathf.Sin(Time.time * 10f)));
                    break;
                case "wrong":
                    transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 18f) * 12f);
                    break;
                case "walk":
                case "climb":
                    transform.localScale = new Vector3(1f, 1f + 0.05f * Mathf.Sin(Time.time * 12f), 1f);
                    break;
                default:
                    transform.localScale = Vector3.one;
                    if (!_hasTarget) transform.localRotation = Quaternion.identity;
                    break;
            }
        }
    }
}
