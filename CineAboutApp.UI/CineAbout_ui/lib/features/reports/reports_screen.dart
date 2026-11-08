import 'package:flutter/material.dart';
import 'package:open_file/open_file.dart';
import 'package:path_provider/path_provider.dart';
import 'package:provider/provider.dart';

import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class ReportsScreen extends StatelessWidget {
  const ReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: const [
        PageHeader(
          title: 'Reports',
          subtitle: 'Generate actionable insights from platform data as PDF.',
        ),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: _ReportCard(
                kind: 'community-activity',
                icon: Icons.bar_chart,
                title: 'Community activity',
                description:
                    'User engagement metrics: new registrations, submitted reviews, active discussion '
                    'threads and quiz sessions over the selected period.',
              ),
            ),
            SizedBox(width: 20),
            Expanded(
              child: _ReportCard(
                kind: 'content-popularity',
                icon: Icons.movie_outlined,
                title: 'Content popularity',
                description:
                    'Top-performing movies and series by ratings, number of reviews, watchlist and '
                    'interested additions and related discussions.',
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _ReportCard extends StatefulWidget {
  const _ReportCard({
    required this.kind,
    required this.icon,
    required this.title,
    required this.description,
  });

  final String kind;
  final IconData icon;
  final String title;
  final String description;

  @override
  State<_ReportCard> createState() => _ReportCardState();
}

class _ReportCardState extends State<_ReportCard> {
  late DateTime _start = DateTime.now().subtract(const Duration(days: 30));
  late DateTime _end = DateTime.now();
  bool _busy = false;

  Future<void> _pick(bool start) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: start ? _start : _end,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked == null) return;
    setState(() {
      if (start) {
        _start = picked;
        if (_end.isBefore(_start)) _end = _start;
      } else {
        _end = picked;
        if (_start.isAfter(_end)) _start = _end;
      }
    });
  }

  Future<void> _generate() async {
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    try {
      final dir = (await getDownloadsDirectory()) ?? await getTemporaryDirectory();
      final path = await repo.downloadReport(widget.kind, _start, _end, dir.path);
      if (!mounted) return;
      showMessage(context, 'Report saved to $path');
      await OpenFile.open(path);
    } catch (e) {
      if (mounted) showMessage(context, e.toString(), error: true);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Panel(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: AppColors.surfaceHigh,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(widget.icon, color: AppColors.red),
        ),
        const SizedBox(height: 16),
        Text(widget.title, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
        const SizedBox(height: 8),
        Text(widget.description, style: const TextStyle(color: AppColors.muted)),
        const SizedBox(height: 20),
        Row(children: [
          Expanded(
            child: OutlinedButton.icon(
              onPressed: () => _pick(true),
              icon: const Icon(Icons.calendar_today, size: 16),
              label: Text('From ${formatDate(_start)}'),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: OutlinedButton.icon(
              onPressed: () => _pick(false),
              icon: const Icon(Icons.calendar_today, size: 16),
              label: Text('To ${formatDate(_end)}'),
            ),
          ),
        ]),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: _busy ? null : _generate,
          icon: _busy
              ? const SizedBox(
                  width: 16,
                  height: 16,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : const Icon(Icons.download, size: 18),
          label: const Text('Generate report'),
        ),
      ]),
    );
  }
}
