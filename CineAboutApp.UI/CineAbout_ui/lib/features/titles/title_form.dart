import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class _Lookups {
  _Lookups(this.genres, this.languages, this.studios, this.actors, this.directors);

  final List<Named> genres, languages, studios;
  final List<Named> actors, directors;
}

class TitleForm extends StatelessWidget {
  const TitleForm({super.key, required this.movieId, required this.onClose});

  final int? movieId;
  final void Function(bool saved) onClose;

  Future<(_Lookups, MovieSummary?)> _load(Repository repo) async {
    final results = await Future.wait([
      repo.reference('Genre'),
      repo.reference('Language'),
      repo.studios(pageSize: 100),
      repo.people(roleType: 'Actor', pageSize: 100),
      repo.people(roleType: 'Director', pageSize: 100),
    ]);
    final movie = movieId == null ? null : await repo.movie(movieId!);
    return (
      _Lookups(
        results[0] as List<Named>,
        results[1] as List<Named>,
        (results[2] as Paged<Studio>).items.map((s) => Named(s.id, s.name)).toList(),
        (results[3] as Paged<Person>).items.map((p) => Named(p.id, p.fullName)).toList(),
        (results[4] as Paged<Person>).items.map((p) => Named(p.id, p.fullName)).toList(),
      ),
      movie,
    );
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return AsyncData<(_Lookups, MovieSummary?)>(
      load: () => _load(repo),
      builder: (context, data, _) =>
          _TitleFormBody(lookups: data.$1, movie: data.$2, onClose: onClose),
    );
  }
}

class _TitleFormBody extends StatefulWidget {
  const _TitleFormBody({required this.lookups, required this.movie, required this.onClose});

  final _Lookups lookups;
  final MovieSummary? movie;
  final void Function(bool saved) onClose;

  @override
  State<_TitleFormBody> createState() => _TitleFormBodyState();
}

class _TitleFormBodyState extends State<_TitleFormBody> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _title;
  late final TextEditingController _original;
  late final TextEditingController _year;
  late final TextEditingController _duration;
  late final TextEditingController _synopsis;
  late final TextEditingController _poster;
  late String _type;
  late bool _visible;
  int? _languageId;
  int? _studioId;
  int? _directorId;
  late List<Named> _genres;
  late List<_CastRow> _cast;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final m = widget.movie;
    _title = TextEditingController(text: m?.title);
    _original = TextEditingController(text: m?.originalTitle);
    _year = TextEditingController(text: m == null ? '' : '${m.releaseYear}');
    _duration = TextEditingController(text: m == null ? '' : '${m.duration}');
    _synopsis = TextEditingController(text: m?.synopsis);
    _poster = TextEditingController(text: m?.posterUrl);
    _type = m?.type ?? 'Movie';
    _visible = m?.isVisible ?? true;
    _languageId = _valid(m?.languageId, widget.lookups.languages);
    _studioId = _valid(m?.studioId, widget.lookups.studios);
    _directorId = _valid(m?.directorId, widget.lookups.directors);
    _genres = [...?m?.genres];
    _cast = [
      for (final c in m?.cast ?? const <CastEntry>[])
        if (c.personId != m?.directorId) _CastRow(c.personId, c.role),
    ];
  }

  int? _valid(int? id, List<Named> options) =>
      id != null && options.any((o) => o.id == id) ? id : null;

  @override
  void dispose() {
    for (final c in [_title, _original, _year, _duration, _synopsis, _poster]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final repo = context.read<Repository>();
    final cast = [
      if (_directorId != null) {'personId': _directorId, 'roleInMovie': 'Director'},
      for (final c in _cast)
        if (c.personId != null && c.personId != _directorId)
          {'personId': c.personId, 'roleInMovie': c.role.text.trim().isEmpty ? 'Actor' : c.role.text.trim()},
    ];
    final body = {
      'title': _title.text.trim(),
      'originalTitle': _original.text.trim().isEmpty ? _title.text.trim() : _original.text.trim(),
      'type': _type,
      'releaseYear': int.parse(_year.text),
      'duration': int.parse(_duration.text),
      'synopsis': _synopsis.text.trim(),
      'posterUrl': _poster.text.trim(),
      'isVisible': _visible,
      'languageId': _languageId,
      'studioId': _studioId,
      'genreIds': _genres.map((g) => g.id).toList(),
      'cast': cast,
    };
    setState(() => _saving = true);
    final ok = await runAction(context, () => repo.saveMovie(widget.movie?.id, body),
        success: 'Title saved.');
    if (!mounted) return;
    setState(() => _saving = false);
    if (ok) widget.onClose(true);
  }

  String? _required(String? v) => (v == null || v.trim().isEmpty) ? 'Required' : null;

  String? _number(String? v, {required int min, required int max}) {
    final n = int.tryParse(v ?? '');
    if (n == null || n < min || n > max) return '$min–$max';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    final l = widget.lookups;
    final availableGenres = l.genres.where((g) => !_genres.contains(g)).toList();
    return Form(
      key: _form,
      child: ListView(
        children: [
          PageHeader(
            title: widget.movie == null ? 'Add new title' : 'Edit title',
            subtitle: 'Enter details for the movie or series.',
            actions: [
              TextButton.icon(
                onPressed: () => widget.onClose(false),
                icon: const Icon(Icons.arrow_back, size: 18),
                label: const Text('Back to list'),
              ),
            ],
          ),
          Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(
              flex: 3,
              child: Panel(
                title: 'Core information',
                child: Column(children: [
                  TextFormField(
                    controller: _title,
                    decoration: const InputDecoration(labelText: 'Title *'),
                    validator: _required,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _original,
                    decoration: const InputDecoration(
                        labelText: 'Original title', hintText: 'Leave blank if same as title'),
                  ),
                  const SizedBox(height: 12),
                  Row(children: [
                    Expanded(
                      child: SegmentedButton<String>(
                        showSelectedIcon: false,
                        segments: const [
                          ButtonSegment(value: 'Movie', label: Text('Film')),
                          ButtonSegment(value: 'Series', label: Text('Series')),
                        ],
                        selected: {_type},
                        onSelectionChanged: (s) => setState(() => _type = s.first),
                      ),
                    ),
                    const SizedBox(width: 12),
                    SizedBox(
                      width: 140,
                      child: TextFormField(
                        controller: _year,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Release year *'),
                        validator: (v) => _number(v, min: 1888, max: 2100),
                      ),
                    ),
                    const SizedBox(width: 12),
                    SizedBox(
                      width: 140,
                      child: TextFormField(
                        controller: _duration,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Duration (min) *'),
                        validator: (v) => _number(v, min: 1, max: 1000),
                      ),
                    ),
                  ]),
                  const SizedBox(height: 12),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Visible in the mobile app'),
                    value: _visible,
                    activeThumbColor: AppColors.red,
                    onChanged: (v) => setState(() => _visible = v),
                  ),
                ]),
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              flex: 2,
              child: Panel(
                title: 'Poster art',
                child: Column(children: [
                  AspectRatio(
                    aspectRatio: 2 / 3,
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Container(
                        color: AppColors.surfaceHigh,
                        child: _poster.text.trim().isEmpty
                            ? const Icon(Icons.image_outlined, size: 48, color: AppColors.muted)
                            : Image.network(
                                _poster.text.trim(),
                                fit: BoxFit.cover,
                                errorBuilder: (_, __, ___) => const Icon(Icons.broken_image_outlined,
                                    size: 48, color: AppColors.muted),
                              ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _poster,
                    decoration: const InputDecoration(labelText: 'Poster image URL'),
                    onChanged: (_) => setState(() {}),
                  ),
                ]),
              ),
            ),
          ]),
          const SizedBox(height: 20),
          Panel(
            title: 'Genres',
            child: Wrap(spacing: 8, runSpacing: 8, crossAxisAlignment: WrapCrossAlignment.center, children: [
              for (final g in _genres)
                InputChip(
                  label: Text(g.name),
                  onDeleted: () => setState(() => _genres.remove(g)),
                ),
              if (availableGenres.isNotEmpty)
                PopupMenuButton<Named>(
                  tooltip: 'Add genre',
                  onSelected: (g) => setState(() => _genres.add(g)),
                  itemBuilder: (_) => [
                    for (final g in availableGenres) PopupMenuItem(value: g, child: Text(g.name)),
                  ],
                  child: const Chip(
                    avatar: Icon(Icons.add, size: 16),
                    label: Text('Add genre'),
                  ),
                ),
            ]),
          ),
          const SizedBox(height: 20),
          Panel(
            title: 'Synopsis',
            child: TextFormField(
              controller: _synopsis,
              minLines: 4,
              maxLines: 8,
              decoration: const InputDecoration(hintText: 'Write a captivating summary of the plot…'),
            ),
          ),
          const SizedBox(height: 20),
          Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Expanded(
              flex: 3,
              child: Panel(
                title: 'Cast / actors',
                trailing: TextButton.icon(
                  onPressed: l.actors.isEmpty
                      ? null
                      : () => setState(() => _cast.add(_CastRow(null, 'Actor'))),
                  icon: const Icon(Icons.add, size: 16),
                  label: const Text('Add actor'),
                ),
                child: Column(children: [
                  if (_cast.isEmpty)
                    const Align(
                      alignment: Alignment.centerLeft,
                      child: Text('No cast added.', style: TextStyle(color: AppColors.muted)),
                    ),
                  for (var i = 0; i < _cast.length; i++)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: Row(children: [
                        Expanded(
                          flex: 3,
                          child: DropdownButtonFormField<int>(
                            initialValue: _cast[i].personId,
                            isExpanded: true,
                            decoration: const InputDecoration(labelText: 'Actor'),
                            items: [
                              for (final a in l.actors)
                                DropdownMenuItem(value: a.id, child: Text(a.name)),
                            ],
                            validator: (v) => v == null ? 'Select an actor' : null,
                            onChanged: (v) => setState(() => _cast[i].personId = v),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          flex: 2,
                          child: TextFormField(
                            controller: _cast[i].role,
                            decoration: const InputDecoration(labelText: 'Role / character'),
                          ),
                        ),
                        IconButton(
                          tooltip: 'Remove',
                          icon: const Icon(Icons.close, size: 18),
                          onPressed: () => setState(() => _cast.removeAt(i)),
                        ),
                      ]),
                    ),
                ]),
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              flex: 2,
              child: Panel(
                title: 'Production',
                child: Column(children: [
                  DropdownButtonFormField<int>(
                    initialValue: _directorId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Director'),
                    items: [
                      for (final d in l.directors) DropdownMenuItem(value: d.id, child: Text(d.name)),
                    ],
                    onChanged: (v) => setState(() => _directorId = v),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<int>(
                    initialValue: _studioId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Studio / production company *'),
                    items: [
                      for (final s in l.studios) DropdownMenuItem(value: s.id, child: Text(s.name)),
                    ],
                    validator: (v) => v == null ? 'Required' : null,
                    onChanged: (v) => setState(() => _studioId = v),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<int>(
                    initialValue: _languageId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Language *'),
                    items: [
                      for (final s in l.languages) DropdownMenuItem(value: s.id, child: Text(s.name)),
                    ],
                    validator: (v) => v == null ? 'Required' : null,
                    onChanged: (v) => setState(() => _languageId = v),
                  ),
                ]),
              ),
            ),
          ]),
          const SizedBox(height: 24),
          Row(mainAxisAlignment: MainAxisAlignment.end, children: [
            TextButton(onPressed: () => widget.onClose(false), child: const Text('Cancel')),
            const SizedBox(width: 12),
            FilledButton.icon(
              onPressed: _saving ? null : _save,
              icon: const Icon(Icons.save_outlined, size: 18),
              label: const Text('Save title'),
            ),
          ]),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

class _CastRow {
  _CastRow(this.personId, String role) : role = TextEditingController(text: role);

  int? personId;
  final TextEditingController role;
}
