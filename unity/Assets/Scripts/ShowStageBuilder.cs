using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kiag
{
    /// <summary>
    /// Builds the claymation TV set at runtime (original art — no Sony assets).
    /// Attach to an empty scene; creates floor, curtains, doors, pads, Maxime, pyramid, UI, cameras.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ShowStageBuilder : MonoBehaviour
    {
        public Transform PadsRoot { get; private set; }
        public Transform DoorsRoot { get; private set; }
        public Transform CastRoot { get; private set; }
        public Transform PyramidRoot { get; private set; }
        public Transform MaximeRoot { get; private set; }
        public Transform ThrowVfxRoot { get; private set; }
        public Camera BroadcastCam { get; private set; }
        public Camera DoorCam { get; private set; }
        public Camera PadCam { get; private set; }
        public Camera PyramidCam { get; private set; }

        public TMP_Text BrandText { get; private set; }
        public TMP_Text RoomCodeText { get; private set; }
        public TMP_Text HostLineText { get; private set; }
        public TMP_Text PhaseText { get; private set; }
        public TMP_Text QuestionText { get; private set; }
        public TMP_Text RevealText { get; private set; }
        public TMP_Text TimerText { get; private set; }
        public Button StartButton { get; private set; }
        public Button ModeClassique { get; private set; }
        public Button ModeDecades { get; private set; }
        public Button ModeRapide { get; private set; }
        public Toggle PowerPlaysToggle { get; private set; }

        void Awake()
        {
            BuildLighting();
            BuildSet();
            BuildDoors();
            BuildPads();
            BuildMaxime();
            BuildPyramid();
            BuildCastRoot();
            BuildCameras();
            BuildUi();
            ThrowVfxRoot = new GameObject("ThrowVfxRoot").transform;
            ThrowVfxRoot.SetParent(transform, false);
        }

        void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.9f, 0.75f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.2f, 0.15f);

            var key = NewLight("KeyLight", LightType.Spot, ClayPalette.ClayWhite, 2.2f, 45f);
            key.transform.position = new Vector3(0, 8, -4);
            key.transform.rotation = Quaternion.Euler(50, 0, 0);

            var fill = NewLight("FillLight", LightType.Point, ClayPalette.NeonCyan, 1.4f, 0);
            fill.transform.position = new Vector3(-5, 4, 2);

            var rim = NewLight("RimLight", LightType.Point, ClayPalette.NeonMagenta, 1.6f, 0);
            rim.transform.position = new Vector3(5, 5, 3);

            var strobe = NewLight("Strobe", LightType.Point, ClayPalette.NeonLime, 0.8f, 0);
            strobe.transform.position = new Vector3(0, 6, -1);
            strobe.gameObject.AddComponent<ClayStrobe>();
        }

        Light NewLight(string name, LightType type, Color color, float intensity, float spotAngle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = 20f;
            if (type == LightType.Spot) l.spotAngle = spotAngle;
            return l;
        }

        void BuildSet()
        {
            var floor = Prim("Floor", PrimitiveType.Cube, ClayPalette.StageFloor, new Vector3(0, -0.05f, 2), new Vector3(18, 0.1f, 14));
            floor.transform.SetParent(transform, false);

            var wall = Prim("BackWall", PrimitiveType.Cube, ClayPalette.BackWall, new Vector3(0, 4, 8), new Vector3(18, 8, 0.3f));
            wall.transform.SetParent(transform, false);

            var curtainL = Prim("CurtainL", PrimitiveType.Cube, ClayPalette.Curtain, new Vector3(-8.5f, 4, 4), new Vector3(1.2f, 8, 6));
            curtainL.transform.SetParent(transform, false);
            var curtainR = Prim("CurtainR", PrimitiveType.Cube, ClayPalette.Curtain, new Vector3(8.5f, 4, 4), new Vector3(1.2f, 8, 6));
            curtainR.transform.SetParent(transform, false);

            // Soft neon arches
            for (int i = 0; i < 5; i++)
            {
                var c = i % 2 == 0 ? ClayPalette.NeonCyan : ClayPalette.NeonMagenta;
                var bulb = Prim($"Neon_{i}", PrimitiveType.Sphere, c, new Vector3(-6 + i * 3f, 7.2f, 7.6f), Vector3.one * 0.45f);
                bulb.transform.SetParent(transform, false);
            }

            // Desk / podium for Maxime
            var desk = Prim("Desk", PrimitiveType.Cube, ClayPalette.Hex("#8B5A2B"), new Vector3(0, 0.6f, 6.2f), new Vector3(3.2f, 1.2f, 1.2f));
            desk.transform.SetParent(transform, false);
        }

        void BuildDoors()
        {
            DoorsRoot = new GameObject("Doors").transform;
            DoorsRoot.SetParent(transform, false);
            DoorsRoot.position = new Vector3(0, 0, 5.5f);
            var colors = new[] { ClayPalette.PadA, ClayPalette.PadB, ClayPalette.PadC, ClayPalette.PadD };
            for (int i = 0; i < 4; i++)
            {
                var door = Prim($"Door_{i}", PrimitiveType.Cube, colors[i], new Vector3(-4.5f + i * 3f, 2.2f, 0), new Vector3(2.2f, 4.2f, 0.35f));
                door.transform.SetParent(DoorsRoot, false);
                var slot = new GameObject("Slot").transform;
                slot.SetParent(door.transform, false);
                slot.localPosition = new Vector3(0, -1.6f, -0.8f);
                var label = TextBillboard(door.transform, $"Porte {(char)('A' + i)}", new Vector3(0, 1.6f, -0.25f));
                label.name = "Label";
            }
        }

        void BuildPads()
        {
            PadsRoot = new GameObject("Pads").transform;
            PadsRoot.SetParent(transform, false);
            PadsRoot.position = new Vector3(0, 0.02f, 0.5f);
            var colors = new[] { ClayPalette.PadA, ClayPalette.PadB, ClayPalette.PadC, ClayPalette.PadD };
            for (int i = 0; i < 4; i++)
            {
                var pad = Prim($"Pad_{i}", PrimitiveType.Cylinder, colors[i], new Vector3(-3.6f + i * 2.4f, 0.08f, 0), new Vector3(1.8f, 0.08f, 1.8f));
                pad.transform.SetParent(PadsRoot, false);
                var letter = TextBillboard(pad.transform, "" + (char)('A' + i), new Vector3(0, 0.6f, 0));
                letter.name = "Letter";
                letter.fontSize = 6;
                var label = TextBillboard(pad.transform, "…", new Vector3(0, 0.25f, 0.9f));
                label.name = "Label";
                label.fontSize = 2.2f;
                var slot = new GameObject("Slot").transform;
                slot.SetParent(pad.transform, false);
                slot.localPosition = new Vector3(0, 0.9f, 0);
                var glow = Prim("CorrectGlow", PrimitiveType.Cylinder, Color.yellow, new Vector3(0, 0.02f, 0), new Vector3(1.05f, 0.05f, 1.05f));
                glow.transform.SetParent(pad.transform, false);
                glow.SetActive(false);
                var cg = pad.AddComponent<CanvasGroup>(); // used as alpha flag via enabled/scale
                cg.alpha = 1f;
            }
            PadsRoot.gameObject.SetActive(false);
        }

        void BuildMaxime()
        {
            MaximeRoot = new GameObject("Maxime").transform;
            MaximeRoot.SetParent(transform, false);
            MaximeRoot.position = new Vector3(0, 0, 5.5f);

            var body = Prim("Body", PrimitiveType.Capsule, ClayPalette.MaximeSuit, new Vector3(0, 1.3f, 0), new Vector3(0.9f, 1.1f, 0.7f));
            body.transform.SetParent(MaximeRoot, false);
            var head = Prim("Head", PrimitiveType.Sphere, ClayPalette.MaximeSkin, new Vector3(0, 2.55f, 0), Vector3.one * 0.85f);
            head.transform.SetParent(MaximeRoot, false);
            var mic = Prim("Mic", PrimitiveType.Cylinder, Color.black, new Vector3(0.55f, 1.7f, -0.35f), new Vector3(0.08f, 0.45f, 0.08f));
            mic.transform.SetParent(MaximeRoot, false);
            mic.transform.rotation = Quaternion.Euler(0, 0, 25);
            var hair = Prim("Hair", PrimitiveType.Sphere, ClayPalette.Hex("#3D2B1F"), new Vector3(0, 2.85f, 0.05f), new Vector3(0.9f, 0.45f, 0.9f));
            hair.transform.SetParent(MaximeRoot, false);
            MaximeRoot.gameObject.AddComponent<MaximeHost>();
        }

        void BuildPyramid()
        {
            PyramidRoot = new GameObject("Pyramid").transform;
            PyramidRoot.SetParent(transform, false);
            PyramidRoot.position = new Vector3(0, 0, 3);
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float w = 8f - i * 0.85f;
                var step = Prim($"Step_{i}", PrimitiveType.Cube, Color.Lerp(ClayPalette.PyramidGold, ClayPalette.Hex("#B8860B"), i / (float)steps),
                    new Vector3(0, 0.25f + i * 0.55f, i * 0.35f), new Vector3(w, 0.5f, 1.2f));
                step.transform.SetParent(PyramidRoot, false);
                var slot = new GameObject("Slot").transform;
                slot.SetParent(step.transform, false);
                slot.localPosition = new Vector3(0, 0.6f, 0);
            }
            PyramidRoot.gameObject.SetActive(false);
        }

        void BuildCastRoot()
        {
            CastRoot = new GameObject("CastRoot").transform;
            CastRoot.SetParent(transform, false);
            CastRoot.position = new Vector3(0, 0, -1.5f);
        }

        void BuildCameras()
        {
            BroadcastCam = MakeCam("CamBroadcast", new Vector3(0, 5.5f, -9), Quaternion.Euler(18, 0, 0), true);
            DoorCam = MakeCam("CamDoors", new Vector3(0, 3.2f, 1.5f), Quaternion.Euler(8, 0, 0), false);
            PadCam = MakeCam("CamPads", new Vector3(0, 4.5f, -5), Quaternion.Euler(28, 0, 0), false);
            PyramidCam = MakeCam("CamPyramid", new Vector3(0, 3.5f, -2), Quaternion.Euler(12, 0, 0), false);
            gameObject.AddComponent<CameraDirector>().Bind(BroadcastCam, DoorCam, PadCam, PyramidCam);
        }

        Camera MakeCam(string name, Vector3 pos, Quaternion rot, bool enabled)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ClayPalette.Hex("#0D1B2A");
            cam.fieldOfView = 50;
            cam.enabled = enabled;
            go.AddComponent<AudioListener>().enabled = enabled;
            return cam;
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("ShowCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            BrandText = UiText(canvasGo.transform, "Brand", KiagConfig.Brand, 28, TextAnchor.UpperLeft, new Vector2(24, -18));
            RoomCodeText = UiText(canvasGo.transform, "Room", "Salle —", 36, TextAnchor.UpperRight, new Vector2(-24, -18));
            PhaseText = UiText(canvasGo.transform, "Phase", "lobby", 22, TextAnchor.UpperCenter, new Vector2(0, -18));
            TimerText = UiText(canvasGo.transform, "Timer", "", 48, TextAnchor.UpperCenter, new Vector2(0, -56));
            HostLineText = UiText(canvasGo.transform, "HostLine", "Bienvenue sur le plateau !", 26, TextAnchor.LowerCenter, new Vector2(0, 96));
            QuestionText = UiText(canvasGo.transform, "Question", "", 30, TextAnchor.LowerCenter, new Vector2(0, 48));
            RevealText = UiText(canvasGo.transform, "Reveal", "", 28, TextAnchor.MiddleCenter, new Vector2(0, 120));

            var panel = new GameObject("LobbyPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 0);
            prt.pivot = new Vector2(0, 0);
            prt.anchoredPosition = new Vector2(24, 24);
            prt.sizeDelta = new Vector2(420, 160);

            ModeClassique = UiButton(panel.transform, "Classique", new Vector2(0, 110));
            ModeDecades = UiButton(panel.transform, "Décades", new Vector2(140, 110));
            ModeRapide = UiButton(panel.transform, "Rapide", new Vector2(280, 110));
            StartButton = UiButton(panel.transform, "Démarrer", new Vector2(0, 50));
            StartButton.GetComponent<Image>().color = ClayPalette.PadA;

            var togGo = new GameObject("PPToggle");
            togGo.transform.SetParent(panel.transform, false);
            var trt = togGo.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(180, 55);
            trt.sizeDelta = new Vector2(180, 28);
            PowerPlaysToggle = togGo.AddComponent<Toggle>();
            PowerPlaysToggle.isOn = true;
            var togLabel = UiText(togGo.transform, "PPLabel", "Power Plays", 18, TextAnchor.MiddleLeft, Vector2.zero);
            if (togLabel != null) togLabel.rectTransform.sizeDelta = new Vector2(160, 28);
        }

        TMP_Text UiText(Transform parent, string name, string text, float size, TextAnchor anchor, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp;
            try
            {
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Kiag] TMP UI failed (import TextMesh Pro Essential Resources): " + e.Message);
                Destroy(go);
                return null;
            }
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(900, 60);
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    tmp.alignment = TextAlignmentOptions.Left;
                    break;
                case TextAnchor.UpperRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    tmp.alignment = TextAlignmentOptions.Right;
                    break;
                case TextAnchor.UpperCenter:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    break;
                case TextAnchor.LowerCenter:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
            }
            rt.anchoredPosition = pos;
            return tmp;
        }

        Button UiButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ClayPalette.BackWall;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchoredPosition = pos;
            var t = UiText(go.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero);
            if (t != null)
            {
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = Vector2.zero;
                t.rectTransform.offsetMax = Vector2.zero;
            }
            return btn;
        }

        static GameObject Prim(string name, PrimitiveType type, Color color, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend) rend.sharedMaterial = ClayPalette.MakeClayMat(color);
            var col = go.GetComponent<Collider>();
            if (col) Object.Destroy(col);
            return go;
        }

        static TextMeshPro TextBillboard(Transform parent, string text, Vector3 localPos)
        {
            var go = new GameObject("TMP");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMeshPro tmp;
            try
            {
                tmp = go.AddComponent<TextMeshPro>();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Kiag] TMP billboard failed (import TextMesh Pro Essential Resources): " + e.Message);
                Object.Destroy(go);
                return null;
            }
            tmp.text = text;
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(3, 1);
            return tmp;
        }
    }

    public class ClayStrobe : MonoBehaviour
    {
        Light _light;
        float _t;
        void Awake() => _light = GetComponent<Light>();
        void Update()
        {
            _t += Time.deltaTime;
            if (_light) _light.intensity = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(_t * 3.2f));
        }
    }
}
