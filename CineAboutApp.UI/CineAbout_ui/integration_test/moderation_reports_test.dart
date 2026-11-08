import 'dart:io';

import 'package:cineabout_desktop/core/api_client.dart';
import 'package:cineabout_desktop/core/auth_state.dart';
import 'package:cineabout_desktop/main.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('moderation sort and date range, report download', (tester) async {
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

    Future<void> settle([int seconds = 2]) async {
      await tester.pump(Duration(seconds: seconds));
      await tester.pumpAndSettle();
    }

    Future<void> nav(String label) async {
      await tester.tap(find.descendant(of: find.byType(Scaffold).first, matching: find.text(label)).first);
      await settle();
    }

    await nav('Moderation');
    await tester.tap(find.text('Reviews').first);
    await settle();
    expect(find.text('Newest first'), findsOneWidget, reason: 'default sort');
    await tester.tap(find.text('Newest first'));
    await settle(1);
    await tester.tap(find.text('Most helpful first').last);
    await settle();
    expect(find.text('Most helpful first'), findsWidgets, reason: 'sort changed');
    expect(find.text('Try again'), findsNothing);
    expect(find.byType(ErrorWidget), findsNothing);

    await tester.tap(find.byIcon(Icons.date_range).first);
    await settle(1);
    final picker = find.byType(DateRangePickerDialog);
    expect(picker, findsOneWidget, reason: 'the date-range picker opened');
    await tester.tap(find.descendant(of: picker, matching: find.text('2')).first);
    await settle(1);
    await tester.tap(find.descendant(of: picker, matching: find.text('6')).first);
    await settle(1);
    await tester.tap(find.descendant(of: picker, matching: find.text('Save')).first);
    await settle();
    expect(picker, findsNothing, reason: 'the picker closed with the range');
    expect(find.text('Try again'), findsNothing, reason: 'the list reloads with the date range');
    expect(find.byType(ErrorWidget), findsNothing);

    await tester.tap(find.byIcon(Icons.picture_as_pdf_outlined).first);
    await settle();
    expect(find.text('Generate report'), findsWidgets, reason: 'the reports screen is open');
    await tester.tap(find.text('Generate report').first);
    await settle(8);
    final message = find.textContaining('Report saved to');
    expect(message, findsOneWidget, reason: 'the success message names the file');
    final text = (tester.widget<Text>(message)).data!;
    final path = text.substring('Report saved to '.length).trim();
    final file = File(path);
    expect(file.existsSync(), isTrue, reason: 'file exists: $path');
    final head = file.readAsBytesSync().sublist(0, 4);
    expect(String.fromCharCodes(head), '%PDF', reason: 'a real PDF');
  });
}
