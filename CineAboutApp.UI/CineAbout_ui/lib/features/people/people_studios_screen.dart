import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/widgets.dart';

class PeopleStudiosScreen extends StatelessWidget {
  const PeopleStudiosScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const PageHeader(
            title: 'People & Studios',
            subtitle: 'Actors, directors and production companies.',
          ),
          const TabBar(
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: [Tab(text: 'People'), Tab(text: 'Studios')],
          ),
          const SizedBox(height: 16),
          const Expanded(
            child: TabBarView(children: [_PeopleTab(), _StudiosTab()]),
          ),
        ],
      ),
    );
  }
}

class _PeopleTab extends StatefulWidget {
  const _PeopleTab();

  @override
  State<_PeopleTab> createState() => _PeopleTabState();
}

class _PeopleTabState extends State<_PeopleTab> {
  final _table = PagedTableController();
  String _search = '';
  String? _role;

  Future<void> _edit([Person? person]) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => _PersonDialog(person: person),
    );
    if (saved == true) _table.reload();
  }

  Future<void> _delete(Person p) async {
    final ok = await confirm(context,
        title: 'Delete person',
        message: 'Delete ${p.fullName}?',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deletePerson(p.id), success: 'Person deleted.')) {
      _table.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 14), children: [
      Row(children: [
        SearchBox(
          hint: 'Search people',
          onChanged: (v) {
            _search = v;
            _table.reset();
          },
        ),
        const SizedBox(width: 12),
        FilterDropdown<String>(
          label: 'Role',
          value: _role,
          options: const {'Actor': 'Actor', 'Director': 'Director'},
          onChanged: (v) {
            setState(() => _role = v);
            _table.reset();
          },
        ),
        const Spacer(),
        FilledButton.icon(
          onPressed: _edit,
          icon: const Icon(Icons.add, size: 18),
          label: const Text('Add person'),
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<Person>(
        controller: _table,
        fetch: (page) => repo.people(search: _search, roleType: _role, page: page),
        columns: const [
          DataColumnSpec('Name', flex: 3),
          DataColumnSpec('Role', width: 110),
          DataColumnSpec('Born', width: 120),
          DataColumnSpec('', width: 90),
        ],
        emptyText: 'No people found.',
        onRowTap: _edit,
        rowBuilder: (p) => [
          NameWithThumb(name: p.fullName, url: p.photoUrl, round: true),
          Text(p.roleType),
          Text(formatDate(p.birthDate)),
          Row(mainAxisSize: MainAxisSize.min, children: [
            IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _edit(p)),
            IconButton(
                tooltip: 'Delete',
                icon: const Icon(Icons.delete_outline, size: 18),
                onPressed: () => _delete(p)),
          ]),
        ],
      ),
    ]);
  }
}

class _PersonDialog extends StatefulWidget {
  const _PersonDialog({this.person});

  final Person? person;

  @override
  State<_PersonDialog> createState() => _PersonDialogState();
}

class _PersonDialogState extends State<_PersonDialog> {
  final _form = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.person?.fullName);
  late final _bio = TextEditingController(text: widget.person?.biography);
  late final _photo = TextEditingController(text: widget.person?.photoUrl);
  late String _role = widget.person?.roleType ?? 'Actor';
  late DateTime _birth = widget.person?.birthDate ?? DateTime(1980);
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _bio.dispose();
    _photo.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _birth,
      firstDate: DateTime(1850),
      lastDate: DateTime.now(),
    );
    if (picked != null) setState(() => _birth = picked);
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final repo = context.read<Repository>();
    setState(() => _saving = true);
    final ok = await runAction(
      context,
      () => repo.savePerson(widget.person?.id, {
        'fullName': _name.text.trim(),
        'roleType': _role,
        'birthDate': _birth.toIso8601String(),
        'biography': _bio.text.trim(),
        'photoUrl': _photo.text.trim(),
      }),
      success: 'Person saved.',
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (ok) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: DialogTitle(widget.person == null ? 'Add person' : 'Edit person'),
      content: SizedBox(
        width: 460,
        child: Form(
          key: _form,
          child: SingleChildScrollView(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              TextFormField(
                controller: _name,
                decoration: const InputDecoration(labelText: 'Full name *'),
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _role,
                    decoration: const InputDecoration(labelText: 'Role'),
                    items: const [
                      DropdownMenuItem(value: 'Actor', child: Text('Actor')),
                      DropdownMenuItem(value: 'Director', child: Text('Director')),
                    ],
                    onChanged: (v) => setState(() => _role = v ?? 'Actor'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _pickDate,
                    icon: const Icon(Icons.calendar_today, size: 16),
                    label: Text('Born ${formatDate(_birth)}'),
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              TextFormField(
                controller: _photo,
                decoration: const InputDecoration(labelText: 'Photo URL'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _bio,
                minLines: 3,
                maxLines: 6,
                decoration: const InputDecoration(labelText: 'Biography'),
              ),
            ]),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(onPressed: _saving ? null : _save, child: const Text('Save')),
      ],
    );
  }
}

class _StudiosTab extends StatefulWidget {
  const _StudiosTab();

  @override
  State<_StudiosTab> createState() => _StudiosTabState();
}

class _StudiosTabState extends State<_StudiosTab> {
  final _table = PagedTableController();
  String _search = '';

  Future<void> _edit([Studio? studio]) async {
    final saved = await showDialog<bool>(
      context: context,
      builder: (_) => _StudioDialog(studio: studio),
    );
    if (saved == true) _table.reload();
  }

  Future<void> _delete(Studio s) async {
    final ok = await confirm(context,
        title: 'Delete studio',
        message: 'Delete ${s.name}? Studios linked to titles cannot be deleted.',
        confirmLabel: 'Delete',
        destructive: true);
    if (!ok || !mounted) return;
    final repo = context.read<Repository>();
    if (await runAction(context, () => repo.deleteStudio(s.id), success: 'Studio deleted.')) {
      _table.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(padding: const EdgeInsets.only(top: 14), children: [
      Row(children: [
        SearchBox(
          hint: 'Search studios',
          onChanged: (v) {
            _search = v;
            _table.reset();
          },
        ),
        const Spacer(),
        FilledButton.icon(
          onPressed: _edit,
          icon: const Icon(Icons.add, size: 18),
          label: const Text('Add studio'),
        ),
      ]),
      const SizedBox(height: 16),
      PagedTable<Studio>(
        controller: _table,
        fetch: (page) => repo.studios(search: _search, page: page),
        columns: const [
          DataColumnSpec('Studio', flex: 3),
          DataColumnSpec('Country', flex: 2),
          DataColumnSpec('Founded', width: 100),
          DataColumnSpec('', width: 90),
        ],
        emptyText: 'No studios found.',
        onRowTap: _edit,
        rowBuilder: (s) => [
          NameWithThumb(name: s.name, url: s.logoUrl),
          Text(s.countryName),
          Text('${s.foundedYear}'),
          Row(mainAxisSize: MainAxisSize.min, children: [
            IconButton(
                tooltip: 'Edit',
                icon: const Icon(Icons.edit_outlined, size: 18),
                onPressed: () => _edit(s)),
            IconButton(
                tooltip: 'Delete',
                icon: const Icon(Icons.delete_outline, size: 18),
                onPressed: () => _delete(s)),
          ]),
        ],
      ),
    ]);
  }
}

class _StudioDialog extends StatefulWidget {
  const _StudioDialog({this.studio});

  final Studio? studio;

  @override
  State<_StudioDialog> createState() => _StudioDialogState();
}

class _StudioDialogState extends State<_StudioDialog> {
  final _form = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.studio?.name);
  late final _year = TextEditingController(
      text: widget.studio == null ? '' : '${widget.studio!.foundedYear}');
  late final _desc = TextEditingController(text: widget.studio?.description);
  late final _logo = TextEditingController(text: widget.studio?.logoUrl);
  int? _countryId;
  List<Named> _countries = const [];
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _countryId = widget.studio?.countryId;
    context.read<Repository>().reference('Country').then((c) {
      if (!mounted) return;
      setState(() {
        _countries = c;
        if (!c.any((e) => e.id == _countryId)) _countryId = null;
      });
    }).catchError((Object e) {
      if (mounted) showMessage(context, e.toString(), error: true);
    });
  }

  @override
  void dispose() {
    _name.dispose();
    _year.dispose();
    _desc.dispose();
    _logo.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final repo = context.read<Repository>();
    setState(() => _saving = true);
    final ok = await runAction(
      context,
      () => repo.saveStudio(widget.studio?.id, {
        'name': _name.text.trim(),
        'countryId': _countryId,
        'foundedYear': int.parse(_year.text),
        'description': _desc.text.trim(),
        'logoUrl': _logo.text.trim(),
      }),
      success: 'Studio saved.',
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (ok) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: DialogTitle(widget.studio == null ? 'Add studio' : 'Edit studio'),
      content: SizedBox(
        width: 460,
        child: Form(
          key: _form,
          child: SingleChildScrollView(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              TextFormField(
                controller: _name,
                decoration: const InputDecoration(labelText: 'Name *'),
                validator: (v) => (v == null || v.trim().isEmpty) ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: DropdownButtonFormField<int>(
                    initialValue: _countryId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Country *'),
                    items: [
                      for (final c in _countries)
                        DropdownMenuItem(value: c.id, child: Text(c.name)),
                    ],
                    validator: (v) => v == null ? 'Required' : null,
                    onChanged: (v) => setState(() => _countryId = v),
                  ),
                ),
                const SizedBox(width: 12),
                SizedBox(
                  width: 130,
                  child: TextFormField(
                    controller: _year,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Founded *'),
                    validator: (v) {
                      final n = int.tryParse(v ?? '');
                      return (n == null || n < 1800 || n > DateTime.now().year) ? 'Invalid' : null;
                    },
                  ),
                ),
              ]),
              const SizedBox(height: 12),
              TextFormField(
                controller: _logo,
                decoration: const InputDecoration(labelText: 'Logo URL'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _desc,
                minLines: 3,
                maxLines: 5,
                decoration: const InputDecoration(labelText: 'Description'),
              ),
            ]),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(onPressed: _saving ? null : _save, child: const Text('Save')),
      ],
    );
  }
}
