import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class LeaderboardScreen extends StatefulWidget {
  const LeaderboardScreen({super.key});

  @override
  State<LeaderboardScreen> createState() => _LeaderboardScreenState();
}

class _LeaderboardScreenState extends State<LeaderboardScreen> {
  String _period = 'AllTime';

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    final me = context.read<AuthState>().userId;
    return Scaffold(
      appBar: AppBar(title: const Text('Leaderboard')),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: SegmentedButton<String>(
            showSelectedIcon: false,
            segments: const [
              ButtonSegment(value: 'AllTime', label: Text('Global')),
              ButtonSegment(value: 'Weekly', label: Text('This week')),
              ButtonSegment(value: 'Daily', label: Text('Today')),
            ],
            selected: {_period},
            onSelectionChanged: (s) => setState(() => _period = s.first),
          ),
        ),
        Expanded(
          child: AsyncData<Leaderboard>(
            key: ValueKey(_period),
            load: () => repo.leaderboard(_period),
            builder: (context, board, reload) {
              if (board.entries.isEmpty) {
                return const EmptyView('Nobody has scored yet.', icon: Icons.leaderboard);
              }
              final meInList = board.entries.any((e) => e.userId == me);
              return RefreshIndicator(
                onRefresh: reload,
                child: ListView(padding: const EdgeInsets.fromLTRB(16, 0, 16, 24), children: [
                  for (final e in board.entries) _EntryTile(entry: e, isMe: e.userId == me),
                  if (!meInList && board.me != null) ...[
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 6),
                      child: Center(child: Text('· · ·', style: TextStyle(color: AppColors.muted))),
                    ),
                    _EntryTile(entry: board.me!, isMe: true),
                  ],
                ]),
              );
            },
          ),
        ),
      ]),
    );
  }
}

class _EntryTile extends StatelessWidget {
  const _EntryTile({required this.entry, required this.isMe});

  final LeaderboardEntry entry;
  final bool isMe;

  @override
  Widget build(BuildContext context) {
    final e = entry;
    final medal = switch (e.rank) {
      1 => AppColors.gold,
      2 => const Color(0xFFC0C0C0),
      3 => const Color(0xFFCD7F32),
      _ => null,
    };
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Card(
        color: isMe ? AppColors.redDark : null,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(color: medal ?? (isMe ? AppColors.red : AppColors.border)),
        ),
        child: ListTile(
          onTap: () => context.push('/user/${e.userId}'),
          leading: SizedBox(
            width: 84,
            child: Row(children: [
              SizedBox(
                width: 32,
                child: Text('#${e.rank}',
                    style: TextStyle(fontWeight: FontWeight.w900, color: medal ?? AppColors.muted)),
              ),
              Avatar(username: e.username, url: e.imageUrl, radius: 18),
            ]),
          ),
          title: Text(isMe ? '${e.username} (you)' : e.username,
              style: const TextStyle(fontWeight: FontWeight.w800)),
          subtitle: Text('${e.streak} day streak · ${e.sessions} games',
              style: const TextStyle(color: AppColors.muted, fontSize: 12)),
          trailing: Text('${e.score} XP',
              style: TextStyle(fontWeight: FontWeight.w900, color: medal ?? AppColors.text)),
        ),
      ),
    );
  }
}
