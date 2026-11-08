import 'dart:io';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../core/auth_state.dart';
import '../../core/models.dart';
import '../../core/repository.dart';
import '../../core/theme.dart';
import '../../core/validators.dart';
import '../../core/widgets.dart';

class _EditData {
  _EditData(this.profile, this.account, this.genres, this.selected);

  final MyProfile profile;
  final MyAccount account;
  final List<Named> genres;
  final Set<int> selected;
}

class EditProfileScreen extends StatelessWidget {
  const EditProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final repo = context.read<Repository>();
    return Scaffold(
      appBar: AppBar(title: const Text('Edit profile')),
      body: AsyncData<_EditData>(
        load: () async {
          final r = await Future.wait([
            repo.myProfile(),
            repo.myAccount(),
            repo.genres(),
            repo.myGenreIds(),
          ]);
          return _EditData(r[0] as MyProfile, r[1] as MyAccount, r[2] as List<Named>,
              (r[3] as List<int>).toSet());
        },
        builder: (context, data, _) => _EditForm(data: data),
      ),
    );
  }
}

class _EditForm extends StatefulWidget {
  const _EditForm({required this.data});

  final _EditData data;

  @override
  State<_EditForm> createState() => _EditFormState();
}

class _EditFormState extends State<_EditForm> {
  final _form = GlobalKey<FormState>();
  late final _first = TextEditingController(text: widget.data.account.firstName);
  late final _last = TextEditingController(text: widget.data.account.lastName);
  late final _email = TextEditingController(text: widget.data.account.email);
  late final _phone = TextEditingController(text: widget.data.profile.phone);
  late int? _cityId = widget.data.profile.cityId;
  List<Named> _cities = const [];
  late final _bio = TextEditingController(text: widget.data.profile.bio);
  final _currentPassword = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirmPassword = TextEditingController();
  late bool _push = widget.data.profile.pushEnabled;
  late bool _emailNotifications = widget.data.profile.emailEnabled;
  late bool _public = widget.data.profile.isPublic;
  late DateTime? _birth = widget.data.profile.dateOfBirth;
  late final Set<int> _genres = {...widget.data.selected};
  String? _imagePath;
  bool _removeImage = false;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    context.read<Repository>().cities().then((items) {
      if (!mounted) return;
      setState(() {
        _cities = items;
        if (!items.any((c) => c.id == _cityId)) _cityId = null;
      });
    });
  }

  @override
  void dispose() {
    for (final c in [_first, _last, _email, _phone, _bio, _currentPassword, _newPassword, _confirmPassword]) {
      c.dispose();
    }
    super.dispose();
  }

  bool get _changingPassword =>
      _currentPassword.text.isNotEmpty || _newPassword.text.isNotEmpty || _confirmPassword.text.isNotEmpty;

  Future<void> _pickImage() async {
    final picked = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 1024, imageQuality: 85);
    if (picked != null) {
      setState(() {
        _imagePath = picked.path;
        _removeImage = false;
      });
    }
  }

  Future<void> _pickBirthDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _birth ?? DateTime(now.year - 25),
      firstDate: DateTime(1900),
      lastDate: now,
    );
    if (picked != null) setState(() => _birth = picked);
  }

  Future<void> _save() async {
    if (!validateForm(context, _form)) return;
    final repo = context.read<Repository>();
    final auth = context.read<AuthState>();
    final changedPassword = _changingPassword;
    setState(() => _busy = true);
    final ok = await runAction(context, () async {
      final account = MyAccount.edited(widget.data.account,
          firstName: _first.text.trim(), lastName: _last.text.trim(), email: _email.text.trim());
      await repo.updateMyAccount(
        account,
        currentPassword: _changingPassword ? _currentPassword.text : null,
        newPassword: _changingPassword ? _newPassword.text : null,
      );
      await repo.updateMyProfile(
        bio: _bio.text.trim(),
        cityId: _cityId ?? 0,
        phone: _phone.text.trim(),
        dateOfBirth: _birth,
        push: _push,
        email: _emailNotifications,
        isPublic: _public,
        imagePath: _imagePath,
        removeImage: _removeImage,
      );
      await repo.setMyGenres(_genres.toList());
    }, success: 'Profile saved.');
    if (!mounted) return;
    setState(() => _busy = false);
    if (!ok) return;
    if (changedPassword) {
      showMessage(context, 'Password changed. Please sign in with your new password.');
      await auth.logout();
      return;
    }
    context.pop();
  }

  @override
  Widget build(BuildContext context) {
    final current = widget.data.profile;
    final picture = _imagePath != null
        ? CircleAvatar(radius: 48, backgroundImage: FileImage(File(_imagePath!)))
        : Avatar(username: current.username, url: _removeImage ? null : current.imageUrl, radius: 48);
    return Form(
      key: _form,
      child: ListView(padding: const EdgeInsets.all(16), children: [
        Center(child: picture),
        const SizedBox(height: 8),
        Row(mainAxisAlignment: MainAxisAlignment.center, children: [
          TextButton.icon(
            onPressed: _pickImage,
            icon: const Icon(Icons.photo_camera_outlined, size: 18),
            label: const Text('Change photo'),
          ),
          if (_imagePath != null || (current.imageUrl != null && !_removeImage))
            TextButton(
              onPressed: () => setState(() {
                _imagePath = null;
                _removeImage = true;
              }),
              child: const Text('Remove', style: TextStyle(color: AppColors.red)),
            ),
        ]),
        const SectionTitle('Personal data'),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
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
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'E-mail', prefixIcon: Icon(Icons.mail_outline)),
              validator: Validators.email,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _phone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Phone', prefixIcon: Icon(Icons.phone_outlined)),
              validator: Validators.phone,
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<int>(
              key: ValueKey('city-${_cities.length}-$_cityId'),
              initialValue: _cityId,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'City', prefixIcon: Icon(Icons.location_city_outlined)),
              items: [for (final c in _cities) DropdownMenuItem(value: c.id, child: Text(c.name, overflow: TextOverflow.ellipsis))],
              onChanged: (v) => setState(() => _cityId = v),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _pickBirthDate,
              icon: const Icon(Icons.cake_outlined, size: 18),
              label: Text(_birth == null ? 'Date of birth' : 'Born ${formatDate(_birth!)}'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _bio,
              maxLength: 300,
              minLines: 2,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Bio'),
            ),
          ]),
        ),
        const SectionTitle('Favourite genres'),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Wrap(spacing: 8, runSpacing: 8, children: [
            for (final g in widget.data.genres)
              FilterChip(
                label: Text(g.name),
                selected: _genres.contains(g.id),
                selectedColor: AppColors.redDark,
                onSelected: (v) => setState(() => v ? _genres.add(g.id) : _genres.remove(g.id)),
              ),
          ]),
        ),
        const SizedBox(height: 4),
        const Padding(
          padding: EdgeInsets.symmetric(horizontal: 16),
          child: Text('Your genres help Discover recommend titles you will like.',
              style: TextStyle(color: AppColors.muted, fontSize: 12)),
        ),
        const SectionTitle('Notifications & privacy'),
        SwitchListTile(
          title: const Text('Push notifications'),
          value: _push,
          activeThumbColor: AppColors.red,
          onChanged: (v) => setState(() => _push = v),
        ),
        SwitchListTile(
          title: const Text('E-mail notifications'),
          value: _emailNotifications,
          activeThumbColor: AppColors.red,
          onChanged: (v) => setState(() => _emailNotifications = v),
        ),
        SwitchListTile(
          title: const Text('Public profile'),
          value: _public,
          activeThumbColor: AppColors.red,
          onChanged: (v) => setState(() => _public = v),
        ),
        const SectionTitle('Password'),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Theme(
            data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
            child: ExpansionTile(
              tilePadding: EdgeInsets.zero,
              childrenPadding: EdgeInsets.zero,
              title: const Text('Change password'),
              subtitle: const Text('Leave empty to keep your current password',
                  style: TextStyle(color: AppColors.muted, fontSize: 12)),
              children: [
                const SizedBox(height: 8),
                TextFormField(
                  controller: _currentPassword,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Current password'),
                  validator: (v) => _changingPassword && (v == null || v.isEmpty)
                      ? 'Enter your current password to change it.'
                      : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _newPassword,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'New password'),
                  validator: (v) => _changingPassword ? Validators.password(v, field: 'The new password') : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _confirmPassword,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Confirm new password'),
                  validator: (v) => _changingPassword ? Validators.confirm(v, _newPassword.text) : null,
                ),
                const SizedBox(height: 8),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: FilledButton(onPressed: _busy ? null : _save, child: const Text('Save')),
        ),
        const SizedBox(height: 24),
      ]),
    );
  }
}
