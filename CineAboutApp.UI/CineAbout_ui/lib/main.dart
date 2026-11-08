import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import 'core/api_client.dart';
import 'core/auth_state.dart';
import 'core/repository.dart';
import 'core/router.dart';
import 'core/theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final auth = AuthState(TokenStore());
  await auth.restore();
  runApp(CineAboutAdminApp(auth: auth));
}

class CineAboutAdminApp extends StatelessWidget {
  CineAboutAdminApp({super.key, required this.auth}) : _router = buildRouter(auth);

  final AuthState auth;
  final GoRouter _router;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthState>.value(value: auth),
        Provider<Repository>.value(value: Repository(auth.api)),
      ],
      child: MaterialApp.router(
        title: 'CineAbout Admin',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
        routerConfig: _router,
      ),
    );
  }
}
