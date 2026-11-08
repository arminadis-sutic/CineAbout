import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class AuditLogScreen extends StatefulWidget {
  const AuditLogScreen({super.key});

  @override
  State<AuditLogScreen> createState() => _AuditLogScreenState();
}

class _AuditLogScreenState extends State<AuditLogScreen> {
  static const _types = {
    'Movie': 'Title',
    'Review': 'Review',
    'Thread': 'Thread',
    'Comment': 'Comment',
    'MyAppUser': 'User',
    'WatchlistItem': 'List item',
    'QuizSession': 'Quiz session',
    'Follow': 'Follow',
  };

  final _table = PagedTableController();
  String? _type;
  String? _action;
  DateTime? _from;
  DateTime? _to;

  Future<void> _pick(bool from) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: (from ? _from : _to) ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked == null) return;
    setState(() => from ? _from = picked : _to = picked);
    _table.reset();
  }

  void _clearDates() {
    setState(() {
      _from = null;
      _to = null;
    });
    _table.reset();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 2), children: [
      const PageHeader(
        title: 'Audit log',
        subtitle: 'Who changed what and when. Tap a row to see the old and new values.',
      ),
      Wrap(spacing: 12, runSpacing: 12, crossAxisAlignment: WrapCrossAlignment.center, children: [
        FilterDropdown<String>(
          label: 'Content',
          value: _type,
          options: _types,
          onChanged: (v) {
            setState(() => _type = v);
            _table.reset();
          },
        ),
        FilterDropdown<String>(
          label: 'Action',
          value: _action,
          options: const {'Create': 'Created', 'Update': 'Changed', 'Delete': 'Deleted'},
          onChanged: (v) {
            setState(() => _action = v);
            _table.reset();
          },
        ),
        OutlinedButton.icon(
          onPressed: () => _pick(true),
          icon: const Icon(Icons.calendar_today, size: 16),
          label: Text(_from == null ? 'From date' : 'From ${formatDate(_from!)}'),
        ),
        OutlinedButton.icon(
          onPressed: () => _pick(false),
          icon: const Icon(Icons.calendar_today, size: 16),
          label: Text(_to == null ? 'To date' : 'To ${formatDate(_to!)}'),
        ),
        if (_from != null || _to != null)
          TextButton(onPressed: _clearDates, child: const Text('Clear dates')),
      ]),
      const SizedBox(height: 16),
      PagedTable<AuditEntry>(
        controller: _table,
        fetch: (page) => repo.auditLog(
            entityType: _type, action: _action, from: _from, to: _to, page: page),
        columns: const [
          DataColumnSpec('When', width: 170),
          DataColumnSpec('User', flex: 2),
          DataColumnSpec('Action', width: 110),
          DataColumnSpec('Content', flex: 2),
        ],
        emptyText: 'No changes recorded for these filters.',
        onRowTap: (e) => showDialog<void>(context: context, builder: (_) => _AuditDialog(entry: e)),
        rowBuilder: (e) => [
          Text(formatDateTime(e.performedAt)),
          Text(e.username ?? 'system'),
          StatusChip(e.actionLabel, color: e.actionColor),
          Text(_types[e.entityType] ?? e.entityType),
        ],
      ),
    ]);
  }
}

class _AuditDialog extends StatelessWidget {
  const _AuditDialog({required this.entry});

  final AuditEntry entry;

  String _pretty(String? json) {
    if (json == null || json.isEmpty) return '—';
    try {
      return const JsonEncoder.withIndent('  ').convert(jsonDecode(json));
    } catch (_) {
      return json;
    }
  }

  @override
  Widget build(BuildContext context) {
    Widget block(String title, String? json) => Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(title.toUpperCase(),
                style: const TextStyle(fontSize: 11, letterSpacing: 1, color: AppColors.muted)),
            const SizedBox(height: 6),
            Container(
              width: double.infinity,
              constraints: const BoxConstraints(maxHeight: 360),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.surfaceHigh,
                borderRadius: BorderRadius.circular(8),
              ),
              child: SingleChildScrollView(
                child: SelectableText(_pretty(json),
                    style: const TextStyle(fontFamily: 'Consolas', fontSize: 12)),
              ),
            ),
          ]),
        );

    return AlertDialog(
      title: DialogTitle('${entry.actionLabel} · ${entry.entityType}'),
      content: SizedBox(
        width: 760,
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('${entry.username ?? 'system'} · ${formatDateTime(entry.performedAt)}'
              '${entry.ipAddress == null ? '' : ' · ${entry.ipAddress}'}',
              style: const TextStyle(color: AppColors.muted)),
          const SizedBox(height: 16),
          Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            block('Before', entry.oldValues),
            const SizedBox(width: 16),
            block('After', entry.newValues),
          ]),
        ]),
      ),
    );
  }
}
