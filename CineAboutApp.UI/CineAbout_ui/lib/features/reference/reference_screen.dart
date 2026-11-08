import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/widgets.dart';

class ReferenceScreen extends StatelessWidget {
  const ReferenceScreen({super.key});

  static const _tabs = [
    ('Genres', 'Genre', 'genre'),
    ('Languages', 'Language', 'language'),
    ('Countries', 'Country', 'country'),
    ('Cities', 'City', 'city'),
    ('Awards', 'AwardType', 'award'),
    ('Quiz categories', 'QuizCategory', 'quiz category'),
    ('Roles', 'Role', 'role'),
  ];

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: _tabs.length,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const PageHeader(
            title: 'Reference data',
            subtitle: 'Manage core taxonomies used across the platform.',
          ),
          TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [for (final t in _tabs) Tab(text: t.$1)],
          ),
          const SizedBox(height: 16),
          Expanded(
            child: TabBarView(children: [
              for (final t in _tabs) _ReferenceList(controller: t.$2, noun: t.$3),
            ]),
          ),
        ],
      ),
    );
  }
}

class _ReferenceInput {
  _ReferenceInput(this.name, this.countryId);

  final String name;
  final int? countryId;
}

class _ReferenceDialog extends StatefulWidget {
  const _ReferenceDialog({required this.title, required this.item, required this.withCountry});

  final String title;
  final Named? item;
  final bool withCountry;

  @override
  State<_ReferenceDialog> createState() => _ReferenceDialogState();
}

class _ReferenceDialogState extends State<_ReferenceDialog> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _name = TextEditingController(text: widget.item?.name);
  int? _countryId;
  List<Named> _countries = const [];

  @override
  void initState() {
    super.initState();
    _countryId = widget.item?.countryId;
    if (widget.withCountry) {
      context.read<Repository>().reference('Country').then((items) {
        if (!mounted) return;
        setState(() {
          _countries = items;
          if (!items.any((c) => c.id == _countryId)) _countryId = null;
        });
      });
    }
  }

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  void _submit() {
    if (_form.currentState!.validate()) {
      Navigator.pop(context, _ReferenceInput(_name.text.trim(), _countryId));
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: DialogTitle(widget.title),
      content: SizedBox(
        width: 360,
        child: Form(
          key: _form,
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            TextFormField(
              controller: _name,
              autofocus: true,
              maxLength: 200,
              decoration: const InputDecoration(labelText: 'Name'),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Required' : null,
              onFieldSubmitted: (_) => _submit(),
            ),
            if (widget.withCountry)
              DropdownButtonFormField<int>(
                key: ValueKey('country-${_countries.length}-$_countryId'),
                initialValue: _countryId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Country *'),
                items: [for (final c in _countries) DropdownMenuItem(value: c.id, child: Text(c.name))],
                onChanged: (v) => setState(() => _countryId = v),
                validator: (v) => v == null ? 'Required' : null,
              ),
          ]),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(onPressed: _submit, child: const Text('Save')),
      ],
    );
  }
}

class _ReferenceList extends StatefulWidget {
  const _ReferenceList({required this.controller, required this.noun});

  final String controller;
  final String noun;

  @override
  State<_ReferenceList> createState() => _ReferenceListState();
}

class _ReferenceListState extends State<_ReferenceList> with AutomaticKeepAliveClientMixin {
  String _search = '';
  int _version = 0;

  bool get _withCountry => widget.controller == 'City';

  bool _builtIn(Named item) => widget.controller == 'Role' && (item.name == 'Admin' || item.name == 'User');

  @override
  bool get wantKeepAlive => true;

  void _reload() => setState(() => _version++);

  Future<void> _edit([Named? item]) async {
    final input = await showDialog<_ReferenceInput>(
      context: context,
      builder: (ctx) => _ReferenceDialog(
        title: '${item == null ? 'Add' : 'Edit'} ${widget.noun}',
        item: item,
        withCountry: _withCountry,
      ),
    );
    if (input == null || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(
        context,
        () => repo.saveReference(widget.controller, item?.id, input.name,
            extra: _withCountry ? {'countryId': input.countryId} : null),
        success: 'Saved.')) {
      _reload();
    }
  }

  Future<void> _delete(Named item) async {
    final ok = await confirm(context,
        title: 'Delete ${widget.noun}',
        message: 'Delete "${item.name}"? Items still in use cannot be deleted.',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteReference(widget.controller, item.id),
        success: 'Deleted.')) {
      _reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final repo = context.read<Repository>();
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Row(children: [
        SearchBox(
          hint: 'Search ${widget.noun}s',
          onChanged: (v) => setState(() {
            _search = v;
            _version++;
          }),
        ),
        const Spacer(),
        FilledButton.icon(
          onPressed: _edit,
          icon: const Icon(Icons.add, size: 18),
          label: Text('Add ${widget.noun}'),
        ),
      ]),
      const SizedBox(height: 16),
      Expanded(
        child: AsyncData<List<Named>>(
          key: ValueKey('$_version'),
          load: () => repo.reference(widget.controller, search: _search),
          builder: (context, items, _) => ListView(children: [
            Text('${items.length} results', style: const TextStyle(color: Colors.grey)),
            const SizedBox(height: 8),
            DataGrid(
              columns: [
                const DataColumnSpec('Name', flex: 3),
                if (_withCountry) const DataColumnSpec('Country', flex: 2),
                const DataColumnSpec('', width: 100),
              ],
              emptyText: 'Nothing here yet.',
              rows: [
                for (final i in items)
                  [
                    Text(i.name, style: const TextStyle(fontWeight: FontWeight.w700)),
                    if (_withCountry) Text(i.countryName ?? ''),
                    Row(mainAxisSize: MainAxisSize.min, children: [
                      IconButton(
                          tooltip: _builtIn(i) ? 'Built-in role: its name is used by the permissions of the app' : 'Edit',
                          icon: const Icon(Icons.edit_outlined, size: 18),
                          onPressed: _builtIn(i) ? null : () => _edit(i)),
                      IconButton(
                          tooltip: _builtIn(i) ? 'Built-in role: it cannot be deleted' : 'Delete',
                          icon: const Icon(Icons.delete_outline, size: 18),
                          onPressed: _builtIn(i) ? null : () => _delete(i)),
                    ]),
                  ],
              ],
            ),
          ]),
        ),
      ),
    ]);
  }
}
