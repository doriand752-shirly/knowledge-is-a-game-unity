import 'dart:async';
import 'dart:convert';

import 'package:colyseus/colyseus.dart';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import 'config.dart';

/// Colyseus Flutter client + JSON `snapshot` messages from MatchRoom.
class KiagRoom {
  KiagRoom._(this.room, this.sessionId);

  final ColyseusRoom<SchemaInstance> room;
  final String sessionId;
  final _snapshotCtrl = StreamController<Map<String, dynamic>>.broadcast();
  final _toastCtrl = StreamController<String>.broadcast();

  Stream<Map<String, dynamic>> get snapshots => _snapshotCtrl.stream;
  Stream<String> get toasts => _toastCtrl.stream;
  Map<String, dynamic>? lastSnapshot;

  void _wire() {
    room.onMessage('snapshot').listen((data) {
      final map = asMap(data);
      if (map != null) {
        lastSnapshot = map;
        if (!_snapshotCtrl.isClosed) _snapshotCtrl.add(map);
      }
    });
    room.onMessage('toast').listen((data) {
      final msg = asMap(data)?['message']?.toString();
      if (msg != null && msg.isNotEmpty && !_toastCtrl.isClosed) {
        _toastCtrl.add(msg);
      }
    });
    room.onMessage('error').listen((data) {
      final msg = asMap(data)?['message']?.toString();
      if (msg != null && msg.isNotEmpty && !_toastCtrl.isClosed) {
        _toastCtrl.add(msg);
      }
    });
    send('request_snapshot');
  }

  void send(String type, [Map<String, dynamic>? payload]) {
    room.send(type, payload ?? <String, dynamic>{});
  }

  Future<void> leave() async {
    try {
      await room.leave();
    } catch (_) {}
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
    for (var i = 0; i < 12; i++) {
      try {
        final res = await http.get(Uri.parse('$base/api/room/${code.toUpperCase()}'));
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

  static Future<KiagRoom> joinPlayer({
    required String roomCode,
    required String name,
    String? httpBase,
  }) async {
    final httpUrl = (httpBase ?? kDefaultApiHttp).replaceAll(RegExp(r'/$'), '');
    final client = ColyseusClient(wsFromHttp(httpUrl));
    final opts = <String, dynamic>{
      'roomCode': roomCode.toUpperCase(),
      'role': 'player',
      'name': name,
    };
    final roomId = await lookupRoomId(roomCode, httpUrl);
    final ColyseusRoom<SchemaInstance> room = roomId != null
        ? await client.joinById<SchemaInstance>(roomId, options: opts)
        : await client.joinOrCreate<SchemaInstance>('match', options: opts);
    final kiag = KiagRoom._(room, room.sessionId);
    kiag._wire();
    return kiag;
  }
}
