import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/validators.dart';
import '../../core/widgets.dart';
import 'login_screen.dart';

class ResetPasswordScreen extends StatefulWidget {
  const ResetPasswordScreen({super.key, this.initialToken});

  final String? initialToken;

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final _form = GlobalKey<FormState>();
  late final _code = TextEditingController(text: widget.initialToken);
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    _code.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!validateForm(context, _form)) return;
    final auth = context.read<AuthState>();
    setState(() => _busy = true);
    final ok = await runAction(
      context,
      () => auth.confirmPasswordReset(_code.text, _password.text),
      success: 'Password changed. You can sign in with the new password.',
    );
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) context.go('/login');
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Choose a new password',
      showBack: true,
      children: [
        const Text(
          'The token from your e-mail link is filled in automatically. If you opened this screen yourself, paste the token here.',
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 16),
        Form(
          key: _form,
          child: Column(children: [
            TextFormField(
              controller: _code,
              autocorrect: false,
              enableSuggestions: false,
              minLines: 1,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'Reset token'),
              validator: (v) => RegExp(r'^\d{1,9}\.[0-9a-fA-F]{64}$').hasMatch(v?.trim() ?? '')
                  ? null
                  : 'Paste the whole token from the e-mail (a number, a dot and 64 characters).',
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _password,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'New password'),
              validator: (v) => Validators.password(v, field: 'The new password'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _confirm,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Confirm new password'),
              validator: (v) => Validators.confirm(v, _password.text),
              onFieldSubmitted: (_) => _submit(),
            ),
          ]),
        ),
        const SizedBox(height: 20),
        FilledButton(
          onPressed: _busy ? null : _submit,
          child: const Text('Change password'),
        ),
      ],
    );
  }
}
