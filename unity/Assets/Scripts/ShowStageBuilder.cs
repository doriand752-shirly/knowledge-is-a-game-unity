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
        public GameObject ShowCanvas { get; private set; }

        void Awake() => BuildNow();

        /// <summary>Idempotent set build — safe from editor capture tools (Awake may not run in edit mode).</summary>
        public void BuildNow()
        {
            if (BroadcastCam != null) return;
            BuildLighting();
            BuildSet();
            BuildDoors();
            BuildPads();
            BuildMaxime();
            BuildPyramid();
            BuildCastRoot();
            BuildCameras();
            BuildUi();
            if (ThrowVfxRoot == null)
            {
                ThrowVfxRoot = new GameObject("ThrowVfxRoot").transform;
                ThrowVfxRoot.SetParent(transform, false);
            }
        }

        void BuildLighting()
        {
            // Soft studio GI — no pitch-black figurine shadows
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.98f, 0.88f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.42f, 0.32f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = ClayPalette.BackdropSky * 0.85f;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 11f;
            RenderSettings.fogEndDistance = 28f;

            var key = NewLight("KeyLight", LightType.Spot, ClayPalette.ClayWhite, 2.4f, 55f);
            key.transform.position = new Vector3(-1.2f, 8.5f, -4.2f);
            key.transform.rotation = Quaternion.Euler(48, 12, 0);
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.45f;

            var fill = NewLight("FillLight", LightType.Point, new Color(0.85f, 0.92f, 1f), 1.5f, 0);
            fill.transform.position = new Vector3(-5.2f, 3.8f, 0.5f);
            fill.range = 16f;

            // Strong rim / halo from behind (figurine separation)
            var rim = NewLight("RimLight", LightType.Directional, new Color(1f, 0.92f, 0.98f), 1.35f, 0);
            rim.transform.rotation = Quaternion.Euler(12, 168, 0);
            rim.shadows = LightShadows.None;

            var rimKick = NewLight("RimKick", LightType.Spot, ClayPalette.NeonCyan, 2.4f, 50f);
            rimKick.transform.position = new Vector3(3.8f, 4.8f, 4.5f);
            rimKick.transform.rotation = Quaternion.Euler(25, -150, 0);
            rimKick.range = 18f;

            var deskKey = NewLight("DeskKey", LightType.Spot, ClayPalette.PyramidGold, 1.35f, 38f);
            deskKey.transform.position = new Vector3(0, 5.2f, 3.2f);
            deskKey.transform.rotation = Quaternion.Euler(55, 0, 0);

            var bounce = NewLight("FloorBounce", LightType.Point, ClayPalette.FloorStripe, 0.85f, 0);
            bounce.transform.position = new Vector3(0, 0.4f, 1.5f);
            bounce.range = 10f;

            var strobe = NewLight("Strobe", LightType.Point, ClayPalette.NeonLime, 0.55f, 0);
            strobe.transform.position = new Vector3(0, 6.5f, -0.5f);
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
            BuildStylizedBackdrop();

            var floor = Prim("Floor", PrimitiveType.Cube, ClayPalette.StageFloor, new Vector3(0, -0.05f, 2), new Vector3(18, 0.1f, 14));
            floor.transform.SetParent(transform, false);
            var fr = floor.GetComponent<Renderer>();
            if (fr) fr.sharedMaterial = ClayPalette.MakeClayMat(ClayPalette.StageFloor, 0.22f);

            // White / gold glitter stripes (figurine stage plate)
            for (int i = -6; i <= 6; i++)
            {
                bool gold = (i & 1) == 0;
                var stripe = Prim($"Stripe_{i}", PrimitiveType.Cube,
                    gold ? ClayPalette.FloorGold : ClayPalette.FloorStripe,
                    new Vector3(0, 0.02f, i * 0.95f), new Vector3(16f, 0.03f, 0.85f));
                stripe.transform.SetParent(transform, false);
                var sr = stripe.GetComponent<Renderer>();
                if (sr) sr.sharedMaterial = gold
                    ? ClayPalette.MakeGoldMat(ClayPalette.FloorGold, 0.68f)
                    : ClayPalette.MakeClayMat(ClayPalette.FloorStripe, 0.18f);
            }

            var wall = Prim("BackWall", PrimitiveType.Cube, ClayPalette.BackWall, new Vector3(0, 4, 8), new Vector3(18, 8, 0.3f));
            wall.transform.SetParent(transform, false);

            // Logo wall plaque
            var logo = Prim("LogoWall", PrimitiveType.Cube, ClayPalette.Hex("#0F2740"), new Vector3(0, 5.6f, 7.75f), new Vector3(7.5f, 2.2f, 0.25f));
            logo.transform.SetParent(transform, false);
            var logoAccent = Prim("LogoAccent", PrimitiveType.Cube, ClayPalette.NeonMagenta, new Vector3(0, 4.35f, 7.7f), new Vector3(7.6f, 0.18f, 0.2f));
            logoAccent.transform.SetParent(transform, false);
            var brand = TextBillboard(logo.transform, "QuiZène", new Vector3(0, 0.15f, -0.2f));
            if (brand != null) brand.fontSize = 8;

            // Curtain folds (layered slabs)
            for (int i = 0; i < 4; i++)
            {
                var cL = Prim($"CurtainL_{i}", PrimitiveType.Cube, Color.Lerp(ClayPalette.Curtain, Color.black, i * 0.08f),
                    new Vector3(-8.2f - i * 0.15f, 4, 2 + i * 0.9f), new Vector3(1.05f, 8.2f, 1.1f));
                cL.transform.SetParent(transform, false);
                var cR = Prim($"CurtainR_{i}", PrimitiveType.Cube, Color.Lerp(ClayPalette.Curtain, Color.black, i * 0.08f),
                    new Vector3(8.2f + i * 0.15f, 4, 2 + i * 0.9f), new Vector3(1.05f, 8.2f, 1.1f));
                cR.transform.SetParent(transform, false);
            }
            var valance = Prim("Valance", PrimitiveType.Cube, ClayPalette.Hex("#9B1238"), new Vector3(0, 7.6f, 5.5f), new Vector3(17, 0.7f, 1.2f));
            valance.transform.SetParent(transform, false);

            // Neon arches (double row)
            for (int i = 0; i < 7; i++)
            {
                var c = i % 2 == 0 ? ClayPalette.NeonCyan : ClayPalette.NeonMagenta;
                var bulb = Prim($"Neon_{i}", PrimitiveType.Sphere, c, new Vector3(-7.5f + i * 2.5f, 7.35f, 7.55f), Vector3.one * 0.42f);
                bulb.transform.SetParent(transform, false);
                var arch = Prim($"Arch_{i}", PrimitiveType.Cylinder, c,
                    new Vector3(-7.5f + i * 2.5f, 6.4f, 7.4f), new Vector3(0.18f, 0.55f, 0.18f));
                arch.transform.SetParent(transform, false);
            }

            // Side benches / audience banks
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 3; row++)
                {
                    var bench = Prim($"Bench_{side}_{row}", PrimitiveType.Cube, ClayPalette.Hex("#5C3A21"),
                        new Vector3(side * (6.2f + row * 0.35f), 0.35f + row * 0.35f, -0.5f + row * 0.8f),
                        new Vector3(1.4f, 0.35f, 4.5f));
                    bench.transform.SetParent(transform, false);
                }
            }

            // Proscenium pillars
            var pillarL = Prim("PillarL", PrimitiveType.Cylinder, ClayPalette.Hex("#F4C430"), new Vector3(-7.2f, 3.2f, 7.2f), new Vector3(0.7f, 3.2f, 0.7f));
            pillarL.transform.SetParent(transform, false);
            var pillarR = Prim("PillarR", PrimitiveType.Cylinder, ClayPalette.Hex("#F4C430"), new Vector3(7.2f, 3.2f, 7.2f), new Vector3(0.7f, 3.2f, 0.7f));
            pillarR.transform.SetParent(transform, false);

            // Desk / podium for Maxime + props
            var desk = Prim("Desk", PrimitiveType.Cube, ClayPalette.Hex("#8B5A2B"), new Vector3(0, 0.6f, 6.2f), new Vector3(3.4f, 1.2f, 1.3f));
            desk.transform.SetParent(transform, false);
            var deskTop = Prim("DeskTop", PrimitiveType.Cube, ClayPalette.Hex("#A67C52"), new Vector3(0, 1.25f, 6.2f), new Vector3(3.6f, 0.12f, 1.45f));
            deskTop.transform.SetParent(transform, false);
            var micStand = Prim("MicStand", PrimitiveType.Cylinder, Color.black, new Vector3(1.1f, 1.7f, 5.7f), new Vector3(0.06f, 0.55f, 0.06f));
            micStand.transform.SetParent(transform, false);
            var cardStack = Prim("CardStack", PrimitiveType.Cube, ClayPalette.ClayWhite, new Vector3(-0.9f, 1.4f, 6.0f), new Vector3(0.55f, 0.12f, 0.4f));
            cardStack.transform.SetParent(transform, false);
        }

        void BuildStylizedBackdrop()
        {
            // Soft sky wash + cooler far band (DOF will blur these cutouts)
            var sky = Prim("SkyBackdrop", PrimitiveType.Cube, ClayPalette.BackdropSky, new Vector3(0, 5.5f, 13.5f), new Vector3(32, 18, 0.2f));
            sky.transform.SetParent(transform, false);
            var skyR = sky.GetComponent<Renderer>();
            if (skyR) skyR.sharedMaterial = ClayPalette.MakeBackdropMat(ClayPalette.BackdropSky);

            var far = Prim("FarHills", PrimitiveType.Cube, ClayPalette.BackdropFar, new Vector3(0, 1.4f, 12.6f), new Vector3(30, 3.6f, 0.3f));
            far.transform.SetParent(transform, false);
            var farR = far.GetComponent<Renderer>();
            if (farR) farR.sharedMaterial = ClayPalette.MakeBackdropMat(ClayPalette.BackdropFar);

            // Castle / mountain silhouettes — original flat cutouts (not IP copies)
            float[] xs = { -9f, -5.5f, -2f, 2.2f, 5.8f, 9.2f };
            float[] hs = { 3.2f, 4.6f, 2.8f, 5.1f, 3.6f, 4.2f };
            for (int i = 0; i < xs.Length; i++)
            {
                var tower = Prim($"Cutout_{i}", PrimitiveType.Cube, ClayPalette.BackdropCutout,
                    new Vector3(xs[i], hs[i] * 0.5f + 0.3f, 12.1f - (i % 2) * 0.35f),
                    new Vector3(1.6f + (i % 3) * 0.35f, hs[i], 0.35f));
                tower.transform.SetParent(transform, false);
                var tr = tower.GetComponent<Renderer>();
                if (tr) tr.sharedMaterial = ClayPalette.MakeBackdropMat(
                    Color.Lerp(ClayPalette.BackdropCutout, ClayPalette.BackdropFar, (i % 3) * 0.15f));

                if (i % 2 == 0)
                {
                    var spire = Prim($"Spire_{i}", PrimitiveType.Cube, ClayPalette.BackdropCutout,
                        new Vector3(xs[i], hs[i] + 0.9f, 12.05f), new Vector3(0.55f, 1.4f, 0.3f));
                    spire.transform.SetParent(transform, false);
                    var sr = spire.GetComponent<Renderer>();
                    if (sr) sr.sharedMaterial = ClayPalette.MakeBackdropMat(ClayPalette.BackdropCutout);
                }
            }
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

            var body = Prim("Body", PrimitiveType.Capsule, ClayPalette.MaximeSuit, new Vector3(0, 1.3f, 0), new Vector3(0.95f, 1.15f, 0.75f));
            body.transform.SetParent(MaximeRoot, false);
            var lapel = Prim("Lapel", PrimitiveType.Cube, ClayPalette.Hex("#1B4332"), new Vector3(0, 1.55f, -0.32f), new Vector3(0.55f, 0.7f, 0.12f));
            lapel.transform.SetParent(MaximeRoot, false);
            var bow = Prim("BowTie", PrimitiveType.Cube, ClayPalette.NeonMagenta, new Vector3(0, 1.95f, -0.38f), new Vector3(0.35f, 0.12f, 0.12f));
            bow.transform.SetParent(MaximeRoot, false);
            var head = Prim("Head", PrimitiveType.Sphere, ClayPalette.MaximeSkin, new Vector3(0, 2.6f, 0), Vector3.one * 0.88f);
            head.transform.SetParent(MaximeRoot, false);
            var headR = head.GetComponent<Renderer>();
            if (headR) headR.sharedMaterial = ClayPalette.MakeSkinMat(ClayPalette.MaximeSkin);
            var nose = Prim("Nose", PrimitiveType.Sphere, Color.Lerp(ClayPalette.MaximeSkin, Color.red, 0.15f), new Vector3(0, 2.55f, -0.4f), Vector3.one * 0.18f);
            nose.transform.SetParent(MaximeRoot, false);
            var noseR = nose.GetComponent<Renderer>();
            if (noseR) noseR.sharedMaterial = ClayPalette.MakeSkinMat(Color.Lerp(ClayPalette.MaximeSkin, Color.red, 0.12f));
            var eyeL = Prim("EyeL", PrimitiveType.Sphere, Color.white, new Vector3(-0.2f, 2.7f, -0.35f), Vector3.one * 0.14f);
            eyeL.transform.SetParent(MaximeRoot, false);
            var eyeR = Prim("EyeR", PrimitiveType.Sphere, Color.white, new Vector3(0.2f, 2.7f, -0.35f), Vector3.one * 0.14f);
            eyeR.transform.SetParent(MaximeRoot, false);
            var pupilL = Prim("PupilL", PrimitiveType.Sphere, Color.black, new Vector3(-0.2f, 2.7f, -0.42f), Vector3.one * 0.06f);
            pupilL.transform.SetParent(MaximeRoot, false);
            var pupilR = Prim("PupilR", PrimitiveType.Sphere, Color.black, new Vector3(0.2f, 2.7f, -0.42f), Vector3.one * 0.06f);
            pupilR.transform.SetParent(MaximeRoot, false);
            var mic = Prim("Mic", PrimitiveType.Cylinder, Color.black, new Vector3(0.55f, 1.75f, -0.4f), new Vector3(0.08f, 0.45f, 0.08f));
            mic.transform.SetParent(MaximeRoot, false);
            mic.transform.rotation = Quaternion.Euler(0, 0, 25);
            var hair = Prim("Hair", PrimitiveType.Sphere, ClayPalette.Hex("#3D2B1F"), new Vector3(0, 2.95f, 0.05f), new Vector3(0.95f, 0.48f, 0.95f));
            hair.transform.SetParent(MaximeRoot, false);
            var armL = Prim("ArmL", PrimitiveType.Capsule, ClayPalette.MaximeSuit, new Vector3(-0.65f, 1.35f, 0), new Vector3(0.28f, 0.55f, 0.28f));
            armL.transform.SetParent(MaximeRoot, false);
            var armR = Prim("ArmR", PrimitiveType.Capsule, ClayPalette.MaximeSuit, new Vector3(0.65f, 1.35f, 0), new Vector3(0.28f, 0.55f, 0.28f));
            armR.transform.SetParent(MaximeRoot, false);
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
            CastRoot.position = new Vector3(0, 0, 0.4f);
            // Demo figurine lineup so the TV plate isn't empty before phones join.
            string[] names = { "Alex", "Sam", "Lou", "Max" };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("DemoCast_" + i);
                go.transform.SetParent(CastRoot, false);
                go.transform.localPosition = new Vector3(-3.6f + i * 2.4f, 0, 0);
                var view = go.AddComponent<CastAvatarView>();
                view.Setup(new PlayerSnap
                {
                    sessionId = "demo-" + i,
                    name = names[i],
                    score = (i + 1) * 120,
                    characterId = ClayPalette.CastIds[i % ClayPalette.CastIds.Length],
                    color = "",
                    anim = "idle",
                    selfieDataUrl = "",
                });
            }
        }

        void BuildCameras()
        {
            // Extra-wide plate: clear margin past side benches (cast never near frame edge).
            BroadcastCam = MakeCam("CamBroadcast", new Vector3(0, 2.7f, -11.2f), true, new Vector3(0, 1.3f, 2.2f), focus: 11.0f);
            DoorCam = MakeCam("CamDoors", new Vector3(0, 2.35f, 1.6f), false, new Vector3(0, 1.9f, 5.5f), focus: 4.4f);
            PadCam = MakeCam("CamPads", new Vector3(0, 2.35f, -7.8f), false, new Vector3(0, 1.25f, 1.0f), focus: 8.0f);
            PyramidCam = MakeCam("CamPyramid", new Vector3(0, 2.4f, -1.4f), false, new Vector3(0, 1.9f, 4.0f), focus: 5.2f);
            gameObject.AddComponent<CameraDirector>().Bind(BroadcastCam, DoorCam, PadCam, PyramidCam);
        }

        Camera MakeCam(string name, Vector3 pos, bool enabled, Vector3 lookAt, float focus)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.LookAt(lookAt);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ClayPalette.BackdropSky * 0.55f;
            cam.fieldOfView = 62; // wide plate — side benches fully inside with margin
            cam.allowHDR = true;
            cam.enabled = enabled;
            // Tag for Camera.main / plate capture
            if (enabled) go.tag = "MainCamera";
            go.AddComponent<AudioListener>().enabled = enabled;
            var fx = go.AddComponent<ClayPostFx>();
            fx.SetFocus(focus, 4.8f);
            fx.bloomIntensity = 0.36f;
            fx.bloomThreshold = 0.58f;
            return cam;
        }

        /// <summary>Hide overlay chrome for clean TV / screenshot plate.</summary>
        public void SetOverlayVisible(bool visible)
        {
            if (ShowCanvas) ShowCanvas.SetActive(visible);
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("ShowCanvas");
            canvasGo.transform.SetParent(transform, false);
            ShowCanvas = canvasGo;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            BrandText = UiText(canvasGo.transform, "Brand", KiagConfig.Brand, 22, TextAnchor.UpperLeft, new Vector2(16, -12));
            RoomCodeText = UiText(canvasGo.transform, "Room", "Salle —", 26, TextAnchor.UpperRight, new Vector2(-16, -12));
            PhaseText = UiText(canvasGo.transform, "Phase", "lobby", 18, TextAnchor.UpperCenter, new Vector2(0, -12));
            TimerText = UiText(canvasGo.transform, "Timer", "", 40, TextAnchor.UpperCenter, new Vector2(0, -44));
            HostLineText = UiText(canvasGo.transform, "HostLine", "Bienvenue sur le plateau !", 22, TextAnchor.LowerCenter, new Vector2(0, 28));
            QuestionText = UiText(canvasGo.transform, "Question", "", 26, TextAnchor.LowerCenter, new Vector2(0, 56));
            RevealText = UiText(canvasGo.transform, "Reveal", "", 24, TextAnchor.MiddleCenter, new Vector2(0, 100));

            var panel = new GameObject("LobbyPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(0, 0);
            prt.pivot = new Vector2(0, 0);
            prt.anchoredPosition = new Vector2(12, 12);
            prt.sizeDelta = new Vector2(360, 120);

            ModeClassique = UiButton(panel.transform, "Classique", new Vector2(0, 110));
            ModeDecades = UiButton(panel.transform, "Décades", new Vector2(140, 110));
            ModeRapide = UiButton(panel.transform, "Rapide", new Vector2(280, 110));
            StartButton = UiButton(panel.transform, "Démarrer", new Vector2(0, 50));
            // Start = solid red pad with white outlined label (high contrast).
            StartButton.GetComponent<Image>().color = ClayPalette.PadA;
            var startLabel = StartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (startLabel != null) KiagTmp.Bind(startLabel, Color.white, outline: true);

            var togGo = new GameObject("PPToggle");
            togGo.transform.SetParent(panel.transform, false);
            var trt = togGo.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(180, 55);
            trt.sizeDelta = new Vector2(180, 28);
            PowerPlaysToggle = togGo.AddComponent<Toggle>();
            PowerPlaysToggle.isOn = true;
            var togLabel = UiText(togGo.transform, "PPLabel", "Power Plays", 18, TextAnchor.MiddleLeft, Vector2.zero);
            KiagTmp.Bind(togLabel, ClayPalette.ClayWhite, outline: true);
            togLabel.rectTransform.sizeDelta = new Vector2(160, 28);
        }

        TMP_Text UiText(Transform parent, string name, string text, float size, TextAnchor anchor, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            // High-contrast cream ink + black outline — readable on dark teal / bright floor / red Start.
            KiagTmp.Bind(tmp, ClayPalette.ClayWhite, outline: true);
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
            // Dark navy face + white outlined label — survives bloom and bright stage floor.
            img.color = ClayPalette.BackWall;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ClayPalette.BackWall;
            colors.highlightedColor = Color.Lerp(ClayPalette.BackWall, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(ClayPalette.BackWall, Color.black, 0.2f);
            btn.colors = colors;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(128, 44);
            rt.anchoredPosition = pos;
            var t = UiText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter, Vector2.zero);
            KiagTmp.Bind(t, Color.white, outline: true);
            t.fontSize = 20;
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;
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
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            KiagTmp.Bind(tmp, ClayPalette.ClayWhite, outline: true);
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
