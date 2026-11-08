import 'package:cineabout_mobile/core/api_client.dart';
import 'package:cineabout_mobile/core/auth_state.dart';
import 'package:cineabout_mobile/features/movie/movie_cards.dart';
import 'package:cineabout_mobile/main.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('user can sign in and use every main area', (tester) async {
    final onDevice = defaultTargetPlatform == TargetPlatform.android || defaultTargetPlatform == TargetPlatform.iOS;
    if (!onDevice) {
      tester.view.physicalSize = const Size(1170, 2532);
      tester.view.devicePixelRatio = 3;
      addTearDown(tester.view.reset);
    }

    final tokens = TokenStore();
    await tokens.clear();
    final auth = AuthState(tokens);
    await auth.restore();

    Future<void> settle([int seconds = 2]) async {
      await tester.pump(Duration(seconds: seconds));
      await tester.pumpAndSettle();
    }

    Future<void> tapVisible(Finder finder) async {
      FocusManager.instance.primaryFocus?.unfocus();
      await tester.pumpAndSettle();
      await tester.ensureVisible(finder.first);
      await tester.pumpAndSettle();
      await tester.tap(finder.first);
    }

    await tester.pumpWidget(CineAboutApp(auth: auth));
    await tester.pumpAndSettle();
    expect(find.text('Sign in'), findsOneWidget);

    await tapVisible(find.text('Forgot password?'));
    await settle();
    expect(find.text('Reset your password'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField).first, 'not-an-email');
    await tapVisible(find.text('Send reset link'));
    await settle();
    expect(find.text('Enter a valid e-mail in the format name@domain.com.'), findsOneWidget);
    await tapVisible(find.text('I already have a token'));
    await settle();
    expect(find.text('Choose a new password'), findsOneWidget);
    await tapVisible(find.text('Change password'));
    await settle();
    expect(find.text('Paste the whole token from the e-mail (a number, a dot and 64 characters).'), findsOneWidget);
    await tester.pageBack();
    await settle();
    await tester.pageBack();
    await settle();

    await tapVisible(find.text('Sign in'));
    await tester.pumpAndSettle();
    expect(find.text('Enter your username'), findsOneWidget);
    await tester.enterText(find.widgetWithText(TextFormField, 'Username'), 'mobile');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'test');
    await tapVisible(find.text('Sign in'));
    await settle(3);

    expect(find.text('Daily quiz'), findsWidgets);
    expect(find.text('Top rated'), findsOneWidget);
    expect(find.text('Try again'), findsNothing);

    await tester.scrollUntilVisible(find.text('Top rated'), 200, scrollable: find.byType(Scrollable).first);
    expect(find.byType(PosterCard), findsWidgets);
    await tester.ensureVisible(find.byType(PosterCard).first);
    await tester.pumpAndSettle();
    final screenHeight = tester.view.physicalSize.height / tester.view.devicePixelRatio;
    final dy = tester.getCenter(find.byType(PosterCard).first).dy;
    await tester.drag(find.byType(Scrollable).first, Offset(0, screenHeight / 2 - dy));
    await tester.pumpAndSettle();
    await tapVisible(find.byType(PosterCard).first);
    await settle();
    expect(find.text('WATCHLIST'), findsOneWidget);
    expect(find.text('INTERESTED'), findsOneWidget);
    for (final tab in ['Reviews', 'Discussion', 'Info']) {
      await tapVisible(find.descendant(of: find.byType(SegmentedButton<int>), matching: find.text(tab)));
      await settle();
      expect(find.text('Try again'), findsNothing, reason: 'movie tab $tab');
    }
    await tester.pageBack();
    await settle();

    Future<void> openTab(String label) async {
      await tapVisible(find.descendant(of: find.byType(NavigationBar), matching: find.text(label)));
      await settle(3);
      expect(find.text('Try again'), findsNothing, reason: '$label failed to load');
    }

    await openTab('Forum');
    expect(find.text('Newest'), findsOneWidget);
    await tapVisible(find.byType(Card).first);
    await settle();
    expect(find.text('Discussion'), findsOneWidget);
    await tester.pageBack();
    await settle();

    await openTab('Discover');
    expect(find.text('NOT FOR ME'), findsOneWidget);
    expect(find.text('INTERESTED'), findsOneWidget);

    await openTab('Watchlist');
    expect(find.text('My lists'), findsOneWidget);
    await tapVisible(find.text('Interested').first);
    await settle();

    await openTab('Profile');
    expect(find.text('RATINGS'), findsOneWidget);
    expect(find.byIcon(Icons.notifications_none), findsWidgets, reason: 'notification bell');
    await tapVisible(find.text('EDIT PROFILE'));
    await settle();
    expect(find.text('Personal data'), findsOneWidget);
    expect(find.text('Favourite genres'), findsOneWidget);
    await tester.scrollUntilVisible(find.text('Change password'), 300, scrollable: find.byType(Scrollable).first);
    await tapVisible(find.text('Change password'));
    await settle();
    await tester.enterText(find.widgetWithText(TextFormField, 'New password'), 'weak');
    await tester.scrollUntilVisible(find.text('Save'), 300, scrollable: find.byType(Scrollable).first);
    await tapVisible(find.text('Save'));
    await settle();
    expect(find.text('Enter your current password to change it.'), findsOneWidget);
    expect(find.textContaining('needs 8-100 characters'), findsOneWidget);
    await tester.pageBack();
    await settle();

    await openTab('Home');
    await tester.drag(find.byType(Scrollable).first, const Offset(0, 3000));
    await settle();
    await tapVisible(find.text('Start').evaluate().isNotEmpty ? find.text('Start').first : find.text('Open quiz').first);
    await settle();
    expect(find.text('Categories'), findsOneWidget);
    await tapVisible(find.text('Global leaderboard'));
    await settle();
    expect(find.text('Leaderboard'), findsOneWidget);
    expect(find.text('(you)'), findsNothing);
    await tester.pageBack();
    await settle();
    await tester.pageBack();
    await settle();

    await tapVisible(find.byIcon(Icons.search).first);
    await settle();
    await tester.enterText(find.byType(TextField).first, 'dark');
    await settle(3);
    expect(find.textContaining('Dark'), findsWidgets);
    await tester.pageBack();
    await settle();

    await openTab('Profile');
    await tapVisible(find.byTooltip('Sign out'));
    await settle();
    await tapVisible(find.text('Sign out').last);
    await settle();
    expect(find.text('Sign in'), findsOneWidget);
  });
}

