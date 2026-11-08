import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

import 'api_client.dart';

class PushService {
  PushService._();

  static final PushService instance = PushService._();

  bool _ready = false;
  String? _token;
  StreamSubscription<String>? _refresh;

  String? get token => _token;

  Future<void> init() async {
    if (_ready) return;
    try {
      await Firebase.initializeApp();
      _ready = true;
    } catch (e) {
      debugPrint('Push disabled: Firebase is not configured in this build ($e)');
    }
  }

  Future<void> register(ApiClient api) async {
    if (!_ready) return;
    try {
      final messaging = FirebaseMessaging.instance;
      final settings = await messaging.requestPermission();
      if (settings.authorizationStatus == AuthorizationStatus.denied) return;
      // ignore: deprecated_member_use
      final token = await messaging.getToken();
      if (token == null || token.isEmpty) return;
      await _send(api, token);
      await _refresh?.cancel();
      // ignore: deprecated_member_use
      _refresh = messaging.onTokenRefresh.listen((t) => _send(api, t));
    } catch (e) {
      debugPrint('Push registration failed: $e');
    }
  }

  Future<void> _send(ApiClient api, String token) async {
    _token = token;
    await api.post('User/register-device', body: {'deviceToken': token, 'deviceType': 'android'});
  }

  Future<void> stop() async {
    await _refresh?.cancel();
    _refresh = null;
  }
}
