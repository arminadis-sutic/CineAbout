import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/forgot_password_screen.dart';
import '../features/auth/login_screen.dart';
import '../features/auth/register_screen.dart';
import '../features/auth/reset_password_screen.dart';
import '../features/discover/discover_screen.dart';
import '../features/catalog/person_studio_screens.dart';
import '../features/forum/create_thread_screen.dart';
import '../features/forum/forum_screen.dart';
import '../features/forum/thread_screen.dart';
import '../features/home/home_screen.dart';
import '../features/home/notifications_screen.dart';
import '../features/home/search_screen.dart';
import '../features/lists/lists_widgets.dart';
import '../features/movie/movie_details_screen.dart';
import '../features/profile/edit_profile_screen.dart';
import '../features/profile/profile_screen.dart';
import '../features/profile/user_profile_screen.dart';
import '../features/quiz/leaderboard_screen.dart';
import '../features/quiz/quiz_home_screen.dart';
import '../features/quiz/quiz_play_screen.dart';
import '../features/shell/main_shell.dart';
import '../features/watchlist/watchlist_screen.dart';
import 'auth_state.dart';
import 'models.dart';

const _publicRoutes = {'/login', '/register', '/forgot', '/reset'};

GoRouter buildRouter(AuthState auth) {
  return GoRouter(
    initialLocation: '/home',
    refreshListenable: auth,
    redirect: (context, state) {
      final location = state.matchedLocation;
      if (location == '/reset') return null;
      if (!auth.signedIn) return _publicRoutes.contains(location) ? null : '/login';
      if (_publicRoutes.contains(location)) return '/home';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
      GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
      GoRoute(path: '/forgot', builder: (_, __) => const ForgotPasswordScreen()),
      GoRoute(
        path: '/reset',
        builder: (_, state) => ResetPasswordScreen(
          key: ValueKey(state.uri.queryParameters['token']),
          initialToken: state.uri.queryParameters['token'],
        ),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => MainShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/forum', builder: (_, __) => const ForumScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/discover', builder: (_, __) => const DiscoverScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/watchlist', builder: (_, __) => const WatchlistScreen()),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(path: '/profile', builder: (_, __) => const ProfileScreen()),
          ]),
        ],
      ),
      GoRoute(
        path: '/search',
        builder: (_, state) => SearchScreen(genreId: int.tryParse(state.uri.queryParameters['genre'] ?? '')),
      ),
      GoRoute(
        path: '/movie/:id',
        builder: (_, state) => MovieDetailsScreen(movieId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/thread/new',
        builder: (_, state) => CreateThreadScreen(
          movieId: int.tryParse(state.uri.queryParameters['movieId'] ?? ''),
          movieTitle: state.uri.queryParameters['movieTitle'],
        ),
      ),
      GoRoute(
        path: '/thread/:id',
        builder: (_, state) => ThreadScreen(threadId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/person/:id',
        builder: (_, state) => PersonScreen(personId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/studio/:id',
        builder: (_, state) => StudioScreen(studioId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: '/list/:id',
        builder: (_, state) => ListDetailScreen(listId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(path: '/quiz', builder: (_, __) => const QuizHomeScreen()),
      GoRoute(
        path: '/quiz/play',
        builder: (_, state) => QuizPlayScreen(quiz: state.extra! as QuizStart),
      ),
      GoRoute(path: '/leaderboard', builder: (_, __) => const LeaderboardScreen()),
      GoRoute(
        path: '/user/:id',
        builder: (_, state) => UserProfileScreen(userId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(path: '/profile/edit', builder: (_, __) => const EditProfileScreen()),
      GoRoute(path: '/notifications', builder: (_, __) => const NotificationsScreen()),
    ],
  );
}
