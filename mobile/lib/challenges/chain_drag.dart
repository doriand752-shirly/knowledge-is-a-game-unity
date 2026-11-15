import 'package:flutter/material.dart';

/// Décades Chain Reaction: drag choices into ordered step slots.
/// Emits `chain_submit` `{answers:[]}`.
class ChainDragChallenge extends StatefulWidget {
  const ChainDragChallenge({
    super.key,
    required this.title,
    required this.start,
    required this.steps,
    required this.choices,
    required this.onSubmit,
  });

  final String title;
  final String start;
  final List<Map<String, dynamic>> steps;
  final List<String> choices;
  final void Function(List<String> answers) onSubmit;

  @override
  State<ChainDragChallenge> createState() => _ChainDragChallengeState();
}

class _ChainDragChallengeState extends State<ChainDragChallenge> {
  late final List<String?> _slots;
  late final List<String> _pool;

  @override
  void initState() {
    super.initState();
    _slots = List<String?>.filled(widget.steps.length, null);
    _pool = List<String>.from(widget.choices);
  }

  void _place(int slot, String value) {
    setState(() {
      final prev = _slots[slot];
      if (prev != null && !_pool.contains(prev)) _pool.add(prev);
      for (var i = 0; i < _slots.length; i++) {
        if (_slots[i] == value) _slots[i] = null;
      }
      _slots[slot] = value;
      _pool.remove(value);
    });
  }

  void _clearSlot(int slot) {
    setState(() {
      final v = _slots[slot];
      if (v != null && !_pool.contains(v)) _pool.add(v);
      _slots[slot] = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final answers = _slots.map((e) => e ?? '').toList();
    final ready = _slots.every((e) => e != null && e.isNotEmpty);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(widget.title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
        Text('Départ : ${widget.start}', style: const TextStyle(fontWeight: FontWeight.w700)),
        const SizedBox(height: 8),
        const Text('Glisse chaque mot dans l’emplacement', style: TextStyle(color: Colors.black54)),
        const SizedBox(height: 12),
        ...List.generate(widget.steps.length, (i) {
          final prompt = widget.steps[i]['prompt']?.toString() ?? 'Étape ${i + 1}';
          return Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(prompt, style: const TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 4),
                DragTarget<String>(
                  onWillAcceptWithDetails: (_) => true,
                  onAcceptWithDetails: (d) => _place(i, d.data),
                  builder: (context, candidate, rejected) {
                    final filled = _slots[i];
                    final hovering = candidate.isNotEmpty;
                    return InkWell(
                      onTap: filled == null ? null : () => _clearSlot(i),
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 120),
                        width: double.infinity,
                        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
                        decoration: BoxDecoration(
                          color: hovering
                              ? const Color(0xFFF4C430)
                              : (filled != null ? const Color(0xFF2A9D8F) : const Color(0xFFE8E0D4)),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: hovering ? const Color(0xFFE63946) : Colors.black26,
                            width: 2,
                          ),
                        ),
                        child: Text(
                          filled ?? 'Dépose ici',
                          style: TextStyle(
                            fontWeight: FontWeight.w800,
                            color: filled != null || hovering ? Colors.white : Colors.black45,
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ],
            ),
          );
        }),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: _pool.map((c) {
            return Draggable<String>(
              data: c,
              feedback: Material(
                elevation: 6,
                borderRadius: BorderRadius.circular(10),
                child: _Chip(label: c, active: true),
              ),
              childWhenDragging: Opacity(opacity: 0.35, child: _Chip(label: c)),
              child: _Chip(label: c),
            );
          }).toList(),
        ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: ready ? () => widget.onSubmit(answers) : null,
          child: const Text('Valider la chaîne'),
        ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, this.active = false});
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
