import 'package:flutter/material.dart';

/// APK-faithful Sorting: one item at a time, swipe into Left / Right buckets.
/// Emits `sorting_submit` payload via [onSubmit]: `{itemId, side:'L'|'R'}`.
class SortingSwipeChallenge extends StatefulWidget {
  const SortingSwipeChallenge({
    super.key,
    required this.title,
    required this.leftLabel,
    required this.rightLabel,
    required this.items,
    required this.onSubmit,
  });

  final String title;
  final String leftLabel;
  final String rightLabel;
  final List<Map<String, dynamic>> items;
  final void Function(String itemId, String side) onSubmit;

  @override
  State<SortingSwipeChallenge> createState() => _SortingSwipeChallengeState();
}

class _SortingSwipeChallengeState extends State<SortingSwipeChallenge> {
  int _index = 0;
  Offset _drag = Offset.zero;
  bool _busy = false;

  static const _threshold = 110.0;

  Map<String, dynamic>? get _current {
    if (_index >= widget.items.length) return null;
    return widget.items[_index];
  }

  void _commit(String side) {
    final cur = _current;
    if (cur == null || _busy) return;
    final id = cur['id']?.toString() ?? '';
    if (id.isEmpty) return;
    setState(() {
      _busy = true;
      _drag = Offset(side == 'L' ? -320 : 320, 0);
    });
    widget.onSubmit(id, side);
    Future.delayed(const Duration(milliseconds: 180), () {
      if (!mounted) return;
      setState(() {
        _index++;
        _drag = Offset.zero;
        _busy = false;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    final cur = _current;
    final remaining = (widget.items.length - _index).clamp(0, 999);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(widget.title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
        const SizedBox(height: 4),
        Text('$remaining restant${remaining == 1 ? '' : 's'} — swipe L / R',
            style: const TextStyle(color: Colors.black54)),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _Bucket(
                label: widget.leftLabel,
                color: const Color(0xFFE63946),
                highlight: _drag.dx < -40,
                arrow: Icons.arrow_back,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _Bucket(
                label: widget.rightLabel,
                color: const Color(0xFF2A9D8F),
                highlight: _drag.dx > 40,
                arrow: Icons.arrow_forward,
              ),
            ),
          ],
        ),
        const SizedBox(height: 28),
        SizedBox(
          height: 180,
          child: cur == null
              ? const Center(
                  child: Text(
                    'Terminé — regarde la TV',
                    style: TextStyle(fontWeight: FontWeight.w800, fontSize: 18),
                  ),
                )
              : GestureDetector(
                  onHorizontalDragUpdate: _busy
                      ? null
                      : (d) => setState(() => _drag += Offset(d.delta.dx, 0)),
                  onHorizontalDragEnd: _busy
                      ? null
                      : (d) {
                          final vx = d.primaryVelocity ?? 0;
                          if (_drag.dx <= -_threshold || vx < -600) {
                            _commit('L');
                          } else if (_drag.dx >= _threshold || vx > 600) {
                            _commit('R');
                          } else {
                            setState(() => _drag = Offset.zero);
                          }
                        },
                  onHorizontalDragCancel: () => setState(() => _drag = Offset.zero),
                  child: Transform.translate(
                    offset: Offset(_drag.dx, _drag.dy * 0.15),
                    child: Transform.rotate(
                      angle: _drag.dx * 0.0015,
                      child: Card(
                        elevation: 8,
                        color: const Color(0xFF1F1630),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
                        child: Center(
                          child: Padding(
                            padding: const EdgeInsets.all(24),
                            child: Text(
                              '${cur['label'] ?? ''}',
                              textAlign: TextAlign.center,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 26,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
        ),
        if (cur != null) ...[
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              OutlinedButton.icon(
                onPressed: _busy ? null : () => _commit('L'),
                icon: const Icon(Icons.arrow_back),
                label: Text(widget.leftLabel),
              ),
              OutlinedButton.icon(
                onPressed: _busy ? null : () => _commit('R'),
                icon: const Icon(Icons.arrow_forward),
                label: Text(widget.rightLabel),
              ),
            ],
          ),
        ],
      ],
    );
  }
}

class _Bucket extends StatelessWidget {
  const _Bucket({
    required this.label,
    required this.color,
    required this.highlight,
    required this.arrow,
  });

  final String label;
  final Color color;
  final bool highlight;
  final IconData arrow;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 120),
      padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 10),
      decoration: BoxDecoration(
        color: highlight ? color : color.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color, width: highlight ? 3 : 1.5),
      ),
      child: Column(
        children: [
          Icon(arrow, color: highlight ? Colors.white : color),
          const SizedBox(height: 4),
          Text(
            label,
            textAlign: TextAlign.center,
            style: TextStyle(
              color: highlight ? Colors.white : color,
              fontWeight: FontWeight.w900,
            ),
          ),
        ],
      ),
    );
  }
}
