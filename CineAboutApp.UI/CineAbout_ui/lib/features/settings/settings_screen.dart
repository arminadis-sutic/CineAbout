import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/widgets.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _saving = false;

  Future<void> _toggle(bool value, Future<void> Function() reload) async {
    final repo = context.read<Repository>();
    setState(() => _saving = true);
    final ok = await runAction(
      context,
      () => repo.setAutoActivate(value),
      success: value
          ? 'New accounts are now activated automatically.'
          : 'New accounts now wait for an administrator to activate them.',
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (ok) await reload();
  }

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return ListView(children: [
      const PageHeader(title: 'Settings', subtitle: 'Platform-wide behaviour.'),
      AsyncData<bool>(
        load: repo.autoActivateNewUsers,
        builder: (context, enabled, reload) => Panel(
          title: 'Registration',
          child: SwitchListTile(
            contentPadding: EdgeInsets.zero,
            activeThumbColor: AppColors.red,
            title: const Text('Activate new accounts automatically'),
            subtitle: const Text(
              'When off, people who register can sign in only after an administrator activates their account in Users.',
              style: TextStyle(color: AppColors.muted),
            ),
            value: enabled,
            onChanged: _saving ? null : (v) => _toggle(v, reload),
          ),
        ),
      ),
    ]);
  }
}
