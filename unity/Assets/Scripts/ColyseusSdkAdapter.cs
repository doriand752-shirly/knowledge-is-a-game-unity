using System.Collections.Generic;
using Colyseus;
using UnityEngine;

namespace Kiag
{
    /// <summary>
    /// Live Colyseus join for the Unity TV host (vendored SDK under Assets/Colyseus).
    /// Creates/joins the match room with the HTTP-issued roomCode so phones can joinById/lookup.
    /// </summary>
    public class ColyseusSdkAdapter : MonoBehaviour
    {
        [SerializeField] ColyseusHostClient host;
        [SerializeField] string wsUrl = "ws://85.17.145.134:2567";

        Client _client;
        Room<NoState> _room;
        bool _joining;

        async void Start()
        {
            if (host == null) host = FindFirstObjectByType<ColyseusHostClient>();
            float t = 0;
            while (host != null && string.IsNullOrEmpty(host.RoomCode) && t < 20f)
            {
                t += Time.deltaTime;
                await System.Threading.Tasks.Task.Yield();
            }
            await JoinAsHost();
        }

        async System.Threading.Tasks.Task JoinAsHost()
        {
            if (_joining) return;
            _joining = true;
            try
            {
                _client = new Client(wsUrl);
                var options = new Dictionary<string, object>
                {
                    { "role", "host" },
                    { "name", "TV-Host" },
                };
                if (host != null && !string.IsNullOrEmpty(host.RoomCode))
                    options["roomCode"] = host.RoomCode;

                _room = await _client.JoinOrCreate("match", options);
                Debug.Log($"[Kiag] Host joined Colyseus room {_room.RoomId} code={host?.RoomCode}");

                _room.OnMessage<object>("snapshot", msg =>
                {
                    if (host != null) host.OnSnapshotMessage(msg);
                });
                _room.OnMessage<object>("toast", msg =>
                {
                    if (host != null) host.OnToastMessage(msg);
                });

                HostSdkBridge.SendImpl = (type, payload) =>
                {
                    if (_room == null) return;
                    if (payload == null)
                    {
                        _room.Send(type, new Dictionary<string, object>());
                        return;
                    }
                    if (payload is Dictionary<string, object> dict)
                    {
                        _room.Send(type, dict);
                        return;
                    }
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var map = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                              ?? new Dictionary<string, object>();
                    _room.Send(type, map);
                };

                _room.Send("request_snapshot", new Dictionary<string, object>());
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Kiag] Colyseus host join failed: " + e);
            }
            finally
            {
                _joining = false;
            }
        }

        async void OnDestroy()
        {
            HostSdkBridge.SendImpl = null;
            if (_room != null)
            {
                try { await _room.Leave(); } catch { /* ignore */ }
                _room = null;
            }
        }
    }
}
