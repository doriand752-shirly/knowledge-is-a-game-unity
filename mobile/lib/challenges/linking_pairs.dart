import 'package:flutter/material.dart';

/// APK-faithful Linking: drag answer→answer, draw pair lines, cancel mid-drag.
/// Emits the same Colyseus payload via [onSubmit]: `{links:[{left,right}]}`.
class LinkingPairsChallenge extends StatefulWidget {
  const LinkingPairsChallenge({
    super.key,
    required this.title,
    required this.lefts,
    required this.rights,
    required this.onSubmit,
  });

  final String title;
  final List<String> lefts;
  final List<String> rights;
  final void Function(List<Map<String, String>> links) onSubmit;

  @override
  State<LinkingPairsChallenge> createState() => _LinkingPairsChallengeState();
}

class _LinkingPairsChallengeState extends State<LinkingPairsChallenge> {
  final _links = <String, String>{};
  String? _dragFrom;
  Offset? _dragPos;
  final _leftKeys = <String, GlobalKey>{};
  final _rightKeys = <String, GlobalKey>{};
  final _stackKey = GlobalKey();

  @override
  void initState() {
    super.initState();
    for (final l in widget.lefts) {
      _leftKeys[l] = GlobalKey();
    }
    for (final r in widget.rights) {
      _rightKeys[r] = GlobalKey();
    }
  }

  Offset? _centerOf(GlobalKey key) {
    final box = key.currentContext?.findRenderObject() as RenderBox?;
    final stack = _stackKey.currentContext?.findRenderObject() as RenderBox?;
    if (box == null || stack == null) return null;
    final global = box.localToGlobal(box.size.center(Offset.zero));
    return stack.globalToLocal(global);
  }

  String? _hitRight(Offset local) {
    final stack = _stackKey.currentContext?.findRenderObject() as RenderBox?;
    if (stack == null) return null;
    final global = stack.localToGlobal(local);
    for (final e in _rightKeys.entries) {
      final box = e.value.currentContext?.findRenderObject() as RenderBox?;
      if (box == null) continue;
      final tl = box.localToGlobal(Offset.zero);
      final rect = tl & box.size;
      if (rect.contains(global)) return e.key;
    }
    return null;
  }

  void _begin(String left, Offset local) {
    setState(() {
      _dragFrom = left;
      _dragPos = local;
      _links.remove(left);
    });
  }

  void _update(Offset local) => setState(() => _dragPos = local);

  void _end() {
    final from = _dragFrom;
    final pos = _dragPos;
    if (from == null || pos == null) {
      setState(() {
        _dragFrom = null;
        _dragPos = null;
      });
      return;
    }
    final hit = _hitRight(pos);
    setState(() {
      if (hit != null) {
        // One right answer per link — drop previous owner of this right.
        _links.removeWhere((_, r) => r == hit);
        _links[from] = hit;
      }
      _dragFrom = null;
      _dragPos = null;
    });
  }

  void _cancel() => setState(() {
        _dragFrom = null;
        _dragPos = null;
      });

  List<Map<String, String>> get _payload => widget.lefts
      .where((l) => (_links[l] ?? '').isNotEmpty)
      .map((l) => {'left': l, 'right': _links[l]!})
      .toList();

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(widget.title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
        const SizedBox(height: 4),
        const Text('Glisse une réponse vers sa paire', style: TextStyle(color: Colors.black54)),
        const SizedBox(height: 12),
        SizedBox(
          height: (widget.lefts.length * 64.0).clamp(220.0, 480.0),
          child: KeyedSubtree(
            key: _stackKey,
            child: LayoutBuilder(
              builder: (context, constraints) {
                return GestureDetector(
                  onPanUpdate: _dragFrom == null
                      ? null
                      : (d) {
                          final box = _stackKey.currentContext?.findRenderObject() as RenderBox?;
                          if (box == null) return;
                          _update(box.globalToLocal(d.globalPosition));
                        },
                  onPanEnd: _dragFrom == null ? null : (_) => _end(),
                  onPanCancel: _dragFrom == null ? null : _cancel,
                  child: Stack(
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Column(
                              children: widget.lefts.map((l) {
                                final linked = _links.containsKey(l);
                                return Padding(
                                  padding: const EdgeInsets.only(bottom: 8),
                                  child: GestureDetector(
                                    onPanStart: (d) {
                                      final box = _stackKey.currentContext?.findRenderObject() as RenderBox?;
                                      if (box == null) return;
                                      _begin(l, box.globalToLocal(d.globalPosition));
                                    },
                                    child: Container(
                                      key: _leftKeys[l],
                                      width: double.infinity,
                                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
                                      decoration: BoxDecoration(
                                        color: linked ? const Color(0xFF2A9D8F) : const Color(0xFFE63946),
                                        borderRadius: BorderRadius.circular(12),
                                      ),
                                      child: Text(
                                        l,
                                        style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800),
                                      ),
                                    ),
                                  ),
                                );
                              }).toList(),
                            ),
                          ),
                          const SizedBox(width: 48),
                          Expanded(
                            child: Column(
                              children: widget.rights.map((r) {
                                final taken = _links.containsValue(r);
                                return Padding(
                                  padding: const EdgeInsets.only(bottom: 8),
                                  child: Container(
                                    key: _rightKeys[r],
                                    width: double.infinity,
                                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
                                    decoration: BoxDecoration(
                                      color: taken ? const Color(0xFF457B9D) : const Color(0xFF1B3A4B),
                                      borderRadius: BorderRadius.circular(12),
                                      border: Border.all(
                                        color: taken ? const Color(0xFFF4C430) : Colors.transparent,
                                        width: 2,
                                      ),
                                    ),
                                    child: Text(
                                      r,
                                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800),
                                    ),
                                  ),
                                );
                              }).toList(),
                            ),
                          ),
                        ],
                      ),
                      Positioned.fill(
                        child: IgnorePointer(
                          child: CustomPaint(
                            painter: _LinkLinesPainter(
                              links: Map.from(_links),
                              leftCenters: {
                                for (final l in widget.lefts)
                                  if (_centerOf(_leftKeys[l]!) != null) l: _centerOf(_leftKeys[l]!)!,
                              },
                              rightCenters: {
                                for (final r in widget.rights)
                                  if (_centerOf(_rightKeys[r]!) != null) r: _centerOf(_rightKeys[r]!)!,
                              },
                              dragFrom: _dragFrom,
                              dragPos: _dragPos,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ),
        ),
        const SizedBox(height: 8),
        FilledButton(
          onPressed: _payload.length == widget.lefts.length
              ? () => widget.onSubmit(_payload)
              : (_payload.isNotEmpty ? () => widget.onSubmit(_payload) : null),
          child: Text('Valider (${_payload.length}/${widget.lefts.length})'),
        ),
      ],
    );
  }
}

class _LinkLinesPainter extends CustomPainter {
  _LinkLinesPainter({
    required this.links,
    required this.leftCenters,
    required this.rightCenters,
    this.dragFrom,
    this.dragPos,
  });

  final Map<String, String> links;
  final Map<String, Offset> leftCenters;
  final Map<String, Offset> rightCenters;
  final String? dragFrom;
  final Offset? dragPos;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFFF4C430)
      ..strokeWidth = 3.5
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    for (final e in links.entries) {
      final a = leftCenters[e.key];
      final b = rightCenters[e.value];
      if (a == null || b == null) continue;
      canvas.drawLine(a, b, paint);
    }

    if (dragFrom != null && dragPos != null) {
      final a = leftCenters[dragFrom!];
      if (a != null) {
        final ghost = paint
          ..color = const Color(0xAAF4C430)
          ..strokeWidth = 2.5;
        canvas.drawLine(a, dragPos!, ghost);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _LinkLinesPainter old) => true;
}
