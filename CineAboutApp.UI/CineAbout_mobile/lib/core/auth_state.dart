import 'package:flutter/foundation.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

import 'api_client.dart';
import 'push_service.dart';

class AuthState extends ChangeNotifier {
  AuthState(this.tokens) {
    api = ApiClient(tokens, onSessionExpired: _sessionExpired);
  }

  final TokenStore tokens;
  late final ApiClient api;

  bool _ready = false;
  bool _signedIn = false;
  int _userId = 0;
  String _username = '';

  bool get ready => _ready;
  bool get signedIn => _signedIn;
  int get userId => _userId;
  String get username => _username;

  Future<void> restore() async {
    final jwt = await tokens.jwt;
    if (jwt != null && _apply(jwt)) {
      _signedIn = true;
      PushService.instance.register(api);
    } else {
      await tokens.clear();
    }
    _ready = true;
    notifyListeners();
  }

  Future<void> login(String username, String password) async {
    final data = await api.post('User/login',
        body: {'username': username, 'password': password}, anonymous: true);
    await _acceptTokens(data as Map);
  }

  Future<bool> register({
    required String username,
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    required int countryId,
  }) async {
    await api.post('User/register',
        body: {
          'username': username,
          'email': email,
          'password': password,
          'passwordConfirmation': password,
          'firstName': firstName,
          'lastName': lastName,
          'countryId': countryId,
        },
        anonymous: true);
    try {
      await login(username, password);
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> requestPasswordReset(String email) =>
      api.post('User/reset-password', body: {'email': email}, anonymous: true);

  Future<void> confirmPasswordReset(String code, String newPassword) => api.post(
      'User/reset-password/confirm',
      body: {'resetToken': code.trim().toLowerCase(), 'password': newPassword},
      anonymous: true);

  Future<void> logout() async {
    final jwt = await tokens.jwt;
    final refresh = await tokens.refresh;
    try {
      if (jwt != null) {
        await api.post('User/logout', body: {
          'jwtToken': jwt,
          'refreshToken': refresh ?? '',
          if (PushService.instance.token != null) 'deviceToken': PushService.instance.token,
        });
      }
    } catch (_) {
    }
    await PushService.instance.stop();
    await tokens.clear();
    _signedIn = false;
    _userId = 0;
    _username = '';
    notifyListeners();
  }

  Future<void> _acceptTokens(Map data) async {
    final jwt = data['jwtToken'] as String;
    await tokens.save(jwt, data['refreshToken'] as String);
    _apply(jwt);
    _signedIn = true;
    notifyListeners();
    PushService.instance.register(api);
  }

  void _sessionExpired() {
    _signedIn = false;
    _userId = 0;
    notifyListeners();
  }

  bool _apply(String jwt) {
    try {
      final claims = JwtDecoder.decode(jwt);
      _userId = int.tryParse('${claims['sub']}') ?? 0;
      _username = (claims['Username'] ?? claims['unique_name'] ?? '').toString();
      return _userId > 0;
    } catch (_) {
      return false;
    }
  }
}
