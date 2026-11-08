import 'package:cineabout_desktop/core/api_client.dart';
import 'package:cineabout_desktop/core/auth_state.dart';
import 'package:cineabout_desktop/main.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

const _base = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5218');

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final dio = Dio(BaseOptions(baseUrl: '$_base/api', validateStatus: (_) => true));

  Future<Options> bearer(String user) async {
    final r = await dio.post('/User/login', data: {'username': user, 'password': 'test'});
    return Options(headers: {'Authorization': 'Bearer ${r.data['jwtToken']}'});
  }

  Future<void> signIn(WidgetTester tester) async {
    final tokens = TokenStore();
    await tokens.clear();
    final auth = AuthState(tokens);
    await auth.restore();
    await tester.pumpWidget(CineAboutAdminApp(auth: auth));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Username'), 'desktop');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'test');
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();
    expect(find.text('Overview'), findsOneWidget);
  }

  testWidgets('dashboard: top 10, 12-week view and automatic refresh', (tester) async {
    final admin = await bearer('desktop');
    await signIn(tester);

    expect(find.text('Top 10 titles'), findsOneWidget);
    expect(find.text('Metrics refresh automatically every 30 seconds.'), findsOneWidget);

    await tester.tap(find.text('12 weeks'));
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();
    expect(find.byType(ErrorWidget), findsNothing);
    expect(find.text('Try again'), findsNothing);
    await tester.tap(find.text('7 days'));
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();

    final before = (await dio.get('/Dashboard/kpis', options: admin)).data['totalUsers'] as int;
    final name = 'zztestdash${DateTime.now().millisecondsSinceEpoch % 1000000}';
    final reg = await dio.post('/User/register', data: {
      'username': name,
      'password': 'Tp1!dashboardTest',
      'passwordConfirmation': 'Tp1!dashboardTest',
      'email': '$name@cineabout.test',
      'firstName': 'Test',
      'lastName': 'Dash',
      'countryId': 1,
    });
    expect(reg.statusCode, 200);
    final newId = reg.data['id'] as int;
    expect(find.text('${before + 1}'), findsNothing, reason: 'not shown before the refresh');
    for (var i = 0; i < 75; i++) {
      await tester.pump(const Duration(seconds: 1));
    }
    await tester.pumpAndSettle();
    final shown = find.text('${before + 1}').evaluate().isNotEmpty;
    await dio.delete('/User/$newId', options: admin);
    expect(shown, isTrue, reason: 'the Users card changed to ${before + 1} without pressing Refresh');
  });

  testWidgets('discussion dialog: comments, edit and delete a comment', (tester) async {
    final admin = await bearer('desktop');
    final user = await bearer('mobile');
    final thread = await dio.post('/Thread', data: {'title': 'TEST pack5 thread', 'content': 'x', 'hasSpoiler': false, 'tags': []}, options: user);
    final tid = thread.data['id'] as int;
    final comment = await dio.post('/Comment', data: {'threadId': tid, 'content': 'TEST pack5 comment'}, options: user);
    final cid = comment.data['id'] as int;
    try {
      await signIn(tester);
      await tester.tap(find.descendant(of: find.byType(Scaffold).first, matching: find.text('Moderation')).first);
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Threads').first);
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
      await tester.enterText(find.widgetWithText(TextField, 'Search threads').first, 'TEST pack5');
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      await tester.tap(find.text('TEST pack5 thread').first);
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();

      expect(find.text('Comments (1)'), findsOneWidget);
      expect(find.text('TEST pack5 comment'), findsOneWidget);
      expect(find.text('Edit thread'), findsOneWidget);

      await tester.tap(find.byTooltip('Edit comment'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextFormField).last, 'TEST pack5 comment edited by admin');
      await tester.tap(find.text('Save'));
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      expect(find.text('TEST pack5 comment edited by admin'), findsOneWidget);
      final stored = (await dio.get('/Thread/$tid', options: admin)).data['comments'] as List;
      expect(stored.single['content'], 'TEST pack5 comment edited by admin');

      await tester.tap(find.byTooltip('Delete comment'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Delete').last);
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      expect(find.text('Comments (0)'), findsOneWidget);
      expect(find.byType(ErrorWidget), findsNothing);
    } finally {
      await dio.delete('/Comment/$cid', options: admin);
      await dio.delete('/Thread/$tid', options: admin);
    }
  });
}
