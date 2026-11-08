
int _int(dynamic v) => v is num ? v.toInt() : int.tryParse('$v') ?? 0;
double _double(dynamic v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;
String _str(dynamic v) => v?.toString() ?? '';
String? _strOrNull(dynamic v) => v?.toString();
DateTime _date(dynamic v) =>
    DateTime.tryParse(_str(v)) ?? DateTime.fromMillisecondsSinceEpoch(0);
List<Map<String, dynamic>> _list(dynamic v) => (v as List? ?? const []).cast<Map<String, dynamic>>();

class Paged<T> {
  Paged(this.items, this.totalCount, this.currentPage, this.totalPages);

  final List<T> items;
  final int totalCount, currentPage, totalPages;

  bool get hasNext => currentPage < totalPages;

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

class Named {
  Named(this.id, this.name);

  Named.fromJson(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['name']);

  final int id;
  final String name;

  @override
  bool operator ==(Object other) => other is Named && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class MovieCard {
  MovieCard(Map<String, dynamic> j)
      : id = _int(j['id']),
        title = _str(j['title']),
        type = _str(j['type']),
        year = _int(j['releaseYear']),
        posterUrl = _strOrNull(j['posterUrl']),
        genres = (j['genres'] as List? ?? const []).map((e) {
          return e is Map ? _str(e['genreName']) : '$e';
        }).toList(),
        rating = _double(j['averageRating']),
        reviewCount = _int(j['reviewCount']),
        role = _strOrNull(j['roleInMovie']);

  final int id, year, reviewCount;
  final String title, type;
  final String? posterUrl;

  final String? role;
  final List<String> genres;
  final double rating;

  bool get isSeries => type == 'Series';
}

class CastMember {
  CastMember(Map<String, dynamic> j)
      : personId = _int(j['personId']),
        name = _str(j['personName']),
        role = _str(j['roleInMovie']),
        photoUrl = _strOrNull(j['photoUrl']);

  final int personId;
  final String name, role;
  final String? photoUrl;
}

class MovieDetail {
  MovieDetail(Map<String, dynamic> j)
      : card = MovieCard(j),
        originalTitle = _str(j['originalTitle']),
        duration = _int(j['duration']),
        synopsis = _str(j['synopsis']),
        directorName = _strOrNull(j['directorName']),
        directorId = j['directorId'] == null ? null : _int(j['directorId']),
        studioId = j['studioId'] == null ? null : _int(j['studioId']),
        studioName = _str(j['studioName']),
        languageName = _str(j['languageName']),
        myListType = _strOrNull(j['myListType']),
        cast = _list(j['cast']).map(CastMember.new).toList();

  final MovieCard card;
  final String originalTitle, synopsis, studioName, languageName;
  final int duration;
  final String? directorName, myListType;
  final int? directorId, studioId;
  final List<CastMember> cast;
}

class PersonInfo {
  PersonInfo(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['fullName']),
        role = _str(j['roleType']),
        birthDate = DateTime.tryParse(_str(j['birthDate'])),
        biography = _str(j['biography']),
        photoUrl = _strOrNull(j['photoUrl']);

  final int id;
  final String name, role, biography;
  final DateTime? birthDate;
  final String? photoUrl;
}

class StudioInfo {
  StudioInfo(Map<String, dynamic> j)
      : id = _int(j['id']),
        name = _str(j['name']),
        countryName = _str(j['countryName']),
        foundedYear = _int(j['foundedYear']),
        description = _str(j['description']),
        logoUrl = _strOrNull(j['logoUrl']);

  final int id, foundedYear;
  final String name, countryName, description;
  final String? logoUrl;
}

class UserListSummary {
  UserListSummary(Map<String, dynamic> j)
      : id = _int(j['id']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        name = _str(j['name']),
        itemCount = _int(j['itemCount']),
        posterUrls = (j['posterUrls'] as List? ?? const []).map((e) => '$e').toList();

  final int id, userId, itemCount;
  final String username, name;
  final List<String> posterUrls;
}

class ListEntry {
  ListEntry(Map<String, dynamic> j)
      : movie = MovieCard({
          'id': j['movieId'],
          'title': j['title'],
          'type': j['type'],
          'releaseYear': j['releaseYear'],
          'posterUrl': j['posterUrl'],
          'averageRating': j['averageRating'],
        }),
        addedAt = _date(j['addedAt']);

  final MovieCard movie;
  final DateTime addedAt;
}

class UserListDetail extends UserListSummary {
  UserListDetail(super.j) : items = _list(j['items']).map(ListEntry.new).toList();

  final List<ListEntry> items;
}

class WatchlistItem {
  WatchlistItem(Map<String, dynamic> j)
      : id = _int(j['id']),
        listType = _str(j['listType']),
        addedAt = _date(j['addedAt']),
        movie = MovieCard(j['movie'] as Map<String, dynamic>);

  final int id;
  final String listType;
  final DateTime addedAt;
  final MovieCard movie;
}

class DiscoverCard {
  DiscoverCard(Map<String, dynamic> j)
      : movie = MovieCard(j['movie'] as Map<String, dynamic>),
        reason = _str(j['reason']);

  final MovieCard movie;
  final String reason;
}

class Review {
  Review(Map<String, dynamic> j)
      : id = _int(j['id']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        userImageUrl = _strOrNull(j['userImageUrl']),
        movieId = _int(j['movieId']),
        movieTitle = _str(j['movieTitle']),
        moviePosterUrl = _strOrNull(j['moviePosterUrl']),
        rating = _int(j['rating']),
        text = _str(j['text']),
        createdAt = _date(j['createdAt']),
        status = _str(j['status']),
        helpful = _int(j['helpfulCount']),
        notHelpful = _int(j['notHelpfulCount']),
        myVote = j['myVote'] as bool?;

  final int id, userId, movieId, rating, helpful, notHelpful;
  final String username, movieTitle, text, status;
  final String? userImageUrl, moviePosterUrl;
  final DateTime createdAt;
  final bool? myVote;
}

class RatingSummary {
  RatingSummary(Map<String, dynamic> j)
      : average = _double(j['averageRating']),
        count = _int(j['reviewCount']),
        histogram = (j['histogram'] as List? ?? const []).map(_int).toList(),
        myRating = j['myRating'] == null ? null : _int(j['myRating']),
        myReviewId = j['myReviewId'] == null ? null : _int(j['myReviewId']);

  final double average;
  final int count;
  final List<int> histogram;
  final int? myRating, myReviewId;
}

class ForumComment {
  ForumComment(Map<String, dynamic> j)
      : id = _int(j['id']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        content = _str(j['content']),
        createdAt = _date(j['createdAt']),
        score = _int(j['score']),
        myVote = _int(j['myVote']);

  final int id, userId, score, myVote;
  final String username, content;
  final DateTime createdAt;
}

class ForumThread {
  ForumThread(Map<String, dynamic> j)
      : id = _int(j['id']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        title = _str(j['title']),
        content = _str(j['content']),
        hasSpoiler = j['hasSpoiler'] == true,
        status = _str(j['status']),
        createdAt = _date(j['createdAt']),
        movieId = j['movieId'] == null ? null : _int(j['movieId']),
        movieTitle = _strOrNull(j['movieTitle']),
        tags = (j['tags'] as List? ?? const []).map((e) => '$e').toList(),
        score = _int(j['score']),
        commentCount = _int(j['commentCount']),
        myVote = _int(j['myVote']),
        comments = _list(j['comments']).map(ForumComment.new).toList();

  final int id, userId, score, commentCount, myVote;
  final int? movieId;
  final String username, title, content, status;
  final String? movieTitle;
  final bool hasSpoiler;
  final DateTime createdAt;
  final List<String> tags;
  final List<ForumComment> comments;

  bool get locked => status == 'Locked';
}

class QuizQuestion {
  QuizQuestion(Map<String, dynamic> j)
      : id = _int(j['questionId']),
        text = _str(j['text']),
        options = [_str(j['optionA']), _str(j['optionB']), _str(j['optionC']), _str(j['optionD'])],
        category = _str(j['category']),
        difficulty = _str(j['difficulty']);

  final int id;
  final String text, category, difficulty;
  final List<String> options;
}

class QuizStart {
  QuizStart(Map<String, dynamic> j)
      : sessionId = _int(j['sessionId']),
        mode = _str(j['mode']),
        secondsPerQuestion = _int(j['secondsPerQuestion']),
        questions = _list(j['questions']).map(QuizQuestion.new).toList();

  final int sessionId, secondsPerQuestion;
  final String mode;
  final List<QuizQuestion> questions;
}

class QuizAnswerResult {
  QuizAnswerResult(Map<String, dynamic> j)
      : questionId = _int(j['questionId']),
        isCorrect = j['isCorrect'] == true,
        correctOption = _str(j['correctOption']),
        points = _int(j['points']);

  final int questionId, points;
  final bool isCorrect;
  final String correctOption;
}

class QuizResult {
  QuizResult(Map<String, dynamic> j)
      : score = _int(j['score']),
        correctCount = _int(j['correctCount']),
        questionCount = _int(j['questionCount']),
        streak = _int(j['currentStreak']),
        answers = _list(j['answers']).map(QuizAnswerResult.new).toList();

  final int score, correctCount, questionCount, streak;
  final List<QuizAnswerResult> answers;
}

class Streak {
  Streak(Map<String, dynamic> j)
      : current = _int(j['currentStreak']),
        longest = _int(j['longestStreak']),
        playedToday = j['playedToday'] == true;

  final int current, longest;
  final bool playedToday;
}

class LeaderboardEntry {
  LeaderboardEntry(Map<String, dynamic> j)
      : rank = _int(j['rank']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        imageUrl = _strOrNull(j['profileImageUrl']),
        score = _int(j['totalScore']),
        sessions = _int(j['sessionsPlayed']),
        streak = _int(j['currentStreak']);

  final int rank, userId, score, sessions, streak;
  final String username;
  final String? imageUrl;
}

class Leaderboard {
  Leaderboard(Map<String, dynamic> j)
      : entries = _list(j['entries']).map(LeaderboardEntry.new).toList(),
        me = j['me'] == null ? null : LeaderboardEntry(j['me'] as Map<String, dynamic>);

  final List<LeaderboardEntry> entries;
  final LeaderboardEntry? me;
}

class PublicProfile {
  PublicProfile(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        firstName = _str(j['firstName']),
        lastName = _str(j['lastName']),
        imageUrl = _strOrNull(j['profileImageUrl']),
        bio = _str(j['bio']),
        city = _str(j['city']),
        isFollowed = j['isFollowedByCurrentUser'] == true,
        followers = _int(j['followersCount']),
        following = _int(j['followingCount']),
        rated = _int(j['ratedMoviesCount']),
        threads = _int(j['threadsCount']),
        comments = _int(j['commentsCount']),
        streak = _int(j['currentStreak']);

  final int id, followers, following, rated, threads, comments, streak;
  final String username, firstName, lastName, bio, city;
  final String? imageUrl;
  final bool isFollowed;

  String get displayName {
    final full = '$firstName $lastName'.trim();
    return full.isEmpty ? username : full;
  }
}

class MyProfile {
  MyProfile(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        firstName = _str(j['firstName']),
        lastName = _str(j['lastName']),
        bio = _str(j['bio']),
        city = _str(j['city']),
        cityId = j['cityId'] == null ? null : _int(j['cityId']),
        phone = _str(j['phone']),
        dateOfBirth = j['dateOfBirth'] == null ? null : DateTime.tryParse(_str(j['dateOfBirth'])),
        imageUrl = _strOrNull(j['profileImageUrl']),
        rated = _int(j['ratedMoviesCount']),
        threads = _int(j['threadsCount']),
        comments = _int(j['commentsCount']),
        streak = _int(j['currentStreak']),
        followers = _int(j['followersCount']),
        following = _int(j['followingCount']),
        isPublic = j['isProfilePublic'] != false,
        pushEnabled = j['pushNotificationsEnabled'] != false,
        emailEnabled = j['emailNotificationsEnabled'] != false;

  final int id, rated, threads, comments, streak, followers, following;
  final String username, firstName, lastName, bio, city, phone;
  final DateTime? dateOfBirth;
  final String? imageUrl;
  final int? cityId;
  final bool isPublic, pushEnabled, emailEnabled;
}

class MyAccount {
  MyAccount(Map<String, dynamic> j)
      : username = _str(j['username']),
        email = _str(j['email']),
        firstName = _str(j['firstName']),
        lastName = _str(j['lastName']),
        countryId = j['country'] is Map ? _int((j['country'] as Map)['id']) : 0;

  MyAccount.edited(MyAccount base, {required this.firstName, required this.lastName, required this.email})
      : username = base.username,
        countryId = base.countryId;

  final String username, email, firstName, lastName;
  final int countryId;
}

class FollowUser {
  FollowUser(Map<String, dynamic> j)
      : userId = _int(j['userId']),
        username = _str(j['username']),
        imageUrl = _strOrNull(j['profileImageUrl']),
        isFollowed = j['isFollowedByCurrentUser'] == true;

  final int userId;
  final String username;
  final String? imageUrl;
  final bool isFollowed;
}

class UserHit {
  UserHit(Map<String, dynamic> j)
      : id = _int(j['id']),
        username = _str(j['username']),
        name = '${_str(j['firstName'])} ${_str(j['lastName'])}'.trim(),
        imageUrl = _strOrNull(j['profileImageUrl']),
        isFollowed = j['isFollowedByCurrentUser'] == true;

  final int id;
  final String username, name;
  final String? imageUrl;
  final bool isFollowed;
}

class FeedItem {
  FeedItem(Map<String, dynamic> j)
      : type = _str(j['type']),
        userId = _int(j['userId']),
        username = _str(j['username']),
        movieId = j['movieId'] == null ? null : _int(j['movieId']),
        movieTitle = _strOrNull(j['movieTitle']),
        posterUrl = _strOrNull(j['moviePosterUrl']),
        rating = j['rating'] == null ? null : _int(j['rating']),
        text = _strOrNull(j['text']),
        threadId = j['threadId'] == null ? null : _int(j['threadId']),
        listId = j['listId'] == null ? null : _int(j['listId']),
        createdAt = _date(j['createdAt']);

  final String type, username;
  final int userId;
  final int? movieId, rating, threadId, listId;
  final String? movieTitle, posterUrl, text;
  final DateTime createdAt;
}

class Upcoming {
  Upcoming(Map<String, dynamic> j)
      : tmdbId = _int(j['tmdbId']),
        title = _str(j['title']),
        overview = _str(j['overview']),
        posterUrl = _strOrNull(j['posterUrl']),
        releaseDate = _date(j['releaseDate']),
        daysUntil = _int(j['daysUntilRelease']),
        hasReminder = j['hasReminder'] == true;

  final int tmdbId, daysUntil;
  final String title, overview;
  final String? posterUrl;
  final DateTime releaseDate;
  final bool hasReminder;
}

class Reminder {
  Reminder(Map<String, dynamic> j)
      : id = _int(j['id']),
        tmdbId = _int(j['tmdbId']),
        title = _str(j['title']),
        posterUrl = _strOrNull(j['posterUrl']),
        releaseDate = _date(j['releaseDate']),
        daysUntil = _int(j['daysUntilRelease']);

  final int id, tmdbId, daysUntil;
  final String title;
  final String? posterUrl;
  final DateTime releaseDate;
}

class AppNotification {
  AppNotification(Map<String, dynamic> j)
      : id = _int(j['id']),
        title = _str(j['title']),
        body = _str(j['body']),
        createdAt = _date(j['createdAt']),
        seen = j['seenAt'] != null;

  final int id;
  final String title, body;
  final DateTime createdAt;
  final bool seen;
}
