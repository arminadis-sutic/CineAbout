import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'core/api_client.dart';
import 'core/auth_state.dart';
import 'core/push_service.dart';
import 'core/repository.dart';
import 'core/router.dart';
import 'core/theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await PushService.instance.init();
  final auth = AuthState(TokenStore());
  await auth.restore();
  runApp(CineAboutApp(auth: auth));
}

class CineAboutApp extends StatefulWidget {
  const CineAboutApp({super.key, required this.auth});

  final AuthState auth;

  @override
  State<CineAboutApp> createState() => _CineAboutAppState();
}

class _CineAboutAppState extends State<CineAboutApp> {
  late final GoRouter _router = buildRouter(widget.auth);
  late final Repository _repo = Repository(widget.auth.api);

  @override
  void dispose() {
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: widget.auth),
        Provider<Repository>.value(value: _repo),
      ],
      child: MaterialApp.router(
        title: 'CineAbout',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
        routerConfig: _router,
      ),
    );
  }
}
