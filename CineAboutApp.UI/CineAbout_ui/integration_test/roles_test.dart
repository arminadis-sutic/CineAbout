import 'package:cineabout_desktop/core/api_client.dart';
import 'package:cineabout_desktop/core/auth_state.dart';
import 'package:cineabout_desktop/main.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('roles: built-in roles are locked, a custom role is added and deleted', (tester) async {
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

    Future<void> settle() async {
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
    }

    await tester.tap(find.descendant(of: find.byType(Scaffold).first, matching: find.text('Reference data')).first);
    await settle();
    await tester.tap(find.text('Roles'));
    await settle();

    expect(find.text('Admin'), findsWidgets);
    expect(find.text('User'), findsWidgets);
    expect(find.byTooltip('Built-in role: it cannot be deleted'), findsNWidgets(2));
    await tester.tap(find.byTooltip('Built-in role: it cannot be deleted').first);
    await settle();
    expect(find.text('Delete role'), findsNothing, reason: 'a built-in role cannot be deleted');

    while (find.text('ZZ GUI role').evaluate().isNotEmpty) {
      await tester.tap(find.widgetWithIcon(IconButton, Icons.delete_outline).first);
      await settle();
      await tester.tap(find.text('Delete').last);
      await settle();
    }

    await tester.tap(find.text('Add role'));
    await settle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Name'), 'ZZ GUI role');
    await tester.tap(find.text('Save'));
    await settle();
    expect(find.text('ZZ GUI role'), findsOneWidget, reason: 'the new role appears in the list without a manual refresh');

    await tester.tap(find.widgetWithIcon(IconButton, Icons.delete_outline).first);
    await settle();
    await tester.tap(find.text('Delete').last);
    await settle();
    expect(find.text('ZZ GUI role'), findsNothing);
    expect(find.byType(ErrorWidget), findsNothing);
  });
}
