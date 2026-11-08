import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

Future<bool?> showReviewSheet(BuildContext context,
    {required MovieCard movie, Review? existing}) {
  return showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    backgroundColor: AppColors.surface,
    shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
    builder: (_) => Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: _ReviewSheet(movie: movie, existing: existing),
    ),
  );
}

class _ReviewSheet extends StatefulWidget {
  const _ReviewSheet({required this.movie, this.existing});

  final MovieCard movie;
  final Review? existing;

  @override
  State<_ReviewSheet> createState() => _ReviewSheetState();
}

class _ReviewSheetState extends State<_ReviewSheet> {
  late int _rating = widget.existing?.rating ?? 7;
  late final _text = TextEditingController(text: widget.existing?.text);
  bool _busy = false;

  static const _labels = {
    0: 'Terrible', 1: 'Awful', 2: 'Bad', 3: 'Poor', 4: 'Weak', 5: 'Average',
    6: 'Decent', 7: 'Good', 8: 'Excellent', 9: 'Outstanding', 10: 'Masterpiece',
  };

  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => repo.saveReview(
          id: widget.existing?.id,
          movieId: widget.movie.id,
          rating: _rating,
          text: _text.text.trim()),
      success: 'Review saved.',
    );
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) Navigator.pop(context, true);
  }

  Future<void> _delete() async {
    final repo = context.read<Repository>();
    final yes = await confirm(context,
        title: 'Delete review', message: 'Delete your review?', confirmLabel: 'Delete');
    if (!yes || !mounted) return;
    final ok = await runAction(context, () => repo.deleteReview(widget.existing!.id),
        success: 'Review deleted.');
    if (ok && mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            Poster(url: widget.movie.posterUrl, title: widget.movie.title, width: 50, height: 75),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(widget.movie.title,
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                Text('${widget.movie.year}', style: const TextStyle(color: AppColors.muted)),
              ]),
            ),
          ]),
          const SizedBox(height: 16),
          Center(
            child: Text('$_rating',
                style: const TextStyle(fontSize: 56, fontWeight: FontWeight.w900, color: AppColors.gold)),
          ),
          Center(child: Text(_labels[_rating]!.toUpperCase(), style: const TextStyle(color: AppColors.muted))),
          Slider(
            value: _rating.toDouble(),
            min: 0,
            max: 10,
            divisions: 10,
            activeColor: AppColors.gold,
            label: '$_rating',
            onChanged: (v) => setState(() => _rating = v.round()),
          ),
          TextField(
            controller: _text,
            minLines: 4,
            maxLines: 6,
            maxLength: 4000,
            decoration: const InputDecoration(
                hintText: 'Share your thoughts… What did you love about the movie?'),
          ),
          const SizedBox(height: 8),
          FilledButton(
            onPressed: _busy ? null : _save,
            child: Text(widget.existing == null ? 'Post review' : 'Save changes'),
          ),
          if (widget.existing != null)
            TextButton(
              onPressed: _busy ? null : _delete,
              child: const Text('Delete review', style: TextStyle(color: AppColors.red)),
            ),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel', style: TextStyle(color: AppColors.muted)),
          ),
        ]),
      ),
    );
  }
}
