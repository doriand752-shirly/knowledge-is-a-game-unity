import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Power Play hazard gesture overlays — same `clear_hazard` Colyseus message.
class HazardOverlays {
  HazardOverlays._();

  /// Freeze: tap cracks until barriers break.
  static Widget freeze({required VoidCallback onCleared}) {
    return _FreezeCrackOverlay(onCleared: onCleared);
  }

  /// Gloop: pan-wipe slime off the screen.
  static Widget gloop({required VoidCallback onCleared, int wipesNeeded = 14}) {
    return _GloopWipeOverlay(onCleared: onCleared, wipesNeeded: wipesNeeded);
  }

  /// Bombles: tap floating bombs before they explode.
  static Widget bombles({required VoidCallback onCleared, int bombCount = 5}) {
    return _BombTapOverlay(onCleared: onCleared, bombCount: bombCount);
  }
}

class _FreezeCrackOverlay extends StatefulWidget {
  const _FreezeCrackOverlay({required this.onCleared});
  final VoidCallback onCleared;

  @override
  State<_FreezeCrackOverlay> createState() => _FreezeCrackOverlayState();
}

class _FreezeCrackOverlayState extends State<_FreezeCrackOverlay> {
  static const _barriers = 5;
  int _cracks = 0;
  final _hits = <Offset>[];

  void _tap(TapDownDetails d) {
    setState(() {
      _cracks++;
      _hits.add(d.localPosition);
    });
    if (_cracks >= _barriers) widget.onCleared();
  }

  @override
  Widget build(BuildContext context) {
    final progress = (_cracks / _barriers).clamp(0.0, 1.0);
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTapDown: _tap,
      child: CustomPaint(
        painter: _IcePainter(cracks: _hits, progress: progress),
        child: Center(
          child: Text(
            _cracks >= _barriers ? 'Brisé !' : 'FREEZE — tape pour fissurer (${_barriers - _cracks})',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.white.withValues(alpha: 0.95),
              fontSize: 22,
              fontWeight: FontWeight.w900,
              shadows: const [Shadow(blurRadius: 8, color: Colors.black54)],
            ),
          ),
        ),
      ),
    );
  }
}

class _IcePainter extends CustomPainter {
  _IcePainter({required this.cracks, required this.progress});
  final List<Offset> cracks;
  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final ice = Paint()..color = Color.lerp(const Color(0xCC7EC8E8), const Color(0x557EC8E8), progress)!;
    canvas.drawRect(Offset.zero & size, ice);

    final crackPaint = Paint()
      ..color = Colors.white
      ..strokeWidth = 2.2
      ..style = PaintingStyle.stroke;

    for (final o in cracks) {
      final rng = math.Random(o.dx.toInt() * 31 + o.dy.toInt());
      for (var i = 0; i < 5; i++) {
        final ang = rng.nextDouble() * math.pi * 2;
        final len = 28.0 + rng.nextDouble() * 48;
        canvas.drawLine(o, o + Offset(math.cos(ang) * len, math.sin(ang) * len), crackPaint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _IcePainter old) =>
      old.cracks.length != cracks.length || old.progress != progress;
}

class _GloopWipeOverlay extends StatefulWidget {
  const _GloopWipeOverlay({required this.onCleared, required this.wipesNeeded});
  final VoidCallback onCleared;
  final int wipesNeeded;

  @override
  State<_GloopWipeOverlay> createState() => _GloopWipeOverlayState();
}

class _GloopWipeOverlayState extends State<_GloopWipeOverlay> {
  int _wipes = 0;
  final _strokes = <List<Offset>>[];
  List<Offset>? _current;

  void _start(DragStartDetails d) {
    _current = [d.localPosition];
    setState(() => _strokes.add(_current!));
  }

  void _update(DragUpdateDetails d) {
    setState(() {
      _current?.add(d.localPosition);
      _wipes++;
    });
    if (_wipes >= widget.wipesNeeded) widget.onCleared();
  }

  void _end(DragEndDetails _) => _current = null;

  @override
  Widget build(BuildContext context) {
    final cleared = (_wipes / widget.wipesNeeded).clamp(0.0, 1.0);
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onPanStart: _start,
      onPanUpdate: _update,
      onPanEnd: _end,
      child: CustomPaint(
        painter: _GloopPainter(strokes: _strokes, cleared: cleared),
        child: Center(
          child: Text(
            cleared >= 1 ? 'Propre !' : 'GLOOP — essuie (${(cleared * 100).round()}%)',
            style: const TextStyle(
              color: Colors.white,
              fontSize: 22,
              fontWeight: FontWeight.w900,
              shadows: [Shadow(blurRadius: 6, color: Colors.black45)],
            ),
          ),
        ),
      ),
    );
  }
}

class _GloopPainter extends CustomPainter {
  _GloopPainter({required this.strokes, required this.cleared});
  final List<List<Offset>> strokes;
  final double cleared;

  @override
  void paint(Canvas canvas, Size size) {
    final slime = Paint()..color = Color.lerp(const Color(0xDD1FA6A0), const Color(0x331FA6A0), cleared)!;
    canvas.drawRect(Offset.zero & size, slime);

    final wipe = Paint()
      ..color = const Color(0x55FFFFFF)
      ..strokeWidth = 42
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke
      ..blendMode = BlendMode.dstOut;

    // Re-draw slime then cut wipes on a layered approach via soft white trails
    final trail = Paint()
      ..color = const Color(0x66FFF6E8)
      ..strokeWidth = 36
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;

    for (final s in strokes) {
      if (s.length < 2) continue;
      final path = Path()..moveTo(s.first.dx, s.first.dy);
      for (var i = 1; i < s.length; i++) {
        path.lineTo(s[i].dx, s[i].dy);
      }
      canvas.drawPath(path, trail);
      canvas.drawPath(path, wipe);
    }
  }

  @override
  bool shouldRepaint(covariant _GloopPainter old) =>
      old.strokes.length != strokes.length || old.cleared != cleared;
}

class _BombTapOverlay extends StatefulWidget {
  const _BombTapOverlay({required this.onCleared, required this.bombCount});
  final VoidCallback onCleared;
  final int bombCount;

  @override
  State<_BombTapOverlay> createState() => _BombTapOverlayState();
}

class _BombTapOverlayState extends State<_BombTapOverlay> with SingleTickerProviderStateMixin {
  late final List<_Bomb> _bombs;
  late final AnimationController _pulse;

  @override
  void initState() {
    super.initState();
    final rng = math.Random(7);
    _bombs = List.generate(widget.bombCount, (i) {
      return _Bomb(
        id: i,
        x: 0.12 + rng.nextDouble() * 0.76,
        y: 0.18 + rng.nextDouble() * 0.64,
      );
    });
    _pulse = AnimationController(vsync: this, duration: const Duration(milliseconds: 700))
      ..repeat(reverse: true);
  }

  @override
  void dispose() {
    _pulse.dispose();
    super.dispose();
  }

  void _tapBomb(int id) {
    setState(() => _bombs.removeWhere((b) => b.id == id));
    if (_bombs.isEmpty) widget.onCleared();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _pulse,
      builder: (context, _) {
        return Stack(
          children: [
            Container(color: const Color(0x66000000)),
            Center(
              child: Text(
                _bombs.isEmpty ? 'Désamorcé !' : 'BOMBES — tape ${_bombs.length}',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 22,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
            ..._bombs.map((b) {
              return Align(
                alignment: Alignment(b.x * 2 - 1, b.y * 2 - 1),
                child: GestureDetector(
                  onTap: () => _tapBomb(b.id),
                  child: Transform.scale(
                    scale: 1 + 0.08 * _pulse.value,
                    child: Container(
                      width: 72,
                      height: 72,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color.lerp(const Color(0xFFE63946), const Color(0xFFF4A261), _pulse.value),
                        boxShadow: const [
                          BoxShadow(color: Colors.black45, blurRadius: 8, offset: Offset(0, 3)),
                        ],
                        border: Border.all(color: Colors.black87, width: 3),
                      ),
                      alignment: Alignment.center,
                      child: const Text('B',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 28,
                            fontWeight: FontWeight.w900,
                          )),
                    ),
                  ),
                ),
              );
            }),
          ],
        );
      },
    );
  }
}

class _Bomb {
  _Bomb({required this.id, required this.x, required this.y});
  final int id;
  final double x;
  final double y;
}
