import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:web_socket_channel/io.dart';
import 'package:web_socket_channel/web_socket_channel.dart';

import 'config.dart';

/// Player room over Colyseus **0.15** wire (VPS), without the 0.18 SDK join path
/// (which rejects nested seat JSON: "missing name" and speaks a newer JOIN_ROOM).
class KiagRoom {
  KiagRoom._(this.sessionId, this._channel);

  final String sessionId;
  final WebSocketChannel _channel;
  final _snapshotCtrl = StreamController<Map<String, dynamic>>.broadcast();
  final _toastCtrl = StreamController<String>.broadcast();
  StreamSubscription? _sub;

  Stream<Map<String, dynamic>> get snapshots => _snapshotCtrl.stream;
  Stream<String> get toasts => _toastCtrl.stream;
  Map<String, dynamic>? lastSnapshot;

  void _wire() {
    _sub = _channel.stream.listen(
      (data) {
        try {
          final bytes = data is List<int>
              ? Uint8List.fromList(data)
              : (data is Uint8List ? data : null);
          if (bytes == null || bytes.isEmpty) return;
          _onPacket(bytes);
        } catch (e) {
          debugPrint('KiagRoom packet: $e');
        }
      },
      onError: (e) => debugPrint('KiagRoom ws error: $e'),
      onDone: () => debugPrint('KiagRoom ws closed'),
    );
  }

  void _onPacket(Uint8List bytes) {
    final code = bytes[0];
    // JOIN_ROOM — ack only (0.15)
    if (code == 10) {
      _channel.sink.add(Uint8List.fromList([10]));
      send('request_snapshot');
      return;
    }
    if (code == 11 || code == 12) return;
    // ROOM_DATA
    if (code == 13) {
      var offset = 1;
      final type = _decodeString(bytes, offset);
      offset = type.next;
      Map<String, dynamic>? payload;
      if (offset < bytes.length) {
        payload = asMap(_msgpackDecode(bytes.sublist(offset)));
      }
      if (type.value == 'snapshot' && payload != null) {
        lastSnapshot = payload;
        if (!_snapshotCtrl.isClosed) _snapshotCtrl.add(payload);
      } else if (type.value == 'toast') {
        final msg = payload?['message']?.toString();
        if (msg != null && msg.isNotEmpty && !_toastCtrl.isClosed) {
          _toastCtrl.add(msg);
        }
      }
    }
  }

  void send(String type, [Map<String, dynamic>? payload]) {
    // ROOM_DATA type is msgpack str (same as @colyseus/schema Encode.string).
    final typeBytes = _msgpackString(type);
    final body = _msgpackEncode(payload ?? <String, dynamic>{});
    final out = Uint8List(1 + typeBytes.length + body.length);
    out[0] = 13; // ROOM_DATA
    out.setRange(1, 1 + typeBytes.length, typeBytes);
    out.setRange(1 + typeBytes.length, out.length, body);
    _channel.sink.add(out);
  }

  Future<void> leave() async {
    try {
      _channel.sink.add(Uint8List.fromList([12])); // LEAVE_ROOM
      await _channel.sink.close();
    } catch (_) {}
    await _sub?.cancel();
    if (!_snapshotCtrl.isClosed) await _snapshotCtrl.close();
    if (!_toastCtrl.isClosed) await _toastCtrl.close();
  }

  static Map<String, dynamic>? asMap(dynamic data) {
    if (data is Map<String, dynamic>) return data;
    if (data is Map) return Map<String, dynamic>.from(data);
    if (data is String) {
      try {
        final decoded = jsonDecode(data);
        if (decoded is Map) return Map<String, dynamic>.from(decoded);
      } catch (_) {}
    }
    return null;
  }

  static Future<String?> lookupRoomId(String code, String httpBase) async {
    final base = httpBase.replaceAll(RegExp(r'/$'), '');
    for (var i = 0; i < 16; i++) {
      try {
        final res =
            await http.get(Uri.parse('$base/api/room/${code.toUpperCase()}'));
        if (res.statusCode == 200) {
          final body = jsonDecode(res.body);
          final id = asMap(body)?['roomId']?.toString();
          if (id != null && id.isNotEmpty) return id;
        }
      } catch (e) {
        debugPrint('lookupRoomId: $e');
      }
      await Future<void>.delayed(Duration(milliseconds: 250 + i * 120));
    }
    return null;
  }

  static Map<String, dynamic> flattenSeat(Map<String, dynamic> raw) {
    final room = asMap(raw['room']);
    if (room == null) {
      return {
        'name': raw['name']?.toString() ?? 'match',
        'roomId': raw['roomId']?.toString() ?? '',
        'processId': raw['processId']?.toString() ?? '',
        'sessionId': raw['sessionId']?.toString() ?? '',
      };
    }
    return {
      'name': room['name']?.toString() ?? 'match',
      'roomId': room['roomId']?.toString() ?? '',
      'processId': room['processId']?.toString() ?? '',
      'sessionId': raw['sessionId']?.toString() ?? '',
    };
  }

  static Future<Map<String, dynamic>> matchmake({
    required String httpUrl,
    required String path,
    required Map<String, dynamic> options,
  }) async {
    final res = await http.post(
      Uri.parse('$httpUrl/matchmake/$path'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(options),
    );
    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw StateError('matchmake ${res.statusCode}: ${res.body}');
    }
    final map = asMap(jsonDecode(res.body));
    if (map == null) throw StateError('matchmake: bad JSON');
    final flat = flattenSeat(map);
    if (flat['roomId']!.isEmpty ||
        flat['processId']!.isEmpty ||
        flat['sessionId']!.isEmpty) {
      throw StateError('matchmake: incomplete seat $flat');
    }
    return flat;
  }

  static Future<KiagRoom> joinPlayer({
    required String roomCode,
    required String name,
    String? httpBase,
  }) async {
    final httpUrl = (httpBase ?? kDefaultApiHttp).replaceAll(RegExp(r'/$'), '');
    final opts = <String, dynamic>{
      'roomCode': roomCode.toUpperCase(),
      'role': 'player',
      'name': name,
    };

    final roomId = await lookupRoomId(roomCode, httpUrl);
    if (roomId == null) {
      throw StateError(
        'Salle ${roomCode.toUpperCase()} introuvable — lance KnowledgeIsAGame.exe et utilise le code affiché à l’écran.',
      );
    }

    final seat = await matchmake(
      httpUrl: httpUrl,
      path: 'joinById/$roomId',
      options: opts,
    );

    final wsBase = wsFromHttp(httpUrl).replaceAll(RegExp(r'/$'), '');
    final wsUrl =
        '$wsBase/${seat['processId']}/${seat['roomId']}?sessionId=${Uri.encodeQueryComponent(seat['sessionId']!)}';
    debugPrint('KiagRoom ws $wsUrl');

    final channel = IOWebSocketChannel.connect(Uri.parse(wsUrl));
    await channel.ready.timeout(const Duration(seconds: 8));

    final kiag = KiagRoom._(seat['sessionId']!, channel);
    kiag._wire();
    return kiag;
  }

  // --- minimal msgpack (maps/strings/bools/numbers) for Colyseus ROOM_DATA ---

  static Uint8List _msgpackString(String value) {
    final u = utf8Encode(value);
    if (u.length < 32) {
      return Uint8List.fromList([0xa0 | u.length, ...u]);
    }
    if (u.length <= 0xff) {
      return Uint8List.fromList([0xd9, u.length, ...u]);
    }
    return Uint8List.fromList([
      0xda,
      (u.length >> 8) & 0xff,
      u.length & 0xff,
      ...u,
    ]);
  }

  static ({String value, int next}) _decodeString(Uint8List bytes, int offset) {
    final b0 = bytes[offset];
    if ((b0 & 0xe0) == 0xa0) {
      final len = b0 & 0x1f;
      final start = offset + 1;
      return (
        value: utf8Decode(bytes.sublist(start, start + len)),
        next: start + len,
      );
    }
    if (b0 == 0xd9) {
      final len = bytes[offset + 1];
      final start = offset + 2;
      return (
        value: utf8Decode(bytes.sublist(start, start + len)),
        next: start + len,
      );
    }
    throw FormatException('bad string at $offset byte=$b0');
  }

  static Uint8List _msgpackEncode(Map<String, dynamic> map) {
    final parts = <int>[];

    void writeVal(dynamic v) {
      if (v == null) {
        parts.add(0xc0);
      } else if (v is bool) {
        parts.add(v ? 0xc3 : 0xc2);
      } else if (v is String) {
        parts.addAll(_msgpackString(v));
      } else if (v is int) {
        if (v >= 0 && v <= 127) {
          parts.add(v);
        } else if (v < 0 && v >= -32) {
          parts.add(v & 0xff);
        } else if (v >= 0 && v <= 0xff) {
          parts.add(0xcc);
          parts.add(v);
        } else if (v >= 0 && v <= 0xffff) {
          parts.add(0xcd);
          parts.add((v >> 8) & 0xff);
          parts.add(v & 0xff);
        } else {
          parts.add(0xce);
          parts.add((v >> 24) & 0xff);
          parts.add((v >> 16) & 0xff);
          parts.add((v >> 8) & 0xff);
          parts.add(v & 0xff);
        }
      } else if (v is double) {
        final bd = ByteData(9);
        bd.setUint8(0, 0xcb);
        bd.setFloat64(1, v);
        parts.addAll(bd.buffer.asUint8List());
      } else if (v is Map) {
        final m = Map<String, dynamic>.from(v);
        final n = m.length;
        if (n < 16) {
          parts.add(0x80 | n);
        } else if (n <= 0xffff) {
          parts.add(0xde);
          parts.add((n >> 8) & 0xff);
          parts.add(n & 0xff);
        } else {
          throw UnsupportedError('map too large');
        }
        m.forEach((k, val) {
          writeVal(k);
          writeVal(val);
        });
      } else if (v is List) {
        final n = v.length;
        if (n < 16) {
          parts.add(0x90 | n);
        } else {
          parts.add(0xdc);
          parts.add((n >> 8) & 0xff);
          parts.add(n & 0xff);
        }
        for (final e in v) {
          writeVal(e);
        }
      } else {
        writeVal(v.toString());
      }
    }

    writeVal(map);
    return Uint8List.fromList(parts);
  }

  static dynamic _msgpackDecode(Uint8List bytes, [int start = 0]) {
    var i = start;
    dynamic read() {
      final b = bytes[i++];
      if (b <= 0x7f) return b;
      if (b >= 0xe0) return b - 256;
      if ((b & 0xe0) == 0xa0) {
        final len = b & 0x1f;
        final s = utf8Decode(bytes.sublist(i, i + len));
        i += len;
        return s;
      }
      if ((b & 0xf0) == 0x80) {
        final n = b & 0x0f;
        final m = <String, dynamic>{};
        for (var k = 0; k < n; k++) {
          final key = read().toString();
          m[key] = read();
        }
        return m;
      }
      if ((b & 0xf0) == 0x90) {
        final n = b & 0x0f;
        return List.generate(n, (_) => read());
      }
      switch (b) {
        case 0xc0:
          return null;
        case 0xc2:
          return false;
        case 0xc3:
          return true;
        case 0xcc:
          return bytes[i++];
        case 0xcd:
          final v = (bytes[i] << 8) | bytes[i + 1];
          i += 2;
          return v;
        case 0xce:
          final v = (bytes[i] << 24) |
              (bytes[i + 1] << 16) |
              (bytes[i + 2] << 8) |
              bytes[i + 3];
          i += 4;
          return v;
        case 0xd0:
          return bytes[i++] - 256;
        case 0xd1:
          final v = (bytes[i] << 8) | bytes[i + 1];
          i += 2;
          return v > 0x7fff ? v - 0x10000 : v;
        case 0xd2:
          final v = (bytes[i] << 24) |
              (bytes[i + 1] << 16) |
              (bytes[i + 2] << 8) |
              bytes[i + 3];
          i += 4;
          return v;
        case 0xca:
          final bd = ByteData.sublistView(bytes, i, i + 4);
          i += 4;
          return bd.getFloat32(0);
        case 0xcb:
          final bd = ByteData.sublistView(bytes, i, i + 8);
          i += 8;
          return bd.getFloat64(0);
        case 0xd9:
          final len = bytes[i++];
          final s = utf8Decode(bytes.sublist(i, i + len));
          i += len;
          return s;
        case 0xda:
          final len = (bytes[i] << 8) | bytes[i + 1];
          i += 2;
          final s = utf8Decode(bytes.sublist(i, i + len));
          i += len;
          return s;
        case 0xde:
          final n = (bytes[i] << 8) | bytes[i + 1];
          i += 2;
          final m = <String, dynamic>{};
          for (var k = 0; k < n; k++) {
            final key = read().toString();
            m[key] = read();
          }
          return m;
        case 0xdc:
          final n = (bytes[i] << 8) | bytes[i + 1];
          i += 2;
          return List.generate(n, (_) => read());
        default:
          throw FormatException('unsupported msgpack 0x${b.toRadixString(16)}');
      }
    }

    return read();
  }
}

List<int> utf8Encode(String s) => utf8.encode(s);
String utf8Decode(List<int> b) => utf8.decode(b);
