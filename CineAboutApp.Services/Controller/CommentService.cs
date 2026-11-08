using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Enums;
using CineAboutApp.Models.Requests.Thread;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class CommentService(ApplicationDbContext db, IIdentityService identity, IUserNotifier notifier) : ICommentService
    {
        public static CommentResponse ToResponse(Comment c, int? currentUserId) => new()
        {
            Id = c.Id,
            ThreadId = c.ThreadId,
            UserId = c.UserId,
            Username = c.User?.Username ?? string.Empty,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            Score = c.CommentVotes.Count(v => v.IsUpvote) - c.CommentVotes.Count(v => !v.IsUpvote),
            MyVote = currentUserId == null
                ? 0
                : c.CommentVotes.Where(v => v.UserId == currentUserId).Select(v => v.IsUpvote ? 1 : -1).FirstOrDefault()
        };

        public async Task<CommentResponse> CreateAsync(CommentInsertRequest request, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            var thread = await db.Threads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken);
            if (thread == null || thread.Status == ThreadStatus.Removed)
                throw new NotFoundException("Diskusija", request.ThreadId);

            if (thread.Status == ThreadStatus.Locked)
                throw new BusinessException("Diskusija je zaključana i nije moguće dodavati komentare.");

            var comment = new Comment
            {
                ThreadId = request.ThreadId,
                UserId = userId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            db.Comments.Add(comment);
            await db.SaveChangesAsync(cancellationToken);

            if (thread.UserId != userId)
            {
                var author = await db.MyAppUsers.AsNoTracking()
                    .Where(u => u.Id == userId).Select(u => u.Username).FirstAsync(cancellationToken);
                await notifier.NotifyAsync(
                    [thread.UserId],
                    "Novi komentar",
                    $"{author} je komentarisao/la vašu diskusiju '{thread.Title}'.",
                    new Dictionary<string, string> { ["type"] = "comment", ["threadId"] = thread.Id.ToString() },
                    cancellationToken);
            }

            return await LoadAsync(comment.Id, cancellationToken);
        }

        public async Task<CommentResponse> UpdateAsync(int id, CommentUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var comment = await db.Comments.Include(c => c.Thread).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("Komentar", id);

            EnsureOwnerOrAdmin(comment.UserId);
            if (!identity.IsAdmin() && comment.Thread.Status != ThreadStatus.Active)
                throw new BusinessException("Komentare u zaključanoj diskusiji nije moguće uređivati.");

            comment.Content = request.Content.Trim();
            await db.SaveChangesAsync(cancellationToken);
            return await LoadAsync(id, cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("Komentar", id);

            EnsureOwnerOrAdmin(comment.UserId);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.CommentVotes.Where(v => v.CommentId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Reports
                .Where(r => r.TargetType == ReportTargetType.Comment && r.TargetId == id)
                .ExecuteDeleteAsync(cancellationToken);
            db.Comments.Remove(comment);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<CommentResponse> VoteAsync(int id, bool isUpvote, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            if (!await db.Comments.AnyAsync(c => c.Id == id, cancellationToken))
                throw new NotFoundException("Komentar", id);

            var vote = await db.CommentVotes.FirstOrDefaultAsync(v => v.CommentId == id && v.UserId == userId, cancellationToken);
            if (vote == null)
                db.CommentVotes.Add(new CommentVote { CommentId = id, UserId = userId, IsUpvote = isUpvote });
            else
                vote.IsUpvote = isUpvote;

            await db.SaveChangesAsync(cancellationToken);
            return await LoadAsync(id, cancellationToken);
        }

        public async Task<CommentResponse> RemoveVoteAsync(int id, CancellationToken cancellationToken = default)
        {
            var userId = identity.GetCurrentUserId();
            await db.CommentVotes
                .Where(v => v.CommentId == id && v.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            return await LoadAsync(id, cancellationToken);
        }

        private async Task<CommentResponse> LoadAsync(int id, CancellationToken ct)
        {
            var comment = await db.Comments.AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.CommentVotes)
                .FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new NotFoundException("Komentar", id);

            return ToResponse(comment, identity.GetCurrentUserIdNullable());
        }

        private void EnsureOwnerOrAdmin(int ownerId)
        {
            if (!identity.IsAdmin() && identity.GetCurrentUserId() != ownerId)
                throw new UnauthorizedAccessException("Možete mijenjati samo vlastite komentare.");
        }
    }
}
