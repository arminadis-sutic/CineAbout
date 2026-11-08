import 'package:flutter/painting.dart';

import 'theme.dart';

int _int(dynamic v) => v is num ? v.toInt() : int.tryParse('$v') ?? 0;
double _double(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
String _str(dynamic v) => v?.toString() ?? '';
DateTime _date(dynamic v) =>
    DateTime.tryParse(_str(v))?.toLocal() ?? DateTime.fromMillisecondsSinceEpoch(0);
DateTime? _dateOrNull(dynamic v) =>
    v == null ? null : DateTime.tryParse(_str(v))?.toLocal();
List<Map<String, dynamic>> _list(dynamic v) =>
    (v as List? ?? const []).cast<Map<String, dynamic>>();

class Paged<T> {
  Paged(this.items, this.totalCount, this.currentPage, this.totalPages);

  final List<T> items;
  final int totalCount;
  final int currentPage;
  final int totalPages;

  factory Paged.fromJson(dynamic json, T Function(Map<String, dynamic>) parse) {
    final map = json as Map<String, dynamic>;
    return Paged(
      _list(map['dataItems']).map(parse).toList(),
      _int(map['totalCount']),
      _int(map['currentPage']),
      _int(map['totalPages']),
    );
  }
}

class AuditEntry {
  AuditEntry(Map<String, dynamic> j)
      : entityType = _str(j['entityType']),
        action = _str(j['action']),
        oldValues = j['oldValuesJson']?.toString(),
        newValues = j['newValuesJson']?.toString(),
        username = j['performedByUsername']?.toString(),
        ipAddress = j['ipAddress']?.toString(),
        performedAt = _date(j['performedAt']);

  final String entityType, action;
  final String? oldValues, newValues, username, ipAddress;
  final DateTime performedAt;

  String get actionLabel => switch (action) {
        'Create' => 'Created',
        'Update' => 'Changed',
        'Delete' => 'Deleted',
        _ => action,
      };

  Color get actionColor => switch (action) {
        'Create' => AppColors.green,
        'Delete' => AppColors.red,
        _ => AppColors.amber,
      };
}

class Kpis {
  Kpis(Map<String, dynamic> j)
      : totalUsers = _int(j['totalUsers']),
        newUsers = _int(j['newUsersLast7Days']),
        totalReviews = _int(j['totalReviews']),
        activeThreads = _int(j['activeThreads']),
        totalMovies = _int(j['totalMovies']),
        quizSessions = _int(j['totalQuizSessions']),
        usersTrend = _int(j['usersTrendPercent']),
        reviewsTrend = _int(j['reviewsTrendPercent']),
        threadsTrend = _int(j['threadsTrendPercent']),
        quizTrend = _int(j['quizSessionsTrendPercent']);

  final int totalUsers,
      newUsers,
      totalReviews,
      activeThreads,
      totalMovies,
      quizSessions,
      usersTrend,
      reviewsTrend,
      threadsTrend,
      quizTrend;
}

class ActivityPoint {
  ActivityPoint(Map<String, dynamic> j)
      : date = _date(j['date']),
        newUsers = _int(j['newUsers']),
        reviews = _int(j['reviews']),
        threads = _int(j['threads']),
        comments = _int(j['comments']),
        quizSessions = _int(j['quizSessions']);

  final DateTime date;
  final int newUsers, reviews, threads, comments, quizSessions;
}

class PopularMovie {
  PopularMovie(Map<String, dynamic> j)
      : movieId = _int(j['movieId']),
        title = _str(j['title']),
        posterUrl = _str(j['posterUrl']),
        reviewCount = _int(j['reviewCount']),
        averageRating = _double(j['averageRating']),
        watchlistCount = _int(j['watchlistCount']),
        interestedCount = _int(j['interestedCount']);

  final int movieId, reviewCount, watchlistCount, interestedCount;
  final String title, posterUrl;
  final double averageRating;
}

class ActiveThread {
  ActiveThread(Map<String, dynamic> j)
      : id = _int(j['threadId']),
        title = _str(j['title']),
        author = _str(j['authorUsername']),
        createdAt = _date(j['createdAt']),
        commentCount = _int(j['commentCount']),
        voteCount = _int(j['voteCount']);

  final int id, commentCount, voteCount;
  final String title, author;
  final DateTime createdAt;
}

class Named {
  Named(this.id, this.name, {this.countryId, this.countryName});

  Named.fromJson(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['name']),
        countryId = j['countryId'] == null ? null : _int(j['countryId']),
        countryName = j['countryName'] == null ? null : _str(j['countryName']);

  final int id;
  final String name;

  final int? countryId;
  final String? countryName;

  @override
  bool operator ==(Object other) => other is Named && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class RoleInfo {
  RoleInfo(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['name']);

  final int id;
  final String name;
}

class AppUser {
  AppUser(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        firstName = _str(j['firstName']),
        lastName = _str(j['lastName']),
        email = _str(j['email']),
        isActive = j['isActive'] == true,
        registrationDate = _date(j['registrationDate']),
        imageUrl = j['profileImageUrl']?.toString(),
        countryId = j['country'] is Map ? _int((j['country'] as Map)['id']) : 0,
        roles = _list(j['roles']).map(RoleInfo.new).toList();

  final int id, countryId;
  final String username, firstName, lastName, email;
  final bool isActive;
  final DateTime registrationDate;
  final String? imageUrl;
  final List<RoleInfo> roles;

  String get fullName => '$firstName $lastName'.trim().isEmpty
      ? username
      : '$firstName $lastName'.trim();
  bool get isAdmin => roles.any((r) => r.name == 'Admin');
}

class UserActivity {
  UserActivity(Map<String, dynamic> j)
      : rated = _int(j['ratedMoviesCount']),
        reviews = _int(j['reviewsCount']),
        threads = _int(j['threadsCount']),
        comments = _int(j['commentsCount']),
        streak = _int(j['currentStreak']),
        followers = _int(j['followersCount']);

  final int rated, reviews, threads, comments, streak, followers;
}

class MovieSummary {
  MovieSummary(Map<String, dynamic> j)
      : id = _int(j['id']),
        title = _str(j['title']),
        originalTitle = _str(j['originalTitle']),
        type = _str(j['type']),
        releaseYear = _int(j['releaseYear']),
        duration = _int(j['duration']),
        synopsis = _str(j['synopsis']),
        posterUrl = _str(j['posterUrl']),
        isVisible = j['isVisible'] != false,
        languageId = _int(j['languageId']),
        studioId = _int(j['studioId']),
        studioName = _str(j['studioName']),
        averageRating = _double(j['averageRating']),
        reviewCount = _int(j['reviewCount']),
        directorId = j['directorId'] == null ? null : _int(j['directorId']),
        directorName = j['directorName']?.toString(),
        genres = _list(j['genres'])
            .map((g) => Named(_int(g['genreId']), _str(g['genreName'])))
            .toList(),
        cast = _list(j['cast'])
            .map((c) => CastEntry(
                _int(c['personId']), _str(c['personName']), _str(c['roleInMovie'])))
            .toList();

  final int id, releaseYear, duration, languageId, studioId, reviewCount;
  final int? directorId;
  final String title, originalTitle, type, synopsis, posterUrl, studioName;
  final String? directorName;
  final bool isVisible;
  final double averageRating;
  final List<Named> genres;
  final List<CastEntry> cast;
}

class CastEntry {
  CastEntry(this.personId, this.personName, this.role);

  final int personId;
  final String personName;
  final String role;
}

class Person {
  Person(Map<String, dynamic> j)
      : id = _int(j['id']),
        fullName = _str(j['fullName']),
        roleType = _str(j['roleType']),
        birthDate = _date(j['birthDate']),
        biography = _str(j['biography']),
        photoUrl = _str(j['photoUrl']);

  final int id;
  final String fullName, roleType, biography, photoUrl;
  final DateTime birthDate;
}

class Studio {
  Studio(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['name']),
        countryId = _int(j['countryId']),
        countryName = _str(j['countryName']),
        foundedYear = _int(j['foundedYear']),
        description = _str(j['description']),
        logoUrl = _str(j['logoUrl']);

  final int id, countryId, foundedYear;
  final String name, countryName, description, logoUrl;
}

class QuizQuestion {
  QuizQuestion(Map<String, dynamic> j)
      : id = _int(j['id']),
        text = _str(j['text']),
        options = [
          _str(j['optionA']),
          _str(j['optionB']),
          _str(j['optionC']),
          _str(j['optionD']),
        ],
        correct = _str(j['correctOption']),
        category = _str(j['category']),
        categoryId = _int(j['categoryId']),
        difficulty = _str(j['difficulty']);

  final int id, categoryId;
  final String text, correct, category, difficulty;
  final List<String> options;
}

class Review {
  Review(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        movieTitle = _str(j['movieTitle']),
        rating = _int(j['rating']),
        text = _str(j['text']),
        createdAt = _date(j['createdAt']),
        status = _str(j['status']),
        helpful = _int(j['helpfulCount']),
        notHelpful = _int(j['notHelpfulCount']);

  final int id, rating, helpful, notHelpful;
  final String username, movieTitle, text, status;
  final DateTime createdAt;
}

class ForumThread {
  ForumThread(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        title = _str(j['title']),
        content = _str(j['content']),
        hasSpoiler = j['hasSpoiler'] == true,
        status = _str(j['status']),
        createdAt = _date(j['createdAt']),
        movieTitle = j['movieTitle']?.toString(),
        tags = (j['tags'] as List? ?? const []).map((e) => '$e').toList(),
        score = _int(j['score']),
        commentCount = _int(j['commentCount']);

  final int id, score, commentCount;
  final String username, title, content, status;
  final bool hasSpoiler;
  final DateTime createdAt;
  final String? movieTitle;
  final List<String> tags;
}

class ThreadComment {
  ThreadComment(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        content = _str(j['content']),
        createdAt = _date(j['createdAt']),
        score = _int(j['score']);

  final int id, score;
  final String username, content;
  final DateTime createdAt;
}

class ThreadDetail {
  ThreadDetail(Map<String, dynamic> j)
      : thread = ForumThread(j),
        movieId = j['movieId'] == null ? null : _int(j['movieId']),
        comments = (j['comments'] as List? ?? const [])
            .map((e) => ThreadComment(e as Map<String, dynamic>))
            .toList();

  final ForumThread thread;
  final int? movieId;
  final List<ThreadComment> comments;
}

class ContentReport {
  ContentReport(Map<String, dynamic> j)
      : id = _int(j['id']),
        reporter = _str(j['reporterUsername']),
        targetType = _str(j['targetType']),
        targetId = _int(j['targetId']),
        preview = _str(j['targetPreview']),
        reason = _str(j['reason']),
        status = _str(j['status']),
        createdAt = _date(j['createdAt']),
        reviewedBy = j['reviewedByUsername']?.toString(),
        reviewedAt = _dateOrNull(j['reviewedAt']),
        adminNote = j['adminNote']?.toString();

  final int id, targetId;
  final String reporter, targetType, preview, reason, status;
  final DateTime createdAt;
  final String? reviewedBy, adminNote;
  final DateTime? reviewedAt;
}
