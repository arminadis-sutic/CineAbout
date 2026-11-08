import 'api_client.dart';
import 'models.dart';

class Repository {
  Repository(this.api);

  final ApiClient api;

  Future<Kpis> kpis() async => Kpis((await api.get('Dashboard/kpis')) as Map<String, dynamic>);

  Future<List<ActivityPoint>> activity(int days) async =>
      ((await api.get('Dashboard/activity', query: {'days': days})) as List)
          .map((e) => ActivityPoint(e as Map<String, dynamic>))
          .toList();

  Future<List<PopularMovie>> popularMovies(int limit) async =>
      ((await api.get('Dashboard/popular-movies', query: {'limit': limit})) as List)
          .map((e) => PopularMovie(e as Map<String, dynamic>))
          .toList();

  Future<List<ActiveThread>> activeThreads(int limit) async =>
      ((await api.get('Dashboard/active-threads', query: {'limit': limit})) as List)
          .map((e) => ActiveThread(e as Map<String, dynamic>))
          .toList();

  Future<Paged<AppUser>> users(
          {String? search, int? roleId, bool? isActive, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('User', query: {
            'NameFTS': search,
            'RoleId': roleId,
            'IsActive': isActive,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          AppUser.new);

  Future<UserActivity> userActivity(int id) async =>
      UserActivity((await api.get('UserProfile/$id')) as Map<String, dynamic>);

  Future<List<RoleInfo>> roles() async {
    final data = await api.get('Role', query: {'PageSize': 100});
    return Paged.fromJson(data, RoleInfo.new).items;
  }

  Future<void> toggleUserActive(int id) => api.post('User/activate-deactivate/$id');

  Future<void> deleteUser(int id) => api.delete('User/$id');

  Future<void> setUserRoles(AppUser user, List<int> roleIds) =>
      api.put('User/update/${user.id}', body: {
        'username': user.username,
        'email': user.email,
        'firstName': user.firstName,
        'lastName': user.lastName,
        'countryId': user.countryId,
        'isActive': user.isActive,
        'roleIds': roleIds,
      });

  Future<Paged<MovieSummary>> movies(
          {String? search, String? type, int? genreId, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Movie', query: {
            'NameFTS': search,
            'Type': type,
            'GenreId': genreId,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          MovieSummary.new);

  Future<MovieSummary> movie(int id) async =>
      MovieSummary((await api.get('Movie/$id')) as Map<String, dynamic>);

  Future<void> saveMovie(int? id, Map<String, dynamic> body) =>
      id == null ? api.post('Movie', body: body) : api.put('Movie/$id', body: body);

  Future<void> deleteMovie(int id) => api.delete('Movie/$id');

  Future<Paged<Person>> people(
          {String? search, String? roleType, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Person', query: {
            'NameFTS': search,
            'RoleType': roleType,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          Person.new);

  Future<void> savePerson(int? id, Map<String, dynamic> body) =>
      id == null ? api.post('Person', body: body) : api.put('Person/$id', body: body);

  Future<void> deletePerson(int id) => api.delete('Person/$id');

  Future<Paged<Studio>> studios({String? search, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Studio',
              query: {'NameFTS': search, 'PageNumber': page, 'PageSize': pageSize}),
          Studio.new);

  Future<void> saveStudio(int? id, Map<String, dynamic> body) =>
      id == null ? api.post('Studio', body: body) : api.put('Studio/$id', body: body);

  Future<void> deleteStudio(int id) => api.delete('Studio/$id');

  Future<Paged<Review>> reviews(
          {String? search,
          String? status,
          int? rating,
          DateTime? from,
          DateTime? to,
          String sortBy = 'Date',
          int page = 1,
          int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Review', query: {
            'NameFTS': search,
            'Status': status,
            'Rating': rating,
            'CreatedFrom': from?.toIso8601String(),
            'CreatedTo': to?.toIso8601String(),
            'SortBy': sortBy,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          Review.new);

  Future<void> setReviewStatus(int id, String status) =>
      api.put('Review/$id/status', body: {'status': status});

  Future<Paged<ForumThread>> threads(
          {String? search, String? status, DateTime? from, DateTime? to, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Thread', query: {
            'NameFTS': search,
            'Status': status,
            'CreatedFrom': from?.toIso8601String(),
            'CreatedTo': to?.toIso8601String(),
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          ForumThread.new);

  Future<void> setThreadStatus(int id, String status) =>
      api.put('Thread/$id/status', body: {'status': status});

  Future<void> deleteThread(int id) => api.delete('Thread/$id');

  Future<ThreadDetail> threadDetail(int id) async =>
      ThreadDetail((await api.get('Thread/$id')) as Map<String, dynamic>);

  Future<void> updateThread(ThreadDetail d, {required String title, required String content, required bool hasSpoiler}) =>
      api.put('Thread/${d.thread.id}', body: {
        'title': title,
        'content': content,
        'hasSpoiler': hasSpoiler,
        'movieId': d.movieId,
        'tags': d.thread.tags,
      });

  Future<void> updateComment(int id, String content) => api.put('Comment/$id', body: {'content': content});

  Future<void> deleteComment(int id) => api.delete('Comment/$id');

  Future<Paged<ContentReport>> reports(
          {String? status, String? targetType, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('Report', query: {
            'Status': status,
            'TargetType': targetType,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          ContentReport.new);

  Future<void> decideReport(int id,
          {required String status, required bool removeContent, String? note}) =>
      api.put('Report/$id/decision',
          body: {'status': status, 'removeContent': removeContent, 'adminNote': note});

  Future<Paged<QuizQuestion>> questions(
          {String? search, String? category, String? difficulty, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('QuizQuestion', query: {
            'NameFTS': search,
            'Category': category,
            'Difficulty': difficulty,
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          QuizQuestion.new);

  Future<void> saveQuestion(int? id, Map<String, dynamic> body) =>
      id == null ? api.post('QuizQuestion', body: body) : api.put('QuizQuestion/$id', body: body);

  Future<void> deleteQuestion(int id) => api.delete('QuizQuestion/$id');

  Future<List<Named>> reference(String controller, {String? search}) async {
    final data = await api.get(controller, query: {'NameFTS': search, 'PageSize': 100});
    return Paged.fromJson(data, Named.fromJson).items;
  }

  Future<void> saveReference(String controller, int? id, String name, {Map<String, dynamic>? extra}) {
    final body = {'name': name, ...?extra};
    return id == null ? api.post(controller, body: body) : api.put('$controller/$id', body: body);
  }

  Future<void> deleteReference(String controller, int id) => api.delete('$controller/$id');

  Future<Paged<AuditEntry>> auditLog(
          {String? entityType, String? action, DateTime? from, DateTime? to, int page = 1, int pageSize = 15}) async =>
      Paged.fromJson(
          await api.get('AuditLog', query: {
            'EntityType': entityType,
            'Action': action,
            'From': from?.toIso8601String(),
            'To': to == null ? null : DateTime(to.year, to.month, to.day, 23, 59, 59).toIso8601String(),
            'PageNumber': page,
            'PageSize': pageSize,
          }),
          AuditEntry.new);

  Future<bool> autoActivateNewUsers() async =>
      ((await api.get('AppSettings')) as Map<String, dynamic>)['autoActivateNewUsers'] == true;

  Future<void> setAutoActivate(bool value) =>
      api.put('AppSettings', body: {'autoActivateNewUsers': value});

  Future<String> downloadReport(String kind, DateTime start, DateTime end, String directory) {
    String d(DateTime v) =>
        '${v.year.toString().padLeft(4, '0')}-${v.month.toString().padLeft(2, '0')}-${v.day.toString().padLeft(2, '0')}';
    return api.download('Export/$kind', directory,
        query: {'startDate': d(start), 'endDate': d(end)}, fallbackName: '$kind.pdf');
  }
}
