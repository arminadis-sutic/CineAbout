import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../movie/movie_cards.dart';

Future<String?> askListName(BuildContext context, {String title = 'New list', String initial = ''}) {
  return showDialog<String>(context: context, builder: (_) => _NameDialog(title: title, initial: initial));
}

class _NameDialog extends StatefulWidget {
  const _NameDialog({required this.title, required this.initial});

  final String title, initial;

  @override
  State<_NameDialog> createState() => _NameDialogState();
}

class _NameDialogState extends State<_NameDialog> {
  late final _controller = TextEditingController(text: widget.initial);
  final _form = GlobalKey<FormState>();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: Form(
        key: _form,
        child: TextFormField(
          controller: _controller,
          autofocus: true,
          maxLength: 100,
          decoration: const InputDecoration(labelText: 'List name'),
          validator: (v) => (v == null || v.trim().isEmpty) ? 'Enter a name' : null,
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        TextButton(
          onPressed: () {
            if (_form.currentState!.validate()) Navigator.pop(context, _controller.text.trim());
          },
          child: const Text('Save'),
        ),
      ],
    );
  }
}

class UserListsTab extends StatefulWidget {
  const UserListsTab({super.key, required this.userId, required this.isMe});

  final int userId;
  final bool isMe;

  @override
  State<UserListsTab> createState() => _UserListsTabState();
}

class _UserListsTabState extends State<UserListsTab> {
  int _version = 0;
  late final ValueNotifier<int> _listsChanged;

  @override
  void initState() {
    super.initState();
    _listsChanged = context.read<Repository>().listsChanged..addListener(_onListsChanged);
  }

  @override
  void dispose() {
    _listsChanged.removeListener(_onListsChanged);
    super.dispose();
  }

  void _onListsChanged() {
    if (mounted) setState(() => _version++);
  }

  Future<void> _create() async {
    final name = await askListName(context);
    if (name == null || !mounted) return;
    final repo = context.read<Repository>();
    await runAction(context, () => repo.createList(name), success: 'List created.');
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Column(children: [
      if (widget.isMe)
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
          child: Align(
            alignment: Alignment.centerRight,
            child: TextButton.icon(onPressed: _create, icon: const Icon(Icons.add, size: 18), label: const Text('NEW LIST')),
          ),
        ),
      AsyncData<List<UserListSummary>>(
        key: ValueKey('lists-${widget.userId}-$_version'),
        load: () => repo.lists(widget.userId),
        builder: (context, lists, _) => lists.isEmpty
            ? Padding(
                padding: const EdgeInsets.all(32),
                child: Center(
                  child: Text(widget.isMe ? 'You have no lists yet.' : 'No lists yet.', style: const TextStyle(color: AppColors.muted)),
                ),
              )
            : Column(children: [
                for (final l in lists)
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                    child: Card(
                      child: ListTile(
                        leading: Poster(url: l.posterUrls.isEmpty ? null : l.posterUrls.first, title: l.name, width: 44, height: 64),
                        title: Text(l.name, style: const TextStyle(fontWeight: FontWeight.w800)),
                        subtitle: Text('${l.itemCount} titles', style: const TextStyle(color: AppColors.muted)),
                        trailing: const Icon(Icons.chevron_right),
                        onTap: () => context.push('/list/${l.id}'),
                      ),
                    ),
                  ),
              ]),
      ),
    ]);
  }
}

class ListDetailScreen extends StatefulWidget {
  const ListDetailScreen({super.key, required this.listId});

  final int listId;

  @override
  State<ListDetailScreen> createState() => _ListDetailScreenState();
}

class _ListDetailScreenState extends State<ListDetailScreen> {
  int _version = 0;
  UserListDetail? _list;

  List<Widget> _actions(BuildContext context, Repository repo) {
    final list = _list;
    final me = context.read<AuthState>().userId;
    if (list == null || list.userId != me) return const [];
    return [
      IconButton(
        tooltip: 'Rename',
        icon: const Icon(Icons.edit_outlined),
        onPressed: () async {
          final name = await askListName(context, title: 'Rename list', initial: list.name);
          if (name == null || !context.mounted) return;
          if (await runAction(context, () => repo.renameList(list.id, name), success: 'List renamed.')) {
            setState(() => _version++);
          }
        },
      ),
      IconButton(
        tooltip: 'Delete list',
        icon: const Icon(Icons.delete_outline),
        onPressed: () async {
          final ok = await confirm(context, title: 'Delete list', message: 'Delete "${list.name}" with its ${list.itemCount} titles?', confirmLabel: 'Delete');
          if (!ok || !context.mounted) return;
          if (await runAction(context, () => repo.deleteList(list.id), success: 'List deleted.') && context.mounted) {
            context.pop();
          }
        },
      ),
    ];
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final me = context.read<AuthState>().userId;
    return Scaffold(
      appBar: AppBar(title: Text(_list?.name ?? 'List'), actions: _actions(context, repo)),
      body: AsyncData<UserListDetail>(
        key: ValueKey(_version),
        load: () async {
          final l = await repo.list(widget.listId);
          if (mounted) setState(() => _list = l);
          return l;
        },
        builder: (context, list, reload) {
          final mine = list.userId == me;
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(padding: const EdgeInsets.all(16), children: [
              GestureDetector(
                onTap: () => context.push('/user/${list.userId}'),
                child: Text('by @${list.username}  ·  ${list.itemCount} titles', style: const TextStyle(color: AppColors.muted)),
              ),
              const SizedBox(height: 12),
              if (list.items.isEmpty)
                const Padding(padding: EdgeInsets.all(24), child: Center(child: Text('No titles in this list yet.', style: TextStyle(color: AppColors.muted)))),
              for (final i in list.items)
                MovieTile(
                  movie: i.movie,
                  trailing: mine
                      ? IconButton(
                          tooltip: 'Remove from list',
                          icon: const Icon(Icons.close),
                          onPressed: () async {
                            if (await runAction(context, () => repo.removeFromList(list.id, i.movie.id))) {
                              setState(() => _version++);
                            }
                          },
                        )
                      : null,
                ),
            ]),
          );
        },
      ),
    );
  }
}

Future<void> showAddToListSheet(BuildContext context, int movieId) {
  final repo = context.read<Repository>();
  return showModalBottomSheet<void>(
    context: context,
    backgroundColor: AppColors.surface,
    isScrollControlled: true,
    builder: (ctx) => SizedBox(
      height: MediaQuery.sizeOf(ctx).height * 0.55,
      child: _AddToListSheet(movieId: movieId, repo: repo),
    ),
  );
}

class _AddToListSheet extends StatefulWidget {
  const _AddToListSheet({required this.movieId, required this.repo});

  final int movieId;
  final Repository repo;

  @override
  State<_AddToListSheet> createState() => _AddToListSheetState();
}

class _AddToListSheetState extends State<_AddToListSheet> {
  Future<void> _addTo(UserListSummary l) async {
    if (await runAction(context, () => widget.repo.addToList(l.id, widget.movieId), success: 'Added to "${l.name}".') && mounted) {
      Navigator.pop(context);
    }
  }

  Future<void> _createAndAdd() async {
    final name = await askListName(context);
    if (name == null || !mounted) return;
    final ok = await runAction(context, () async {
      final created = await widget.repo.createList(name);
      await widget.repo.addToList(created.id, widget.movieId);
    }, success: 'Added to "$name".');
    if (ok && mounted) Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return Column(children: [
      const Padding(
        padding: EdgeInsets.all(16),
        child: Text('Add to list', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
      ),
      ListTile(
        leading: const Icon(Icons.add_circle_outline),
        title: const Text('New list'),
        onTap: _createAndAdd,
      ),
      Expanded(
        child: AsyncData<List<UserListSummary>>(
          load: () => widget.repo.lists(null),
          builder: (context, lists, _) => lists.isEmpty
              ? const Center(child: Text('You have no lists yet.', style: TextStyle(color: AppColors.muted)))
              : ListView(children: [
                  for (final l in lists)
                    ListTile(
                      leading: const Icon(Icons.list_alt),
                      title: Text(l.name),
                      subtitle: Text('${l.itemCount} titles', style: const TextStyle(color: AppColors.muted)),
                      onTap: () => _addTo(l),
                    ),
                ]),
        ),
      ),
    ]);
  }
}
