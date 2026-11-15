using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kiag
{
    /// <summary>
    /// Clay/figurine puppet with foot name/score badge (It's Quiz Time–style plate).
    /// Original cast art only.
    /// </summary>
    public class CastAvatarView : MonoBehaviour
    {
        TextMeshProUGUI _badgeName;
        TextMeshProUGUI _badgeScore;
        RawImage _badgeSelfie;
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
        Transform _badgeRoot;

        public string SessionId { get; private set; }
        public string CharacterId => _characterId;

        public void Setup(PlayerSnap p)
        {
            SessionId = p.sessionId;
            BuildClayBody(p);
            BuildFootBadge();
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
            _body.localPosition = new Vector3(0, 0.95f, 0);
            _body.localScale = new Vector3(0.82f, 0.85f, 0.62f);
            _bodyRend = _body.GetComponent<Renderer>();
            _bodyRend.sharedMaterial = ClayPalette.MakeClayMat(color, 0.3f);
            DestroyCollider(_body.gameObject);

            var belt = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            belt.name = "Belt";
            belt.SetParent(transform, false);
            belt.localPosition = new Vector3(0, 0.55f, 0);
            belt.localScale = new Vector3(0.78f, 0.12f, 0.58f);
            belt.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakePlasticMat(Color.Lerp(color, Color.black, 0.35f), 0.45f);
            DestroyCollider(belt.gameObject);

            // Skin-ish head with soft SSS glow
            var skin = Color.Lerp(ClayPalette.MaximeSkin, color, 0.18f);
            _head = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _head.name = "Head";
            _head.SetParent(transform, false);
            _head.localPosition = new Vector3(0, 1.98f, 0);
            _head.localScale = Vector3.one * 0.78f;
            _headRend = _head.GetComponent<Renderer>();
            _headRend.sharedMaterial = ClayPalette.MakeSkinMat(skin);
            DestroyCollider(_head.gameObject);

            var eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            eyeL.name = "EyeL";
            eyeL.SetParent(transform, false);
            eyeL.localPosition = new Vector3(-0.17f, 2.05f, -0.32f);
            eyeL.localScale = Vector3.one * 0.13f;
            eyeL.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakePlasticMat(Color.white, 0.7f);
            DestroyCollider(eyeL.gameObject);
            var eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            eyeR.name = "EyeR";
            eyeR.SetParent(transform, false);
            eyeR.localPosition = new Vector3(0.17f, 2.05f, -0.32f);
            eyeR.localScale = Vector3.one * 0.13f;
            eyeR.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakePlasticMat(Color.white, 0.7f);
            DestroyCollider(eyeR.gameObject);

            var pupilL = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            pupilL.name = "PupilL";
            pupilL.SetParent(transform, false);
            pupilL.localPosition = new Vector3(-0.17f, 2.05f, -0.38f);
            pupilL.localScale = Vector3.one * 0.055f;
            pupilL.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(Color.black, 0.1f);
            DestroyCollider(pupilL.gameObject);
            var pupilR = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            pupilR.name = "PupilR";
            pupilR.SetParent(transform, false);
            pupilR.localPosition = new Vector3(0.17f, 2.05f, -0.38f);
            pupilR.localScale = Vector3.one * 0.055f;
            pupilR.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(Color.black, 0.1f);
            DestroyCollider(pupilR.gameObject);

            var accent = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            accent.name = "Accent";
            accent.SetParent(transform, false);
            accent.localPosition = new Vector3(0, 2.4f, 0);
            accent.localScale = new Vector3(0.62f, 0.3f, 0.62f);
            accent.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(Color.Lerp(color, Color.black, 0.22f), 0.25f);
            DestroyCollider(accent.gameObject);

            var armL = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            armL.name = "ArmL";
            armL.SetParent(transform, false);
            armL.localPosition = new Vector3(-0.58f, 1.05f, 0);
            armL.localScale = new Vector3(0.24f, 0.42f, 0.24f);
            armL.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(color, 0.3f);
            DestroyCollider(armL.gameObject);
            var armR = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            armR.name = "ArmR";
            armR.SetParent(transform, false);
            armR.localPosition = new Vector3(0.58f, 1.05f, 0);
            armR.localScale = new Vector3(0.24f, 0.42f, 0.24f);
            armR.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeClayMat(color, 0.3f);
            DestroyCollider(armR.gameObject);

            // Soft contact shadow disc under feet
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            shadow.name = "ContactShadow";
            shadow.SetParent(transform, false);
            shadow.localPosition = new Vector3(0, 0.02f, 0);
            shadow.localScale = new Vector3(0.95f, 0.01f, 0.7f);
            var shMat = ClayPalette.MakeClayMat(new Color(0, 0, 0, 0.35f), 0f);
            shMat.color = new Color(0.05f, 0.04f, 0.06f, 0.4f);
            shadow.GetComponent<Renderer>().sharedMaterial = shMat;
            DestroyCollider(shadow.gameObject);
        }

        void BuildFootBadge()
        {
            _badgeRoot = new GameObject("FootBadge").transform;
            _badgeRoot.SetParent(transform, false);
            _badgeRoot.localPosition = new Vector3(0, 0.08f, -0.55f);
            _badgeRoot.localRotation = Quaternion.Euler(18, 0, 0);

            var canvasGo = new GameObject("BadgeCanvas");
            canvasGo.transform.SetParent(_badgeRoot, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 22f;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 160);
            canvasGo.transform.localScale = Vector3.one * 0.0085f;

            // Outer gold ring
            var ring = BadgeCircle(canvasGo.transform, "Ring", new Vector2(0, 0), new Vector2(150, 150), ClayPalette.PyramidGold);
            // White plate
            BadgeCircle(canvasGo.transform, "Plate", new Vector2(0, 8), new Vector2(118, 118), ClayPalette.ClayWhite);
            // Selfie disc
            var selfieGo = new GameObject("Selfie");
            selfieGo.transform.SetParent(canvasGo.transform, false);
            _badgeSelfie = selfieGo.AddComponent<RawImage>();
            _badgeSelfie.color = ClayPalette.Hex("#DDE3EA");
            var srt = _badgeSelfie.rectTransform;
            srt.sizeDelta = new Vector2(78, 78);
            srt.anchoredPosition = new Vector2(0, 28);

            _badgeName = BadgeTmp(canvasGo.transform, "Name", "", 18, new Vector2(0, -28), ClayPalette.BadgeInk);
            _badgeScore = BadgeTmp(canvasGo.transform, "Score", "0", 22, new Vector2(0, -52), ClayPalette.Hex("#C45C26"));

            // Soft stand under badge
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            stand.name = "BadgeStand";
            stand.SetParent(_badgeRoot, false);
            stand.localPosition = new Vector3(0, -0.02f, 0.02f);
            stand.localScale = new Vector3(0.55f, 0.03f, 0.55f);
            stand.GetComponent<Renderer>().sharedMaterial = ClayPalette.MakeGoldMat(ClayPalette.FloorGold, 0.6f);
            DestroyCollider(stand.gameObject);

            // Keep ring ref unused warning quiet
            _ = ring;
        }

        static Image BadgeCircle(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            // Unity default UI sprite is a rounded square; scale reads as disc at distance
            var rt = img.rectTransform;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return img;
        }

        static TextMeshProUGUI BadgeTmp(Transform parent, string name, string text, float size, Vector2 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(140, 36);
            tmp.rectTransform.anchoredPosition = pos;
            return tmp;
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
        }

        public void Refresh(PlayerSnap p)
        {
            SessionId = p.sessionId;
            _characterId = p.characterId;
            _anim = p.anim ?? "idle";

            if (_badgeName) _badgeName.text = string.IsNullOrEmpty(p.name) ? "—" : p.name;
            if (_badgeScore) _badgeScore.text = p.score.ToString();

            var color = ClayPalette.ForCast(p.characterId);
            if (!string.IsNullOrEmpty(p.color) && ColorUtility.TryParseHtmlString(p.color, out var parsed))
                color = parsed;
            if (_bodyRend) _bodyRend.sharedMaterial = ClayPalette.MakeClayMat(color, 0.3f);

            ApplySelfie(p.selfieDataUrl);
        }

        void ApplySelfie(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith("data:image"))
                return;
            try
            {
                var b64 = dataUrl.Substring(dataUrl.IndexOf(',') + 1);
                var bytes = Convert.FromBase64String(b64);
                if (_selfieTex == null) _selfieTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _selfieTex.LoadImage(bytes);
                if (_badgeSelfie)
                {
                    _badgeSelfie.texture = _selfieTex;
                    _badgeSelfie.color = Color.white;
                }
                if (_headRend != null)
                {
                    var mat = ClayPalette.MakeSkinMat(Color.white, 0.35f);
                    mat.mainTexture = _selfieTex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", _selfieTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", _selfieTex);
                    _headRend.sharedMaterial = mat;
                }
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
            var lateral = (SessionId?.GetHashCode() ?? 0) % 5 - 2;
            MoveToWorld(slot.position + new Vector3(lateral * 0.45f, 0, 0), "climb");
        }

        void LateUpdate()
        {
            // Billboard badge toward active camera
            if (_badgeRoot == null) return;
            var cam = Camera.main;
            if (!cam)
            {
                foreach (var c in Camera.allCameras)
                {
                    if (c && c.enabled) { cam = c; break; }
                }
            }
            if (!cam) return;
            var flat = cam.transform.position - _badgeRoot.position;
            flat.y = 0;
            if (flat.sqrMagnitude > 0.001f)
                _badgeRoot.rotation = Quaternion.LookRotation(-flat.normalized) * Quaternion.Euler(12, 0, 0);
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
