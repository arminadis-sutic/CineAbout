import 'package:cineabout_desktop/core/models.dart';
import 'package:cineabout_desktop/core/repository.dart';
import 'package:cineabout_desktop/core/theme.dart';
import 'package:cineabout_desktop/features/reference/reference_screen.dart' as current;
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

class _FakeRepository implements Repository {
  final saved = <String>[];
  final extras = <Map<String, dynamic>?>[];

  @override
  Future<List<Named>> reference(String controller, {String? search}) async {
    if (controller == 'Country') return [Named(1, 'Bosna i Hercegovina'), Named(2, 'Hrvatska')];
    if (controller == 'City') return [Named(5, 'Mostar', countryId: 1, countryName: 'Bosna i Hercegovina')];
    return [Named(1, 'Drama')];
  }

  @override
  Future<void> saveReference(String controller, int? id, String name, {Map<String, dynamic>? extra}) async {
    saved.add('$controller:${id ?? 'new'}:$name');
    extras.add(extra);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

Future<_FakeRepository> _pump(WidgetTester tester, Widget screen) async {
  tester.view.physicalSize = const Size(1400, 900);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final repo = _FakeRepository();
  await tester.pumpWidget(Provider<Repository>.value(
    value: repo,
    child: MaterialApp(theme: buildTheme(), home: Scaffold(body: Padding(padding: const EdgeInsets.all(16), child: screen))),
  ));
  await tester.pumpAndSettle();
  return repo;
}

void main() {
  testWidgets('add a genre: dialog closes cleanly and the name is saved', (tester) async {
    final repo = await _pump(tester, const current.ReferenceScreen());
    await tester.tap(find.text('Add genre'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).first, '  Western ');
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(repo.saved, ['Genre:new:Western']);
  });

  testWidgets('cancel closes the dialog cleanly', (tester) async {
    final repo = await _pump(tester, const current.ReferenceScreen());
    await tester.tap(find.text('Add genre'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(repo.saved, isEmpty);
  });

  testWidgets('empty name is refused inside the dialog', (tester) async {
    final repo = await _pump(tester, const current.ReferenceScreen());
    await tester.tap(find.text('Add genre'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(find.text('Required'), findsOneWidget);
    expect(repo.saved, isEmpty);
  });

  testWidgets('add a city needs a country and sends countryId', (tester) async {
    final repo = await _pump(tester, const current.ReferenceScreen());
    await tester.tap(find.text('Cities'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Add city'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).first, 'Split');
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(find.text('Required'), findsOneWidget);
    await tester.tap(find.byType(DropdownButtonFormField<int>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Hrvatska').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(repo.saved, ['City:new:Split']);
    expect(repo.extras.single, {'countryId': 2});
  });

  testWidgets('edit a city keeps its country selected', (tester) async {
    final repo = await _pump(tester, const current.ReferenceScreen());
    await tester.tap(find.text('Cities'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Edit').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(repo.saved, ['City:5:Mostar']);
    expect(repo.extras.single, {'countryId': 1});
  });
}
