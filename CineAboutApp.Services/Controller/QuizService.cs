using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Services.General;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Quiz;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class QuizService(ApplicationDbContext db, IIdentityService identity) : IQuizService
    {
        public const int SecondsPerQuestion = 20;
        private const int DailyQuestionCount = 5;
        private const int BasePoints = 100;
        private const int MaxSpeedBonus = 50;
        private const double ClockToleranceSeconds = 5;

        public async Task<QuizStartResponse> StartDailyAsync(CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var today = DateTime.UtcNow.Date;

            var todaysSessions = await db.QuizSessions
                .Where(s => s.UserId == userId && s.Mode == QuizMode.Daily && s.PlayedAt >= today)
                .OrderByDescending(s => s.PlayedAt)
                .ToListAsync(cancellationToken);

            if (todaysSessions.Any(s => s.IsCompleted))
                throw new BusinessException("Dnevni kviz ste već odigrali danas. Vratite se sutra za novi.");

            var open = todaysSessions.FirstOrDefault();
            if (open != null)
                return await BuildStartResponseAsync(open, cancellationToken);

            var allIds = await db.QuizQuestions.AsNoTracking().OrderBy(q => q.Id).Select(q => q.Id).ToListAsync(cancellationToken);
            if (allIds.Count == 0)
                throw new BusinessException("Trenutno nema dostupnih kviz pitanja.");

            var random = new Random(today.Year * 10000 + today.Month * 100 + today.Day);
            var picked = allIds.OrderBy(_ => random.Next()).Take(DailyQuestionCount).ToList();

            return await CreateSessionAsync(userId, QuizMode.Daily, picked, cancellationToken);
        }

        public async Task<QuizStartResponse> StartFreeAsync(FreeQuizRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            var query = db.QuizQuestions.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim().ToLower();
                query = query.Where(q => q.Category!.Name.ToLower() == category);
            }

            if (request.Difficulty.HasValue)
                query = query.Where(q => q.Difficulty == request.Difficulty.Value);

            var picked = await query
                .OrderBy(_ => Guid.NewGuid())
                .Take(request.Count)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (picked.Count == 0)
                throw new BusinessException("Nema pitanja za odabranu kategoriju i težinu.");

            return await CreateSessionAsync(userId, QuizMode.Free, picked, cancellationToken);
        }

        private async Task<QuizStartResponse> CreateSessionAsync(
            int userId, QuizMode mode, List<int> questionIds, CancellationToken ct)
        {
            var session = new QuizSession
            {
                UserId = userId,
                Mode = mode,
                PlayedAt = DateTime.UtcNow,
                Score = 0,
                IsCompleted = false
            };

            foreach (var questionId in questionIds)
                session.QuizSessionAnswers.Add(new QuizSessionAnswer { QuizQuestionId = questionId, SelectedOption = string.Empty });

            db.QuizSessions.Add(session);
            await db.SaveChangesAsync(ct);

            return await BuildStartResponseAsync(session, ct);
        }

        private async Task<QuizStartResponse> BuildStartResponseAsync(QuizSession session, CancellationToken ct)
        {
            var questions = await db.QuizSessionAnswers.AsNoTracking()
                .Where(a => a.QuizSessionId == session.Id)
                .OrderBy(a => a.Id)
                .Select(a => new QuizPlayQuestionResponse
                {
                    QuestionId = a.QuizQuestionId,
                    Text = a.QuizQuestion.Text,
                    OptionA = a.QuizQuestion.OptionA,
                    OptionB = a.QuizQuestion.OptionB,
                    OptionC = a.QuizQuestion.OptionC,
                    OptionD = a.QuizQuestion.OptionD,
                    Category = a.QuizQuestion.Category!.Name,
                    Difficulty = a.QuizQuestion.Difficulty
                })
                .ToListAsync(ct);

            return new QuizStartResponse
            {
                SessionId = session.Id,
                Mode = session.Mode,
                SecondsPerQuestion = SecondsPerQuestion,
                Questions = questions
            };
        }

        public async Task<QuizResultResponse> SubmitAsync(QuizSubmitRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();

            var session = await db.QuizSessions
                .Include(s => s.QuizSessionAnswers).ThenInclude(a => a.QuizQuestion)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("Kviz sesija", request.SessionId);

            if (session.IsCompleted)
                throw new BusinessException("Ovaj kviz je već predat.");

            var submitted = request.Answers;
            if (submitted.Select(a => a.QuestionId).Distinct().Count() != submitted.Count)
                throw new BusinessException("Odgovori sadrže duplicirana pitanja.");

            var sessionQuestionIds = session.QuizSessionAnswers.Select(a => a.QuizQuestionId).ToHashSet();
            if (submitted.Any(a => !sessionQuestionIds.Contains(a.QuestionId)))
                throw new BusinessException("Odgovori sadrže pitanje koje nije dio ovog kviza.");

            var elapsedSeconds = (DateTime.UtcNow - session.PlayedAt).TotalSeconds;
            if (submitted.Sum(a => a.ResponseSeconds) > elapsedSeconds + ClockToleranceSeconds)
                throw new BusinessException("Vrijeme odgovora nije ispravno.");

            var answeredCount = submitted.Count;
            var unanswered = session.QuizSessionAnswers.Count - answeredCount;
            var claimedTotal = submitted.Sum(a => a.ResponseSeconds);
            var serverTotal = Math.Max(0, elapsedSeconds - unanswered * SecondsPerQuestion);
            var timeScale = 1.0;
            var evenSeconds = (double?)null;
            if (answeredCount > 0 && claimedTotal < serverTotal - ClockToleranceSeconds)
            {
                if (claimedTotal > 0) timeScale = serverTotal / claimedTotal;
                else evenSeconds = serverTotal / answeredCount;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var result = new QuizResultResponse { SessionId = session.Id, QuestionCount = session.QuizSessionAnswers.Count };
            foreach (var answer in session.QuizSessionAnswers.OrderBy(a => a.Id))
            {
                var given = submitted.FirstOrDefault(a => a.QuestionId == answer.QuizQuestionId);
                var option = (given?.SelectedOption ?? string.Empty).Trim().ToUpperInvariant();
                var seconds = given == null ? SecondsPerQuestion : evenSeconds ?? given.ResponseSeconds * timeScale;
                var inTime = given != null && seconds <= SecondsPerQuestion;
                var correct = inTime && option.Length > 0
                    && string.Equals(option, answer.QuizQuestion.CorrectOption, StringComparison.OrdinalIgnoreCase);

                var points = correct
                    ? BasePoints + (int)Math.Round(MaxSpeedBonus * (1 - Math.Clamp(seconds, 0, SecondsPerQuestion) / SecondsPerQuestion))
                    : 0;

                answer.SelectedOption = option;
                answer.IsCorrect = correct;
                answer.ResponseSeconds = Math.Min(seconds, SecondsPerQuestion);

                result.Score += points;
                if (correct)
                    result.CorrectCount++;

                result.Answers.Add(new QuizAnswerResultResponse
                {
                    QuestionId = answer.QuizQuestionId,
                    IsCorrect = correct,
                    CorrectOption = answer.QuizQuestion.CorrectOption,
                    Points = points
                });
            }

            session.Score = result.Score;
            session.IsCompleted = true;

            if (session.Mode == QuizMode.Daily)
                await UpdateStreakAsync(userId, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var streak = await db.Streaks.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            result.CurrentStreak = EffectiveStreak(streak);
            return result;
        }

        private async Task UpdateStreakAsync(int userId, CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;
            var streak = await db.Streaks.FirstOrDefaultAsync(s => s.UserId == userId, ct);

            if (streak == null)
            {
                db.Streaks.Add(new Streak { UserId = userId, CurrentStreak = 1, LongestStreak = 1, LastPlayedDate = today });
                return;
            }

            var last = streak.LastPlayedDate.Date;
            if (last == today)
                return;

            streak.CurrentStreak = last == today.AddDays(-1) ? streak.CurrentStreak + 1 : 1;
            streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);
            streak.LastPlayedDate = today;
        }

        private static int EffectiveStreak(Streak? streak) => StreakRules.Effective(streak);

        public async Task<StreakResponse> GetStreakAsync(int? userId, CancellationToken cancellationToken = default)
        {
            var currentUserId = identity.GetCurrentUserId();
            var targetId = userId ?? currentUserId;

            if (targetId != currentUserId && !identity.IsAdmin())
            {
                var isPublic = await db.UserProfiles.AsNoTracking()
                    .Where(p => p.UserId == targetId)
                    .Select(p => (bool?)p.IsProfilePublic)
                    .FirstOrDefaultAsync(cancellationToken);
                if (isPublic != true)
                    throw new NotFoundException("Profil nije pronađen ili je privatan.");
            }

            var streak = await db.Streaks.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == targetId, cancellationToken);
            return new StreakResponse
            {
                CurrentStreak = EffectiveStreak(streak),
                LongestStreak = streak?.LongestStreak ?? 0,
                PlayedToday = streak != null && streak.LastPlayedDate.Date == DateTime.UtcNow.Date
            };
        }

        public async Task<LeaderboardResponse> GetLeaderboardAsync(
            LeaderboardPeriod period, int count, CancellationToken cancellationToken = default)
        {
            count = Math.Clamp(count, 1, 100);
            var today = DateTime.UtcNow.Date;
            var from = period switch
            {
                LeaderboardPeriod.Daily => today,
                LeaderboardPeriod.Weekly => today.AddDays(-6),
                _ => DateTime.MinValue
            };

            var totals = db.QuizSessions.AsNoTracking()
                .Where(s => s.IsCompleted && s.PlayedAt >= from && s.User.IsActive)
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(s => s.Score), Sessions = g.Count() });

            var top = await totals
                .OrderByDescending(t => t.Total)
                .ThenBy(t => t.UserId)
                .Take(count)
                .ToListAsync(cancellationToken);

            var currentUserId = identity.GetCurrentUserIdNullable();
            var userIds = top.Select(t => t.UserId).ToList();
            if (currentUserId != null)
                userIds.Add(currentUserId.Value);

            var users = await db.MyAppUsers.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    Image = u.UserProfile != null ? u.UserProfile.ProfileImageUrl : null,
                    u.Streak
                })
                .ToListAsync(cancellationToken);
            var userMap = users.ToDictionary(u => u.Id);

            LeaderboardEntryResponse ToEntry(int rank, int userId, int total, int sessions) => new()
            {
                Rank = rank,
                UserId = userId,
                Username = userMap[userId].Username,
                ProfileImageUrl = userMap[userId].Image,
                TotalScore = total,
                SessionsPlayed = sessions,
                CurrentStreak = EffectiveStreak(userMap[userId].Streak)
            };

            var response = new LeaderboardResponse
            {
                Entries = top.Select((t, index) => ToEntry(index + 1, t.UserId, t.Total, t.Sessions)).ToList()
            };

            if (currentUserId != null)
            {
                var mine = await totals.FirstOrDefaultAsync(t => t.UserId == currentUserId, cancellationToken);
                if (mine != null)
                {
                    var better = await totals.CountAsync(t => t.Total > mine.Total, cancellationToken);
                    response.Me = ToEntry(better + 1, mine.UserId, mine.Total, mine.Sessions);
                }
            }

            return response;
        }

        public async Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return await db.QuizCategories.AsNoTracking()
                .Where(c => c.Questions.Any())
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
