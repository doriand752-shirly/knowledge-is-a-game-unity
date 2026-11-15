using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiag
{
    /// <summary>
    /// Host-side Colyseus bridge for Unity.
    /// Uses HTTP create-room + matchmake, then WebSocket via ColyseusSdkAdapter.
    /// </summary>
    public class ColyseusHostClient : MonoBehaviour
    {
        const string PrefsHttpKey = "kiag_api";

        [SerializeField] string httpBase = KiagConfig.DefaultHttp;
        [SerializeField] HostStageController stage;

        public string HttpBase => httpBase;
        public string RoomCode { get; private set; }
        public MatchSnapshot Snapshot { get; private set; }
        public string JoinUrl { get; private set; }
        public string LastError { get; private set; }

        public event Action<MatchSnapshot> SnapshotReceived;
        public event Action<string> ToastReceived;

        void Awake()
        {
            ResolveHttpBase();
        }

        void ResolveHttpBase()
        {
            var fromPrefs = PlayerPrefs.GetString(PrefsHttpKey, "");
            if (string.IsNullOrWhiteSpace(fromPrefs))
                fromPrefs = PlayerPrefs.GetString("KIAG_API", "");
            if (!string.IsNullOrWhiteSpace(fromPrefs))
                httpBase = fromPrefs.Trim().TrimEnd('/');
            else if (string.IsNullOrWhiteSpace(httpBase))
                httpBase = KiagConfig.DefaultHttp;
            else
                httpBase = httpBase.Trim().TrimEnd('/');

            // Persist default so Editor/Play and builds share the same override key.
            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PrefsHttpKey, "")))
            {
                PlayerPrefs.SetString(PrefsHttpKey, httpBase);
                PlayerPrefs.Save();
            }

            Debug.Log($"[Kiag] API base = {httpBase}");
        }

        public string WsBase
        {
            get
            {
                var h = httpBase.Trim().TrimEnd('/');
                if (h.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return "wss://" + h.Substring("https://".Length);
                if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    return "ws://" + h.Substring("http://".Length);
                return h;
            }
        }

        /// <summary>Invoked by Colyseus SDK adapter when a snapshot message arrives.</summary>
        public void OnSnapshotMessage(object data)
        {
            try
            {
                MatchSnapshot snap;
                if (data is string s) snap = MatchSnapshot.FromJson(s);
                else snap = MatchSnapshot.FromObject(data);
                Snapshot = snap;
                SnapshotReceived?.Invoke(snap);
                if (stage != null) stage.ApplySnapshot(snap);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kiag] snapshot parse: " + e.Message);
            }
        }

        public void OnToastMessage(object data)
        {
            try
            {
                var jo = data is string s ? JObject.Parse(s) : JObject.FromObject(data);
                var msg = jo.Value<string>("message");
                if (!string.IsNullOrEmpty(msg)) ToastReceived?.Invoke(msg);
            }
            catch { /* ignore */ }
        }

        public IEnumerator CreateRoomRoutine()
        {
            LastError = null;
            RoomCode = null;
            JoinUrl = null;
            ResolveHttpBase();

            var url = httpBase + "/api/create-room";
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var body = Encoding.UTF8.GetBytes("{}");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                LastError = $"create-room failed: {req.error} ({req.responseCode}) → {url}";
                Debug.LogError("[Kiag] " + LastError);
                yield break;
            }

            try
            {
                var jo = JObject.Parse(req.downloadHandler.text);
                RoomCode = jo.Value<string>("roomCode");
                JoinUrl = jo.Value<string>("joinUrl");
                if (string.IsNullOrEmpty(RoomCode))
                {
                    LastError = "create-room returned empty roomCode: " + req.downloadHandler.text;
                    Debug.LogError("[Kiag] " + LastError);
                    yield break;
                }
                Debug.Log($"[Kiag] Room {RoomCode} — join {JoinUrl}");
            }
            catch (Exception e)
            {
                LastError = "create-room parse: " + e.Message;
                Debug.LogError("[Kiag] " + LastError);
            }
        }

        public void Send(string type, object payload = null)
        {
            Debug.Log($"[Kiag] Send {type} {JsonConvert.SerializeObject(payload ?? new { })}");
            HostSdkBridge.Send(type, payload);
        }

        public void StartMatch(string mode, bool powerPlays)
        {
            Send("start_match", new { mode, powerPlaysEnabled = powerPlays });
        }

        public void SetMatchOptions(string mode, bool powerPlays)
        {
            Send("set_match_options", new { mode, powerPlaysEnabled = powerPlays });
        }
    }

    /// <summary>
    /// Static bridge filled by ColyseusSdkAdapter once the host room is joined.
    /// </summary>
    public static class HostSdkBridge
    {
        public static Action<string, object> SendImpl;

        public static void Send(string type, object payload)
        {
            if (SendImpl != null) SendImpl(type, payload);
            else Debug.LogWarning("[Kiag] Colyseus Unity SDK not wired — open Assets/Scripts/ColyseusSdkAdapter.cs");
        }
    }
}
