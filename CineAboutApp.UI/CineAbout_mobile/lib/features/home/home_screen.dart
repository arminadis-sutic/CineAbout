import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../movie/movie_cards.dart';
import 'notification_bell.dart';

class _HomeData {
  _HomeData(this.movies, this.genres, this.streak, this.upcoming, this.feed);

  final List<MovieCard> movies;
  final List<Named> genres;
  final Streak? streak;
  final List<Upcoming>? upcoming;
  final List<FeedItem> feed;
}

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Future<T?> _optional<T>(Future<T> f) async {
    try {
      return await f;
    } catch (_) {
      return null;
    }
  }

  Future<_HomeData> _load(Repository repo) async {
    final movies = repo.movies(pageSize: 100);
    final genres = repo.genres();
    final streak = _optional(repo.myStreak());
    final upcoming = _optional(repo.upcoming());
    final feed = _optional(repo.feed());
    final page = await movies;
    final feedPage = await feed;
    return _HomeData(page.items, await genres, await streak, await upcoming, feedPage?.items ?? const []);
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(
        title: const Text.rich(
          TextSpan(children: [
            TextSpan(text: 'CINE', style: TextStyle(color: AppColors.red)),
            TextSpan(text: 'ABOUT'),
          ]),
          style: TextStyle(fontWeight: FontWeight.w900, letterSpacing: 1.5),
        ),
        actions: [
          IconButton(
            tooltip: 'Search',
            icon: const Icon(Icons.search),
            onPressed: () => context.push('/search'),
          ),
          const NotificationBell(),
        ],
      ),
      body: AsyncData<_HomeData>(
        load: () => _load(repo),
        builder: (context, data, reload) {
          final top = [...data.movies]..sort((a, b) {
              final byRating = b.rating.compareTo(a.rating);
              return byRating != 0 ? byRating : b.reviewCount.compareTo(a.reviewCount);
            });
          final series = data.movies.where((m) => m.isSeries).toList();
          final newest = [...data.movies]..sort((a, b) => b.year.compareTo(a.year));
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(children: [
              _DailyQuizCard(streak: data.streak),
              if (data.genres.isNotEmpty)
                SizedBox(
                  height: 44,
                  child: ListView.separated(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
                    itemCount: data.genres.length,
                    separatorBuilder: (_, __) => const SizedBox(width: 8),
                    itemBuilder: (_, i) => ActionChip(
                      label: Text(data.genres[i].name),
                      backgroundColor: AppColors.surfaceHigh,
                      side: const BorderSide(color: AppColors.border),
                      onPressed: () => context.push('/search?genre=${data.genres[i].id}'),
                    ),
                  ),
                ),
              if (data.upcoming != null && data.upcoming!.isNotEmpty) ...[
                const SectionTitle('Coming soon'),
                _UpcomingRow(items: data.upcoming!, onChanged: reload),
              ],
              const SectionTitle('Top rated'),
              PosterRow(movies: top.take(12).toList()),
              if (series.isNotEmpty) ...[
                const SectionTitle('Series'),
                PosterRow(movies: series),
              ],
              const SectionTitle('Latest releases'),
              PosterRow(movies: newest.take(12).toList()),
              if (data.feed.isNotEmpty) ...[
                const SectionTitle('Friends activity'),
                for (final item in data.feed.take(5)) _FeedTile(item: item),
                if (data.feed.length > 5)
                  TextButton(
                    onPressed: () => _showAllActivity(context, data.feed),
                    child: const Text('See all activity'),
                  ),
              ],
              const SizedBox(height: 24),
            ]),
          );
        },
      ),
    );
  }
}

class _DailyQuizCard extends StatelessWidget {
  const _DailyQuizCard({required this.streak});

  final Streak? streak;

  @override
  Widget build(BuildContext context) {
    final played = streak?.playedToday ?? false;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFF3A1114), Color(0xFF181818)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Chip2('${streak?.current ?? 0} day streak', color: AppColors.amber),
            const SizedBox(width: 8),
            if (played) const Chip2('Done today', color: AppColors.green),
          ]),
          const SizedBox(height: 14),
          const Text('Daily quiz',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.w900)),
          const SizedBox(height: 6),
          const Text('Test your cinema knowledge and keep your streak alive.',
              style: TextStyle(color: AppColors.muted)),
          const SizedBox(height: 14),
          FilledButton(
            style: FilledButton.styleFrom(minimumSize: const Size(140, 44)),
            onPressed: () => context.push('/quiz'),
            child: Text(played ? 'Open quiz' : 'Start'),
          ),
        ]),
      ),
    );
  }
}

class _UpcomingRow extends StatelessWidget {
  const _UpcomingRow({required this.items, required this.onChanged});

  final List<Upcoming> items;
  final Future<void> Function() onChanged;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 250,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: items.length,
        separatorBuilder: (_, __) => const SizedBox(width: 12),
        itemBuilder: (_, i) => _UpcomingCard(item: items[i], onChanged: onChanged),
      ),
    );
  }
}

class _UpcomingCard extends StatefulWidget {
  const _UpcomingCard({required this.item, required this.onChanged});

  final Upcoming item;
  final Future<void> Function() onChanged;

  @override
  State<_UpcomingCard> createState() => _UpcomingCardState();
}

class _UpcomingCardState extends State<_UpcomingCard> {
  late bool _reminder = widget.item.hasReminder;
  bool _busy = false;

  Future<void> _toggle() async {
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    final ok = await runAction(context, () async {
      if (_reminder) {
        final mine = await repo.myReminders();
        final r = mine.where((e) => e.tmdbId == widget.item.tmdbId).firstOrNull;
        if (r != null) await repo.deleteReminder(r.id);
      } else {
        await repo.createReminder(widget.item.tmdbId);
      }
    }, success: _reminder ? 'Reminder removed.' : 'We will remind you on release day.');
    if (!mounted) return;
    setState(() {
      _busy = false;
      if (ok) _reminder = !_reminder;
    });
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    final label = item.daysUntil <= 0
        ? 'TODAY!'
        : item.daysUntil == 1
            ? 'TOMORROW'
            : 'IN ${item.daysUntil} DAYS';
    return SizedBox(
      width: 150,
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Stack(children: [
          Poster(url: item.posterUrl, title: item.title, width: 150, height: 200),
          Positioned(
            left: 6,
            bottom: 6,
            child: Chip2(label, color: item.daysUntil <= 0 ? AppColors.red : AppColors.muted),
          ),
          Positioned(
            right: 4,
            top: 4,
            child: IconButton.filledTonal(
              visualDensity: VisualDensity.compact,
              tooltip: _reminder ? 'Remove reminder' : 'Remind me',
              onPressed: _busy ? null : _toggle,
              icon: Icon(_reminder ? Icons.notifications_active : Icons.notifications_none,
                  size: 18, color: _reminder ? AppColors.gold : null),
            ),
          ),
        ]),
        const SizedBox(height: 6),
        Text(item.title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
      ]),
    );
  }
}

void _showAllActivity(BuildContext context, List<FeedItem> items) {
  showModalBottomSheet<void>(
    context: context,
    backgroundColor: AppColors.surface,
    isScrollControlled: true,
    builder: (ctx) => SizedBox(
      height: MediaQuery.sizeOf(ctx).height * 0.8,
      child: Column(children: [
        const Padding(
          padding: EdgeInsets.all(16),
          child: Text('Friends activity', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
        ),
        Expanded(child: ListView(children: [for (final i in items) _FeedTile(item: i)])),
      ]),
    ),
  );
}

String _describe(FeedItem item) {
  final who = item.username;
  final movie = item.movieTitle ?? 'a title';
  switch (item.type) {
    case 'Review':
      return '$who rated $movie${item.rating == null ? '' : ' ${item.rating}/10'}';
    case 'Thread':
      return '$who started the discussion "${item.text ?? ''}"${item.movieTitle == null ? '' : ' about ${item.movieTitle}'}';
    case 'Watchlist':
      return '$who added $movie to their Watchlist';
    case 'Interested':
      return '$who is interested in $movie';
    case 'List':
      return '$who created the list "${item.text ?? ''}"';
    default:
      return '$who: ${item.text ?? movie}';
  }
}

class _FeedTile extends StatelessWidget {
  const _FeedTile({required this.item});

  final FeedItem item;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: () {
        if (item.movieId != null) {
          context.push('/movie/${item.movieId}');
        } else if (item.threadId != null) {
          context.push('/thread/${item.threadId}');
        } else if (item.listId != null) {
          context.push('/list/${item.listId}');
        }
      },
      leading: Avatar(username: item.username),
      title: Text(_describe(item)),
      subtitle: Text(timeAgo(item.createdAt), style: const TextStyle(color: AppColors.muted)),
    );
  }
}
