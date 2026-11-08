import 'package:flutter/foundation.dart';

import 'api_client.dart';
import 'models.dart';

class Repository {
  Repository(this.api);

  final ApiClient api;

  final ValueNotifier<int> listsChanged = ValueNotifier<int>(0);

  Future<T> _listChange<T>(Future<T> call) async {
    final result = await call;
    listsChanged.value++;
    return result;
  }

  final ValueNotifier<int> activityChanged = ValueNotifier<int>(0);

  Future<T> _activityChange<T>(Future<T> call) async {
    final result = await call;
    activityChanged.value++;
    return result;
  }

  List<T> _all<T>(dynamic data, T Function(Map<String, dynamic>) parse) =>
      (data as List).cast<Map<String, dynamic>>().map(parse).toList();

  Future<Paged<MovieCard>> movies(
          {String? search, String? type, int? genreId, int page = 1, int pageSize = 20}) async =>
      Paged.fromJson(
          await api.get('Movie', query: {
            'NameFTS': search,
            'Type': type,
            'GenreId': genreId,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          MovieCard.new);

  Future<MovieDetail> movie(int id) async =>
      MovieDetail((await api.get('Movie/$id')) as Map<String, dynamic>);

  Future<List<Named>> genres() async =>
      Paged.fromJson(await api.get('Genre', query: {'PageSize': 100}), Named.fromJson).items;

  Future<List<Named>> countries() async =>
      Paged.fromJson(await api.get('Country', query: {'PageSize': 100}), Named.fromJson).items;

  Future<PersonInfo> person(int id) async => PersonInfo((await api.get('Person/$id')) as Map<String, dynamic>);

  Future<List<MovieCard>> personMovies(int id) async =>
      ((await api.get('Person/$id/movies')) as List).map((e) => MovieCard(e as Map<String, dynamic>)).toList();

  Future<StudioInfo> studio(int id) async => StudioInfo((await api.get('Studio/$id')) as Map<String, dynamic>);

  Future<List<MovieCard>> studioMovies(int id) async =>
      ((await api.get('Studio/$id/movies')) as List).map((e) => MovieCard(e as Map<String, dynamic>)).toList();

  Future<List<UserListSummary>> lists(int? userId) async =>
      ((await api.get('UserList', query: {'userId': userId})) as List)
          .map((e) => UserListSummary(e as Map<String, dynamic>))
          .toList();

  Future<UserListDetail> list(int id) async => UserListDetail((await api.get('UserList/$id')) as Map<String, dynamic>);

  Future<UserListSummary> createList(String name) async => _listChange(
      api.post('UserList', body: {'name': name}).then((json) => UserListSummary(json as Map<String, dynamic>)));

  Future<void> renameList(int id, String name) => _listChange(api.put('UserList/$id', body: {'name': name}));

  Future<void> deleteList(int id) => _listChange(api.delete('UserList/$id'));

  Future<void> addToList(int listId, int movieId) => _listChange(api.post('UserList/$listId/items/$movieId'));

  Future<void> removeFromList(int listId, int movieId) => _listChange(api.delete('UserList/$listId/items/$movieId'));

  Future<List<Named>> cities() async =>
      Paged.fromJson(await api.get('City', query: {'PageSize': 100}), Named.fromJson).items;

  Future<Paged<Review>> reviews(
          {int? movieId, int? userId, String sortBy = 'Relevance', int page = 1, int pageSize = 20}) async =>
      Paged.fromJson(
          await api.get('Review', query: {
            'MovieId': movieId,
            'UserId': userId,
            'Status': 'Active',
            'SortBy': sortBy,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          Review.new);

  Future<RatingSummary> ratingSummary(int movieId) async =>
      RatingSummary((await api.get('Review/summary/$movieId')) as Map<String, dynamic>);

  Future<void> saveReview(
          {int? id, required int movieId, required int rating, required String text}) =>
      id == null
          ? api.post('Review', body: {'movieId': movieId, 'rating': rating, 'text': text})
          : api.put('Review/$id', body: {'movieId': movieId, 'rating': rating, 'text': text});

  Future<void> deleteReview(int id) => api.delete('Review/$id');

  Future<void> voteReview(int id, bool helpful) =>
      api.post('Review/$id/vote', body: {'isHelpful': helpful});

  Future<void> removeReviewVote(int id) => api.delete('Review/$id/vote');

  Future<Paged<ForumThread>> threads(
          {String? search, String sortBy = 'Newest', String? tag, int? movieId, int page = 1, int pageSize = 20}) async =>
      Paged.fromJson(
          await api.get('Thread', query: {
            'NameFTS': search,
            'SortBy': sortBy,
            'Tags': tag,
            'MovieId': movieId,
            'Status': null,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          ForumThread.new);

  final ValueNotifier<ForumThread?> threadLoaded = ValueNotifier<ForumThread?>(null);

  Future<ForumThread> thread(int id) async {
    final loaded = ForumThread((await api.get('Thread/$id')) as Map<String, dynamic>);
    threadLoaded.value = loaded;
    return loaded;
  }

  Future<List<String>> popularTags() async =>
      ((await api.get('Thread/tags', query: {'count': 12})) as List).map((e) => '$e').toList();

  Future<void> createThread(
          {required String title,
          required String content,
          required bool spoiler,
          int? movieId,
          List<String> tags = const []}) =>
      _activityChange(api.post('Thread', body: {
        'title': title,
        'content': content,
        'hasSpoiler': spoiler,
        'movieId': movieId,
        'tags': tags,
      }));

  Future<void> deleteThread(int id) => _activityChange(api.delete('Thread/$id'));

  Future<void> voteThread(int id, bool up) => api.post('Thread/$id/vote', body: {'isUpvote': up});

  Future<void> removeThreadVote(int id) => api.delete('Thread/$id/vote');

  Future<void> addComment(int threadId, String content) =>
      _activityChange(api.post('Comment', body: {'threadId': threadId, 'content': content}));

  Future<void> deleteComment(int id) => _activityChange(api.delete('Comment/$id'));

  Future<void> voteComment(int id, bool up) => api.post('Comment/$id/vote', body: {'isUpvote': up});

  Future<void> removeCommentVote(int id) => api.delete('Comment/$id/vote');

  Future<void> report(String targetType, int targetId, String reason) => api.post('Report',
      body: {'targetType': targetType, 'targetId': targetId, 'reason': reason});

  Future<Paged<WatchlistItem>> watchlist(
          {String? listType, String? type, int page = 1, int pageSize = 30}) async =>
      Paged.fromJson(
          await api.get('Watchlist', query: {
            'ListType': listType,
            'Type': type,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          WatchlistItem.new);

  Future<Paged<WatchlistItem>> userWatchlist(int userId,
          {String? listType, int page = 1, int pageSize = 30}) async =>
      Paged.fromJson(
          await api.get('Watchlist/user/$userId',
              query: {'ListType': listType, 'PageNumber': page, 'PageSize': pageSize}),
          WatchlistItem.new);

  Future<void> setWatchlist(int movieId, String listType) =>
      api.put('Watchlist/$movieId', body: {'listType': listType});

  Future<void> removeWatchlist(int movieId) => api.delete('Watchlist/$movieId');

  Future<QuizStart> startDailyQuiz() async =>
      QuizStart((await api.post('Quiz/daily/start')) as Map<String, dynamic>);

  Future<QuizStart> startFreeQuiz({String? category, String? difficulty, int count = 5}) async =>
      QuizStart((await api.post('Quiz/free/start',
          body: {'category': category, 'difficulty': difficulty, 'count': count})) as Map<String, dynamic>);

  Future<QuizResult> submitQuiz(int sessionId, List<Map<String, dynamic>> answers) async =>
      QuizResult((await api.post('Quiz/submit',
          body: {'sessionId': sessionId, 'answers': answers})) as Map<String, dynamic>);

  Future<Streak> myStreak() async =>
      Streak((await api.get('Quiz/streak')) as Map<String, dynamic>);

  Future<Leaderboard> leaderboard(String period) async => Leaderboard(
      (await api.get('Quiz/leaderboard', query: {'period': period, 'count': 50})) as Map<String, dynamic>);

  Future<List<String>> quizCategories() async =>
      ((await api.get('Quiz/categories')) as List).map((e) => '$e').toList();

  Future<List<DiscoverCard>> discover({int count = 10}) async =>
      _all(await api.get('Discover', query: {'count': count}), DiscoverCard.new);

  Future<void> swipe(int movieId, bool like) =>
      api.post('Discover/swipe', body: {'movieId': movieId, 'direction': like ? 'Right' : 'Left'});

  Future<MyProfile> myProfile() async =>
      MyProfile((await api.get('UserProfile/me')) as Map<String, dynamic>);

  Future<void> updateMyProfile({
    String? bio,
    int? cityId,
    String? phone,
    DateTime? dateOfBirth,
    bool? push,
    bool? email,
    bool? isPublic,
    String? imagePath,
    bool removeImage = false,
  }) =>
      api.putForm('UserProfile/me', {
        if (bio != null) 'Bio': bio,
        if (cityId != null) 'CityId': cityId,
        if (phone != null) 'Phone': phone,
        if (dateOfBirth != null) 'DateOfBirth': dateOfBirth.toIso8601String(),
        if (push != null) 'PushNotificationsEnabled': push,
        if (email != null) 'EmailNotificationsEnabled': email,
        if (isPublic != null) 'IsProfilePublic': isPublic,
        if (removeImage) 'RemoveImage': true,
      }, filePath: imagePath, fileField: 'Image');

  Future<MyAccount> myAccount() async =>
      MyAccount((await api.get('User/me')) as Map<String, dynamic>);

  Future<void> updateMyAccount(MyAccount a,
          {String? currentPassword, String? newPassword}) =>
      api.put('User/me', body: {
        'username': a.username,
        'email': a.email,
        'firstName': a.firstName,
        'lastName': a.lastName,
        'countryId': a.countryId,
        'isActive': true,
        if (newPassword != null) ...{
          'password': newPassword,
          'passwordConfirmation': newPassword,
          'currentPassword': currentPassword,
        },
      });

  Future<PublicProfile> publicProfile(int id) async =>
      PublicProfile((await api.get('UserProfile/$id')) as Map<String, dynamic>);

  Future<List<int>> myGenreIds() async =>
      ((await api.get('UserProfile/me/genres'))['genreIds'] as List).map((e) => e as int).toList();

  Future<void> setMyGenres(List<int> ids) =>
      api.put('UserProfile/me/genres', body: {'genreIds': ids});

  Future<Paged<UserHit>> searchUsers(String username, {int page = 1}) async => Paged.fromJson(
      await api.get('UserProfile/search',
          query: {'Username': username, 'PageNumber': page, 'PageSize': 20}),
      UserHit.new);

  Future<void> follow(int userId) => api.post('Follow/$userId');

  Future<void> unfollow(int userId) => api.delete('Follow/$userId');

  Future<Paged<FollowUser>> followers(int userId) async => Paged.fromJson(
      await api.get('Follow/$userId/followers', query: {'PageSize': 50}), FollowUser.new);

  Future<Paged<FollowUser>> following(int userId) async => Paged.fromJson(
      await api.get('Follow/$userId/following', query: {'PageSize': 50}), FollowUser.new);

  Future<Paged<FeedItem>> feed({int page = 1}) async => Paged.fromJson(
      await api.get('Follow/feed', query: {'PageNumber': page, 'PageSize': 20}), FeedItem.new);

  Future<List<Upcoming>> upcoming() async => _all(await api.get('Reminder/upcoming'), Upcoming.new);

  Future<List<Reminder>> myReminders() async {
    final all = <Reminder>[];
    for (var page = 1;; page++) {
      final paged = Paged.fromJson(await api.get('Reminder', query: {'PageNumber': page, 'PageSize': 100}), Reminder.new);
      all.addAll(paged.items);
      if (!paged.hasNext || paged.items.isEmpty) break;
    }
    return all;
  }

  Future<void> createReminder(int tmdbId) => api.post('Reminder', body: {'tmdbId': tmdbId});

  Future<void> deleteReminder(int id) => api.delete('Reminder/$id');

  Future<Paged<AppNotification>> notifications({int page = 1}) async => Paged.fromJson(
      await api.get('Notification', query: {'PageNumber': page, 'PageSize': 30}),
      AppNotification.new);

  Future<void> markAllNotificationsRead() => api.put('Notification/mark-all-as-read');

  Future<void> markNotificationRead(int id) => api.put('Notification/mark-as-read/$id');

  Future<int> unreadNotificationCount() async => (await api.get('Notification/unread-count')) as int;
}
