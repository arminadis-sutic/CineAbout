import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../movie/movie_cards.dart';

class SearchScreen extends StatefulWidget {
  const SearchScreen({super.key, this.genreId});

  final int? genreId;

  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final _controller = TextEditingController();
  final _list = PagedListController();
  Timer? _debounce;
  String _query = '';
  String? _type;
  int? _genreId;
  List<Named> _genres = const [];
  bool _people = false;

  @override
  void initState() {
    super.initState();
    _genreId = widget.genreId;
    context.read<Repository>().genres().then((g) {
      if (mounted) setState(() => _genres = g);
    }).catchError((_) {});
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _changed() => _list.reload();

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(
        title: TextField(
          controller: _controller,
          autofocus: widget.genreId == null,
          decoration: InputDecoration(
            hintText: _people ? 'Search people' : 'Search movies and series',
            border: InputBorder.none,
            enabledBorder: InputBorder.none,
            focusedBorder: InputBorder.none,
            filled: false,
          ),
          onChanged: (v) {
            _debounce?.cancel();
            _debounce = Timer(const Duration(milliseconds: 350), () {
              if (!mounted) return;
              setState(() => _query = v.trim());
              _changed();
            });
          },
        ),
      ),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
          child: Row(children: [
            ChoiceChip(
              label: const Text('Titles'),
              selected: !_people,
              onSelected: (_) => setState(() => _people = false),
            ),
            const SizedBox(width: 8),
            ChoiceChip(
              label: const Text('People'),
              selected: _people,
              onSelected: (_) => setState(() => _people = true),
            ),
          ]),
        ),
        if (!_people)
          SizedBox(
            height: 44,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                for (final t in const {'Movie': 'Films', 'Series': 'Series'}.entries)
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: FilterChip(
                      label: Text(t.value),
                      selected: _type == t.key,
                      onSelected: (v) {
                        setState(() => _type = v ? t.key : null);
                        _changed();
                      },
                    ),
                  ),
                for (final g in _genres)
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: FilterChip(
                      label: Text(g.name),
                      selected: _genreId == g.id,
                      onSelected: (v) {
                        setState(() => _genreId = v ? g.id : null);
                        _changed();
                      },
                    ),
                  ),
              ],
            ),
          ),
        Expanded(
          child: _people
              ? _PeopleResults(query: _query)
              : PagedList<MovieCard>(
                  key: const ValueKey('titles'),
                  controller: _list,
                  emptyText: 'No titles match your search.',
                  fetch: (page) => repo.movies(
                      search: _query, type: _type, genreId: _genreId, page: page),
                  itemBuilder: (_, m) => MovieTile(movie: m),
                ),
        ),
      ]),
    );
  }
}

class _PeopleResults extends StatelessWidget {
  const _PeopleResults({required this.query});

  final String query;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    if (query.isEmpty) {
      return const EmptyView('Type a username to find people.', icon: Icons.people_outline);
    }
    return PagedList<UserHit>(
      key: ValueKey(query),
      emptyText: 'Nobody found.',
      fetch: (page) => repo.searchUsers(query, page: page),
      itemBuilder: (context, u) => Card(
        child: ListTile(
          onTap: () => context.push('/user/${u.id}'),
          leading: Avatar(username: u.username, url: u.imageUrl),
          title: Text(u.username),
          subtitle: u.name.isEmpty
              ? null
              : Text(u.name, style: const TextStyle(color: AppColors.muted)),
          trailing: const Icon(Icons.chevron_right),
        ),
      ),
    );
  }
}
