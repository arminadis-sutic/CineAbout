import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/widgets.dart';
import 'profile_view.dart';

class UserProfileScreen extends StatelessWidget {
  const UserProfileScreen({super.key, required this.userId});

  final int userId;

  @override
  Widget build(BuildContext context) {
    final me = context.read<AuthState>().userId;
    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: ProfileView(
        userId: userId,
        isMe: userId == me,
        actions: userId == me ? null : (p, reload) => _FollowButton(profile: p, onChanged: reload),
      ),
    );
  }
}

class _FollowButton extends StatefulWidget {
  const _FollowButton({required this.profile, required this.onChanged});

  final PublicProfile profile;
  final VoidCallback onChanged;

  @override
  State<_FollowButton> createState() => _FollowButtonState();
}

class _FollowButtonState extends State<_FollowButton> {
  bool _busy = false;

  Future<void> _toggle() async {
    final repo = context.read<Repository>();
    final p = widget.profile;
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => p.isFollowed ? repo.unfollow(p.id) : repo.follow(p.id),
      success: p.isFollowed ? 'You unfollowed ${p.username}.' : 'You are now following ${p.username}.',
    );
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) widget.onChanged();
  }

  @override
  Widget build(BuildContext context) {
    final followed = widget.profile.isFollowed;
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: followed
          ? OutlinedButton.icon(
              onPressed: _busy ? null : _toggle,
              icon: const Icon(Icons.check, size: 18),
              label: const Text('FOLLOWING'),
            )
          : FilledButton.icon(
              onPressed: _busy ? null : _toggle,
              icon: const Icon(Icons.person_add_alt, size: 18),
              label: const Text('FOLLOW'),
            ),
    );
  }
}
