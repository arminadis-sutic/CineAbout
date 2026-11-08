import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../shared/report_dialog.dart';

class ThreadScreen extends StatefulWidget {
  const ThreadScreen({super.key, required this.threadId});

  final int threadId;

  @override
  State<ThreadScreen> createState() => _ThreadScreenState();
}

class _ThreadScreenState extends State<ThreadScreen> {
  Future<void> Function()? _reload;
  final _comment = TextEditingController();
  bool _sending = false;
  bool _showSpoiler = false;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final text = _comment.text.trim();
    if (text.isEmpty) return;
    final repo = context.read<Repository>();
    setState(() => _sending = true);
    final ok = await runAction(context, () => repo.addComment(widget.threadId, text));
    if (!mounted) return;
    setState(() => _sending = false);
    if (ok) {
      _comment.clear();
      await _reload?.call();
    }
  }

  Future<void> _deleteThread(ForumThread t) async {
    final repo = context.read<Repository>();
    final yes = await confirm(context,
        title: 'Delete thread', message: 'Delete this thread and its comments?', confirmLabel: 'Delete');
    if (!yes || !mounted) return;
    if (await runAction(context, () => repo.deleteThread(t.id), success: 'Thread deleted.') && mounted) {
      context.pop(true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final me = context.read<AuthState>().userId;
    return AsyncData<ForumThread>(
      load: () => repo.thread(widget.threadId),
      builder: (context, t, reload) {
        _reload = reload;
        final hidden = t.hasSpoiler && !_showSpoiler && t.userId != me;
        return Scaffold(
          appBar: AppBar(
            title: const Text('Discussion'),
            actions: [
              if (t.userId == me)
                IconButton(
                  tooltip: 'Delete',
                  icon: const Icon(Icons.delete_outline),
                  onPressed: () => _deleteThread(t),
                )
              else
                IconButton(
                  tooltip: 'Report',
                  icon: const Icon(Icons.flag_outlined),
                  onPressed: () => showReportDialog(context, targetType: 'Thread', targetId: t.id),
                ),
            ],
          ),
          body: Column(children: [
            Expanded(
              child: RefreshIndicator(
                onRefresh: reload,
                child: ListView(padding: const EdgeInsets.all(16), children: [
                  Row(children: [
                    GestureDetector(
                      onTap: () => context.push('/user/${t.userId}'),
                      child: Avatar(username: t.username, radius: 14),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text('${t.username} · ${timeAgo(t.createdAt)}',
                          style: const TextStyle(color: AppColors.muted)),
                    ),
                    if (t.locked) const Chip2('Locked', color: AppColors.amber),
                  ]),
                  const SizedBox(height: 12),
                  Text(t.title, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900)),
                  if (t.movieTitle != null || t.tags.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    Wrap(spacing: 6, runSpacing: 6, children: [
                      if (t.movieTitle != null)
                        GestureDetector(
                          onTap: t.movieId == null ? null : () => context.push('/movie/${t.movieId}'),
                          child: Chip2(t.movieTitle!, color: AppColors.red),
                        ),
                      for (final tag in t.tags) Chip2(tag),
                    ]),
                  ],
                  const SizedBox(height: 14),
                  if (hidden)
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppColors.surfaceHigh,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Column(children: [
                        const Text('This thread contains spoilers.'),
                        TextButton(
                          onPressed: () => setState(() => _showSpoiler = true),
                          child: const Text('Show anyway', style: TextStyle(color: AppColors.red)),
                        ),
                      ]),
                    )
                  else
                    Text(t.content.isEmpty ? '(no text)' : t.content, style: const TextStyle(height: 1.45)),
                  const SizedBox(height: 12),
                  _VoteBar(
                    score: t.score,
                    myVote: t.myVote,
                    onVote: (up) async {
                      final removing = (up && t.myVote == 1) || (!up && t.myVote == -1);
                      if (await runAction(context,
                          () => removing ? repo.removeThreadVote(t.id) : repo.voteThread(t.id, up))) {
                        await reload();
                      }
                    },
                  ),
                  const Divider(height: 32),
                  Text('${t.comments.length} comments',
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                  const SizedBox(height: 8),
                  for (final c in t.comments)
                    _CommentTile(
                      comment: c,
                      isMine: c.userId == me,
                      onChanged: () => reload(),
                    ),
                ]),
              ),
            ),
            if (t.locked)
              const Padding(
                padding: EdgeInsets.all(14),
                child: Text('This discussion is locked.', style: TextStyle(color: AppColors.muted)),
              )
            else
              SafeArea(
                top: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(12, 6, 12, 8),
                  child: Row(children: [
                    Expanded(
                      child: TextField(
                        controller: _comment,
                        maxLength: 2000,
                        buildCounter: (_, {required currentLength, required isFocused, maxLength}) => null,
                        decoration: const InputDecoration(hintText: 'Add a comment…'),
                        onSubmitted: (_) => _send(),
                      ),
                    ),
                    const SizedBox(width: 8),
                    IconButton.filled(
                      style: IconButton.styleFrom(backgroundColor: AppColors.red),
                      tooltip: 'Send',
                      onPressed: _sending ? null : _send,
                      icon: const Icon(Icons.send, size: 20),
                    ),
                  ]),
                ),
              ),
          ]),
        );
      },
    );
  }
}

class _VoteBar extends StatelessWidget {
  const _VoteBar({required this.score, required this.myVote, required this.onVote});

  final int score, myVote;
  final void Function(bool up) onVote;

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      IconButton(
        tooltip: 'Upvote',
        icon: Icon(Icons.arrow_upward, color: myVote == 1 ? AppColors.gold : AppColors.muted),
        onPressed: () => onVote(true),
      ),
      Text('$score', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
      IconButton(
        tooltip: 'Downvote',
        icon: Icon(Icons.arrow_downward, color: myVote == -1 ? AppColors.red : AppColors.muted),
        onPressed: () => onVote(false),
      ),
    ]);
  }
}

class _CommentTile extends StatelessWidget {
  const _CommentTile({required this.comment, required this.isMine, required this.onChanged});

  final ForumComment comment;
  final bool isMine;
  final VoidCallback onChanged;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final c = comment;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Avatar(username: c.username, radius: 14),
        const SizedBox(width: 10),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('${c.username} · ${timeAgo(c.createdAt)}',
                style: const TextStyle(color: AppColors.muted, fontSize: 12)),
            const SizedBox(height: 4),
            Text(c.content, style: const TextStyle(height: 1.4)),
            Row(children: [
              IconButton(
                visualDensity: VisualDensity.compact,
                tooltip: 'Upvote',
                icon: Icon(Icons.arrow_upward, size: 16, color: c.myVote == 1 ? AppColors.gold : AppColors.muted),
                onPressed: () async {
                  final removing = c.myVote == 1;
                  if (await runAction(context,
                      () => removing ? repo.removeCommentVote(c.id) : repo.voteComment(c.id, true))) {
                    onChanged();
                  }
                },
              ),
              Text('${c.score}', style: const TextStyle(fontWeight: FontWeight.w700)),
              IconButton(
                visualDensity: VisualDensity.compact,
                tooltip: 'Downvote',
                icon: Icon(Icons.arrow_downward, size: 16, color: c.myVote == -1 ? AppColors.red : AppColors.muted),
                onPressed: () async {
                  final removing = c.myVote == -1;
                  if (await runAction(context,
                      () => removing ? repo.removeCommentVote(c.id) : repo.voteComment(c.id, false))) {
                    onChanged();
                  }
                },
              ),
              const Spacer(),
              if (isMine)
                IconButton(
                  visualDensity: VisualDensity.compact,
                  tooltip: 'Delete',
                  icon: const Icon(Icons.delete_outline, size: 18, color: AppColors.muted),
                  onPressed: () async {
                    final yes = await confirm(context,
                        title: 'Delete comment', message: 'Delete your comment?', confirmLabel: 'Delete');
                    if (yes && context.mounted && await runAction(context, () => repo.deleteComment(c.id))) {
                      onChanged();
                    }
                  },
                )
              else
                IconButton(
                  visualDensity: VisualDensity.compact,
                  tooltip: 'Report',
                  icon: const Icon(Icons.flag_outlined, size: 18, color: AppColors.muted),
                  onPressed: () => showReportDialog(context, targetType: 'Comment', targetId: c.id),
                ),
            ]),
          ]),
        ),
      ]),
    );
  }
}
