import 'package:flutter/foundation.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

import 'api_client.dart';
import 'config.dart';

class AuthState extends ChangeNotifier {
  AuthState(this.tokens) {
    api = ApiClient(tokens, onSessionExpired: _sessionExpired);
  }

  final TokenStore tokens;
  late final ApiClient api;

  bool _ready = false;
  bool _signedIn = false;
  String? _username;

  bool get ready => _ready;
  bool get signedIn => _signedIn;
  String get username => _username ?? '';

  Future<void> restore() async {
    final jwt = await tokens.jwt;
    if (jwt != null && _isAdminToken(jwt)) {
      _signedIn = true;
      _username = _usernameOf(jwt);
    } else {
      await tokens.clear();
    }
    _ready = true;
    notifyListeners();
  }

  Future<void> login(String username, String password) async {
    final data = await api.post('User/login',
        body: {'username': username, 'password': password}, anonymous: true);
    final jwt = (data as Map)['jwtToken'] as String;
    final refresh = data['refreshToken'] as String;
    if (!_isAdminToken(jwt)) {
      throw ApiException('This application is for administrators only.');
    }
    await tokens.save(jwt, refresh);
    _signedIn = true;
    _username = _usernameOf(jwt);
    notifyListeners();
  }

  Future<void> logout() async {
    final jwt = await tokens.jwt;
    final refresh = await tokens.refresh;
    try {
      if (jwt != null) {
        await api.post('User/logout',
            body: {'jwtToken': jwt, 'refreshToken': refresh ?? ''});
      }
    } catch (_) {
    }
    await tokens.clear();
    _signedIn = false;
    _username = null;
    notifyListeners();
  }

  void _sessionExpired() {
    _signedIn = false;
    _username = null;
    notifyListeners();
  }

  static bool _isAdminToken(String jwt) {
    try {
      if (JwtDecoder.isExpired(jwt)) {
      }
      final claims = JwtDecoder.decode(jwt);
      final role = claims['role'] ??
          claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      final roles = role is List ? role : [role];
      return roles.contains(AppConfig.adminRole);
    } catch (_) {
      return false;
    }
  }

  static String? _usernameOf(String jwt) {
    try {
      final claims = JwtDecoder.decode(jwt);
      return (claims['Username'] ?? claims['unique_name'] ?? claims['name'])?.toString();
    } catch (_) {
      return null;
    }
  }
}
