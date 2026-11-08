import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/theme.dart';

class _NavItem {
  const _NavItem(this.label, this.icon, this.path);

  final String label;
  final IconData icon;
  final String path;
}

const _items = [
  _NavItem('Dashboard', Icons.dashboard_outlined, '/dashboard'),
  _NavItem('Users', Icons.people_outline, '/users'),
  _NavItem('Titles', Icons.movie_outlined, '/titles'),
  _NavItem('People & Studios', Icons.theater_comedy_outlined, '/people'),
  _NavItem('Moderation', Icons.forum_outlined, '/moderation'),
  _NavItem('Quiz', Icons.quiz_outlined, '/quiz'),
  _NavItem('Reports', Icons.picture_as_pdf_outlined, '/reports'),
  _NavItem('Reference data', Icons.category_outlined, '/reference'),
  _NavItem('Audit log', Icons.history, '/audit'),
  _NavItem('Settings', Icons.settings_outlined, '/settings'),
];

class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.location, required this.child});

  final String location;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthState>();
    return Scaffold(
      body: Row(
        children: [
          Container(
            width: 248,
            decoration: const BoxDecoration(
              color: AppColors.surface,
              border: Border(right: BorderSide(color: AppColors.border)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Padding(
                  padding: EdgeInsets.fromLTRB(24, 28, 24, 24),
                  child: Text.rich(TextSpan(children: [
                    TextSpan(text: 'Cine', style: TextStyle(color: AppColors.red)),
                    TextSpan(text: 'About'),
                  ]), style: TextStyle(fontSize: 24, fontWeight: FontWeight.w900)),
                ),
                Expanded(
                  child: ListView(children: [
                    for (final item in _items)
                      _NavTile(
                        item: item,
                        selected: location.startsWith(item.path),
                        onTap: () => context.go(item.path),
                      ),
                  ]),
                ),
                const Divider(height: 1),
                ListTile(
                  leading: const CircleAvatar(
                    radius: 16,
                    backgroundColor: AppColors.redDark,
                    child: Icon(Icons.shield_outlined, size: 16, color: AppColors.red),
                  ),
                  title: Text(auth.username.isEmpty ? 'Admin' : auth.username,
                      overflow: TextOverflow.ellipsis),
                  subtitle: const Text('Administrator'),
                  trailing: IconButton(
                    tooltip: 'Sign out',
                    icon: const Icon(Icons.logout, size: 20),
                    onPressed: auth.logout,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(32, 28, 32, 24),
              child: child,
            ),
          ),
        ],
      ),
    );
  }
}

class _NavTile extends StatelessWidget {
  const _NavTile({required this.item, required this.selected, required this.onTap});

  final _NavItem item;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 2),
      child: Material(
        color: selected ? AppColors.redDark : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
            child: Row(children: [
              Icon(item.icon, size: 20, color: selected ? AppColors.red : AppColors.muted),
              const SizedBox(width: 12),
              Text(item.label,
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    color: selected ? Colors.white : AppColors.muted,
                  )),
            ]),
          ),
        ),
      ),
    );
  }
}
