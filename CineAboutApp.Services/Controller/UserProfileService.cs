using Microsoft.AspNetCore.Hosting;
using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Services.General;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.Responses.User;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.Validators;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    internal static class ProfileImageRules
    {
        public const int MaxBytes = 2 * 1024 * 1024;
        public const string AllowedExtensions = ".jpg,.jpeg,.png,.webp";
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

        public static void Validate(IFormFile file)
        {
            if (file.Length <= 0)
            {
                throw new UserException("Slikovna datoteka je prazna.");
            }

            if (file.Length > MaxBytes)
            {
                throw new UserException($"Slika može imati najviše {MaxBytes / (1024 * 1024)} MB.");
            }

            if (!AllowedContentTypes.Contains((file.ContentType ?? string.Empty).Trim().ToLowerInvariant()))
            {
                throw new UserException("Dozvoljeni formati slike su JPG, PNG i WEBP.");
            }
        }
    }

    public class UserProfileService(
        ApplicationDbContext db,
        IMapper mapper,
        IFileHandler<IFormFile> fileHandler,
        IIdentityService identityService,
        IPendingFileCleanup fileCleanup,
        IWebHostEnvironment webRoot) : IUserProfileService
    {
        public async Task<UserProfileResponse?> GetMyProfileAsync(CancellationToken ct = default)
        {
            int userId = identityService.GetCurrentUserId();

            var profile = await db.UserProfiles
                .Include(up => up.User)
                .Include(up => up.City)
                .FirstOrDefaultAsync(up => up.UserId == userId, ct);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                db.UserProfiles.Add(profile);
                await db.SaveChangesAsync(ct);
                await db.Entry(profile).Reference(p => p.User).LoadAsync(ct);
            }

            var response = mapper.Map<UserProfileResponse>(profile);
            response.CityId = profile.CityId;
            response.City = profile.City?.Name;
            response.Id = profile.UserId;
            response.Username = profile.User?.Username ?? string.Empty;
            response.FirstName = profile.User?.FirstName;
            response.LastName = profile.User?.LastName;
            response.LastActivity = profile.User?.LastActivity;

            response.RatedMoviesCount = await db.Reviews.CountAsync(r => r.UserId == userId, ct);
            response.ReviewsCount = await db.Reviews.CountAsync(r => r.UserId == userId && r.Text != "", ct);
            response.ThreadsCount = await db.Threads.CountAsync(t => t.UserId == userId, ct);
            response.CommentsCount = await db.Comments.CountAsync(c => c.UserId == userId, ct);
            response.CurrentStreak = await GetEffectiveStreakAsync(userId, ct);
            response.FollowersCount = await db.Set<Follow>().CountAsync(f => f.FollowingId == userId, ct);
            response.FollowingCount = await db.Set<Follow>().CountAsync(f => f.FollowerId == userId, ct);

            return response;
        }

        public async Task<UserProfileResponse> UpdateProfileAsync(UserProfileUpdateRequest request, CancellationToken ct = default)
        {
            int userId = identityService.GetCurrentUserId();

            var validation = await new UserProfileUpdateValidator().ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                throw new UserException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage).Distinct()));
            }

            var profile = await db.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId, ct);
            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                db.UserProfiles.Add(profile);
            }

            if (IsSet(request.Bio)) profile.Bio = request.Bio;
            if (request.CityId.HasValue)
            {
                if (request.CityId.Value == 0)
                {
                    profile.CityId = null;
                }
                else if (await db.Cities.AnyAsync(c => c.Id == request.CityId.Value, ct))
                {
                    profile.CityId = request.CityId.Value;
                }
                else
                {
                    throw new UserException("Odabrani grad ne postoji.");
                }
            }
            if (IsSet(request.Phone)) profile.Phone = request.Phone;
            if (request.DateOfBirth.HasValue) profile.DateOfBirth = request.DateOfBirth;
            if (request.IsProfilePublic.HasValue) profile.IsProfilePublic = request.IsProfilePublic.Value;
            if (request.IsPhonePublic.HasValue) profile.IsPhonePublic = request.IsPhonePublic.Value;
            if (request.IsEmailPublic.HasValue) profile.IsEmailPublic = request.IsEmailPublic.Value;
            if (request.EmailNotificationsEnabled.HasValue) profile.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
            if (request.PushNotificationsEnabled.HasValue) profile.PushNotificationsEnabled = request.PushNotificationsEnabled.Value;

            string? newFile = null;
            var oldPath = ProfileImages.ToRelativePath(profile.ProfileImageUrl);
            if (request.RemoveImage == true)
            {
                if (!string.IsNullOrEmpty(profile.ProfileImageUrl))
                {
                    fileCleanup.Enqueue(oldPath);
                    profile.ProfileImageUrl = null;
                }
            }
            else if (request.Image != null)
            {
                ProfileImageRules.Validate(request.Image);
                try
                {
                    newFile = await fileHandler.UploadFileAsync("UserProfile", request.Image, ProfileImageRules.MaxBytes, ProfileImageRules.AllowedExtensions, ct);
                }
                catch (ArgumentException ex)
                {
                    throw new UserException($"Slikovna datoteka nije prihvaćena: {ex.Message}");
                }

                profile.ProfileImageUrl = ProfileImages.ToUrl(profile.UserId, newFile);
                if (!string.IsNullOrEmpty(oldPath))
                {
                    fileCleanup.Enqueue(oldPath);
                }
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                if (newFile != null)
                {
                    await fileHandler.DeleteFile("UserProfile/" + newFile);
                }

                throw;
            }

            try
            {
                await fileCleanup.ProcessPendingAsync(ct);
            }
            catch
            {
            }

            return (await GetMyProfileAsync(ct))!;
        }

        public async Task<UserPublicProfileResponse?> GetPublicProfileAsync(int userId, CancellationToken ct = default)
        {
            var profile = await db.UserProfiles
                .Include(up => up.City)
                .Include(up => up.User!)
                    .ThenInclude(u => u.Country)
                .FirstOrDefaultAsync(up => up.UserId == userId, ct);

            if (profile == null || !profile.IsProfilePublic || profile.User!.IsDeleted) return null;

            int? currentUserId = identityService.IsAuthenticated()
                ? identityService.GetCurrentUserId()
                : null;

            bool isFollowedByCurrentUser = false;
            if (currentUserId.HasValue && currentUserId.Value != userId)
            {
                isFollowedByCurrentUser = await db.Set<Follow>()
                    .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId, ct);
            }

            var response = new UserPublicProfileResponse
            {
                Id = profile.UserId,
                Username = profile.User!.Username,
                FirstName = profile.User.FirstName,
                LastName = profile.User.LastName,
                ProfileImageUrl = profile.ProfileImageUrl,
                Bio = profile.Bio,
                City = profile.City?.Name,
                CityId = profile.CityId,
                CountryName = profile.User.Country?.Name,
                IsPhonePublic = profile.IsPhonePublic,
                IsEmailPublic = profile.IsEmailPublic,
                Phone = profile.IsPhonePublic ? profile.Phone : null,
                Email = profile.IsEmailPublic ? profile.User.Email : null,
                IsFollowedByCurrentUser = isFollowedByCurrentUser,
                FollowersCount = await db.Set<Follow>().CountAsync(f => f.FollowingId == userId, ct),
                FollowingCount = await db.Set<Follow>().CountAsync(f => f.FollowerId == userId, ct),
                RatedMoviesCount = await db.Reviews.CountAsync(r => r.UserId == userId, ct),
                ReviewsCount = await db.Reviews.CountAsync(r => r.UserId == userId && r.Text != "", ct),
                ThreadsCount = await db.Threads.CountAsync(t => t.UserId == userId, ct),
                CommentsCount = await db.Comments.CountAsync(c => c.UserId == userId, ct),
                CurrentStreak = await GetEffectiveStreakAsync(userId, ct)
            };

            return response;
        }

        private async Task<int> GetEffectiveStreakAsync(int userId, CancellationToken ct)
        {
            var streak = await db.Streaks.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
            return StreakRules.Effective(streak);
        }

        public async Task<(string Path, string ContentType)> GetImageAsync(int userId, string fileName, CancellationToken ct = default)
        {
            var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && !p.User!.IsDeleted, ct);
            if (profile == null || !ProfileImages.IsSafeFileName(fileName) || ProfileImages.FileNameOf(profile.ProfileImageUrl) != fileName)
                throw new NotFoundException("Slika", userId);

            var isOwnerOrAdmin = identityService.GetCurrentUserId() == userId || identityService.IsAdmin();
            if (!isOwnerOrAdmin && !profile.IsProfilePublic)
                throw new NotFoundException("Slika", userId);

            var path = System.IO.Path.Combine(webRoot.WebRootPath, ProfileImages.Folder, fileName);
            if (!File.Exists(path))
                throw new NotFoundException("Slika", userId);

            return (path, ProfileImages.ContentTypeOf(fileName));
        }

        public async Task<UserPublicProfileResponse?> GetPublicProfileAsync(string username, CancellationToken ct = default)
        {
            var user = await db.MyAppUsers.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, ct);
            return user == null ? null : await GetPublicProfileAsync(user.Id, ct);
        }

        public async Task<MyPagedList<UserSearchResponse>> SearchUsersAsync(UserSearchRequest request, CancellationToken ct = default)
        {
            int? currentUserId = identityService.IsAuthenticated()
                ? identityService.GetCurrentUserId()
                : null;

            var query = db.MyAppUsers
                .Include(u => u.UserProfile!).ThenInclude(p => p.City)
                .Where(u => !u.IsDeleted);

            if (IsSet(request.Username))
            {
                var search = request.Username!.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search)
                    || u.FirstName.ToLower().Contains(search)
                    || u.LastName.ToLower().Contains(search));
            }

            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var pageNumber = Math.Max(request.PageNumber, 1);
            var total = await query.CountAsync(ct);

            var users = await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var followingIds = currentUserId.HasValue
                ? await db.Set<Follow>()
                    .Where(f => f.FollowerId == currentUserId.Value)
                    .Select(f => f.FollowingId)
                    .ToListAsync(ct)
                : new List<int>();

            var responseList = users.Select(u => new UserSearchResponse
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfileImageUrl = u.UserProfile?.ProfileImageUrl,
                City = u.UserProfile?.City?.Name,
                CityId = u.UserProfile?.CityId,
                IsFollowedByCurrentUser = followingIds.Contains(u.Id)
            }).ToList();

            return new MyPagedList<UserSearchResponse>(responseList, total, pageNumber, pageSize);
        }

        private static bool IsSet(string? value) => value != null && value != "string";
    }
}
