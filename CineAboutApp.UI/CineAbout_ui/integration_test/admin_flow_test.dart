import 'package:cineabout_desktop/core/api_client.dart';
import 'package:cineabout_desktop/core/auth_state.dart';
import 'package:cineabout_desktop/main.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

Finder pageScrollable() =>
    find.descendant(of: find.byType(ListView).last, matching: find.byType(Scrollable)).first;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('admin can sign in and open every section', (tester) async {
    final tokens = TokenStore();
    await tokens.clear();
    final auth = AuthState(tokens);
    await auth.restore();

    await tester.pumpWidget(CineAboutAdminApp(auth: auth));
    await tester.pumpAndSettle();
    expect(find.text('Sign in'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextFormField, 'Username'), 'desktop');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'test');
    await tester.tap(find.text('Sign in'));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.pump(const Duration(seconds: 3));
    await tester.pumpAndSettle();

    expect(find.text('Overview'), findsOneWidget, reason: 'dashboard after login');
    expect(find.text('desktop'), findsOneWidget, reason: 'username in sidebar');

    Future<void> open(String nav, String expected) async {
      await tester.tap(find.descendant(
          of: find.byType(Scaffold).first, matching: find.text(nav)).first);
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      expect(find.text(expected), findsWidgets, reason: '$nav -> $expected');
      expect(find.byType(ErrorWidget), findsNothing, reason: nav);
      expect(find.text('Try again'), findsNothing, reason: '$nav failed to load');
    }

    await open('Users', 'Users');
    await open('Titles', 'Add title');
    await open('People & Studios', 'Add person');
    await open('Moderation', 'Content review');
    await open('Quiz', 'Add question');
    await open('Reports', 'Generate report');
    await open('Reference data', 'Add genre');
    await open('Audit log', 'Who changed what and when. Tap a row to see the old and new values.');
    await open('Settings', 'Activate new accounts automatically');
    await open('Dashboard', 'Overview');

    Future<void> settle() async {
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
    }

    Future<void> nav(String label) async {
      await tester.tap(find.text(label).first);
      await settle();
    }

    await nav('Users');
    await tester.tap(find.textContaining('(@').first);
    await settle();
    expect(find.text('User details'), findsOneWidget);
    expect(find.text('Ratings (rated titles)'), findsOneWidget, reason: 'activity loaded');
    expect(find.text('Reviews (with text)'), findsOneWidget, reason: 'the number of reviews is shown');
    await tester.tap(find.byIcon(Icons.close).first);
    await settle();

    await nav('Titles');
    await tester.tap(find.byIcon(Icons.edit_outlined).first);
    await settle();
    expect(find.text('Edit title'), findsOneWidget);
    await tester.scrollUntilVisible(find.text('Save title'), 400,
        scrollable: pageScrollable());
    expect(find.text('Save title'), findsOneWidget);
    await tester.drag(pageScrollable(), const Offset(0, 5000));
    await settle();
    await tester.tap(find.text('Back to list'));
    await settle();
    await tester.tap(find.text('Add title'));
    await settle();
    expect(find.text('Add new title'), findsOneWidget);
    await tester.scrollUntilVisible(find.text('Save title'), 400,
        scrollable: pageScrollable());
    await tester.tap(find.text('Save title'));
    await settle();
    expect(find.text('Title saved.'), findsNothing, reason: 'invalid form is not submitted');
    await tester.drag(pageScrollable(), const Offset(0, 5000));
    await settle();
    expect(find.text('Required'), findsWidgets, reason: 'validation messages shown');
    await tester.tap(find.text('Back to list'));
    await settle();

    await nav('People & Studios');
    await tester.tap(find.text('Add person'));
    await settle();
    expect(find.text('Full name *'), findsOneWidget);
    await tester.tap(find.text('Cancel'));
    await settle();
    await tester.tap(find.text('Studios'));
    await settle();
    await tester.tap(find.text('Add studio'));
    await settle();
    expect(find.text('Country *'), findsOneWidget);
    await tester.tap(find.text('Cancel'));
    await settle();

    await nav('Moderation');
    for (final tab in ['Threads', 'Reviews', 'Reports']) {
      await tester.tap(find.widgetWithText(Tab, tab));
      await settle();
      expect(find.text('Try again'), findsNothing, reason: 'moderation $tab failed to load');
    }

    await nav('Quiz');
    await tester.tap(find.text('Add question'));
    await settle();
    expect(find.text('Answer A *'), findsOneWidget);
    await tester.tap(find.text('Cancel'));
    await settle();

    await nav('Reference data');
    for (final tab in ['Languages', 'Countries', 'Awards', 'Genres']) {
      await tester.tap(find.text(tab));
      await settle();
    }

    await tester.tap(find.byTooltip('Sign out'));
    await settle();
    expect(find.text('Sign in'), findsOneWidget);
  });
}
