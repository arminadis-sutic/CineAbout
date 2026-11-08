import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../shared/report_dialog.dart';

class ReviewTile extends StatefulWidget {
  const ReviewTile({
    super.key,
    required this.review,
    required this.isMine,
    required this.onChanged,
    this.onEdit,
    this.showMovie = false,
  });

  final Review review;
  final bool isMine;
  final VoidCallback onChanged;
  final VoidCallback? onEdit;
  final bool showMovie;

  @override
  State<ReviewTile> createState() => _ReviewTileState();
}

class _ReviewTileState extends State<ReviewTile> {
  late bool? _vote = widget.review.myVote;
  late int _helpful = widget.review.helpful;
  bool _busy = false;

  Future<void> _voteHelpful() async {
    final repo = context.read<Repository>();
    final r = widget.review;
    final removing = _vote == true;
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => removing ? repo.removeReviewVote(r.id) : repo.voteReview(r.id, true),
    );
    if (!mounted) return;
    setState(() {
      _busy = false;
      if (ok) {
        _helpful += removing ? -1 : 1;
        _vote = removing ? null : true;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final r = widget.review;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            GestureDetector(
              onTap: () => context.push('/user/${r.userId}'),
              child: Avatar(username: r.username, url: r.userImageUrl, radius: 16),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(widget.showMovie ? r.movieTitle : r.username,
                    style: const TextStyle(fontWeight: FontWeight.w800)),
                Text(timeAgo(r.createdAt), style: const TextStyle(color: AppColors.muted, fontSize: 12)),
              ]),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: AppColors.gold.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                const Icon(Icons.star, size: 14, color: AppColors.gold),
                const SizedBox(width: 4),
                Text('${r.rating}/10',
                    style: const TextStyle(color: AppColors.gold, fontWeight: FontWeight.w800)),
              ]),
            ),
          ]),
          if (r.text.isNotEmpty) ...[
            const SizedBox(height: 10),
            Text(r.text, style: const TextStyle(height: 1.4)),
          ],
          const SizedBox(height: 8),
          Row(children: [
            TextButton.icon(
              onPressed: _busy || widget.isMine ? null : _voteHelpful,
              icon: Icon(_vote == true ? Icons.thumb_up : Icons.thumb_up_outlined,
                  size: 16, color: _vote == true ? AppColors.red : AppColors.muted),
              label: Text('Helpful · $_helpful',
                  style: TextStyle(color: _vote == true ? AppColors.red : AppColors.muted)),
            ),
            const Spacer(),
            if (widget.isMine && widget.onEdit != null)
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: widget.onEdit,
              )
            else if (!widget.isMine)
              IconButton(
                tooltip: 'Report',
                icon: const Icon(Icons.flag_outlined, size: 18),
                onPressed: () => showReportDialog(context, targetType: 'Review', targetId: r.id),
              ),
          ]),
        ]),
      ),
    );
  }
}
