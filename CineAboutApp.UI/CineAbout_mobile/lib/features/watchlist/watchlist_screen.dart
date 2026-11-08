import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../movie/movie_cards.dart';

class WatchlistScreen extends StatelessWidget {
  const WatchlistScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('My lists'),
          bottom: const TabBar(tabs: [Tab(text: 'Watchlist'), Tab(text: 'Interested')]),
        ),
        body: const TabBarView(children: [
          _ListTab(listType: 'Watchlist'),
          _ListTab(listType: 'Interested'),
        ]),
      ),
    );
  }
}

class _ListTab extends StatefulWidget {
  const _ListTab({required this.listType});

  final String listType;

  @override
  State<_ListTab> createState() => _ListTabState();
}

class _ListTabState extends State<_ListTab> with AutomaticKeepAliveClientMixin {
  final _list = PagedListController();
  String? _type;

  @override
  bool get wantKeepAlive => true;

  Future<void> _remove(WatchlistItem item) async {
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.removeWatchlist(item.movie.id), success: 'Removed.')) {
      _list.reload();
    }
  }

  Future<void> _move(WatchlistItem item) async {
    final repo = context.read<Repository>();
    final target = item.listType == 'Watchlist' ? 'Interested' : 'Watchlist';
    if (await runAction(context, () => repo.setWatchlist(item.movie.id, target),
        success: 'Moved to $target.')) {
      _list.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final repo = context.read<Repository>();
    final isWatchlist = widget.listType == 'Watchlist';
    return Column(children: [
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
        child: Align(
          alignment: Alignment.centerLeft,
          child: Text(
            isWatchlist
                ? 'Titles you are watching or have watched.'
                : 'Titles you plan to watch in the future.',
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
        ),
      ),
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
        child: Row(children: [
          for (final f in const {null: 'All', 'Movie': 'Movies', 'Series': 'Series'}.entries)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ChoiceChip(
                label: Text(f.value),
                selected: _type == f.key,
                onSelected: (_) {
                  setState(() => _type = f.key);
                  _list.reload();
                },
              ),
            ),
        ]),
      ),
      Expanded(
        child: PagedList<WatchlistItem>(
          controller: _list,
          emptyText: isWatchlist
              ? 'Titles you are watching or have watched appear here.'
              : 'Titles you plan to watch appear here.',
          fetch: (page) => repo.watchlist(listType: widget.listType, type: _type, page: page),
          itemBuilder: (context, item) => MovieTile(
            movie: item.movie,
            subtitle: 'Added ${formatDate(item.addedAt)}',
            trailing: Column(mainAxisSize: MainAxisSize.min, children: [
              IconButton(
                tooltip: isWatchlist ? 'Move to Interested' : 'Move to Watchlist',
                icon: Icon(isWatchlist ? Icons.favorite_border : Icons.check, size: 20),
                onPressed: () => _move(item),
              ),
              IconButton(
                tooltip: 'Remove',
                icon: const Icon(Icons.close, size: 20, color: AppColors.muted),
                onPressed: () => _remove(item),
              ),
            ]),
          ),
        ),
      ),
    ]);
  }
}
