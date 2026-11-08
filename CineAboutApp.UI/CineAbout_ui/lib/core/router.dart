import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../features/audit/audit_log_screen.dart';
import '../features/dashboard/dashboard_screen.dart';
import '../features/login/login_screen.dart';
import '../features/moderation/moderation_screen.dart';
import '../features/people/people_studios_screen.dart';
import '../features/quiz/quiz_screen.dart';
import '../features/reference/reference_screen.dart';
import '../features/reports/reports_screen.dart';
import '../features/settings/settings_screen.dart';
import '../features/shell/app_shell.dart';
import '../features/titles/titles_screen.dart';
import '../features/users/users_screen.dart';
import 'auth_state.dart';

GoRouter buildRouter(AuthState auth) {
  Page<void> page(Widget child) => NoTransitionPage<void>(child: child);

  return GoRouter(
    initialLocation: '/dashboard',
    refreshListenable: auth,
    redirect: (context, state) {
      final atLogin = state.matchedLocation == '/login';
      if (!auth.signedIn) return atLogin ? null : '/login';
      if (atLogin) return '/dashboard';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      ShellRoute(
        builder: (context, state, child) =>
            AppShell(location: state.matchedLocation, child: child),
        routes: [
          GoRoute(path: '/dashboard', pageBuilder: (_, __) => page(const DashboardScreen())),
          GoRoute(path: '/users', pageBuilder: (_, __) => page(const UsersScreen())),
          GoRoute(path: '/titles', pageBuilder: (_, __) => page(const TitlesScreen())),
          GoRoute(path: '/people', pageBuilder: (_, __) => page(const PeopleStudiosScreen())),
          GoRoute(path: '/moderation', pageBuilder: (_, __) => page(const ModerationScreen())),
          GoRoute(path: '/quiz', pageBuilder: (_, __) => page(const QuizScreen())),
          GoRoute(path: '/reports', pageBuilder: (_, __) => page(const ReportsScreen())),
          GoRoute(path: '/reference', pageBuilder: (_, __) => page(const ReferenceScreen())),
          GoRoute(path: '/audit', pageBuilder: (_, __) => page(const AuditLogScreen())),
          GoRoute(path: '/settings', pageBuilder: (_, __) => page(const SettingsScreen())),
        ],
      ),
    ],
  );
}
