import 'package:flutter/material.dart';

/// Décades Fill the Blanks: drag options into blank slots.
/// Emits `fill_blanks_submit` `{answers:{}}`.
class FillDragChallenge extends StatefulWidget {
  const FillDragChallenge({
    super.key,
    required this.sentence,
    required this.blanks,
    required this.onSubmit,
  });

  final String sentence;
  final List<Map<String, dynamic>> blanks;
  final void Function(Map<String, String> answers) onSubmit;

  @override
  State<FillDragChallenge> createState() => _FillDragChallengeState();
}

class _FillDragChallengeState extends State<FillDragChallenge> {
  final _answers = <String, String>{};
  late final Map<String, List<String>> _pools;

  @override
  void initState() {
    super.initState();
    _pools = {
      for (final b in widget.blanks)
        (b['id']?.toString() ?? ''): List<String>.from(
          (b['options'] as List?)?.map((e) => e.toString()) ?? const [],
        ),
    };
  }

  List<String> get _allUnused {
    final used = _answers.values.toSet();
    final out = <String>[];
    for (final opts in _pools.values) {
      for (final o in opts) {
        if (!used.contains(o) && !out.contains(o)) out.add(o);
      }
    }
    return out;
  }

  @override
  Widget build(BuildContext context) {
    final ready = widget.blanks.every((b) {
      final id = b['id']?.toString() ?? '';
      return (_answers[id] ?? '').isNotEmpty;
    });
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(widget.sentence, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        const SizedBox(height: 8),
        const Text('Glisse un mot dans chaque trou', style: TextStyle(color: Colors.black54)),
        const SizedBox(height: 12),
        ...widget.blanks.map((b) {
          final id = b['id']?.toString() ?? '';
          final filled = _answers[id];
          return Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: DragTarget<String>(
              onWillAcceptWithDetails: (_) => true,
              onAcceptWithDetails: (d) => setState(() => _answers[id] = d.data),
              builder: (context, candidate, _) {
                final hovering = candidate.isNotEmpty;
                return InkWell(
                  onTap: filled == null ? null : () => setState(() => _answers.remove(id)),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 120),
                    width: double.infinity,
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
                    decoration: BoxDecoration(
                      color: hovering
                          ? const Color(0xFFF4C430)
                          : (filled != null ? const Color(0xFF457B9D) : const Color(0xFFE8E0D4)),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.black26, width: 2),
                    ),
                    child: Text(
                      filled ?? 'Trou $id',
                      style: TextStyle(
                        fontWeight: FontWeight.w800,
                        color: filled != null || hovering ? Colors.white : Colors.black45,
                      ),
                    ),
                  ),
                );
              },
            ),
          );
        }),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: _allUnused.map((c) {
            return Draggable<String>(
              data: c,
              feedback: Material(
                elevation: 6,
                borderRadius: BorderRadius.circular(10),
                child: _Opt(label: c, active: true),
              ),
              childWhenDragging: Opacity(opacity: 0.35, child: _Opt(label: c)),
              child: _Opt(label: c),
            );
          }).toList(),
        ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: ready ? () => widget.onSubmit(Map.from(_answers)) : null,
          child: const Text('Valider'),
        ),
      ],
    );
  }
}

class _Opt extends StatelessWidget {
  const _Opt({required this.label, this.active = false});
  final String label;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: active ? const Color(0xFFE63946) : const Color(0xFF1B3A4B),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(label, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800)),
    );
  }
}
