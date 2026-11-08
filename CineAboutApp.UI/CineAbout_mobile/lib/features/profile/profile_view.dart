import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../lists/lists_widgets.dart';
import '../movie/movie_cards.dart';
import '../movie/review_tile.dart';

class ProfileView extends StatefulWidget {
  const ProfileView({super.key, required this.userId, required this.isMe, this.actions});

  final int userId;
  final bool isMe;
  final Widget Function(PublicProfile profile, VoidCallback reload)? actions;

  @override
  State<ProfileView> createState() => _ProfileViewState();
}

class _ProfileViewState extends State<ProfileView> {
  int _tab = 0;
  Future<void> Function()? _reload;
  late final ValueNotifier<int> _activityChanged;

  @override
  void initState() {
    super.initState();
    _activityChanged = context.read<Repository>().activityChanged..addListener(_onActivityChanged);
  }

  @override
  void dispose() {
    _activityChanged.removeListener(_onActivityChanged);
    super.dispose();
  }

  void _onActivityChanged() {
    if (widget.isMe) _reload?.call();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return AsyncData<PublicProfile>(
      key: ValueKey('profile-${widget.userId}'),
      load: () => repo.publicProfile(widget.userId),
      builder: (context, p, reload) {
        _reload = reload;
        return RefreshIndicator(
          onRefresh: reload,
          child: ListView(children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(children: [
                Avatar(username: p.username, url: p.imageUrl, radius: 44),
                const SizedBox(height: 12),
                Text(p.username, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w900)),
                if (p.displayName != p.username)
                  Text(p.displayName, style: const TextStyle(color: AppColors.muted)),
                if (p.bio.isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Text(p.bio, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.muted)),
                ],
                if (p.city.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(p.city, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
                  ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: Row(children: [
                    _Stat(p.rated, 'RATINGS'),
                    _Stat(p.threads, 'POSTS'),
                    _Stat(p.comments, 'COMMENTS'),
                    _Stat(p.streak, 'STREAK'),
                  ]),
                ),
                const SizedBox(height: 12),
                Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                  TextButton(
                    onPressed: () => _showPeople(context, p, followers: true),
                    child: Text('${p.followers} followers'),
                  ),
                  const Text('·', style: TextStyle(color: AppColors.muted)),
                  TextButton(
                    onPressed: () => _showPeople(context, p, followers: false),
                    child: Text('${p.following} following'),
                  ),
                ]),
                if (widget.actions != null) widget.actions!(p, () => reload()),
              ]),
            ),
            _ProfileTabs(selected: _tab, onChanged: (i) => setState(() => _tab = i)),
            const SizedBox(height: 8),
            if (_tab == 0) _UserReviews(userId: widget.userId, isMe: widget.isMe),
            if (_tab == 1) _UserList(userId: widget.userId, listType: 'Watchlist'),
            if (_tab == 2) _UserList(userId: widget.userId, listType: 'Interested'),
            if (_tab == 3) UserListsTab(userId: widget.userId, isMe: widget.isMe),
            const SizedBox(height: 24),
          ]),
        );
      },
    );
  }

  void _showPeople(BuildContext context, PublicProfile p, {required bool followers}) {
    final repo = context.read<Repository>();
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: AppColors.surface,
      isScrollControlled: true,
      builder: (ctx) => SizedBox(
        height: MediaQuery.sizeOf(context).height * 0.6,
        child: Column(children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Text(followers ? 'Followers' : 'Following',
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
          ),
          Expanded(
            child: PagedList<FollowUser>(
              emptyText: followers ? 'No followers yet.' : 'Not following anyone yet.',
              fetch: (_) => followers ? repo.followers(p.id) : repo.following(p.id),
              itemBuilder: (context, u) => ListTile(
                leading: Avatar(username: u.username, url: u.imageUrl),
                title: Text(u.username),
                onTap: () {
                  Navigator.pop(ctx);
                  context.push('/user/${u.userId}');
                },
              ),
            ),
          ),
        ]),
      ),
    );
  }
}

class _ProfileTabs extends StatelessWidget {
  const _ProfileTabs({required this.selected, required this.onChanged});

  final int selected;
  final ValueChanged<int> onChanged;

  static const _names = ['Reviews', 'Watchlist', 'Interested', 'Lists'];

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Row(children: [
        for (var i = 0; i < _names.length; i++) ...[
          if (i > 0) const SizedBox(width: 8),
          ChoiceChip(
            label: Text(_names[i], maxLines: 1, softWrap: false, style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.text)),
            selected: selected == i,
            showCheckmark: false,
            selectedColor: AppColors.redDark,
            side: BorderSide(color: selected == i ? AppColors.red : AppColors.border),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            onSelected: (_) => onChanged(i),
          ),
        ],
      ]),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat(this.value, this.label);

  final int value;
  final String label;

  @override
  Widget build(BuildContext context) => Expanded(
        child: Column(children: [
          Text('$value', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900)),
          const SizedBox(height: 2),
          Text(label, style: const TextStyle(fontSize: 10, color: AppColors.muted, letterSpacing: 1)),
        ]),
      );
}

class _UserReviews extends StatelessWidget {
  const _UserReviews({required this.userId, required this.isMe});

  final int userId;
  final bool isMe;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final me = context.read<AuthState>().userId;
    return AsyncData<List<Review>>(
      load: () async => (await repo.reviews(userId: userId, sortBy: 'Date', pageSize: 30)).items,
      builder: (context, reviews, reload) => reviews.isEmpty
          ? const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: Text('No reviews yet.', style: TextStyle(color: AppColors.muted))),
            )
          : Column(children: [
              for (final r in reviews)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                  child: GestureDetector(
                    onTap: () => context.push('/movie/${r.movieId}'),
                    child: ReviewTile(
                      review: r,
                      isMine: r.userId == me,
                      showMovie: true,
                      onChanged: reload,
                    ),
                  ),
                ),
            ]),
    );
  }
}

class _UserList extends StatelessWidget {
  const _UserList({required this.userId, required this.listType});

  final int userId;
  final String listType;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return AsyncData<List<WatchlistItem>>(
      key: ValueKey(listType),
      load: () async => (await repo.userWatchlist(userId, listType: listType)).items,
      builder: (context, items, _) => items.isEmpty
          ? const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: Text('Nothing here yet.', style: TextStyle(color: AppColors.muted))),
            )
          : Column(children: [
              for (final i in items)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                  child: MovieTile(movie: i.movie, subtitle: 'Added ${formatDate(i.addedAt)}'),
                ),
            ]),
    );
  }
}
