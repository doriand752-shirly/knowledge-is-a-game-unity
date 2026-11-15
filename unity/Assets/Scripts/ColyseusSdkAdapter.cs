using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Colyseus.Schema;
using Colyseus.Schema.Utils;
using GameDevWare.Serialization;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Kiag
{
    /// <summary>
    /// Host Colyseus join that speaks the VPS protocol (Colyseus 0.15 wire)
    /// while the vendored Unity SDK is 0.18 (incompatible JOIN_ROOM framing).
    /// Matchmake HTTP + WebSocket + ROOM_DATA JSON snapshots/toasts only.
    /// </summary>
    public class ColyseusSdkAdapter : MonoBehaviour
    {
        [SerializeField] ColyseusHostClient host;

        WebSocket _ws;
        bool _joining;
        bool _joined;
        string _roomId;
        string _sessionId;

        async void Start()
        {
            if (host == null) host = FindFirstObjectByType<ColyseusHostClient>();
            float t = 0;
            while (host != null && string.IsNullOrEmpty(host.RoomCode) && string.IsNullOrEmpty(host.LastError) && t < 25f)
            {
                t += Time.deltaTime;
                await Task.Yield();
            }

            if (host == null || string.IsNullOrEmpty(host.RoomCode))
            {
                Debug.LogError("[Kiag] Skipping Colyseus join — no roomCode (" + (host?.LastError ?? "timeout") + ")");
                return;
            }

            await JoinAsHost();
        }

        void Update()
        {
            _ws?.DispatchMessageQueue();
        }

        async Task JoinAsHost()
        {
            if (_joining || _joined) return;
            _joining = true;
            try
            {
                var http = host.HttpBase.TrimEnd('/');
                var options = new Dictionary<string, object>
                {
                    { "role", "host" },
                    { "name", "TV-Host" },
                    { "roomCode", host.RoomCode },
                };

                Debug.Log($"[Kiag] Colyseus matchmake {http} roomCode={host.RoomCode}");
                var seat = await Matchmake(http, "match", options);
                _roomId = seat.RoomId;
                _sessionId = seat.SessionId;

                var wsUrl = $"{host.WsBase.TrimEnd('/')}/{seat.ProcessId}/{seat.RoomId}?sessionId={Uri.EscapeDataString(seat.SessionId)}";
                Debug.Log($"[Kiag] Colyseus ws {wsUrl}");

                _ws = new WebSocket(wsUrl);
                var joinedTcs = new TaskCompletionSource<bool>();

                _ws.OnOpen += () => Debug.Log("[Kiag] Colyseus WS open");
                _ws.OnError += msg =>
                {
                    Debug.LogError("[Kiag] Colyseus WS error: " + msg);
                    joinedTcs.TrySetException(new Exception(msg));
                };
                _ws.OnClose += code =>
                {
                    Debug.LogWarning("[Kiag] Colyseus WS closed: " + code);
                    _joined = false;
                    HostSdkBridge.SendImpl = null;
                };
                _ws.OnMessage += bytes =>
                {
                    try { HandleMessage(bytes, joinedTcs); }
                    catch (Exception e) { Debug.LogWarning("[Kiag] WS parse: " + e.Message); }
                };

                // NativeWebSocket.Connect() runs Receive() until close — fire-and-forget like Colyseus Room.
                _ = _ws.Connect();

                float wait = 0f;
                while (!joinedTcs.Task.IsCompleted && wait < 10f)
                {
                    _ws.DispatchMessageQueue();
                    wait += 0.05f;
                    await Task.Delay(50);
                }
                if (!joinedTcs.Task.IsCompleted)
                    throw new TimeoutException("JOIN_ROOM timeout");
                await joinedTcs.Task;

                HostSdkBridge.SendImpl = (type, payload) =>
                {
                    try { _ = SendRoomData(type, payload); }
                    catch (Exception e) { Debug.LogWarning("[Kiag] send failed: " + e.Message); }
                };

                await SendRoomData("request_snapshot", new Dictionary<string, object>());
                Debug.Log($"[Kiag] Host joined Colyseus room {_roomId} code={host.RoomCode} session={_sessionId}");
            }
            catch (Exception e)
            {
                Debug.LogError("[Kiag] Colyseus host join failed: " + e);
            }
            finally
            {
                _joining = false;
            }
        }

        void HandleMessage(byte[] bytes, TaskCompletionSource<bool> joinedTcs)
        {
            if (bytes == null || bytes.Length == 0) return;
            byte code = bytes[0];

            // Colyseus 0.15 JOIN_ROOM
            if (code == 10)
            {
                _ = _ws.Send(new byte[] { 10 });
                _joined = true;
                joinedTcs.TrySetResult(true);
                return;
            }

            // ERROR
            if (code == 11)
            {
                var it = new Iterator { Offset = 1 };
                float errCode = Decode.DecodeNumber(bytes, it);
                string errMsg = Decode.DecodeString(bytes, it);
                joinedTcs.TrySetException(new Exception($"Colyseus ERROR {errCode}: {errMsg}"));
                return;
            }

            // LEAVE
            if (code == 12)
            {
                _joined = false;
                return;
            }

            // ROOM_DATA — JSON messages (snapshot / toast)
            if (code == 13)
            {
                var it = new Iterator { Offset = 1 };
                string type;
                if (Decode.NumberCheck(bytes, it))
                    type = Decode.DecodeNumber(bytes, it).ToString();
                else
                    type = Decode.DecodeString(bytes, it);

                object payload = null;
                if (bytes.Length > it.Offset)
                {
                    payload = MsgPack.Deserialize(typeof(object),
                        new MemoryStream(bytes, it.Offset, bytes.Length - it.Offset, false));
                }

                if (type == "snapshot" && host != null)
                    host.OnSnapshotMessage(ToPlain(payload));
                else if (type == "toast" && host != null)
                    host.OnToastMessage(ToPlain(payload));
            }

            // ROOM_STATE / ROOM_STATE_PATCH ignored — TV uses snapshot JSON.
        }

        async Task SendRoomData(string type, object payload)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            var frame = new List<byte> { 13 };
            Encode.String(frame, type);

            if (payload != null)
            {
                using var ms = new MemoryStream();
                var dict = payload as Dictionary<string, object> ?? ToDict(payload);
                MsgPack.Serialize(dict, ms, SerializationOptions.SuppressTypeInformation);
                frame.AddRange(ms.ToArray());
            }

            await _ws.Send(frame.ToArray());
        }

        static Dictionary<string, object> ToDict(object payload)
        {
            if (payload is Dictionary<string, object> d) return d;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                   ?? new Dictionary<string, object>();
        }

        static object ToPlain(object payload)
        {
            if (payload == null) return null;
            if (payload is string) return payload;
            // GameDevWare may yield IndexedDictionary — round-trip via JSON for MatchSnapshot.
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                return JToken.Parse(json);
            }
            catch
            {
                return payload;
            }
        }

        static async Task<Seat> Matchmake(string httpBase, string roomName, Dictionary<string, object> options)
        {
            var url = httpBase + "/matchmake/joinOrCreate/" + roomName;
            var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(options));
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"matchmake failed: {req.error} ({req.responseCode}) {req.downloadHandler?.text}");

            var jo = JObject.Parse(req.downloadHandler.text);
            // Colyseus 0.15 nested { room: { roomId, processId, ... }, sessionId }
            var room = jo["room"] as JObject ?? jo;
            var seat = new Seat
            {
                SessionId = jo.Value<string>("sessionId"),
                RoomId = room.Value<string>("roomId"),
                ProcessId = room.Value<string>("processId"),
                Name = room.Value<string>("name") ?? roomName,
            };
            if (string.IsNullOrEmpty(seat.RoomId) || string.IsNullOrEmpty(seat.ProcessId) || string.IsNullOrEmpty(seat.SessionId))
                throw new Exception("matchmake missing roomId/processId/sessionId: " + req.downloadHandler.text);
            return seat;
        }

        async void OnDestroy()
        {
            HostSdkBridge.SendImpl = null;
            if (_ws != null)
            {
                try
                {
                    if (_ws.State == WebSocketState.Open)
                        await _ws.Send(new byte[] { 12 }); // LEAVE_ROOM
                    await _ws.Close();
                }
                catch { /* ignore */ }
                _ws = null;
            }
        }

        struct Seat
        {
            public string SessionId;
            public string RoomId;
            public string ProcessId;
            public string Name;
        }
    }
}
