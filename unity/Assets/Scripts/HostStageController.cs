using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kiag
{
    /// <summary>
    /// TV stage director: wires ShowStageBuilder roots, pads, cast, PP throws, pyramid, Maxime.
    /// </summary>
    public class HostStageController : MonoBehaviour
    {
        [Header("Optional overrides — auto-filled from ShowStageBuilder")]
        [SerializeField] TMP_Text brandText;
        [SerializeField] TMP_Text roomCodeText;
        [SerializeField] TMP_Text hostLineText;
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text revealText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Transform padsRoot;
        [SerializeField] Transform doorsRoot;
        [SerializeField] Transform castRoot;
        [SerializeField] Transform pyramidRoot;
        [SerializeField] Transform maximeRoot;
        [SerializeField] Transform throwVfxRoot;
        [SerializeField] Button startButton;
        [SerializeField] Button modeClassique;
        [SerializeField] Button modeDecades;
        [SerializeField] Button modeRapide;
        [SerializeField] Toggle powerPlaysToggle;
        [SerializeField] ColyseusHostClient client;

        string _mode = "classique";
        string _lastPhase;
        string _lastThrowJson;
        readonly Dictionary<string, CastAvatarView> _avatars = new Dictionary<string, CastAvatarView>();
        CameraDirector _cams;
        MaximeHost _maxime;
        ShowStageBuilder _stage;

        void Awake()
        {
            _stage = FindFirstObjectByType<ShowStageBuilder>();
            if (_stage == null)
            {
                var go = new GameObject("ShowStage");
                _stage = go.AddComponent<ShowStageBuilder>();
            }
            BindFromStage();

            if (brandText) brandText.text = KiagConfig.Brand;
            if (startButton) startButton.onClick.AddListener(OnStart);
            if (modeClassique) modeClassique.onClick.AddListener(() => SetMode("classique"));
            if (modeDecades) modeDecades.onClick.AddListener(() => SetMode("decades"));
            if (modeRapide) modeRapide.onClick.AddListener(() => SetMode("rapide"));
        }

        void BindFromStage()
        {
            if (_stage == null) return;
            brandText = brandText ? brandText : _stage.BrandText;
            roomCodeText = roomCodeText ? roomCodeText : _stage.RoomCodeText;
            hostLineText = hostLineText ? hostLineText : _stage.HostLineText;
            phaseText = phaseText ? phaseText : _stage.PhaseText;
            questionText = questionText ? questionText : _stage.QuestionText;
            revealText = revealText ? revealText : _stage.RevealText;
            timerText = timerText ? timerText : _stage.TimerText;
            padsRoot = padsRoot ? padsRoot : _stage.PadsRoot;
            doorsRoot = doorsRoot ? doorsRoot : _stage.DoorsRoot;
            castRoot = castRoot ? castRoot : _stage.CastRoot;
            pyramidRoot = pyramidRoot ? pyramidRoot : _stage.PyramidRoot;
            maximeRoot = maximeRoot ? maximeRoot : _stage.MaximeRoot;
            throwVfxRoot = throwVfxRoot ? throwVfxRoot : _stage.ThrowVfxRoot;
            startButton = startButton ? startButton : _stage.StartButton;
            modeClassique = modeClassique ? modeClassique : _stage.ModeClassique;
            modeDecades = modeDecades ? modeDecades : _stage.ModeDecades;
            modeRapide = modeRapide ? modeRapide : _stage.ModeRapide;
            powerPlaysToggle = powerPlaysToggle ? powerPlaysToggle : _stage.PowerPlaysToggle;
            _cams = FindFirstObjectByType<CameraDirector>();
            _maxime = maximeRoot ? maximeRoot.GetComponent<MaximeHost>() : null;
        }

        void Start()
        {
            if (client == null) client = FindFirstObjectByType<ColyseusHostClient>();
            if (client != null && client.GetComponent<ColyseusSdkAdapter>() == null)
                client.gameObject.AddComponent<ColyseusSdkAdapter>();
            StartCoroutine(Boot());
        }

        System.Collections.IEnumerator Boot()
        {
            yield return client.CreateRoomRoutine();
            if (roomCodeText) roomCodeText.text = "Salle " + client.RoomCode;
            if (hostLineText) hostLineText.text = "Scannez le QR Flutter · " + client.JoinUrl;
        }

        void SetMode(string mode)
        {
            _mode = mode;
            client.SetMatchOptions(mode, powerPlaysToggle == null || powerPlaysToggle.isOn);
            if (phaseText) phaseText.text = "Mode " + mode;
        }

        void OnStart()
        {
            client.StartMatch(_mode, powerPlaysToggle == null || powerPlaysToggle.isOn);
        }

        public void ApplySnapshot(MatchSnapshot snap)
        {
            if (snap == null) return;
            if (hostLineText) hostLineText.text = snap.hostLine ?? "";
            if (phaseText) phaseText.text = $"{snap.phase} · Q{snap.roundIndex + 1}/{snap.totalRounds}";
            if (questionText)
            {
                var showQ = snap.phase == "question" || snap.phase == "votes_show" || snap.phase == "reveal"
                            || snap.phase == "pyramid";
                questionText.text = showQ ? (snap.question?.prompt ?? "") : "";
            }
            var left = Mathf.Max(0, Mathf.CeilToInt((snap.phaseEndsAt - snap.serverNow) / 1000f));
            if (timerText) timerText.text = left > 0 ? left + "s" : "";

            if (revealText)
            {
                if (snap.phase == "reveal")
                {
                    try
                    {
                        var jo = JsonConvert.DeserializeObject<Dictionary<string, object>>(snap.lastRevealJson ?? "{}");
                        revealText.text = jo != null && jo.ContainsKey("correctAnswer")
                            ? "Réponse : " + jo["correctAnswer"]
                            : "";
                    }
                    catch { revealText.text = ""; }
                }
                else if (snap.phase == "votes_show")
                    revealText.text = "Votes lockés — suspense…";
                else if (snap.phase == "linking" || snap.phase == "sorting" || snap.phase == "chain_reaction"
                         || snap.phase == "fill_blanks" || snap.phase == "odd_one_out" || snap.phase == "spin_to_win")
                    revealText.text = "Mini-jeu · regardez vos téléphones !";
                else
                    revealText.text = "";
            }

            if (_lastPhase != snap.phase)
            {
                _lastPhase = snap.phase;
                if (_cams) _cams.CutForPhase(snap.phase);
                if (_maxime) _maxime.SetMood(snap.hostMood ?? snap.phase);
                UpdateDoorsVisibility(snap);
            }

            UpdatePads(snap);
            UpdateCast(snap);
            UpdateThemeDoorGather(snap);
            UpdatePyramid(snap);
            UpdateThrowVfx(snap);
        }

        void UpdateDoorsVisibility(MatchSnapshot snap)
        {
            if (doorsRoot == null) return;
            bool show = snap.phase == "theme_vote" || snap.phase == "theme_reveal";
            doorsRoot.gameObject.SetActive(show || snap.phase == "lobby");
        }

        void UpdateThemeDoorGather(MatchSnapshot snap)
        {
            if (doorsRoot == null || snap.players == null) return;
            if (snap.phase != "theme_vote" && snap.phase != "theme_reveal") return;

            List<Dictionary<string, object>> themes = new List<Dictionary<string, object>>();
            try
            {
                themes = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(snap.themeOptionsJson ?? "[]")
                         ?? themes;
            }
            catch { /* ignore */ }

            for (int i = 0; i < doorsRoot.childCount && i < 4; i++)
            {
                var door = doorsRoot.GetChild(i);
                var label = door.Find("Label")?.GetComponent<TextMeshPro>();
                if (label != null && i < themes.Count)
                    label.text = themes[i].ContainsKey("label") ? "" + themes[i]["label"] : $"Porte {(char)('A' + i)}";
            }

            foreach (var p in snap.players)
            {
                if (p.isHost) continue;
                if (!_avatars.TryGetValue(p.sessionId, out var view)) continue;
                int doorIdx = -1;
                if (!string.IsNullOrEmpty(p.themeVote))
                    doorIdx = themes.FindIndex(t => t.ContainsKey("id") && "" + t["id"] == p.themeVote);
                if (doorIdx >= 0 && doorIdx < doorsRoot.childCount)
                {
                    var slot = doorsRoot.GetChild(doorIdx).Find("Slot") ?? doorsRoot.GetChild(doorIdx);
                    view.MoveTo(slot, "walk");
                }
            }
        }

        void UpdatePads(MatchSnapshot snap)
        {
            if (padsRoot == null) return;
            var show = snap.phase == "question" || snap.phase == "votes_show" || snap.phase == "reveal";
            padsRoot.gameObject.SetActive(show);
            if (!show) return;

            List<ChoiceSnap> choices = new List<ChoiceSnap>();
            try { choices = JsonConvert.DeserializeObject<List<ChoiceSnap>>(snap.question?.choicesJson ?? "[]") ?? choices; }
            catch { /* ignore */ }

            int correctIdx = -1;
            if (snap.phase == "reveal")
            {
                try
                {
                    var jo = JsonConvert.DeserializeObject<Dictionary<string, string>>(snap.lastRevealJson ?? "{}");
                    var ans = jo != null && jo.ContainsKey("correctAnswer") ? jo["correctAnswer"] : null;
                    if (ans != null)
                        correctIdx = choices.FindIndex(c => c.label == ans);
                }
                catch { /* ignore */ }
            }

            for (int i = 0; i < padsRoot.childCount && i < 4; i++)
            {
                var pad = padsRoot.GetChild(i);
                var label = pad.Find("Label")?.GetComponent<TextMeshPro>()
                            ?? pad.Find("Label")?.GetComponent<TMP_Text>();
                var letter = pad.Find("Letter")?.GetComponent<TextMeshPro>()
                             ?? pad.Find("Letter")?.GetComponent<TMP_Text>();
                if (letter) letter.text = "" + (char)('A' + i);
                if (label) label.text = i < choices.Count ? choices[i].label : "…";
                var glow = pad.Find("CorrectGlow");
                if (glow) glow.gameObject.SetActive(snap.phase == "reveal" && i == correctIdx);
                pad.localScale = (snap.phase == "reveal" && correctIdx >= 0 && i != correctIdx)
                    ? new Vector3(0.85f, 0.85f, 0.85f)
                    : Vector3.one;
            }

            if (snap.players == null) return;
            foreach (var p in snap.players)
            {
                if (p.isHost) continue;
                if (!_avatars.TryGetValue(p.sessionId, out var view)) continue;
                if (p.lockedAnswerIndex >= 0 && p.lockedAnswerIndex < padsRoot.childCount
                    && (snap.phase == "votes_show" || snap.phase == "reveal" || snap.phase == "question"))
                {
                    // On question: only move if locked; votes_show/reveal always show distribution
                    if (snap.phase == "question" && p.lockedAnswerIndex < 0) continue;
                    var pad = padsRoot.GetChild(p.lockedAnswerIndex);
                    var slot = pad.Find("Slot") != null ? pad.Find("Slot") : pad;
                    view.MoveTo(slot, snap.phase == "reveal" ? p.anim : "walk");
                }
                else if (castRoot != null && snap.phase == "question")
                {
                    view.MoveTo(castRoot, "idle");
                }
            }
        }

        void UpdateCast(MatchSnapshot snap)
        {
            if (castRoot == null || snap.players == null) return;
            // Drop procedural demo lineup once a real phone player appears.
            bool hasHuman = false;
            foreach (var p in snap.players)
            {
                if (!p.isHost) { hasHuman = true; break; }
            }
            if (hasHuman)
            {
                for (int i = castRoot.childCount - 1; i >= 0; i--)
                {
                    var child = castRoot.GetChild(i);
                    if (child && child.name.StartsWith("DemoCast_"))
                        Destroy(child.gameObject);
                }
            }

            var alive = new HashSet<string>();
            int slot = 0;
            foreach (var p in snap.players)
            {
                if (p.isHost) continue;
                alive.Add(p.sessionId);
                if (!_avatars.TryGetValue(p.sessionId, out var view))
                {
                    var go = new GameObject("Cast_" + p.name);
                    go.transform.SetParent(castRoot, false);
                    go.transform.localPosition = new Vector3(-3.6f + slot * 2.4f, 0, 0);
                    view = go.AddComponent<CastAvatarView>();
                    view.Setup(p);
                    _avatars[p.sessionId] = view;
                }
                else view.Refresh(p);
                slot++;
            }
        }

        void UpdatePyramid(MatchSnapshot snap)
        {
            if (pyramidRoot == null) return;
            pyramidRoot.gameObject.SetActive(snap.phase == "pyramid" || snap.phase == "podium");
            if ((snap.phase != "pyramid" && snap.phase != "podium") || snap.players == null) return;
            foreach (var p in snap.players)
            {
                if (p.isHost) continue;
                if (_avatars.TryGetValue(p.sessionId, out var view))
                    view.SetPyramidStep(p.pyramidStep, pyramidRoot);
            }
        }

        void UpdateThrowVfx(MatchSnapshot snap)
        {
            if (string.IsNullOrEmpty(snap.throwVfxJson) || snap.throwVfxJson == "null") return;
            if (snap.throwVfxJson == _lastThrowJson) return;
            if (snap.phase != "power_play" && snap.phase != "question") return;
            _lastThrowJson = snap.throwVfxJson;
            try
            {
                var jo = JsonConvert.DeserializeObject<Dictionary<string, string>>(snap.throwVfxJson);
                if (jo == null) return;
                jo.TryGetValue("kind", out var kind);
                jo.TryGetValue("targetSessionId", out var tgt);
                jo.TryGetValue("fromSessionId", out var from);

                Vector3 fromPos = maximeRoot ? maximeRoot.position + Vector3.up * 1.5f : Vector3.zero;
                Vector3 toPos = castRoot ? castRoot.position : Vector3.forward * 2f;
                if (!string.IsNullOrEmpty(from) && _avatars.TryGetValue(from, out var attacker))
                    fromPos = attacker.transform.position + Vector3.up;
                if (!string.IsNullOrEmpty(tgt) && _avatars.TryGetValue(tgt, out var victim))
                    toPos = victim.transform.position + Vector3.up;

                var root = throwVfxRoot != null ? throwVfxRoot : transform;
                PowerPlayThrowVfx.Spawn(root, fromPos, toPos, kind ?? "gloop");
                if (_cams) _cams.CutForPhase("power_play");
            }
            catch { /* ignore */ }
        }
    }
}
