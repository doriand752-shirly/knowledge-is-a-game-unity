import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:image/image.dart' as img;
import 'package:image_picker/image_picker.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'cast_data.dart';
import 'challenges/chain_drag.dart';
import 'challenges/fill_drag.dart';
import 'challenges/hazard_overlays.dart';
import 'challenges/linking_pairs.dart';
import 'challenges/sorting_swipe.dart';
import 'challenges/spin_wheel.dart';
import 'colyseus_native.dart';
import 'config.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const KiagApp());
}

class KiagApp extends StatelessWidget {
  const KiagApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: kBrand,
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFFE63946),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        fontFamily: 'Roboto',
      ),
      home: const JoinPage(),
    );
  }
}

class JoinPage extends StatefulWidget {
  const JoinPage({super.key});

  @override
  State<JoinPage> createState() => _JoinPageState();
}

class _JoinPageState extends State<JoinPage> {
  final _code = TextEditingController();
  final _name = TextEditingController();
  String? _err;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    SharedPreferences.getInstance().then((p) {
      _name.text = p.getString('kiag-name') ?? '';
      setState(() {});
    });
  }

  Future<void> _join() async {
    final code = _code.text.trim().toUpperCase();
    final name = _name.text.trim();
    if (code.length < 3 || name.isEmpty) {
      setState(() => _err = 'Code salle + pseudo requis');
      return;
    }
    setState(() {
      _busy = true;
      _err = null;
    });
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('kiag-name', name);
      final room = await KiagRoom.joinPlayer(roomCode: code, name: name);
      if (!mounted) return;
      await Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => GamePage(room: room, name: name)),
      );
    } catch (e) {
      setState(() => _err = 'Connexion impossible — le host Unity doit ouvrir la salle.\n$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _scan() async {
    final code = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const QrScanPage()),
    );
    if (code != null && code.isNotEmpty) {
      // Accept raw code or URL ending with /CODE
      final m = RegExp(r'([A-Z0-9]{4,6})$', caseSensitive: false).firstMatch(code.trim());
      _code.text = (m?.group(1) ?? code).toUpperCase();
      setState(() {});
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFFFFE4C4), Color(0xFF5EC8E8)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: SafeArea(
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              Text(kBrand, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900)),
              const SizedBox(height: 8),
              const Text('Scanne le QR sur la TV (Unity) ou entre le code.', style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 24),
              TextField(
                controller: _code,
                textCapitalization: TextCapitalization.characters,
                decoration: const InputDecoration(labelText: 'Code salle', border: OutlineInputBorder(), filled: true),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _name,
                decoration: const InputDecoration(labelText: 'Pseudo', border: OutlineInputBorder(), filled: true),
              ),
              const SizedBox(height: 16),
              FilledButton(onPressed: _busy ? null : _join, child: Text(_busy ? 'Connexion…' : 'Monter sur le plateau')),
              const SizedBox(height: 8),
              OutlinedButton.icon(onPressed: _scan, icon: const Icon(Icons.qr_code_scanner), label: const Text('Scanner QR')),
              if (_err != null) ...[
                const SizedBox(height: 12),
                Text(_err!, style: const TextStyle(color: Color(0xFFC9184A))),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class QrScanPage extends StatelessWidget {
  const QrScanPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Scanner QR')),
      body: MobileScanner(
        onDetect: (capture) {
          final raw = capture.barcodes.firstOrNull?.rawValue;
          if (raw != null) Navigator.pop(context, raw);
        },
      ),
    );
  }
}

class GamePage extends StatefulWidget {
  const GamePage({super.key, required this.room, required this.name});
  final KiagRoom room;
  final String name;

  @override
  State<GamePage> createState() => _GamePageState();
}

class _GamePageState extends State<GamePage> {
  Map<String, dynamic>? snap;
  String? toast;
  String? characterId;
  String? selfieDataUrl;
  String? targetId;
  int locksLeft = 3;
  double zipOpen = 0;
  StreamSubscription? _snapSub;
  StreamSubscription? _toastSub;

  @override
  void initState() {
    super.initState();
    _snapSub = widget.room.snapshots.listen((s) {
      setState(() => snap = s);
    });
    _toastSub = widget.room.toasts.listen((t) {
      setState(() => toast = t);
      Future.delayed(const Duration(seconds: 3), () {
        if (mounted && toast == t) setState(() => toast = null);
      });
    });
  }

  @override
  void dispose() {
    _snapSub?.cancel();
    _toastSub?.cancel();
    widget.room.leave();
    super.dispose();
  }

  Map<String, dynamic>? get me {
    final players = (snap?['players'] as List?) ?? const [];
    for (final p in players) {
      if (p is Map && p['sessionId'] == widget.room.sessionId) {
        return Map<String, dynamic>.from(p);
      }
    }
    return null;
  }

  List<Map<String, dynamic>> get others {
    final players = (snap?['players'] as List?) ?? const [];
    return players
        .whereType<Map>()
        .map((e) => Map<String, dynamic>.from(e))
        .where((p) => p['isHost'] != true && p['sessionId'] != widget.room.sessionId)
        .toList();
  }

  String get phase => snap?['phase']?.toString() ?? 'lobby';

  int get left {
    final ends = (snap?['phaseEndsAt'] as num?)?.toInt() ?? 0;
    final now = (snap?['serverNow'] as num?)?.toInt() ?? DateTime.now().millisecondsSinceEpoch;
    return ((ends - now) / 1000).ceil().clamp(0, 999);
  }

  Future<void> _pickCharacter(String id) async {
    setState(() => characterId = id);
    widget.room.send('set_character', {'characterId': id});
  }

  Future<void> _captureSelfie() async {
    final picker = ImagePicker();
    final file = await picker.pickImage(source: ImageSource.camera, maxWidth: 512, imageQuality: 85);
    if (file == null) return;
    final bytes = await file.readAsBytes();
    final decoded = img.decodeImage(bytes);
    if (decoded == null) return;
    final side = decoded.width < decoded.height ? decoded.width : decoded.height;
    final x = (decoded.width - side) ~/ 2;
    final y = (decoded.height - side) ~/ 2;
    var cropped = img.copyCrop(decoded, x: x, y: y, width: side, height: side);
    cropped = img.copyResize(cropped, width: 192, height: 192);
    // simple posterize clay-ish
    for (final p in cropped) {
      p.r = (p.r ~/ 36) * 36;
      p.g = (p.g ~/ 36) * 36;
      p.b = (p.b ~/ 36) * 36;
    }
    final jpg = img.encodeJpg(cropped, quality: 70);
    var dataUrl = 'data:image/jpeg;base64,${base64Encode(jpg)}';
    if (dataUrl.length > 150000 * 1.37) {
      final smaller = img.copyResize(cropped, width: 128, height: 128);
      dataUrl = 'data:image/jpeg;base64,${base64Encode(img.encodeJpg(smaller, quality: 55))}';
    }
    setState(() => selfieDataUrl = dataUrl);
    widget.room.send('set_selfie', {'dataUrl': dataUrl});
  }

  void _usePower(String id) {
    var tgt = targetId;
    if (tgt == null && others.length == 1) tgt = others.first['sessionId']?.toString();
    if ((id == 'points_party') == false && tgt == null && id != 'points_party') {
      // needs target for saboteages/bet — points_party exempt
      final needs = !const {'points_party'}.contains(id);
      if (needs && tgt == null) {
        setState(() => toast = 'Choisis une cible');
        return;
      }
    }
    widget.room.send('use_powerup', {
      'powerUpId': id,
      if (tgt != null) 'targetSessionId': tgt,
    });
  }

  @override
  Widget build(BuildContext context) {
    final hazard = me?['hazard']?.toString() ?? '';
    return Scaffold(
      appBar: AppBar(
        title: Text('${widget.name} · ${left}s'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: Center(child: Text('${me?['score'] ?? 0} pts', style: const TextStyle(fontWeight: FontWeight.w800))),
          ),
        ],
      ),
      body: Stack(
        children: [
          ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (snap?['hostLine'] != null)
                Card(
                  color: const Color(0xFF1F1630),
                  child: ListTile(
                    title: const Text('Maxime', style: TextStyle(color: Color(0xFFF4C430), fontWeight: FontWeight.bold)),
                    subtitle: Text('${snap!['hostLine']}', style: const TextStyle(color: Colors.white)),
                  ),
                ),
              const SizedBox(height: 8),
              Text('Phase: $phase', style: const TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 12),
              ..._phaseBody(hazard),
            ],
          ),
          if (hazard == 'gloop')
            Positioned.fill(
              child: HazardOverlays.gloop(onCleared: () => widget.room.send('clear_hazard')),
            ),
          if (hazard == 'bombles')
            Positioned.fill(
              child: HazardOverlays.bombles(onCleared: () => widget.room.send('clear_hazard')),
            ),
          if (hazard == 'disco_inferno')
            IgnorePointer(
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                color: Colors.purple.withValues(alpha: 0.25),
              ),
            ),
          if (toast != null)
            Positioned(
              left: 16,
              right: 16,
              bottom: 24,
              child: Material(
                elevation: 6,
                borderRadius: BorderRadius.circular(12),
                color: const Color(0xFF1F1630),
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Text(toast!, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700)),
                ),
              ),
            ),
        ],
      ),
    );
  }

  List<Widget> _phaseBody(String hazard) {
    switch (phase) {
      case 'lobby':
        return [
          const Text('Choisis ton personnage', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: castCharacters.map((c) {
              final selected = characterId == c.id || me?['characterId'] == c.id;
              return ChoiceChip(
                selected: selected,
                label: Text(c.name.split(' ').last),
                onSelected: (_) => _pickCharacter(c.id),
                selectedColor: Color(c.color).withValues(alpha: 0.35),
              );
            }).toList(),
          ),
          const SizedBox(height: 16),
          FilledButton.icon(
            onPressed: _captureSelfie,
            icon: const Icon(Icons.camera_alt),
            label: const Text('Selfie clay'),
          ),
          if (selfieDataUrl != null || (me?['selfieDataUrl']?.toString().isNotEmpty ?? false))
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: CircleAvatar(
                radius: 48,
                backgroundImage: MemoryImage(
                  base64Decode(
                    (selfieDataUrl ?? me!['selfieDataUrl'].toString()).split(',').last,
                  ),
                ),
              ),
            ),
          const SizedBox(height: 12),
          const Text('Attends le démarrage sur la TV Unity (modes Classique / Décades / Rapide).'),
        ];
      case 'theme_vote':
        final themes = _decodeList(snap?['themeOptionsJson']);
        return [
          const Text('Choisis une porte', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
          ...themes.map((t) {
            final id = t['id']?.toString() ?? '';
            return Padding(
              padding: const EdgeInsets.only(top: 8),
              child: FilledButton(
                style: FilledButton.styleFrom(backgroundColor: _parseColor(t['color'])),
                onPressed: () => widget.room.send('vote_theme', {'themeId': id}),
                child: Text('${t['emoji'] ?? ''} ${t['label'] ?? id}'),
              ),
            );
          }),
        ];
      case 'power_play':
        final charges = _decodeMap(me?['powerUpsJson']);
        return [
          const Text('Power Plays', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
          const Text('1) Cible  2) Lance'),
          Wrap(
            spacing: 8,
            children: others.map((o) {
              final id = o['sessionId']?.toString() ?? '';
              return ChoiceChip(
                selected: targetId == id,
                label: Text('${o['name']}'),
                onSelected: (_) => setState(() => targetId = id),
              );
            }).toList(),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: charges.entries.map((e) {
              final n = (e.value as num?)?.toInt() ?? 0;
              return FilledButton(
                onPressed: n > 0 ? () => _usePower(e.key) : null,
                child: Text('${e.key} ×$n'),
              );
            }).toList(),
          ),
        ];
      case 'question':
        final choices = _decodeList(snap?['question']?['choicesJson'] ?? snap?['question']?['choicesJson']);
        final qChoices = _decodeList((snap?['question'] as Map?)?['choicesJson']);
        final list = qChoices.isNotEmpty ? qChoices : choices;
        if (hazard == 'freeze' || me?['frozen'] == true) {
          return [
            const Text('Freeze ! Tape pour fissurer la glace.', style: TextStyle(fontWeight: FontWeight.w800)),
            const SizedBox(height: 12),
            SizedBox(
              height: 280,
              child: Stack(
                children: [
                  HazardOverlays.freeze(onCleared: () {
                    widget.room.send('clear_hazard');
                    setState(() {});
                  }),
                ],
              ),
            ),
          ];
        }
        if (hazard == 'lockdown' && locksLeft > 0) {
          return [
            Text('Lockdown ($locksLeft)'),
            FilledButton(
              onPressed: () {
                setState(() => locksLeft--);
                if (locksLeft <= 0) widget.room.send('clear_hazard');
              },
              child: const Text('Ouvrir cadenas'),
            ),
          ];
        }
        if (hazard == 'zipper' && zipOpen < 100) {
          return [
            const Text('Zipper'),
            Slider(
              value: zipOpen,
              max: 100,
              onChanged: (v) {
                setState(() => zipOpen = v);
                if (v >= 100) widget.room.send('clear_hazard');
              },
            ),
          ];
        }
        if (me?['hasAnswered'] == true) {
          return [const Text('Locké — regarde la TV')];
        }
        return [
          Text('${(snap?['question'] as Map?)?['prompt'] ?? ''}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          ...List.generate(list.length, (i) {
            final c = list[i];
            final id = c['id']?.toString() ?? '$i';
            var label = c['label']?.toString() ?? id;
            if (hazard == 'nibblers') {
              label = label.replaceAll(RegExp(r'[aeiouyAEIOUY]'), '·');
            }
            final letter = ['A', 'B', 'C', 'D'][i.clamp(0, 3)];
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: FilledButton(
                onPressed: () => widget.room.send('submit_answer', {'value': id}),
                child: Text('$letter. $label'),
              ),
            );
          }),
        ];
      case 'votes_show':
        return [const Text('Suspense… pads sur la TV', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w900))];
      case 'reveal':
        final reveal = KiagRoom.asMap(
          snap?['lastRevealJson'] is String
              ? jsonDecode(snap!['lastRevealJson'] as String)
              : snap?['lastRevealJson'],
        );
        return [Text('Réponse : ${reveal?['correctAnswer'] ?? '—'}')];
      case 'linking':
        return _linkingBody();
      case 'sorting':
        return _sortingBody();
      case 'chain_reaction':
        return _chainBody();
      case 'fill_blanks':
        return _fillBody();
      case 'odd_one_out':
        return _oddBody();
      case 'spin_to_win':
        return _spinBody();
      case 'pyramid':
        return _pyramidBody();
      case 'podium':
        return [const Text('Podium — bravo !', style: TextStyle(fontSize: 22, fontWeight: FontWeight.w900))];
      default:
        return [Text('… $phase')];
    }
  }

  List<Widget> _linkingBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final lefts = (ch['lefts'] as List?)?.map((e) => e.toString()).toList() ?? [];
    final rights = (ch['rights'] as List?)?.map((e) => e.toString()).toList() ?? [];
    return [
      LinkingPairsChallenge(
        title: '${ch['title'] ?? 'Linking'}',
        lefts: lefts,
        rights: rights,
        onSubmit: (links) => widget.room.send('linking_submit', {'links': links}),
      ),
    ];
  }

  List<Widget> _sortingBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final items = (ch['items'] as List?)
            ?.whereType<Map>()
            .map((e) => Map<String, dynamic>.from(e))
            .toList() ??
        [];
    return [
      SortingSwipeChallenge(
        title: '${ch['title'] ?? 'Sorting'}',
        leftLabel: '${ch['leftLabel'] ?? 'Gauche'}',
        rightLabel: '${ch['rightLabel'] ?? 'Droite'}',
        items: items,
        onSubmit: (itemId, side) => widget.room.send('sorting_submit', {'itemId': itemId, 'side': side}),
      ),
    ];
  }

  List<Widget> _chainBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final steps = (ch['steps'] as List?)
            ?.whereType<Map>()
            .map((e) => Map<String, dynamic>.from(e))
            .toList() ??
        [];
    final choices = (ch['choices'] as List?)?.map((e) => e.toString()).toList() ?? [];
    return [
      ChainDragChallenge(
        title: '${ch['title'] ?? 'Chain'}',
        start: '${ch['start'] ?? ''}',
        steps: steps,
        choices: choices,
        onSubmit: (answers) => widget.room.send('chain_submit', {'answers': answers}),
      ),
    ];
  }

  List<Widget> _fillBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final blanks = (ch['blanks'] as List?)
            ?.whereType<Map>()
            .map((e) => Map<String, dynamic>.from(e))
            .toList() ??
        [];
    return [
      FillDragChallenge(
        sentence: '${ch['sentence'] ?? ''}',
        blanks: blanks,
        onSubmit: (answers) => widget.room.send('fill_blanks_submit', {'answers': answers}),
      ),
    ];
  }

  List<Widget> _oddBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final items = (ch['items'] as List?)?.whereType<Map>().toList() ?? [];
    return [
      Text('${ch['prompt'] ?? 'Intrus'}', style: const TextStyle(fontWeight: FontWeight.w900)),
      ...items.map((it) {
        final id = it['id']?.toString() ?? '';
        return Padding(
          padding: const EdgeInsets.only(top: 8),
          child: FilledButton(
            onPressed: () => widget.room.send('odd_one_out_submit', {'itemId': id}),
            child: Text('${it['label']}'),
          ),
        );
      }),
    ];
  }

  List<Widget> _spinBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    final options = (ch['options'] as List?)?.map((e) => e.toString()).toList() ?? [];
    return [
      SpinWheelChallenge(
        prompt: '${ch['prompt'] ?? 'Spin'}',
        options: options,
        onSubmit: (value) => widget.room.send('spin_to_win_submit', {'value': value}),
      ),
    ];
  }

  List<Widget> _pyramidBody() {
    final ch = KiagRoom.asMap(jsonDecode(snap?['challengeJson'] ?? '{}')) ?? {};
    if (ch['kind'] == 'scroll') {
      return [Text('📜 ${ch['fact'] ?? snap?['scrollFact'] ?? ''}')];
    }
    final q = ch['question'] is Map ? Map<String, dynamic>.from(ch['question'] as Map) : null;
    final choices = (q?['choices'] as List?)?.whereType<Map>().toList() ?? [];
    return [
      Text('Pyramide · marche ${(me?['pyramidStep'] ?? 0) + 1}', style: const TextStyle(fontWeight: FontWeight.w900)),
      Text('${q?['prompt'] ?? (snap?['question'] as Map?)?['prompt'] ?? ''}'),
      ...choices.map((c) {
        final id = c['id']?.toString() ?? '';
        return Padding(
          padding: const EdgeInsets.only(top: 8),
          child: FilledButton(
            onPressed: () => widget.room.send('pyramid_answer', {'value': id}),
            child: Text('${c['label']}'),
          ),
        );
      }),
    ];
  }

  List<Map<String, dynamic>> _decodeList(dynamic raw) {
    try {
      final v = raw is String ? jsonDecode(raw) : raw;
      if (v is List) {
        return v.whereType<Map>().map((e) => Map<String, dynamic>.from(e)).toList();
      }
    } catch (_) {}
    return [];
  }

  Map<String, dynamic> _decodeMap(dynamic raw) {
    try {
      final v = raw is String ? jsonDecode(raw) : raw;
      if (v is Map) return Map<String, dynamic>.from(v);
    } catch (_) {}
    return {};
  }

  Color _parseColor(dynamic raw) {
    final s = raw?.toString() ?? '#E63946';
    final hex = s.replaceAll('#', '');
    return Color(int.parse(hex.length == 6 ? 'FF$hex' : hex, radix: 16));
  }
}
