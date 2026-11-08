import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/models.dart';
import '../../core/paged_table.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final _table = PagedTableController();
  String _search = '';
  int? _roleId;
  bool? _active;
  List<RoleInfo> _roles = const [];

  @override
  void initState() {
    super.initState();
    context.read<Repository>().roles().then((r) {
      if (mounted) setState(() => _roles = r);
    }).catchError((_) {});
  }

  void _filterChanged() => _table.reset();

  Future<void> _open(AppUser user) async {
    final changed = await showDialog<bool>(
      context: context,
      builder: (_) => _UserDetailsDialog(user: user, roles: _roles),
    );
    if (changed == true) _table.reload();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(
      children: [
        const PageHeader(
          title: 'Users',
          subtitle: 'Browse accounts, change roles and moderate users.',
        ),
        Wrap(spacing: 12, runSpacing: 12, children: [
          SearchBox(
            hint: 'Search by username or e-mail',
            onChanged: (v) {
              _search = v;
              _filterChanged();
            },
          ),
          FilterDropdown<int>(
            label: 'Role',
            value: _roleId,
            options: {for (final r in _roles) r.id: r.name},
            onChanged: (v) {
              setState(() => _roleId = v);
              _filterChanged();
            },
          ),
          FilterDropdown<bool>(
            label: 'Status',
            value: _active,
            options: const {true: 'Active', false: 'Deactivated'},
            onChanged: (v) {
              setState(() => _active = v);
              _filterChanged();
            },
          ),
        ]),
        const SizedBox(height: 16),
        PagedTable<AppUser>(
          controller: _table,
          fetch: (page) => repo.users(
            search: _search,
            roleId: _roleId,
            isActive: _active,
            page: page,
          ),
          columns: const [
            DataColumnSpec('User', flex: 3),
            DataColumnSpec('E-mail', flex: 3),
            DataColumnSpec('Role', width: 110),
            DataColumnSpec('Registered', width: 120),
            DataColumnSpec('Status', width: 120),
          ],
          emptyText: 'No users match the filters.',
          onRowTap: _open,
          rowBuilder: (u) => [
            NameWithThumb(name: u.fullName, url: u.imageUrl, round: true, bold: false, extra: '(@${u.username})'),
            Text(u.email, overflow: TextOverflow.ellipsis),
            StatusChip(u.isAdmin ? 'Admin' : 'User',
                color: u.isAdmin ? AppColors.gold : AppColors.muted),
            Text(formatDate(u.registrationDate)),
            StatusChip(u.isActive ? 'Active' : 'Inactive'),
          ],
        ),
      ],
    );
  }
}

class _UserDetailsDialog extends StatelessWidget {
  const _UserDetailsDialog({required this.user, required this.roles});

  final AppUser user;
  final List<RoleInfo> roles;

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();

    Future<void> done(Future<void> Function() action, String message) async {
      if (await runAction(context, action, success: message) && context.mounted) {
        Navigator.pop(context, true);
      }
    }

    Widget fact(String label, String value) => Expanded(
          child: Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: AppColors.surfaceHigh,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(label, style: const TextStyle(color: AppColors.muted, fontSize: 12)),
              const SizedBox(height: 4),
              Text(value, style: const TextStyle(fontWeight: FontWeight.w700)),
            ]),
          ),
        );

    Widget stat(String label, int value) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 6),
          child: Row(children: [
            Expanded(child: Text(label)),
            Text('$value',
                style: const TextStyle(
                    color: AppColors.red, fontWeight: FontWeight.w800, fontSize: 18)),
          ]),
        );

    final adminRole = roles.where((r) => r.name == 'Admin').firstOrNull;
    final userRole = roles.where((r) => r.name == 'User').firstOrNull;

    return Dialog(
      alignment: Alignment.centerRight,
      insetPadding: const EdgeInsets.all(16),
      child: SizedBox(
        width: 420,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(children: [
                const Expanded(
                  child: Text('User details',
                      style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
                ),
                IconButton(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close),
                ),
              ]),
              const SizedBox(height: 12),
              Center(
                child: CircleAvatar(
                  radius: 34,
                  backgroundColor: AppColors.surfaceHigh,
                  child: Text(user.username.isEmpty ? '?' : user.username[0].toUpperCase(),
                      style: const TextStyle(fontSize: 26)),
                ),
              ),
              const SizedBox(height: 12),
              Text(user.fullName,
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
              Text(user.email,
                  textAlign: TextAlign.center, style: const TextStyle(color: AppColors.muted)),
              const SizedBox(height: 8),
              Center(child: StatusChip(user.isActive ? 'Active' : 'Inactive')),
              const SizedBox(height: 20),
              Row(children: [
                fact('Role', user.isAdmin ? 'Admin' : 'User'),
                const SizedBox(width: 10),
                fact('Registered', formatDate(user.registrationDate)),
              ]),
              const SizedBox(height: 10),
              const SizedBox(height: 20),
              const Text('COMMUNITY ACTIVITY',
                  style: TextStyle(fontSize: 11, letterSpacing: 1, color: AppColors.muted)),
              const SizedBox(height: 6),
              FutureBuilder<UserActivity>(
                future: repo.userActivity(user.id),
                builder: (context, snap) {
                  final a = snap.data;
                  if (snap.hasError) {
                    return const Padding(
                      padding: EdgeInsets.symmetric(vertical: 8),
                      child: Text('Activity is unavailable.',
                          style: TextStyle(color: AppColors.muted)),
                    );
                  }
                  if (a == null) {
                    return const Padding(
                      padding: EdgeInsets.symmetric(vertical: 16),
                      child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
                    );
                  }
                  return Column(children: [
                    stat('Ratings (rated titles)', a.rated),
                    stat('Reviews (with text)', a.reviews),
                    stat('Threads', a.threads),
                    stat('Comments', a.comments),
                    stat('Quiz streak (days)', a.streak),
                  ]);
                },
              ),
              const SizedBox(height: 20),
              if (adminRole != null && userRole != null)
                OutlinedButton.icon(
                  icon: Icon(user.isAdmin ? Icons.arrow_downward : Icons.arrow_upward, size: 18),
                  label: Text(user.isAdmin ? 'Demote to User' : 'Promote to Admin'),
                  onPressed: () async {
                    final ok = await confirm(
                      context,
                      title: user.isAdmin ? 'Demote to User' : 'Promote to Admin',
                      message:
                          '${user.username} will ${user.isAdmin ? 'lose' : 'gain'} administrator access.',
                    );
                    if (!ok) return;
                    await done(
                      () => repo.setUserRoles(
                          user, user.isAdmin ? [userRole.id] : [userRole.id, adminRole.id]),
                      'Role updated.',
                    );
                  },
                ),
              const SizedBox(height: 10),
              OutlinedButton.icon(
                icon: Icon(user.isActive ? Icons.block : Icons.check_circle_outline, size: 18),
                label: Text(user.isActive ? 'Deactivate account' : 'Activate account'),
                onPressed: () => done(
                  () => repo.toggleUserActive(user.id),
                  user.isActive ? 'Account deactivated.' : 'Account activated.',
                ),
              ),
              const SizedBox(height: 10),
              FilledButton.icon(
                style: FilledButton.styleFrom(backgroundColor: AppColors.redDark),
                icon: const Icon(Icons.delete_outline, size: 18, color: AppColors.red),
                label: const Text('Delete account', style: TextStyle(color: AppColors.red)),
                onPressed: () async {
                  final ok = await confirm(
                    context,
                    title: 'Delete account',
                    message: 'Delete ${user.username}? This removes the account from the platform.',
                    confirmLabel: 'Delete',
                    destructive: true,
                  );
                  if (ok) await done(() => repo.deleteUser(user.id), 'Account deleted.');
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
