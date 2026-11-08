import 'package:cineabout_mobile/core/models.dart';
import 'package:cineabout_mobile/core/widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Map<String, dynamic> threadJson(int id) =>
    {'id': id, 'userId': 1, 'username': 'u$id', 'title': 'Thread $id', 'content': 'c', 'createdAt': '2026-11-01T10:00:00'};

class FakeForumServer {
  FakeForumServer(int count, {this.pageSize = 5}) : ids = List.generate(count, (i) => i + 1);

  final int pageSize;
  final List<int> ids;
  final List<String> calls = [];
  String filter = 'all';
  int? failOncePage;

  List<int> get _visible => filter == 'even' ? ids.where((i) => i.isEven).toList() : List.of(ids);

  Paged<ForumThread> page(int page) {
    calls.add('$filter:$page');
    if (failOncePage == page) {
      failOncePage = null;
      throw Exception('network down');
    }
    final visible = _visible;
    final slice = visible.skip((page - 1) * pageSize).take(pageSize).map((i) => ForumThread(threadJson(i))).toList();
    return Paged(slice, visible.length, page, (visible.length / pageSize).ceil());
  }

  List<int> get expected => _visible;
}

Future<void> settle(WidgetTester tester) async {
  for (var i = 0; i < 4; i++) {
    await tester.pump(const Duration(milliseconds: 50));
  }
}

Future<void> pumpList(WidgetTester tester, FakeForumServer s, PagedListController c) async {
  tester.view.physicalSize = const Size(400, 300);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    home: Scaffold(
      body: PagedList<ForumThread>(
        controller: c,
        padding: EdgeInsets.zero,
        separator: 0,
        fetch: (page) async {
          await Future<void>.delayed(const Duration(milliseconds: 10));
          return s.page(page);
        },
        itemBuilder: (_, t) => SizedBox(height: 100, child: Text('T${t.id}')),
      ),
    ),
  ));
  await settle(tester);
}

Future<void> scrollOnce(WidgetTester tester) async {
  await tester.drag(find.byType(ListView), const Offset(0, -200));
  await settle(tester);
}

Future<void> scrollToTheEnd(WidgetTester tester) async {
  for (var i = 0; i < 12; i++) {
    await scrollOnce(tester);
  }
}

Future<List<int>> shownIds(WidgetTester tester) async {
  tester.view.physicalSize = const Size(400, 8000);
  await settle(tester);
  return tester
      .widgetList<Text>(find.byType(Text))
      .map((t) => t.data ?? '')
      .where((d) => RegExp(r'^T\d+$').hasMatch(d))
      .map((d) => int.parse(d.substring(1)))
      .toList();
}

void deleteThread(FakeForumServer s, PagedListController c, int id) {
  s.ids.remove(id);
  c.removeWhere((x) => x is ForumThread && x.id == id);
}

void main() {
  testWidgets('without any removal paging is unchanged: page 1, 2, 3 and nothing else', (tester) async {
    final s = FakeForumServer(13);
    final c = PagedListController();
    await pumpList(tester, s, c);
    await scrollToTheEnd(tester);

    expect(s.calls, ['all:1', 'all:2', 'all:3']);
    expect(await shownIds(tester), s.expected);
  });

  testWidgets('page 1 loaded, one thread deleted, next page loaded: nothing skipped, nothing duplicated', (tester) async {
    final s = FakeForumServer(13);
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 3);
    await tester.pump();
    await scrollToTheEnd(tester);

    final ids = await shownIds(tester);
    expect(ids, s.expected);
    expect(ids.toSet().length, ids.length);
    expect(ids, isNot(contains(3)));
    expect(s.calls, ['all:1', 'all:1', 'all:2', 'all:3']);
  });

  testWidgets('deleted on the second of two loaded pages', (tester) async {
    final s = FakeForumServer(23);
    final c = PagedListController();
    await pumpList(tester, s, c);
    await scrollOnce(tester);
    expect(s.calls, ['all:1', 'all:2']);

    deleteThread(s, c, 8);
    await tester.pump();
    await scrollToTheEnd(tester);

    final ids = await shownIds(tester);
    expect(ids, s.expected);
    expect(ids.toSet().length, ids.length);
  });

  testWidgets('two deletions on different loaded pages', (tester) async {
    final s = FakeForumServer(23);
    final c = PagedListController();
    await pumpList(tester, s, c);
    await scrollOnce(tester);

    deleteThread(s, c, 3);
    deleteThread(s, c, 8);
    await tester.pump();
    await scrollToTheEnd(tester);

    final ids = await shownIds(tester);
    expect(ids, s.expected);
    expect(ids.toSet().length, ids.length);
  });

  testWidgets('active filter is kept: every request, including the aligning one, uses it', (tester) async {
    final s = FakeForumServer(30)..filter = 'even';
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 4);
    await tester.pump();
    await scrollToTheEnd(tester);

    expect(s.calls.every((x) => x.startsWith('even:')), isTrue, reason: s.calls.toString());
    final ids = await shownIds(tester);
    expect(ids, s.expected);
    expect(ids.toSet().length, ids.length);
  });

  testWidgets('everything loaded deleted while the server has more: the list reloads instead of saying "nothing here"',
      (tester) async {
    final s = FakeForumServer(9, pageSize: 3);
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 1);
    deleteThread(s, c, 2);
    deleteThread(s, c, 3);
    await settle(tester);
    expect(find.text('Nothing here yet.'), findsNothing);
    expect(find.text('T4'), findsOneWidget);

    await scrollToTheEnd(tester);
    expect(await shownIds(tester), s.expected);
  });

  testWidgets('a failed aligning load leaves no half result: after the retry the list is exact', (tester) async {
    final s = FakeForumServer(13);
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 3);
    s.failOncePage = 2;
    await tester.pump();
    await scrollToTheEnd(tester);

    expect(s.calls.where((x) => x == 'all:2').length, greaterThanOrEqualTo(2), reason: s.calls.toString());
    final ids = await shownIds(tester);
    expect(ids, s.expected);
    expect(ids.toSet().length, ids.length);
  });

  testWidgets('reload() after a removal starts clean: next page is plain page 2 again', (tester) async {
    final s = FakeForumServer(13);
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 3);
    c.reload();
    await settle(tester);
    s.calls.clear();
    await scrollOnce(tester);

    expect(s.calls, ['all:2']);
  });

  testWidgets('refreshSilently() after a removal starts clean as well', (tester) async {
    final s = FakeForumServer(13);
    final c = PagedListController();
    await pumpList(tester, s, c);

    deleteThread(s, c, 3);
    c.refreshSilently();
    await settle(tester);
    s.calls.clear();
    await scrollOnce(tester);

    expect(s.calls, ['all:2']);
  });
}
