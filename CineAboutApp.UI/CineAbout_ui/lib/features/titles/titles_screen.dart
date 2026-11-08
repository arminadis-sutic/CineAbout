import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import 'title_form.dart';

class TitlesScreen extends StatefulWidget {
  const TitlesScreen({super.key});

  @override
  State<TitlesScreen> createState() => _TitlesScreenState();
}

class _TitlesScreenState extends State<TitlesScreen> {
  final _table = PagedTableController();
  String _search = '';
  String? _type;
  bool _editing = false;
  int? _editId;

  void _edit(int? id) => setState(() {
        _editing = true;
        _editId = id;
      });

  void _closeForm(bool saved) {
    setState(() => _editing = false);
    if (saved) _table.reload();
  }

  Future<void> _delete(MovieSummary m) async {
    final ok = await confirm(
      context,
      title: 'Delete title',
      message: 'Delete "${m.title}"? Reviews linked to it will no longer be shown.',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteMovie(m.id), success: 'Title deleted.')) {
      _table.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_editing) {
      return TitleForm(movieId: _editId, onClose: _closeForm);
    }
    final repo = context.read<Repository>();
    return ListView(
      children: [
        PageHeader(
          title: 'Titles',
          subtitle: 'Manage movies and series in the catalogue.',
          actions: [
            FilledButton.icon(
              onPressed: () => _edit(null),
              icon: const Icon(Icons.add, size: 18),
              label: const Text('Add title'),
            ),
          ],
        ),
        Wrap(spacing: 12, runSpacing: 12, children: [
          SearchBox(
            hint: 'Search titles',
            onChanged: (v) {
              _search = v;
              _table.reset();
            },
          ),
          FilterDropdown<String>(
            label: 'Type',
            value: _type,
            options: const {'Movie': 'Film', 'Series': 'Series'},
            onChanged: (v) {
              setState(() => _type = v);
              _table.reset();
            },
          ),
        ]),
        const SizedBox(height: 16),
        PagedTable<MovieSummary>(
          controller: _table,
          fetch: (page) => repo.movies(search: _search, type: _type, page: page),
          columns: const [
            DataColumnSpec('Title', flex: 3),
            DataColumnSpec('Type', width: 90),
            DataColumnSpec('Year', width: 80),
            DataColumnSpec('Director', flex: 2),
            DataColumnSpec('Rating', width: 110),
            DataColumnSpec('Visible', width: 90),
            DataColumnSpec('', width: 90),
          ],
          emptyText: 'No titles found.',
          onRowTap: (m) => _edit(m.id),
          rowBuilder: (m) => [
            NameWithThumb(name: m.title, url: m.posterUrl),
            Text(m.type == 'Series' ? 'Series' : 'Film'),
            Text('${m.releaseYear}'),
            Text(m.directorName ?? '—', overflow: TextOverflow.ellipsis),
            Row(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.star, size: 14, color: AppColors.gold),
              const SizedBox(width: 4),
              Text('${m.averageRating.toStringAsFixed(1)} (${m.reviewCount})'),
            ]),
            StatusChip(m.isVisible ? 'Active' : 'Hidden',
                color: m.isVisible ? AppColors.green : AppColors.muted),
            Row(mainAxisSize: MainAxisSize.min, children: [
              IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _edit(m.id),
              ),
              IconButton(
                tooltip: m.reviewCount > 0
                    ? 'Cannot be deleted: it has ${m.reviewCount} ${m.reviewCount == 1 ? 'review' : 'reviews'}'
                    : 'Delete',
                icon: const Icon(Icons.delete_outline, size: 18),
                onPressed: m.reviewCount > 0 ? null : () => _delete(m),
              ),
            ]),
          ],
        ),
      ],
    );
  }
}
