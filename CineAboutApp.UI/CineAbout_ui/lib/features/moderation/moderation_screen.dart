import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class ModerationScreen extends StatelessWidget {
  const ModerationScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 3,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const PageHeader(
            title: 'Content review',
            subtitle: 'Manage threads, reviews and user reports.',
          ),
          const TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [Tab(text: 'Reports'), Tab(text: 'Threads'), Tab(text: 'Reviews')],
          ),
          const SizedBox(height: 16),
          const Expanded(
            child: TabBarView(children: [_ReportsTab(), _ThreadsTab(), _ReviewsTab()]),
          ),
        ],
      ),
    );
  }
}

class _ReviewsTab extends StatefulWidget {
  const _ReviewsTab();

  @override
  State<_ReviewsTab> createState() => _ReviewsTabState();
}

class _ReviewsTabState extends State<_ReviewsTab> {
  final _table = PagedTableController();
  String _search = '';
  String? _status;
  int? _rating;
  DateTimeRange? _range;
  String _sort = 'Date';

  Future<void> _setStatus(Review r, String status) async {
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.setReviewStatus(r.id, status),
        success: 'Review marked as $status.')) {
      _table.reload();
    }
  }

  void _show(Review r) {
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: DialogTitle('${r.movieTitle} — ${r.rating}/10'),
        content: SizedBox(
          width: 480,
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('@${r.username} · ${formatDate(r.createdAt)}',
                style: const TextStyle(color: AppColors.muted)),
            const SizedBox(height: 12),
            Text(r.text.isEmpty ? '(no text)' : r.text),
            const SizedBox(height: 12),
            Text('Helpful: ${r.helpful} · Not helpful: ${r.notHelpful}',
                style: const TextStyle(color: AppColors.muted)),
          ]),
        ),
        actions: [TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Close'))],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 14), children: [
      Wrap(spacing: 12, runSpacing: 12, children: [
        SearchBox(
          hint: 'Search movie or user',
          onChanged: (v) {
            _search = v;
            _table.reset();
          },
        ),
        FilterDropdown<String>(
          label: 'Status',
          value: _status,
          options: const {'Active': 'Active', 'Reported': 'Reported', 'Removed': 'Removed'},
          onChanged: (v) {
            setState(() => _status = v);
            _table.reset();
          },
        ),
        FilterDropdown<int>(
          label: 'Rating',
          value: _rating,
          options: {for (var i = 0; i <= 10; i++) i: '$i/10'},
          onChanged: (v) {
            setState(() => _rating = v);
            _table.reset();
          },
        ),
        FilterDropdown<String>(
          label: 'Sort',
          value: _sort,
          options: const {'Date': 'Newest first', 'Relevance': 'Most helpful first'},
          onChanged: (v) {
            setState(() => _sort = v ?? 'Date');
            _table.reset();
          },
        ),
        _DateRangeButton(
          range: _range,
          onChanged: (r) {
            setState(() => _range = r);
            _table.reset();
          },
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<Review>(
        controller: _table,
        fetch: (page) =>
            repo.reviews(
                search: _search,
                status: _status,
                rating: _rating,
                from: _range?.start,
                to: _range?.end,
                sortBy: _sort,
                page: page),
        columns: const [
          DataColumnSpec('Movie', flex: 2),
          DataColumnSpec('User', flex: 2),
          DataColumnSpec('Rating', width: 90),
          DataColumnSpec('Helpful', width: 90),
          DataColumnSpec('Date', width: 110),
          DataColumnSpec('Status', width: 110),
          DataColumnSpec('', width: 90),
        ],
        emptyText: 'No reviews match the filters.',
        onRowTap: _show,
        rowBuilder: (r) {
          final removed = r.status == 'Removed';
          final dim = TextStyle(
            color: removed ? AppColors.muted : null,
            decoration: removed ? TextDecoration.lineThrough : null,
          );
          return [
            Text(r.movieTitle, overflow: TextOverflow.ellipsis, style: dim),
            Text('@${r.username}', style: dim),
            Row(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.star, size: 14, color: AppColors.gold),
              const SizedBox(width: 4),
              Text('${r.rating}/10'),
            ]),
            Row(mainAxisSize: MainAxisSize.min, children: [
              Text('${r.helpful}'),
              if (r.helpful - r.notHelpful >= 3) ...[
                const SizedBox(width: 6),
                const Tooltip(message: 'Marked helpful by the community', child: Icon(Icons.thumb_up, size: 14, color: AppColors.green)),
              ],
            ]),
            Text(formatDate(r.createdAt)),
            StatusChip(r.status),
            removed
                ? IconButton(
                    tooltip: 'Restore',
                    icon: const Icon(Icons.restore, size: 18),
                    onPressed: () => _setStatus(r, 'Active'),
                  )
                : IconButton(
                    tooltip: 'Remove',
                    icon: const Icon(Icons.delete_outline, size: 18),
                    onPressed: () async {
                      final ok = await confirm(context,
                          title: 'Remove review',
                          message: 'Hide this review from the community? The author is notified.',
                          confirmLabel: 'Remove',
                          destructive: true);
                      if (ok) _setStatus(r, 'Removed');
                    },
                  ),
          ];
        },
      ),
    ]);
  }
}

class _ThreadsTab extends StatefulWidget {
  const _ThreadsTab();

  @override
  State<_ThreadsTab> createState() => _ThreadsTabState();
}

class _ThreadsTabState extends State<_ThreadsTab> {
  final _table = PagedTableController();
  String _search = '';
  String? _status;
  DateTimeRange? _range;

  Future<void> _setStatus(ForumThread t, String status) async {
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.setThreadStatus(t.id, status),
        success: 'Thread is now $status.')) {
      _table.reload();
    }
  }

  Future<void> _delete(ForumThread t) async {
    final ok = await confirm(context,
        title: 'Delete thread',
        message: 'Permanently delete "${t.title}" with its ${t.commentCount} comments?',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteThread(t.id), success: 'Thread deleted.')) {
      _table.reload();
    }
  }

  Future<void> _show(ForumThread t) async {
    await showDialog<void>(context: context, builder: (ctx) => _ThreadDialog(threadId: t.id));
    if (mounted) _table.reload();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 14), children: [
      Wrap(spacing: 12, runSpacing: 12, children: [
        SearchBox(
          hint: 'Search threads',
          onChanged: (v) {
            _search = v;
            _table.reset();
          },
        ),
        FilterDropdown<String>(
          label: 'Status',
          value: _status,
          options: const {'Active': 'Active', 'Locked': 'Locked', 'Removed': 'Removed'},
          onChanged: (v) {
            setState(() => _status = v);
            _table.reset();
          },
        ),
        _DateRangeButton(
          range: _range,
          onChanged: (r) {
            setState(() => _range = r);
            _table.reset();
          },
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<ForumThread>(
        controller: _table,
        fetch: (page) =>
            repo.threads(search: _search, status: _status, from: _range?.start, to: _range?.end, page: page),
        columns: const [
          DataColumnSpec('Thread', flex: 4),
          DataColumnSpec('Author', flex: 2),
          DataColumnSpec('Replies', width: 80),
          DataColumnSpec('Score', width: 70),
          DataColumnSpec('Date', width: 110),
          DataColumnSpec('Status', width: 100),
          DataColumnSpec('', width: 130),
        ],
        emptyText: 'No threads match the filters.',
        onRowTap: _show,
        rowBuilder: (t) => [
          Row(children: [
            Flexible(
              child: Text(t.title,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w700)),
            ),
            if (t.hasSpoiler) ...[
              const SizedBox(width: 8),
              const StatusChip('Spoiler', color: AppColors.amber),
            ],
          ]),
          Text('@${t.username}'),
          Text('${t.commentCount}'),
          Text('${t.score}'),
          Text(formatDate(t.createdAt)),
          StatusChip(t.status),
          Row(mainAxisSize: MainAxisSize.min, children: [
            if (t.status == 'Removed')
              IconButton(
                  tooltip: 'Restore',
                  icon: const Icon(Icons.restore, size: 18),
                  onPressed: () => _setStatus(t, 'Active'))
            else ...[
              IconButton(
                tooltip: t.status == 'Locked' ? 'Unlock' : 'Lock',
                icon: Icon(t.status == 'Locked' ? Icons.lock_open : Icons.lock_outline, size: 18),
                onPressed: () => _setStatus(t, t.status == 'Locked' ? 'Active' : 'Locked'),
              ),
              IconButton(
                  tooltip: 'Remove',
                  icon: const Icon(Icons.visibility_off_outlined, size: 18),
                  onPressed: () => _setStatus(t, 'Removed')),
            ],
            IconButton(
                tooltip: 'Delete permanently',
                icon: const Icon(Icons.delete_outline, size: 18),
                onPressed: () => _delete(t)),
          ]),
        ],
      ),
    ]);
  }
}

class _ReportsTab extends StatefulWidget {
  const _ReportsTab();

  @override
  State<_ReportsTab> createState() => _ReportsTabState();
}

class _ReportsTabState extends State<_ReportsTab> {
  final _table = PagedTableController();
  String? _status = 'Pending';
  String? _type;

  Future<void> _decide(ContentReport r) async {
    final decided = await showDialog<bool>(
      context: context,
      builder: (_) => _DecisionDialog(report: r),
    );
    if (decided == true) _table.reload();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 14), children: [
      Wrap(spacing: 12, runSpacing: 12, children: [
        FilterDropdown<String>(
          label: 'Status',
          value: _status,
          options: const {'Pending': 'Pending', 'Resolved': 'Resolved', 'Dismissed': 'Dismissed'},
          onChanged: (v) {
            setState(() => _status = v);
            _table.reset();
          },
        ),
        FilterDropdown<String>(
          label: 'Content',
          value: _type,
          options: const {'Review': 'Review', 'Thread': 'Thread', 'Comment': 'Comment'},
          onChanged: (v) {
            setState(() => _type = v);
            _table.reset();
          },
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<ContentReport>(
        controller: _table,
        fetch: (page) => repo.reports(status: _status, targetType: _type, page: page),
        columns: const [
          DataColumnSpec('Content', flex: 4),
          DataColumnSpec('Reason', flex: 3),
          DataColumnSpec('Reporter', flex: 2),
          DataColumnSpec('Date', width: 110),
          DataColumnSpec('Status', width: 110),
        ],
        emptyText: 'No reports match the filters.',
        onRowTap: _decide,
        rowBuilder: (r) => [
          Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(r.targetType, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
            Text(r.preview.isEmpty ? '#${r.targetId}' : r.preview,
                maxLines: 2, overflow: TextOverflow.ellipsis),
          ]),
          Text(r.reason, maxLines: 2, overflow: TextOverflow.ellipsis),
          Text('@${r.reporter}'),
          Text(formatDate(r.createdAt)),
          StatusChip(r.status),
        ],
      ),
    ]);
  }
}

class _DecisionDialog extends StatefulWidget {
  const _DecisionDialog({required this.report});

  final ContentReport report;

  @override
  State<_DecisionDialog> createState() => _DecisionDialogState();
}

class _DecisionDialogState extends State<_DecisionDialog> {
  final _note = TextEditingController();
  bool _remove = true;
  bool _busy = false;
  String? _noteError;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _decide(String status) async {
    final note = _note.text.trim();
    if (status == 'Dismissed' && note.isEmpty) {
      setState(() => _noteError = 'Enter the reason for dismissing this report (up to 500 characters).');
      return;
    }
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => repo.decideReport(widget.report.id,
          status: status,
          removeContent: status == 'Resolved' && _remove,
          note: note.isEmpty ? null : note),
      success: status == 'Resolved' ? 'Report resolved.' : 'Report dismissed.',
    );
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final r = widget.report;
    final pending = r.status == 'Pending';
    return AlertDialog(
      title: DialogTitle('${r.targetType} report'),
      content: SizedBox(
        width: 480,
        child: SingleChildScrollView(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('Reported by @${r.reporter} · ${formatDate(r.createdAt)}',
                style: const TextStyle(color: AppColors.muted)),
            const SizedBox(height: 14),
            const Text('REPORTED CONTENT',
                style: TextStyle(fontSize: 11, letterSpacing: 1, color: AppColors.muted)),
            const SizedBox(height: 4),
            Text(r.preview.isEmpty ? '#${r.targetId}' : r.preview),
            const SizedBox(height: 14),
            const Text('REASON',
                style: TextStyle(fontSize: 11, letterSpacing: 1, color: AppColors.muted)),
            const SizedBox(height: 4),
            Text(r.reason),
            const SizedBox(height: 14),
            if (pending) ...[
              TextField(
                controller: _note,
                maxLength: 500,
                minLines: 2,
                maxLines: 4,
                onChanged: (_) => setState(() => _noteError = null),
                decoration: InputDecoration(
                  labelText: 'Admin note (required when dismissing)',
                  errorText: _noteError,
                ),
              ),
              CheckboxListTile(
                contentPadding: EdgeInsets.zero,
                value: _remove,
                activeColor: AppColors.red,
                title: const Text('Remove the content when resolving'),
                onChanged: (v) => setState(() => _remove = v ?? false),
              ),
            ] else ...[
              Row(children: [
                StatusChip(r.status),
                const SizedBox(width: 10),
                Text('by ${r.reviewedBy ?? '—'}'
                    '${r.reviewedAt == null ? '' : ' on ${formatDate(r.reviewedAt!)}'}',
                    style: const TextStyle(color: AppColors.muted)),
              ]),
              if ((r.adminNote ?? '').isNotEmpty) ...[
                const SizedBox(height: 10),
                Text('Note: ${r.adminNote}'),
              ],
            ],
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
        if (pending) ...[
          OutlinedButton(
              onPressed: _busy ? null : () => _decide('Dismissed'), child: const Text('Dismiss')),
          FilledButton(
              onPressed: _busy ? null : () => _decide('Resolved'), child: const Text('Resolve')),
        ],
      ],
    );
  }
}

class _DateRangeButton extends StatelessWidget {
  const _DateRangeButton({required this.range, required this.onChanged});

  final DateTimeRange? range;
  final ValueChanged<DateTimeRange?> onChanged;

  @override
  Widget build(BuildContext context) {
    final label = range == null ? 'Date range' : '${formatDate(range!.start)} – ${formatDate(range!.end)}';
    return Row(mainAxisSize: MainAxisSize.min, children: [
      OutlinedButton.icon(
        icon: const Icon(Icons.date_range, size: 18),
        label: Text(label),
        onPressed: () async {
          final now = DateTime.now();
          final picked = await showDateRangePicker(
            context: context,
            firstDate: DateTime(2020),
            lastDate: DateTime(now.year, now.month, now.day),
            initialDateRange: range,
          );
          if (picked != null) onChanged(picked);
        },
      ),
      if (range != null)
        IconButton(tooltip: 'Clear dates', icon: const Icon(Icons.close, size: 18), onPressed: () => onChanged(null)),
    ]);
  }
}

class _ThreadDialog extends StatefulWidget {
  const _ThreadDialog({required this.threadId});

  final int threadId;

  @override
  State<_ThreadDialog> createState() => _ThreadDialogState();
}

class _ThreadDialogState extends State<_ThreadDialog> {
  ThreadDetail? _detail;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final d = await context.read<Repository>().threadDetail(widget.threadId);
      if (mounted) setState(() => _detail = d);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  Future<String?> _editText(String title, String initial, {bool multiline = true}) {
    return showDialog<String>(
      context: context,
      builder: (ctx) => _TextEditDialog(title: title, initial: initial, multiline: multiline),
    );
  }

  Future<void> _editThread(ThreadDetail d) async {
    final title = await _editText('Edit thread title', d.thread.title, multiline: false);
    if (title == null || !mounted) return;
    final content = await _editText('Edit thread text', d.thread.content);
    if (content == null || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(
        context, () => repo.updateThread(d, title: title, content: content, hasSpoiler: d.thread.hasSpoiler),
        success: 'Thread updated.')) {
      _load();
    }
  }

  Future<void> _editComment(ThreadComment c) async {
    final text = await _editText('Edit comment by @${c.username}', c.content);
    if (text == null || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.updateComment(c.id, text), success: 'Comment updated.')) {
      _load();
    }
  }

  Future<void> _deleteComment(ThreadComment c) async {
    final ok = await confirm(context,
        title: 'Delete comment',
        message: 'Delete the comment by @${c.username}?',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteComment(c.id), success: 'Comment deleted.')) {
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final d = _detail;
    return AlertDialog(
      title: DialogTitle(d?.thread.title ?? 'Thread'),
      content: SizedBox(
        width: 560,
        height: 460,
        child: d == null
            ? Center(child: _error == null ? const CircularProgressIndicator() : Text('$_error'))
            : ListView(children: [
                Text(
                    '@${d.thread.username} · ${formatDate(d.thread.createdAt)}'
                    '${d.thread.movieTitle == null ? '' : ' · ${d.thread.movieTitle}'}',
                    style: const TextStyle(color: AppColors.muted)),
                if (d.thread.tags.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Wrap(spacing: 6, children: [for (final tag in d.thread.tags) Chip(label: Text('#$tag'))]),
                ],
                const SizedBox(height: 12),
                Text(d.thread.content),
                Align(
                  alignment: Alignment.centerLeft,
                  child: TextButton.icon(
                    onPressed: () => _editThread(d),
                    icon: const Icon(Icons.edit_outlined, size: 16),
                    label: const Text('Edit thread'),
                  ),
                ),
                const Divider(),
                Text('Comments (${d.comments.length})', style: const TextStyle(fontWeight: FontWeight.w700)),
                const SizedBox(height: 8),
                if (d.comments.isEmpty) const Text('No comments.', style: TextStyle(color: AppColors.muted)),
                for (final c in d.comments)
                  Container(
                    margin: const EdgeInsets.only(bottom: 8),
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: AppColors.surfaceHigh,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text('@${c.username} · ${formatDateTime(c.createdAt)} · score ${c.score}',
                              style: const TextStyle(color: AppColors.muted, fontSize: 12)),
                          const SizedBox(height: 4),
                          Text(c.content),
                        ]),
                      ),
                      IconButton(
                          tooltip: 'Edit comment',
                          icon: const Icon(Icons.edit_outlined, size: 18),
                          onPressed: () => _editComment(c)),
                      IconButton(
                          tooltip: 'Delete comment',
                          icon: const Icon(Icons.delete_outline, size: 18),
                          onPressed: () => _deleteComment(c)),
                    ]),
                  ),
              ]),
      ),
      actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close'))],
    );
  }
}

class _TextEditDialog extends StatefulWidget {
  const _TextEditDialog({required this.title, required this.initial, required this.multiline});

  final String title, initial;
  final bool multiline;

  @override
  State<_TextEditDialog> createState() => _TextEditDialogState();
}

class _TextEditDialogState extends State<_TextEditDialog> {
  late final _controller = TextEditingController(text: widget.initial);
  final _form = GlobalKey<FormState>();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: DialogTitle(widget.title),
      content: SizedBox(
        width: 480,
        child: Form(
          key: _form,
          child: TextFormField(
            controller: _controller,
            autofocus: true,
            minLines: widget.multiline ? 4 : 1,
            maxLines: widget.multiline ? 10 : 1,
            maxLength: widget.multiline ? 8000 : 200,
            validator: (v) => (v == null || v.trim().isEmpty) ? 'Required' : null,
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(
          onPressed: () {
            if (_form.currentState!.validate()) Navigator.pop(context, _controller.text.trim());
          },
          child: const Text('Save'),
        ),
      ],
    );
  }
}
