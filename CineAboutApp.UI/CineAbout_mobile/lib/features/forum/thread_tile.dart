import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/models.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class ThreadTile extends StatelessWidget {
  const ThreadTile({super.key, required this.thread, this.onDeleted});

  final ForumThread thread;

  final VoidCallback? onDeleted;

  @override
  Widget build(BuildContext context) {
    final t = thread;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () async {
          final deleted = await context.push<bool>('/thread/${t.id}');
          if (deleted == true) onDeleted?.call();
        },
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Avatar(username: t.username, radius: 11),
              const SizedBox(width: 8),
              Expanded(
                child: Text('${t.username} · ${timeAgo(t.createdAt)}',
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppColors.muted, fontSize: 12)),
              ),
              if (t.locked) const Icon(Icons.lock_outline, size: 14, color: AppColors.muted),
            ]),
            const SizedBox(height: 10),
            Text(t.title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            if (t.hasSpoiler)
              const Padding(
                padding: EdgeInsets.only(top: 6),
                child: Chip2('Spoiler', color: AppColors.amber),
              )
            else if (t.content.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 6),
                child: Text(t.content,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(color: AppColors.muted)),
              ),
            if (t.movieTitle != null || t.tags.isNotEmpty) ...[
              const SizedBox(height: 10),
              Wrap(spacing: 6, runSpacing: 6, children: [
                if (t.movieTitle != null) Chip2(t.movieTitle!, color: AppColors.red),
                for (final tag in t.tags) Chip2(tag),
              ]),
            ],
            const SizedBox(height: 12),
            Row(children: [
              Icon(Icons.arrow_upward, size: 16, color: t.score > 0 ? AppColors.gold : AppColors.muted),
              const SizedBox(width: 4),
              Text('${t.score}', style: const TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(width: 16),
              const Icon(Icons.chat_bubble_outline, size: 15, color: AppColors.muted),
              const SizedBox(width: 4),
              Text('${t.commentCount} replies', style: const TextStyle(color: AppColors.muted)),
            ]),
          ]),
        ),
      ),
    );
  }
}
