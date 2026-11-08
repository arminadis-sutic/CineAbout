import 'package:cineabout_mobile/core/api_client.dart';
import 'package:cineabout_mobile/core/models.dart';
import 'package:cineabout_mobile/core/repository.dart';
import 'package:cineabout_mobile/core/widgets.dart';
import 'package:cineabout_mobile/features/forum/thread_tile.dart';
import 'package:cineabout_mobile/features/lists/lists_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

class FakeApi implements ApiClient {
  int itemCount = 0;
  bool failNext = false;

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);

  void _maybeFail() {
    if (failNext) {
      failNext = false;
      throw ApiException('Server said no.', statusCode: 500);
    }
  }

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    if (path == 'UserList') {
      return [
        {'id': 1, 'userId': 7, 'username': 'me', 'name': 'Weekend', 'itemCount': itemCount, 'posterUrls': []}
      ];
    }
    throw UnimplementedError(path);
  }

  @override
  Future<dynamic> post(String path, {Object? body, bool anonymous = false}) async {
    _maybeFail();
    if (path == 'UserList/1/items/5') itemCount++;
    return {'id': 1, 'userId': 7, 'username': 'me', 'name': 'Weekend', 'itemCount': itemCount, 'posterUrls': []};
  }

  @override
  Future<dynamic> delete(String path) async {
    _maybeFail();
    if (path == 'UserList/1/items/5') itemCount--;
  }
}

Map<String, dynamic> threadJson(int id) =>
    {'id': id, 'userId': 1, 'username': 'u$id', 'title': 'Thread $id', 'content': 'c', 'createdAt': '2026-11-01T10:00:00'};

void main() {
  group('Profile "Lists" tab counters', () {
    testWidgets('a title added from anywhere (e.g. the Add to list sheet) updates "0 titles" without a manual refresh',
        (tester) async {
      final api = FakeApi();
      final repo = Repository(api);
      await tester.pumpWidget(Provider<Repository>.value(
        value: repo,
        child: const MaterialApp(home: Scaffold(body: SingleChildScrollView(child: UserListsTab(userId: 7, isMe: true)))),
      ));
      await tester.pumpAndSettle();
      expect(find.text('0 titles'), findsOneWidget);

      await repo.addToList(1, 5);
      await tester.pumpAndSettle();

      expect(find.text('1 titles'), findsOneWidget);
      expect(find.text('0 titles'), findsNothing);
    });

    testWidgets('a failed add does not change the counter and does not reload', (tester) async {
      final api = FakeApi();
      final repo = Repository(api);
      await tester.pumpWidget(Provider<Repository>.value(
        value: repo,
        child: const MaterialApp(home: Scaffold(body: SingleChildScrollView(child: UserListsTab(userId: 7, isMe: true)))),
      ));
      await tester.pumpAndSettle();

      final before = repo.listsChanged.value;
      api.failNext = true;
      await expectLater(repo.addToList(1, 5), throwsA(isA<ApiException>()));
      await tester.pumpAndSettle();

      expect(repo.listsChanged.value, before);
      expect(find.text('0 titles'), findsOneWidget);
    });

    testWidgets('removing a title also updates the counter, and the listener is released on dispose', (tester) async {
      final api = FakeApi()..itemCount = 2;
      final repo = Repository(api);
      await tester.pumpWidget(Provider<Repository>.value(
        value: repo,
        child: const MaterialApp(home: Scaffold(body: SingleChildScrollView(child: UserListsTab(userId: 7, isMe: true)))),
      ));
      await tester.pumpAndSettle();
      expect(find.text('2 titles'), findsOneWidget);

      await repo.removeFromList(1, 5);
      await tester.pumpAndSettle();
      expect(find.text('1 titles'), findsOneWidget);

      await tester.pumpWidget(const SizedBox());
      await repo.addToList(1, 5);
      await tester.pump();
    });
  });

  group('Forum list after deleting a thread', () {
    testWidgets('PagedList.removeWhere drops only that item and keeps loaded pages (no new request)', (tester) async {
      final controller = PagedListController();
      var fetches = 0;
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: PagedList<ForumThread>(
            controller: controller,
            fetch: (page) async {
              fetches++;
              return Paged([ForumThread(threadJson(1)), ForumThread(threadJson(2)), ForumThread(threadJson(3))], 3, 1, 1);
            },
            itemBuilder: (_, t) => Text(t.title),
          ),
        ),
      ));
      await tester.pumpAndSettle();
      expect(find.text('Thread 2'), findsOneWidget);
      final fetchesBefore = fetches;

      controller.removeWhere((x) => x is ForumThread && x.id == 2);
      await tester.pump();

      expect(find.text('Thread 2'), findsNothing);
      expect(find.text('Thread 1'), findsOneWidget);
      expect(find.text('Thread 3'), findsOneWidget);
      expect(fetches, fetchesBefore);
    });

    Future<List<int>> openThread(WidgetTester tester, {required bool deleted}) async {
      final calls = <int>[];
      final router = GoRouter(routes: [
        GoRoute(
          path: '/',
          builder: (_, __) => Scaffold(
            body: ThreadTile(thread: ForumThread(threadJson(9)), onDeleted: () => calls.add(9)),
          ),
        ),
        GoRoute(
          path: '/thread/:id',
          builder: (context, __) => Scaffold(
            body: TextButton(
              onPressed: () => deleted ? context.pop(true) : context.pop(),
              child: const Text('leave'),
            ),
          ),
        ),
      ]);
      await tester.pumpWidget(MaterialApp.router(routerConfig: router));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(InkWell).first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('leave'));
      await tester.pumpAndSettle();
      return calls;
    }

    testWidgets('ThreadTile calls onDeleted only when the thread screen closed with true', (tester) async {
      expect(await openThread(tester, deleted: true), [9]);
    });

    testWidgets('ThreadTile does nothing when the thread screen was just closed (e.g. delete failed)', (tester) async {
      expect(await openThread(tester, deleted: false), isEmpty);
    });
  });
}
