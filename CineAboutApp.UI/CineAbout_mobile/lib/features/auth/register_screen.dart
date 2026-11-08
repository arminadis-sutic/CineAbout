import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/validators.dart';
import '../../core/widgets.dart';
import 'login_screen.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _form = GlobalKey<FormState>();
  final _first = TextEditingController();
  final _last = TextEditingController();
  final _username = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  List<Named> _countries = const [];
  int? _countryId;
  bool _busy = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    context.read<Repository>().countries().then((c) {
      if (mounted) setState(() => _countries = c);
    }).catchError((Object e) {
      if (mounted) setState(() => _error = e.toString());
    });
  }

  @override
  void dispose() {
    for (final c in [_first, _last, _username, _email, _password, _confirm]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!validateForm(context, _form)) return;
    final auth = context.read<AuthState>();
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final signedIn = await auth.register(
        username: _username.text.trim(),
        email: _email.text.trim(),
        password: _password.text,
        firstName: _first.text.trim(),
        lastName: _last.text.trim(),
        countryId: _countryId!,
      );
      if (!signedIn && mounted) {
        showMessage(context, 'Account created. An administrator has to activate it before you can sign in.');
        context.go('/login');
      }
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthScaffold(
      title: 'Create your account',
      showBack: true,
      children: [
        Form(
          key: _form,
          child: Column(children: [
            Row(children: [
              Expanded(
                child: TextFormField(
                  controller: _first,
                  textCapitalization: TextCapitalization.words,
                  decoration: const InputDecoration(labelText: 'First name'),
                  validator: (v) => Validators.required(v, 'First name', max: 75),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: TextFormField(
                  controller: _last,
                  textCapitalization: TextCapitalization.words,
                  decoration: const InputDecoration(labelText: 'Last name'),
                  validator: (v) => Validators.required(v, 'Last name', max: 75),
                ),
              ),
            ]),
            const SizedBox(height: 12),
            TextFormField(
              controller: _username,
              autocorrect: false,
              decoration: const InputDecoration(labelText: 'Username'),
              validator: Validators.username,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              autocorrect: false,
              decoration: const InputDecoration(labelText: 'E-mail'),
              validator: Validators.email,
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<int>(
              initialValue: _countryId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Country'),
              items: [for (final c in _countries) DropdownMenuItem(value: c.id, child: Text(c.name))],
              validator: (v) => v == null ? 'Select your country' : null,
              onChanged: (v) => setState(() => _countryId = v),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _password,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Password'),
              validator: Validators.password,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _confirm,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Confirm password'),
              validator: (v) => Validators.confirm(v, _password.text),
            ),
          ]),
        ),
        if (_error != null) ...[
          const SizedBox(height: 12),
          Text(_error!, style: const TextStyle(color: Color(0xFFFF7A7A))),
        ],
        const SizedBox(height: 20),
        FilledButton(
          onPressed: _busy ? null : _submit,
          child: _busy
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : const Text('Sign up'),
        ),
      ],
    );
  }
}
