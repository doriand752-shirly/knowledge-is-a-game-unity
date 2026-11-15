import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Décades Spin to Win: swipe / fling a wheel, then validate selected value.
/// Emits `spin_to_win_submit` `{value}`.
class SpinWheelChallenge extends StatefulWidget {
  const SpinWheelChallenge({
    super.key,
    required this.prompt,
    required this.options,
    required this.onSubmit,
  });

  final String prompt;
  final List<String> options;
  final void Function(String value) onSubmit;

  @override
  State<SpinWheelChallenge> createState() => _SpinWheelChallengeState();
}

class _SpinWheelChallengeState extends State<SpinWheelChallenge>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;
  double _angle = 0;
  double _velocity = 0;
  bool _spinning = false;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController.unbounded(vsync: this)
      ..addListener(() {
        if (!_spinning) return;
        setState(() {
          _angle = _ctrl.value;
          // friction via velocity decay handled by fling curve end
        });
      });
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  int get _selectedIndex {
    if (widget.options.isEmpty) return 0;
    final n = widget.options.length;
    final seg = (2 * math.pi) / n;
    // Wheel draws option i at angle i*seg; pointer at top (-pi/2).
    var a = (-_angle - math.pi / 2) % (2 * math.pi);
    if (a < 0) a += 2 * math.pi;
    return (a / seg).floor() % n;
  }

  String get _selected =>
      widget.options.isEmpty ? '' : widget.options[_selectedIndex];

  void _fling(double vx) {
    if (widget.options.isEmpty) return;
    _spinning = true;
    _velocity = vx.clamp(-40.0, 40.0);
    final end = _angle + _velocity * 1.8 + (_velocity.sign * (2.5 + math.Random().nextDouble()));
    _ctrl.value = _angle;
    _ctrl
        .animateTo(
          end,
          duration: Duration(milliseconds: (900 + _velocity.abs() * 40).round().clamp(700, 2800)),
          curve: Curves.easeOutCubic,
        )
        .whenComplete(() {
      if (!mounted) return;
      setState(() {
        _spinning = false;
        _angle = end;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    if (widget.options.isEmpty) {
      return const Text('Spin…');
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(widget.prompt, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
        const SizedBox(height: 4),
        const Text('Swipe horizontal pour faire tourner', style: TextStyle(color: Colors.black54)),
        const SizedBox(height: 16),
        Center(
          child: GestureDetector(
            onHorizontalDragUpdate: (d) {
              setState(() {
                _spinning = false;
                _ctrl.stop();
                _angle += d.delta.dx * 0.02;
              });
            },
            onHorizontalDragEnd: (d) {
              final v = (d.primaryVelocity ?? 0) / 400;
              if (v.abs() > 0.4) {
                _fling(v);
              } else {
                setState(() {});
              }
            },
            child: SizedBox(
              width: 260,
              height: 260,
              child: CustomPaint(
                painter: _WheelPainter(
                  options: widget.options,
                  angle: _angle,
                  selected: _selectedIndex,
                ),
                child: const SizedBox.expand(),
              ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          _selected,
          textAlign: TextAlign.center,
          style: const TextStyle(fontSize: 28, fontWeight: FontWeight.w900),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: OutlinedButton(
                onPressed: _spinning ? null : () => _fling(8 + math.Random().nextDouble() * 10),
                child: const Text('Tourner'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: FilledButton(
                onPressed: _spinning ? null : () => widget.onSubmit(_selected),
                child: const Text('Valider'),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _WheelPainter extends CustomPainter {
  _WheelPainter({required this.options, required this.angle, required this.selected});

  final List<String> options;
  final double angle;
  final int selected;

  static const _colors = [
    Color(0xFFE63946),
    Color(0xFF457B9D),
    Color(0xFF2A9D8F),
    Color(0xFFF4A261),
    Color(0xFF9B5DE5),
    Color(0xFFF4C430),
  ];

  @override
  void paint(Canvas canvas, Size size) {
    final c = size.center(Offset.zero);
    final r = size.shortestSide / 2;
    final n = options.length;
    final seg = 2 * math.pi / n;

    canvas.save();
    canvas.translate(c.dx, c.dy);
    canvas.rotate(angle);

    for (var i = 0; i < n; i++) {
      final paint = Paint()..color = _colors[i % _colors.length];
      canvas.drawArc(Rect.fromCircle(center: Offset.zero, radius: r), i * seg, seg, true, paint);
      final mid = i * seg + seg / 2;
      final tp = TextPainter(
        text: TextSpan(
          text: options[i],
          style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w800),
        ),
        textDirection: TextDirection.ltr,
      )..layout(maxWidth: r * 0.7);
      canvas.save();
      canvas.rotate(mid);
      tp.paint(canvas, Offset(r * 0.35, -tp.height / 2));
      canvas.restore();
    }

    canvas.drawCircle(Offset.zero, 18, Paint()..color = const Color(0xFF1F1630));
    canvas.restore();

    // Pointer at top
    final pointer = Path()
      ..moveTo(c.dx, c.dy - r - 4)
      ..lineTo(c.dx - 10, c.dy - r + 18)
      ..lineTo(c.dx + 10, c.dy - r + 18)
      ..close();
    canvas.drawPath(pointer, Paint()..color = const Color(0xFF1F1630));
  }

  @override
  bool shouldRepaint(covariant _WheelPainter old) =>
      old.angle != angle || old.selected != selected || old.options != options;
}
