import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../forum/thread_tile.dart';
import '../lists/lists_widgets.dart';
import 'review_sheet.dart';
import 'review_tile.dart';

class MovieDetailsScreen extends StatefulWidget {
  const MovieDetailsScreen({super.key, required this.movieId});

  final int movieId;

  @override
  State<MovieDetailsScreen> createState() => _MovieDetailsScreenState();
}

class _MovieDetailsScreenState extends State<MovieDetailsScreen> {
  int _tab = 0;
  Future<void> Function()? _reload;

  void _refresh() => _reload?.call();

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(),
      body: AsyncData<MovieDetail>(
        key: ValueKey('movie-${widget.movieId}'),
        load: () => repo.movie(widget.movieId),
        builder: (context, movie, reload) {
          _reload = reload;
          return ListView(
          children: [
            _Header(movie: movie, onChanged: _refresh),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
              child: SegmentedButton<int>(
                showSelectedIcon: false,
                segments: const [
                  ButtonSegment(value: 0, label: Text('Info')),
                  ButtonSegment(value: 1, label: Text('Reviews')),
                  ButtonSegment(value: 2, label: Text('Discussion')),
                ],
                selected: {_tab},
                onSelectionChanged: (s) => setState(() => _tab = s.first),
              ),
            ),
            if (_tab == 0) _InfoTab(movie: movie),
            if (_tab == 1) _ReviewsTab(movie: movie, onChanged: _refresh),
            if (_tab == 2) _DiscussionTab(movie: movie),
            const SizedBox(height: 32),
          ],
        );
        },
      ),
    );
  }
}

class _Header extends StatefulWidget {
  const _Header({required this.movie, required this.onChanged});

  final MovieDetail movie;
  final VoidCallback onChanged;

  @override
  State<_Header> createState() => _HeaderState();
}

class _HeaderState extends State<_Header> {
  late String? _list = widget.movie.myListType;
  bool _expanded = false;
  bool _busy = false;

  Future<void> _toggle(String type) async {
    final repo = context.read<Repository>();
    final remove = _list == type;
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => remove
          ? repo.removeWatchlist(widget.movie.card.id)
          : repo.setWatchlist(widget.movie.card.id, type),
      success: remove
          ? 'Removed from your lists.'
          : (_list != null ? 'Moved from $_list to $type.' : 'Added to $type.'),
    );
    if (!mounted) return;
    setState(() {
      _busy = false;
      if (ok) _list = remove ? null : type;
    });
  }

  @override
  Widget build(BuildContext context) {
    final m = widget.movie;
    final c = m.card;
    final meta = [
      if (c.genres.isNotEmpty) c.genres.take(2).join(' / '),
      '${c.year}',
      if (m.duration > 0) '${m.duration ~/ 60}h ${m.duration % 60}m',
    ].join('  ·  ');
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Poster(url: c.posterUrl, title: c.title, width: 120, height: 180, radius: 12),
          const SizedBox(width: 16),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(c.title, style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w900)),
              const SizedBox(height: 8),
              RatingBadge(c.rating, count: c.reviewCount),
              const SizedBox(height: 8),
              Text(meta, style: const TextStyle(color: AppColors.muted)),
              if (m.directorName != null) ...[
                const SizedBox(height: 8),
                Text.rich(TextSpan(children: [
                  const TextSpan(text: 'Director: ', style: TextStyle(color: AppColors.muted)),
                  TextSpan(
                    text: m.directorName,
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      color: m.directorId == null ? null : AppColors.red,
                      decoration: m.directorId == null ? null : TextDecoration.underline,
                    ),
                    recognizer: m.directorId == null
                        ? null
                        : (TapGestureRecognizer()..onTap = () => context.push('/person/${m.directorId}')),
                  ),
                ])),
              ],
            ]),
          ),
        ]),
        const SizedBox(height: 16),
        GestureDetector(
          onTap: () => setState(() => _expanded = !_expanded),
          child: Text(
            m.synopsis.isEmpty ? 'No synopsis available.' : m.synopsis,
            maxLines: _expanded ? null : 4,
            overflow: _expanded ? null : TextOverflow.ellipsis,
            style: const TextStyle(height: 1.4),
          ),
        ),
        if (m.synopsis.length > 180)
          TextButton(
            onPressed: () => setState(() => _expanded = !_expanded),
            child: Text(_expanded ? 'Less' : 'More', style: const TextStyle(color: AppColors.red)),
          ),
        const SizedBox(height: 8),
        Row(children: [
          Expanded(
            child: _ListButton(
              icon: _list == 'Watchlist' ? Icons.bookmark : Icons.bookmark_border,
              label: 'Watchlist',
              active: _list == 'Watchlist',
              onTap: _busy ? null : () => _toggle('Watchlist'),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: _ListButton(
              icon: _list == 'Interested' ? Icons.favorite : Icons.favorite_border,
              label: 'Interested',
              active: _list == 'Interested',
              onTap: _busy ? null : () => _toggle('Interested'),
            ),
          ),
        ]),
        const Padding(
          padding: EdgeInsets.only(top: 6),
          child: Text(
            'Watchlist: titles you are watching or have watched.  Interested: titles you plan to watch.',
            style: TextStyle(color: AppColors.muted, fontSize: 11),
          ),
        ),
        const SizedBox(height: 8),
        _ListButton(
          icon: Icons.playlist_add,
          label: 'Add to list',
          active: false,
          onTap: () => showAddToListSheet(context, m.card.id),
        ),
      ]),
    );
  }
}

class _ListButton extends StatelessWidget {
  const _ListButton({required this.icon, required this.label, required this.active, this.onTap});

  final IconData icon;
  final String label;
  final bool active;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(46),
        backgroundColor: active ? AppColors.redDark : null,
        side: BorderSide(color: active ? AppColors.red : AppColors.border),
      ),
      onPressed: onTap,
      icon: Icon(icon, size: 18, color: active ? AppColors.red : null),
      label: Text(label.toUpperCase(), style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700)),
    );
  }
}

class _InfoTab extends StatelessWidget {
  const _InfoTab({required this.movie});

  final MovieDetail movie;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      const SectionTitle('Ratings'),
      AsyncData<RatingSummary>(
        load: () => repo.ratingSummary(movie.card.id),
        builder: (context, s, _) => Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
            Column(children: [
              Text(s.count == 0 ? '—' : s.average.toStringAsFixed(1),
                  style: const TextStyle(fontSize: 40, fontWeight: FontWeight.w900, color: AppColors.gold)),
              Text('${s.count} reviews', style: const TextStyle(color: AppColors.muted, fontSize: 12)),
            ]),
            const SizedBox(width: 20),
            Expanded(child: _Histogram(values: s.histogram)),
          ]),
        ),
      ),
      if (movie.cast.isNotEmpty) ...[
        const SectionTitle('Cast'),
        SizedBox(
          height: 116,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: movie.cast.length,
            separatorBuilder: (_, __) => const SizedBox(width: 14),
            itemBuilder: (_, i) {
              final c = movie.cast[i];
              return GestureDetector(
                onTap: () => context.push('/person/${c.personId}'),
                child: SizedBox(
                width: 90,
                child: Column(children: [
                  Avatar(username: c.name, url: c.photoUrl, radius: 28),
                  const SizedBox(height: 6),
                  Text(c.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
                  Text(c.role,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 10, color: AppColors.muted)),
                ]),
              ),
              );
            },
          ),
        ),
      ],
      const SectionTitle('Details'),
      Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        child: Column(children: [
          if (movie.originalTitle.isNotEmpty && movie.originalTitle != movie.card.title)
            _Fact('Original title', movie.originalTitle),
          if (movie.studioName.isNotEmpty)
            GestureDetector(
              onTap: movie.studioId == null ? null : () => context.push('/studio/${movie.studioId}'),
              child: _Fact('Studio', movie.studioName, link: movie.studioId != null),
            ),
          if (movie.languageName.isNotEmpty) _Fact('Language', movie.languageName),
          _Fact('Type', movie.card.isSeries ? 'Series' : 'Film'),
        ]),
      ),
    ]);
  }
}

class _Fact extends StatelessWidget {
  const _Fact(this.label, this.value, {this.link = false});

  final String label, value;
  final bool link;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Row(children: [
          SizedBox(width: 110, child: Text(label, style: const TextStyle(color: AppColors.muted))),
          Expanded(
            child: Text(value,
                style: link ? const TextStyle(color: AppColors.red, decoration: TextDecoration.underline) : null),
          ),
        ]),
      );
}

class _Histogram extends StatelessWidget {
  const _Histogram({required this.values});

  final List<int> values;

  @override
  Widget build(BuildContext context) {
    final max = values.fold<int>(1, (a, b) => b > a ? b : a);
    return SizedBox(
      height: 70,
      child: Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
        for (var i = 0; i < values.length; i++)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 1.5),
              child: Column(mainAxisAlignment: MainAxisAlignment.end, children: [
                Container(
                  height: 4 + 44 * (values[i] / max),
                  decoration: BoxDecoration(
                    color: values[i] == 0 ? AppColors.border : AppColors.red,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
                const SizedBox(height: 3),
                Text('$i', style: const TextStyle(fontSize: 9, color: AppColors.muted)),
              ]),
            ),
          ),
      ]),
    );
  }
}

class _ReviewsTab extends StatefulWidget {
  const _ReviewsTab({required this.movie, required this.onChanged});

  final MovieDetail movie;
  final VoidCallback onChanged;

  @override
  State<_ReviewsTab> createState() => _ReviewsTabState();
}

class _ReviewsTabState extends State<_ReviewsTab> {
  String _sort = 'Relevance';
  Future<void> Function()? _reload;

  Future<void> _write(Review? mine) async {
    final saved = await showReviewSheet(context, movie: widget.movie.card, existing: mine);
    if (saved == true) {
      await _reload?.call();
      widget.onChanged();
    }
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final me = context.read<AuthState>().userId;
    return AsyncData<List<Review>>(
      key: ValueKey('reviews-$_sort'),
      load: () async => (await repo.reviews(movieId: widget.movie.card.id, sortBy: _sort, pageSize: 50)).items,
      builder: (context, reviews, reload) {
        _reload = reload;
        final mine = reviews.where((r) => r.userId == me).firstOrNull;
        return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: FilledButton.icon(
              onPressed: () => _write(mine),
              icon: Icon(mine == null ? Icons.rate_review_outlined : Icons.edit_outlined, size: 18),
              label: Text(mine == null ? 'Write a review' : 'Edit your review'),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Wrap(spacing: 8, children: [
              for (final s in const {'Relevance': 'Top', 'Date': 'Newest', 'Rating': 'Highest'}.entries)
                ChoiceChip(
                  label: Text(s.value),
                  selected: _sort == s.key,
                  onSelected: (_) => setState(() => _sort = s.key),
                ),
            ]),
          ),
          if (reviews.isEmpty)
            const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: Text('No reviews yet. Be the first!', style: TextStyle(color: AppColors.muted))),
            ),
          for (final r in reviews)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
              child: ReviewTile(
                review: r,
                isMine: r.userId == me,
                onEdit: () => _write(r),
                onChanged: () {
                  reload();
                  widget.onChanged();
                },
              ),
            ),
        ]);
      },
    );
  }
}

class _DiscussionTab extends StatelessWidget {
  const _DiscussionTab({required this.movie});

  final MovieDetail movie;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
        child: OutlinedButton.icon(
          onPressed: () => context.push(
              '/thread/new?movieId=${movie.card.id}&movieTitle=${Uri.encodeQueryComponent(movie.card.title)}'),
          icon: const Icon(Icons.edit_outlined, size: 18),
          label: const Text('Start a discussion'),
        ),
      ),
      AsyncData<List<ForumThread>>(
        load: () async => (await repo.threads(movieId: movie.card.id, pageSize: 30)).items,
        builder: (context, threads, reload) => threads.isEmpty
            ? const Padding(
                padding: EdgeInsets.all(32),
                child: Center(
                    child: Text('No discussions yet.', style: TextStyle(color: AppColors.muted))),
              )
            : Column(children: [
                for (final t in threads)
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
                    child: ThreadTile(thread: t, onDeleted: () => reload()),
                  ),
              ]),
      ),
    ]);
  }
}
