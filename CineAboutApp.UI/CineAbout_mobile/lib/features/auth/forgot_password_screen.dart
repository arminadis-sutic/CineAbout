import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'package:go_router/go_router.dart';

import '../../core/auth_state.dart';
import '../../core/validators.dart';
import '../../core/widgets.dart';
import 'login_screen.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  bool _busy = false;
  bool _sent = false;

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!validateForm(context, _form, message: 'Enter a valid e-mail address.')) return;
    final auth = context.read<AuthState>();
    setState(() => _busy = true);
    final ok = await runAction(context, () => auth.requestPasswordReset(_email.text.trim()));
    if (!mounted) return;
    setState(() {
      _busy = false;
      _sent = ok;
    });
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Reset your password',
      showBack: true,
      children: [
        if (_sent) ...[
          const Text(
            'If an account exists for that e-mail, we sent a link to choose a new password. It works once and is valid for 15 minutes.',
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: () => context.push('/reset'),
            child: const Text('I have a token'),
          ),
        ] else ...[
          const Text('Enter the e-mail address of your account and we will send you a link to choose a new password.',
              textAlign: TextAlign.center),
          const SizedBox(height: 16),
          Form(
            key: _form,
            child: TextFormField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              autocorrect: false,
              decoration: const InputDecoration(labelText: 'E-mail'),
              validator: Validators.email,
              onFieldSubmitted: (_) => _submit(),
            ),
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _busy ? null : _submit,
            child: const Text('Send reset link'),
          ),
          TextButton(
            onPressed: () => context.push('/reset'),
            child: const Text('I already have a token'),
          ),
        ],
      ],
    );
  }
}
