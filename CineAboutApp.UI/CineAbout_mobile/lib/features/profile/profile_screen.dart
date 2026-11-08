import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';
import '../home/notification_bell.dart';
import 'profile_view.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthState>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Profile'),
        actions: [
          const NotificationBell(),
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout),
            onPressed: () async {
              final yes = await confirm(context,
                  title: 'Sign out', message: 'Sign out of CineAbout?', confirmLabel: 'Sign out');
              if (yes && context.mounted) await auth.logout();
            },
          ),
        ],
      ),
      body: ProfileView(
        userId: auth.userId,
        isMe: true,
        actions: (profile, reload) => Padding(
          padding: const EdgeInsets.only(top: 4),
          child: Row(children: [
            Expanded(
              child: OutlinedButton(
                style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(44)),
                onPressed: () async {
                  await context.push('/profile/edit');
                  reload();
                },
                child: const Text('EDIT PROFILE'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(44)),
                onPressed: () => context.push('/leaderboard'),
                icon: const Icon(Icons.leaderboard, size: 18, color: AppColors.gold),
                label: const Text('RANKING'),
              ),
            ),
          ]),
        ),
      ),
    );
  }
}
