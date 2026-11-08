import 'package:cineabout_mobile/core/api_client.dart';
import 'package:cineabout_mobile/core/auth_state.dart';
import 'package:cineabout_mobile/core/models.dart';
import 'package:cineabout_mobile/core/repository.dart';
import 'package:cineabout_mobile/core/widgets.dart';
import 'package:cineabout_mobile/features/forum/forum_screen.dart';
import 'package:cineabout_mobile/features/forum/thread_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

class FakeForumApi implements ApiClient {
  int score2 = 7;
  int myVote2 = 0;
  int comments2 = 3;
  bool failVote = false;
  final calls = <String>[];

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);

  Map<String, dynamic> _thread(int id, int score, int replies, int myVote, {bool withComments = false}) => {
        'id': id,
        'userId': 1,
        'username': 'u$id',
        'title': 'Thread $id',
        'content': 'content $id',
        'createdAt': '2026-11-01T10:00:00',
        'score': score,
        'commentCount': replies,
        'myVote': myVote,
        'tags': [],
        'comments': withComments
            ? [for (var i = 0; i < replies; i++) {'id': 100 + i, 'userId': 5, 'username': 'c$i', 'content': 'reply $i', 'createdAt': '2026-11-01T11:00:00', 'score': 0, 'myVote': 0}]
            : [],
      };

  List<Map<String, dynamic>> get _all => [
        _thread(1, 1, 0, 0),
        _thread(2, score2, comments2, myVote2),
        _thread(3, 2, 5, 0),
      ];

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    calls.add('GET $path');
    if (path == 'Thread') return {'dataItems': _all, 'totalCount': 3, 'currentPage': 1, 'totalPages': 1};
    if (path == 'Thread/tags') return [];
    if (path == 'Thread/2') return _thread(2, score2, comments2, myVote2, withComments: true);
    throw UnimplementedError(path);
  }

  @override
  Future<dynamic> post(String path, {Object? body, bool anonymous = false}) async {
    calls.add('POST $path');
    if (path == 'Thread/2/vote') {
      if (failVote) throw ApiException('Voting failed.', statusCode: 500);
      score2 += 1;
      myVote2 = 1;
    } else if (path == 'Comment') {
      comments2 += 1;
    } else {
      throw UnimplementedError(path);
    }
    return null;
  }

  @override
  Future<dynamic> delete(String path) async {
    calls.add('DELETE $path');
    if (path == 'Thread/2/vote') {
      score2 -= 1;
      myVote2 = 0;
    }
    return null;
  }
}

Future<(GoRouter, FakeForumApi)> pumpForum(WidgetTester tester) async {
  final api = FakeForumApi();
  final repo = Repository(api);
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => const ForumScreen()),
    GoRoute(path: '/thread/:id', builder: (_, s) => ThreadScreen(threadId: int.parse(s.pathParameters['id']!))),
  ]);
  await tester.pumpWidget(MultiProvider(
    providers: [
      Provider<Repository>.value(value: repo),
      ChangeNotifierProvider<AuthState>.value(value: AuthState(TokenStore())),
    ],
    child: MaterialApp.router(routerConfig: router),
  ));
  await tester.pumpAndSettle();
  return (router, api);
}

void main() {
  testWidgets('vote and reply in the thread: back on the Forum list the new score and reply count are shown without a refresh',
      (tester) async {
    final (_, api) = await pumpForum(tester);
    expect(find.text('7'), findsOneWidget);
    expect(find.text('3 replies'), findsOneWidget);

    await tester.tap(find.text('Thread 2'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Upvote').first);
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'a new reply');
    await tester.tap(find.byTooltip('Send'));
    await tester.pumpAndSettle();

    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(find.text('Thread 2'), findsOneWidget);
    expect(find.text('8'), findsOneWidget);
    expect(find.text('4 replies'), findsOneWidget);
    expect(find.text('7'), findsNothing);
    expect(find.text('3 replies'), findsNothing);
    expect(find.text('1'), findsOneWidget);
    expect(find.text('0 replies'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.text('5 replies'), findsOneWidget);
    expect(api.calls.where((c) => c == 'GET Thread').length, 1);
  });

  testWidgets('taking the vote back is shown on the list as well', (tester) async {
    final (_, api) = await pumpForum(tester);
    api.score2 = 8;
    api.myVote2 = 1;
    await tester.tap(find.text('Thread 2'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Upvote').first);
    await tester.pumpAndSettle();
    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(find.text('7'), findsOneWidget);
    expect(api.calls, contains('DELETE Thread/2/vote'));
  });

  testWidgets('a failed vote changes nothing on the list', (tester) async {
    final (_, api) = await pumpForum(tester);
    api.failVote = true;

    await tester.tap(find.text('Thread 2'));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Upvote').first);
    await tester.pumpAndSettle();
    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(find.text('7'), findsOneWidget);
    expect(find.text('8'), findsNothing);
    expect(find.text('3 replies'), findsOneWidget);
  });

  testWidgets('Repository.thread publishes only after a successful request', (tester) async {
    final api = FakeForumApi();
    final repo = Repository(api);
    expect(repo.threadLoaded.value, isNull);

    final t = await repo.thread(2);
    expect(repo.threadLoaded.value, same(t));

    await expectLater(repo.thread(99), throwsA(isA<UnimplementedError>()));
    expect(repo.threadLoaded.value, same(t));
  });

  testWidgets('PagedListController.replaceWhere swaps one loaded item, no request, no change of the others', (tester) async {
    final controller = PagedListController();
    var fetches = 0;
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: PagedList<ForumThread>(
          controller: controller,
          fetch: (page) async {
            fetches++;
            return Paged([for (var i = 1; i <= 3; i++) ForumThread({'id': i, 'title': 'Thread $i', 'score': i})], 3, 1, 1);
          },
          itemBuilder: (_, t) => Text('${t.title} score ${t.score}'),
        ),
      ),
    ));
    await tester.pumpAndSettle();

    controller.replaceWhere((x) => x is ForumThread && x.id == 2, ForumThread({'id': 2, 'title': 'Thread 2', 'score': 9}));
    await tester.pump();

    expect(find.text('Thread 2 score 9'), findsOneWidget);
    expect(find.text('Thread 2 score 2'), findsNothing);
    expect(find.text('Thread 1 score 1'), findsOneWidget);
    expect(find.text('Thread 3 score 3'), findsOneWidget);
    expect(fetches, 1);
  });
}
