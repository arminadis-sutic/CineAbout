import 'package:cineabout_mobile/core/api_client.dart';
import 'package:cineabout_mobile/core/auth_state.dart';
import 'package:cineabout_mobile/core/repository.dart';
import 'package:cineabout_mobile/features/profile/profile_view.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

class FakeProfileApi implements ApiClient {
  int threads = 2;
  int comments = 5;
  bool failWrites = false;
  final calls = <String>[];

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    calls.add('GET $path');
    if (path == 'UserProfile/7') {
      return {
        'id': 7,
        'username': 'me',
        'ratedMoviesCount': 11,
        'threadsCount': threads,
        'commentsCount': comments,
        'currentStreak': 9,
        'followersCount': 6,
        'followingCount': 8,
      };
    }
    if (path == 'UserList') return [];
    if (path == 'Review') return {'dataItems': [], 'totalCount': 0, 'currentPage': 1, 'totalPages': 1};
    throw UnimplementedError(path);
  }

  @override
  Future<dynamic> post(String path, {Object? body, bool anonymous = false}) async {
    calls.add('POST $path');
    if (failWrites) throw ApiException('Server said no.', statusCode: 500);
    if (path == 'Thread') threads++;
    if (path == 'Comment') comments++;
    return null;
  }

  @override
  Future<dynamic> delete(String path) async {
    calls.add('DELETE $path');
    if (failWrites) throw ApiException('Server said no.', statusCode: 500);
    if (path == 'Thread/5') threads--;
    if (path == 'Comment/9') comments--;
    return null;
  }
}

Future<(Repository, FakeProfileApi)> pumpProfile(WidgetTester tester, {required bool isMe}) async {
  final api = FakeProfileApi();
  final repo = Repository(api);
  await tester.pumpWidget(MultiProvider(
    providers: [
      Provider<Repository>.value(value: repo),
      ChangeNotifierProvider<AuthState>.value(value: AuthState(TokenStore())),
    ],
    child: MaterialApp(home: Scaffold(body: ProfileView(userId: 7, isMe: isMe))),
  ));
  await tester.pumpAndSettle();
  return (repo, api);
}

Future<void> createThread(Repository repo) =>
    repo.createThread(title: 'New', content: 'c', spoiler: false);

void main() {
  testWidgets('adding a comment raises COMMENTS, deleting one lowers it, without a manual refresh', (tester) async {
    final (repo, api) = await pumpProfile(tester, isMe: true);
    expect(find.text('5'), findsOneWidget);

    await repo.addComment(2, 'hello');
    await tester.pumpAndSettle();
    expect(find.text('6'), findsOneWidget);
    expect(find.text('5'), findsNothing);
    expect(find.text('2'), findsOneWidget);

    await repo.deleteComment(9);
    await tester.pumpAndSettle();
    expect(find.text('5'), findsOneWidget);
    expect(find.text('6'), findsNothing);
    expect(api.calls.where((c) => c == 'GET UserProfile/7').length, 3);
  });

  testWidgets('a failed comment add or delete changes nothing and does not reload', (tester) async {
    final (repo, api) = await pumpProfile(tester, isMe: true);
    api.failWrites = true;

    await expectLater(repo.addComment(2, 'hello'), throwsA(isA<ApiException>()));
    await expectLater(repo.deleteComment(9), throwsA(isA<ApiException>()));
    await tester.pumpAndSettle();

    expect(find.text('5'), findsOneWidget);
    expect(api.calls.where((c) => c == 'GET UserProfile/7').length, 1);
  });

  testWidgets('creating a thread raises POSTS, deleting one lowers it, both without a manual refresh', (tester) async {
    final (repo, api) = await pumpProfile(tester, isMe: true);
    expect(find.text('2'), findsOneWidget);

    await createThread(repo);
    await tester.pumpAndSettle();
    expect(find.text('3'), findsOneWidget);
    expect(find.text('2'), findsNothing);

    await repo.deleteThread(5);
    await tester.pumpAndSettle();
    expect(find.text('2'), findsOneWidget);
    expect(find.text('3'), findsNothing);
    expect(api.calls.where((c) => c == 'GET UserProfile/7').length, 3);
  });

  testWidgets('a failed create or delete changes nothing and does not reload', (tester) async {
    final (repo, api) = await pumpProfile(tester, isMe: true);
    api.failWrites = true;

    await expectLater(createThread(repo), throwsA(isA<ApiException>()));
    await expectLater(repo.deleteThread(5), throwsA(isA<ApiException>()));
    await tester.pumpAndSettle();

    expect(find.text('2'), findsOneWidget);
    expect(api.calls.where((c) => c == 'GET UserProfile/7').length, 1);
  });

  testWidgets('the reload keeps the profile in place: the open tab and the header stay, no spinner replaces them', (tester) async {
    final (repo, _) = await pumpProfile(tester, isMe: true);
    await tester.tap(find.text('Lists'));
    await tester.pumpAndSettle();

    await createThread(repo);
    await tester.pump();
    expect(find.text('RATINGS'), findsOneWidget);
    expect(find.text('You have no lists yet.'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);
    await tester.pumpAndSettle();
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets("someone else's profile is not reloaded by my thread changes", (tester) async {
    final (repo, api) = await pumpProfile(tester, isMe: false);

    await createThread(repo);
    await tester.pumpAndSettle();

    expect(api.calls.where((c) => c == 'GET UserProfile/7').length, 1);
  });

  testWidgets('the listener is released when the profile leaves the tree', (tester) async {
    final (repo, _) = await pumpProfile(tester, isMe: true);
    await tester.pumpWidget(const SizedBox());
    await createThread(repo);
    await tester.pump();
  });
}
