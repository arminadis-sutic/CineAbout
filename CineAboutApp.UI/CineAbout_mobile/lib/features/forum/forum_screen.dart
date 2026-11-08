import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import 'thread_tile.dart';

class ForumScreen extends StatefulWidget {
  const ForumScreen({super.key});

  @override
  State<ForumScreen> createState() => _ForumScreenState();
}

class _ForumScreenState extends State<ForumScreen> {
  final _list = PagedListController();
  Timer? _debounce;
  String _query = '';
  String _sort = 'Newest';
  String? _tag;
  List<String> _tags = const [];
  late final ValueNotifier<ForumThread?> _threadLoaded;

  @override
  void initState() {
    super.initState();
    final repo = context.read<Repository>();
    _threadLoaded = repo.threadLoaded..addListener(_onThreadLoaded);
    repo.popularTags().then((t) {
      if (mounted) setState(() => _tags = t);
    }).catchError((_) {});
  }

  void _onThreadLoaded() {
    final fresh = _threadLoaded.value;
    if (fresh == null) return;
    _list.replaceWhere((x) => x is ForumThread && x.id == fresh.id, fresh);
  }

  @override
  void dispose() {
    _threadLoaded.removeListener(_onThreadLoaded);
    _debounce?.cancel();
    super.dispose();
  }

  Future<void> _create() async {
    await context.push('/thread/new');
    _list.reload();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(
        title: const Text.rich(
          TextSpan(children: [
            TextSpan(text: 'FO', style: TextStyle(color: AppColors.red)),
            TextSpan(text: 'RUM'),
          ]),
          style: TextStyle(fontWeight: FontWeight.w900, letterSpacing: 1.5),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: AppColors.red,
        foregroundColor: Colors.white,
        tooltip: 'New thread',
        onPressed: _create,
        child: const Icon(Icons.edit),
      ),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
          child: TextField(
            decoration: const InputDecoration(
              hintText: 'Search discussions',
              prefixIcon: Icon(Icons.search, size: 20),
            ),
            onChanged: (v) {
              _debounce?.cancel();
              _debounce = Timer(const Duration(milliseconds: 350), () {
                _query = v.trim();
                _list.reload();
              });
            },
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: SegmentedButton<String>(
            showSelectedIcon: false,
            segments: const [
              ButtonSegment(value: 'Newest', label: Text('Newest')),
              ButtonSegment(value: 'Top', label: Text('Trending')),
            ],
            selected: {_sort},
            onSelectionChanged: (s) {
              setState(() => _sort = s.first);
              _list.reload();
            },
          ),
        ),
        if (_tags.isNotEmpty)
          SizedBox(
            height: 46,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.fromLTRB(16, 6, 16, 6),
              itemCount: _tags.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (_, i) => FilterChip(
                label: Text('#${_tags[i]}'),
                selected: _tag == _tags[i],
                onSelected: (v) {
                  setState(() => _tag = v ? _tags[i] : null);
                  _list.reload();
                },
              ),
            ),
          ),
        Expanded(
          child: PagedList<ForumThread>(
            controller: _list,
            emptyText: 'No discussions found. Start one!',
            fetch: (page) =>
                repo.threads(search: _query, sortBy: _sort, tag: _tag, page: page),
            itemBuilder: (_, t) => ThreadTile(
              thread: t,
              onDeleted: () => _list.removeWhere((x) => x is ForumThread && x.id == t.id),
            ),
          ),
        ),
      ]),
    );
  }
}
