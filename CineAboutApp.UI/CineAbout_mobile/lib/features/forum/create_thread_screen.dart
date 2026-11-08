import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class CreateThreadScreen extends StatefulWidget {
  const CreateThreadScreen({super.key, this.movieId, this.movieTitle});

  final int? movieId;
  final String? movieTitle;

  @override
  State<CreateThreadScreen> createState() => _CreateThreadScreenState();
}

class _CreateThreadScreenState extends State<CreateThreadScreen> {
  final _form = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _content = TextEditingController();
  final _tagInput = TextEditingController();
  final List<String> _tags = [];
  bool _spoiler = false;
  bool _busy = false;
  int? _movieId;
  String? _movieTitle;

  @override
  void initState() {
    super.initState();
    _movieId = widget.movieId;
    _movieTitle = widget.movieTitle;
  }

  @override
  void dispose() {
    _title.dispose();
    _content.dispose();
    _tagInput.dispose();
    super.dispose();
  }

  void _addTag() {
    final tag = _tagInput.text.trim().toLowerCase().replaceAll('#', '');
    if (tag.isEmpty || _tags.contains(tag)) return;
    if (_tags.length >= 5) {
      showMessage(context, 'You can add up to 5 tags.', error: true);
      return;
    }
    setState(() {
      _tags.add(tag);
      _tagInput.clear();
    });
  }

  Future<void> _pickMovie() async {
    final repo = context.read<Repository>();
    final picked = await showModalBottomSheet<MovieCard>(
      context: context,
      isScrollControlled: true,
      backgroundColor: AppColors.surface,
      builder: (_) => _MoviePicker(repo: repo),
    );
    if (picked != null) {
      setState(() {
        _movieId = picked.id;
        _movieTitle = picked.title;
      });
    }
  }

  Future<void> _post() async {
    if (!validateForm(context, _form, message: 'Please give your thread a title (at least 3 characters).')) return;
    final repo = context.read<Repository>();
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => repo.createThread(
        title: _title.text.trim(),
        content: _content.text.trim(),
        spoiler: _spoiler,
        movieId: _movieId,
        tags: _tags,
      ),
      success: 'Thread posted.',
    );
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) context.pop();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Create thread'), leading: const CloseButton()),
      body: Form(
        key: _form,
        child: ListView(padding: const EdgeInsets.all(16), children: [
          TextFormField(
            controller: _title,
            maxLength: 200,
            style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
            decoration: const InputDecoration(hintText: 'Thread title…'),
            validator: (v) =>
                (v == null || v.trim().length < 3) ? 'Title must have at least 3 characters' : null,
          ),
          const SizedBox(height: 8),
          Card(
            child: ListTile(
              leading: const Icon(Icons.movie_outlined, color: AppColors.red),
              title: Text(_movieTitle ?? 'Link a movie (optional)'),
              trailing: _movieId == null
                  ? const Icon(Icons.add)
                  : IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () => setState(() {
                        _movieId = null;
                        _movieTitle = null;
                      }),
                    ),
              onTap: _pickMovie,
            ),
          ),
          const SizedBox(height: 14),
          Wrap(spacing: 8, runSpacing: 4, children: [
            for (final t in _tags)
              InputChip(label: Text('#$t'), onDeleted: () => setState(() => _tags.remove(t))),
          ]),
          TextField(
            controller: _tagInput,
            decoration: InputDecoration(
              hintText: 'Add a tag (e.g. nolan)',
              suffixIcon: IconButton(icon: const Icon(Icons.add), onPressed: _addTag),
            ),
            onSubmitted: (_) => _addTag(),
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _content,
            minLines: 6,
            maxLines: 12,
            maxLength: 8000,
            decoration: const InputDecoration(hintText: 'Write your thoughts here…'),
          ),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Contains spoilers'),
            value: _spoiler,
            activeThumbColor: AppColors.red,
            onChanged: (v) => setState(() => _spoiler = v),
          ),
          const SizedBox(height: 8),
          FilledButton.icon(
            onPressed: _busy ? null : _post,
            icon: const Icon(Icons.send, size: 18),
            label: const Text('Post'),
          ),
        ]),
      ),
    );
  }
}

class _MoviePicker extends StatefulWidget {
  const _MoviePicker({required this.repo});

  final Repository repo;

  @override
  State<_MoviePicker> createState() => _MoviePickerState();
}

class _MoviePickerState extends State<_MoviePicker> {
  final _list = PagedListController();
  String _query = '';

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: MediaQuery.sizeOf(context).height * 0.8,
      child: Padding(
        padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
        child: Column(children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              autofocus: true,
              decoration: const InputDecoration(
                  hintText: 'Search movies and series', prefixIcon: Icon(Icons.search)),
              onChanged: (v) {
                _query = v.trim();
                _list.reload();
              },
            ),
          ),
          Expanded(
            child: PagedList<MovieCard>(
              controller: _list,
              emptyText: 'No titles found.',
              fetch: (page) => widget.repo.movies(search: _query, page: page, pageSize: 20),
              itemBuilder: (context, m) => ListTile(
                leading: Poster(url: m.posterUrl, title: m.title, width: 40, height: 60, radius: 6),
                title: Text(m.title),
                subtitle: Text('${m.year}'),
                onTap: () => Navigator.pop(context, m),
              ),
            ),
          ),
        ]),
      ),
    );
  }
}
