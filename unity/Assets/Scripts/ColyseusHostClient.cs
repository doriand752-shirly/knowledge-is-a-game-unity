using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiag
{
    /// <summary>
    /// Host-side Colyseus bridge for Unity.
    /// Uses HTTP create-room + matchmake, then WebSocket via native Colyseus Unity SDK when present.
    /// Fallback: HTTP polling of a lightweight debug endpoint is not used — install Colyseus Unity SDK
    /// (https://github.com/colyseus/colyseus-unity-sdk) and wire JoinOrCreate in OnEnable.
    /// </summary>
    public class ColyseusHostClient : MonoBehaviour
    {
        [SerializeField] string httpBase = KiagConfig.DefaultHttp;
        [SerializeField] HostStageController stage;

        public string RoomCode { get; private set; }
        public MatchSnapshot Snapshot { get; private set; }
        public string JoinUrl { get; private set; }

        public event Action<MatchSnapshot> SnapshotReceived;
        public event Action<string> ToastReceived;

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
            using var req = UnityWebRequest.PostWwwForm(httpBase.TrimEnd('/') + "/api/create-room", "");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Kiag] create-room failed: " + req.error);
                yield break;
            }
            var jo = JObject.Parse(req.downloadHandler.text);
            RoomCode = jo.Value<string>("roomCode");
            JoinUrl = jo.Value<string>("joinUrl");
            Debug.Log($"[Kiag] Room {RoomCode} — join {JoinUrl}");
        }

        /// <summary>
        /// Placeholder send — replace body with Colyseus Room.Send when SDK is linked.
        /// </summary>
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
    /// Static bridge filled by ColyseusSdkAdapter.cs once the official Unity SDK package is imported.
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
