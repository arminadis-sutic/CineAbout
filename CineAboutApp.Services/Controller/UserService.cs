using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Services.General;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Events;
using CineAboutApp.Models.Mail;
using CineAboutApp.Models.Messages;
using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.Responses.User;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.Hubs;
using CineAboutApp.Services.Validators;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using CineAboutApp.Models;

namespace CineAboutApp.Services.Controller
{
    public class UserService(IMapper mapper, IRoleService rs, ITokenHandler<MyAppUser> th, IConfiguration cfg, 
        IRabbitMQHandler rmqh, IAppSettingsService appSettings, IDomainEventPublisher eventPublisher, ApplicationDbContext db, IIdentityService identityService,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger, ITokenBlacklistService blacklistService, IPendingFileCleanup fileCleanup)
        : BaseCRUDService<UserResponse, UserSearchObject, UserInsertOrUpdateRequest, UserInsertOrUpdateRequest, MyAppUser>(db, mapper), IUserService
    {

        protected override IQueryable<MyAppUser> GetIncludeQuery(IQueryable<MyAppUser> query)
        {
            return query.Include(x => x.UserProfile).Include(x => x.UserRoles).ThenInclude(ur => ur.Role);
        }

        protected override IQueryable<MyAppUser> ApplyFilters(IQueryable<MyAppUser> query, UserSearchObject search)
        {
           

            if (search.IsActive != null)
            {
                query = query.Where(u => u.IsActive == search.IsActive);
            }

            if (!string.IsNullOrEmpty(search.NameFTS))
            {
                query = query.Where(u => u.Username.Contains(search.NameFTS)
                || u.FirstName.Contains(search.NameFTS)
                || u.LastName.Contains(search.NameFTS));
            }

            if (search.RoleId != null)
            {
                query = query.Include(u => u.UserRoles).Where(u => u.UserRoles.Select(r => r.RoleId).Contains(search.RoleId.Value));
            }

            return base.ApplyFilters(query, search)
                .Include(u => u.UserProfile)
                .Include(u => u.Country)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        }

        public override async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await db.MyAppUsers
                .Include(u => u.UserProfile)
                .Include(u => u.Country)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
                
            if (entity == null) return null;
            return MapToResponse(entity);
        }

        protected async Task BeforeCreate(MyAppUser entity, UserInsertOrUpdateRequest request, int? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            var validator = new UserInsertOrUpdateValidator(db, userId, isAdmin);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new UserException(validationResult.ToString("~"));
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                var (hashedPassword, salt) = passwordHasher.Hash(request.Password);

                entity.PasswordHash = hashedPassword;
                entity.PasswordSalt = salt;
            }
        }

        protected async Task BeforeCreate(MyAppUser entity, UserRegisterRequest request, CancellationToken cancellationToken = default)
        {
            var mappedRequest = new UserInsertOrUpdateRequest
            {
                Username = request.Username,
                Password = request.Password,
                PasswordConfirmation = request.PasswordConfirmation,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CountryId = request.CountryId
            };
            await BeforeCreate(entity, mappedRequest, null, false, cancellationToken);
        }

        public async Task<UserResponse> RegisterUser(UserRegisterRequest request, CancellationToken cancellationToken = default)
        {
            bool isAdmin = false;

            if (identityService.IsAuthenticated())
            {
                isAdmin = identityService.IsAdmin();
            }

            var entity = mapper.Map<MyAppUser>(request);
            await BeforeCreate(entity, request, cancellationToken);


            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;
            try
            {
                entity.RegistrationDate = DateTime.UtcNow;
                db.MyAppUsers.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                var userRole = await db.Roles.FirstAsync(r => r.Name == AppRoles.User, cancellationToken);
                await rs.AddToRole(entity.Id, userRole.Id);

                var autoActivate = isAdmin || (await appSettings.GetAppSettingsAsync()).AutoActivateNewUsers;
                entity.IsActive = autoActivate;
                await db.SaveChangesAsync(cancellationToken);

                await rmqh.PublishAsync(new Email()
                {
                    MailData = new MailData()
                    {
                        Subject = "Welcome to CineAbout",
                        IsBodyHtml = false,
                        To = entity.Email,
                        Body = new StringBuilder($"Hi {entity.Username},\n\nWelcome to CineAbout! {(autoActivate ? "Your account is ready, you can sign in now." : "An administrator will activate your account shortly.")}\n\nThank you,\nCineAbout Team")
                    }
                });

                if (transaction != null) await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            var registrationEvent = new UserRegisteredEvent
            {
                EventId = Guid.NewGuid(),
                UserId = entity.Id,
                Username = entity.Username,
                Email = entity.Email,
                RegistrationDate = entity.RegistrationDate
            };
            await eventPublisher.PublishAsync(registrationEvent);
            
            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }

        public override Task<UserResponse> CreateAsync(UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            throw new MethodAccessException("Use RegisterUser method instead for user registration.");
        }

        public override Task<UserResponse?> UpdateAsync(int id, UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            throw new MethodAccessException("Use UpdateUser method instead for user updates.");
        }

        protected override UserResponse MapToResponse(MyAppUser entity)
        {
            var response = base.MapToResponse(entity);
            response.ProfileImageUrl = entity.UserProfile?.ProfileImageUrl;

            response.Roles = entity.UserRoles
                .Where(ur => ur.IsActive)
                .Select(ur => new RoleResponse
                {
                    Id = ur.RoleId,
                    Name = ur.Role!.Name
                }).ToList();
            return response;
        }

        public async Task<bool> ActivateDeactivateUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await db.MyAppUsers.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            if (user.IsDeleted)
            {
                throw new UserException("Obrisani nalog se ne može aktivirati ni deaktivirati.");
            }

            user.IsActive = !user.IsActive;
            if (!user.IsActive)
            {
                await RevokeSessionsAsync(user, cancellationToken);
            }
            await db.SaveChangesAsync(cancellationToken);
            if (!user.IsActive)
            {
                NotificationHub.DisconnectUser(user.Id.ToString());
            }

            if (user.IsActive)
            {
                await rmqh.PublishAsync(new Email()
                {
                    MailData = new MailData()
                    {
                        Subject = "Your account has been activated",
                        IsBodyHtml = false,
                        To = user.Email,
                        Body = new StringBuilder($"Hi {user.Username},\n\nYour account has been activated. You can now log in.\n\nThank you,\nCineAbout Team")
                    }
                });
            }
            else
            {
                await rmqh.PublishAsync(new Email()
                {
                    MailData = new MailData()
                    {
                        Subject = "Your account has been deactivated",
                        IsBodyHtml = false,
                        To = user.Email,
                        Body = new StringBuilder($"Hi {user.Username},\n\nYour account has been deactivated. You can no longer log in. If you think this is an error please contact us.\n\nThank you,\nCineAbout Team")
                    }
                });
            }

            return user.IsActive;
        }

        public async Task<LoginResponse> GetRefreshedJwt(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            MyRefreshToken? myRefToken = await db.MyRefreshTokens.Where(t => t.Token == request.RefreshToken)
                .Include(t => t.MyAppUser!)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role).FirstOrDefaultAsync(cancellationToken);

            if (myRefToken == null)
            {
                throw new UserException("Invalid or expired refresh token.");
            }

            if (myRefToken.ExpiryTime < DateTime.UtcNow)
            {
                db.MyRefreshTokens.Remove(myRefToken);
                await db.SaveChangesAsync(cancellationToken);

                throw new UserException("Invalid or expired refresh token.");
            }


            if (myRefToken.MyAppUser is not { IsActive: true, IsDeleted: false })
            {
                db.MyRefreshTokens.Remove(myRefToken);
                await db.SaveChangesAsync(cancellationToken);

                throw new UserException("Invalid or expired refresh token.");
            }

            var jwtToken = th.GetDecodedJwt(request.JwtToken, validateSignature: true);
            int sub = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "0");
            if (myRefToken.MyAppUserId != sub)
            {
                throw new UserException("Invalid or expired refresh token.");
            }

            var token = th.CreateRefreshToken();

            myRefToken.Token = token;
            myRefToken.ExpiryTime = DateTime.UtcNow.AddHours(cfg.GetValue<int>("Jwt:RefreshExpirationInHours"));
            await db.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                JwtToken = th.Create(myRefToken.MyAppUser!),
                RefreshToken = token
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                throw new UserException("Username and password are required.");
            }
            var user = await db.MyAppUsers.Where(u => u.Username.ToLower() == request.Username.ToLower()).Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(cancellationToken);
            if (user == null || user.IsDeleted || !passwordHasher.Verify(user.PasswordHash, request.Password, user.PasswordSalt))
            {
                throw new UserException("Invalid username or password.");
            }
            if (!user.IsActive)
            {
                throw new UserException("Your account is not active. An administrator will activate it shortly.");
            }
            string refresh = th.CreateRefreshToken();
            var myRefTok = new MyRefreshToken
            {
                ExpiryTime = DateTime.UtcNow.AddHours(cfg.GetValue<int>("Jwt:RefreshExpirationInHours")),
                MyAppUserId = user.Id,
                Token = refresh
            };
            await db.MyRefreshTokens.AddAsync(myRefTok, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                JwtToken = th.Create(user),
                RefreshToken = refresh,
            };
        }

        public async Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            string? userId = null;
            if (!string.IsNullOrEmpty(request.JwtToken))
            {
                try
                {
                    var jwtToken = th.GetDecodedJwt(request.JwtToken, validateSignature: true);
                    userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        await blacklistService.BlacklistTokenAsync(jti, jwtToken.ValidTo, int.TryParse(userId, out var ownerId) ? ownerId : null);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse or blacklist JWT token from request");
                }
            }
            
            MyRefreshToken? myRefToken = await db.MyRefreshTokens
                .Where(t => t.Token == request.RefreshToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (myRefToken != null)
            {
                db.MyRefreshTokens.Remove(myRefToken);
            }

            if (!string.IsNullOrWhiteSpace(request.DeviceToken) && int.TryParse(userId, out var signedOutUserId))
            {
                await db.UserDeviceTokens.Where(dt => dt.Token == request.DeviceToken && dt.UserId == signedOutUserId).ExecuteDeleteAsync(cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<UserResponse> UpdateUser(int id, UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;
            try
            {
                var user = await db.MyAppUsers.FindAsync(id);

                if (user == null)
                {
                    throw new NotFoundException("User", id);
                }

                bool isAdmin = identityService.IsAdmin();

                if (!isAdmin && user.Id != identityService.GetCurrentUserId())
                {
                    throw new UserException("You can only update your own profile.");
                }

                if (user.IsDeleted)
                {
                    throw new UserException("Obrisani nalog se ne može mijenjati.");
                }

                if (!string.IsNullOrEmpty(request.Password) && user.Id == identityService.GetCurrentUserId())
                {
                    if (string.IsNullOrEmpty(request.CurrentPassword))
                    {
                        throw new UserException("Unesite trenutnu lozinku da biste promijenili lozinku.");
                    }

                    if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword, user.PasswordSalt))
                    {
                        throw new UserException("Trenutna lozinka nije ispravna. Molimo potvrdite vaš identitet.");
                    }
                }

                if (!isAdmin)
                {
                    request.IsActive = user.IsActive;
                }

                var wasActive = user.IsActive;
                var passwordChanged = !string.IsNullOrEmpty(request.Password);
                var rolesChanged = false;

                await BeforeCreate(user, request, id, isAdmin, cancellationToken);
                MapUpdateToEntity(request, user);

                if (isAdmin)
                {
                    var existingRoles = await db.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync(cancellationToken);
                    var existingRoleIds = existingRoles.Select(ur => ur.RoleId).ToList();
                    
                    request.RoleIds ??= new List<int>();
                    var rolesToRemove = existingRoles.Where(ur => !request.RoleIds.Contains(ur.RoleId)).ToList();
                    foreach (var ur in rolesToRemove)
                    {
                        await rs.RemoveFromRole(user.Id, ur.RoleId);
                    }
                    rolesChanged = rolesToRemove.Count > 0;
                    
                    var rolesToAdd = request.RoleIds.Where(roleId => !existingRoleIds.Contains(roleId)).ToList();
                    foreach (var roleId in rolesToAdd)
                    {
                        await rs.AddToRole(user.Id, roleId);
                    }
                    rolesChanged |= rolesToAdd.Count > 0;
                }

                var sessionsRevoked = passwordChanged || rolesChanged || (wasActive && !user.IsActive);
                if (sessionsRevoked)
                {
                    await RevokeSessionsAsync(user, cancellationToken);
                }
                await db.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                if (sessionsRevoked)
                {
                    NotificationHub.DisconnectUser(user.Id.ToString());
                }
                return (await GetByIdAsync(user.Id, cancellationToken))!;
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private const string ResetPurpose = "password-reset";
        private const string ResetRejected = "Link za resetovanje lozinke nije ispravan, istekao je ili je već iskorišten. Zatražite novi.";
        private const string ResetNeutralReply = "Ako postoji nalog s ovom e-mail adresom, poslali smo vam link za resetovanje lozinke.";
        private static readonly Regex ResetTokenFormat = new(@"^(?<uid>\d{1,9})\.(?<secret>[0-9a-f]{64})$", RegexOptions.Compiled);

        private string HashResetToken(int userId, string token)
        {
            var key = Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!);
            var data = Encoding.UTF8.GetBytes($"{ResetPurpose}|{userId}|{token}");
            return Convert.ToBase64String(HMACSHA256.HashData(key, data));
        }

        private async Task RevokeSessionsAsync(MyAppUser user, CancellationToken cancellationToken)
        {
            user.SecurityStamp++;
            db.MyRefreshTokens.RemoveRange(await db.MyRefreshTokens.Where(t => t.MyAppUserId == user.Id).ToListAsync(cancellationToken));
        }

        public async Task<string> RequestPasswordReset(string request, CancellationToken cancellationToken = default)
        {
            if (!Regex.IsMatch(request, "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"))
            {
                throw new UserException("Invalid email format");
            }

            var user = await db.MyAppUsers.SingleOrDefaultAsync(u => u.Email == request && u.IsActive && !u.IsDeleted, cancellationToken);
            if (user == null)
            {
                return ResetNeutralReply;
            }

            var lifetime = TimeSpan.FromMinutes(cfg.GetValue<int>("Jwt:ResetExpirationInMinutes"));
            var now = DateTime.UtcNow;

            var requestedJustNow = await db.MyResetTokens.AnyAsync(
                t => t.MyAppUserId == user.Id && t.ExpiryTime > now.Add(lifetime).AddMinutes(-1), cancellationToken);
            if (requestedJustNow)
            {
                return ResetNeutralReply;
            }

            db.MyResetTokens.RemoveRange(await db.MyResetTokens.Where(t => t.MyAppUserId == user.Id).ToListAsync(cancellationToken));

            var token = $"{user.Id}.{th.CreateResetToken()}";
            db.MyResetTokens.Add(new MyResetToken
            {
                ResetToken = HashResetToken(user.Id, token),
                ExpiryTime = now.Add(lifetime),
                MyAppUserId = user.Id
            });
            await db.SaveChangesAsync(cancellationToken);

            var mailBody = new StringBuilder();
            mailBody.Append($"Hi {user.Username},\n\n");
            mailBody.Append("Open this link to choose a new password:\n");
            mailBody.Append($"{cfg["BackendUrl"]}/reset-password?token={token}\n\n");
            mailBody.Append($"The link works once and is valid for the next {cfg["Jwt:ResetExpirationInMinutes"]} minutes. ");
            mailBody.Append("If you did not ask for it, you can ignore this e-mail.\n\n");
            mailBody.Append("CineAbout Team");

            await rmqh.PublishAsync(new Email()
            {
                MailData = new MailData()
                {
                    Subject = "Password Reset - CineAbout",
                    IsBodyHtml = false,
                    To = user.Email,
                    Body = mailBody
                }
            });

            return ResetNeutralReply;
        }

        public async Task<UserResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            int userId = identityService.GetCurrentUserId();
            return await GetByIdAsync(userId, cancellationToken);
        }

        public async Task<string> ResetPassword(PasswordResetRequest request, CancellationToken cancellationToken = default)
        {
            if (!Regex.IsMatch(request.Password ?? string.Empty, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,100}$"))
            {
                throw new UserException("Lozinka mora imati 8-100 karaktera, uključujući veliko slovo, malo slovo, broj i specijalni znak (npr. Lozinka123!).");
            }

            var token = request.ResetToken?.Trim() ?? string.Empty;
            var match = ResetTokenFormat.Match(token);
            if (!match.Success)
            {
                throw new UserException(ResetRejected);
            }
            var userId = int.Parse(match.Groups["uid"].Value);
            var hashed = HashResetToken(userId, token);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var row = await db.MyResetTokens.Include(t => t.MyAppUser)
                .SingleOrDefaultAsync(t => t.MyAppUserId == userId && t.ResetToken == hashed && t.ExpiryTime > DateTime.UtcNow, cancellationToken);
            if (row?.MyAppUser is not { IsActive: true, IsDeleted: false } user)
            {
                throw new UserException(ResetRejected);
            }

            var claimed = await db.MyResetTokens.Where(t => t.Id == row.Id).ExecuteDeleteAsync(cancellationToken);
            if (claimed == 0)
            {
                throw new UserException(ResetRejected);
            }

            if (passwordHasher.Verify(user.PasswordHash, request.Password!, user.PasswordSalt))
            {
                throw new UserException("Password can't be the same as before!");
            }

            var (pass, salt) = passwordHasher.Hash(request.Password!);
            user.PasswordHash = pass;
            user.PasswordSalt = salt;

            db.MyResetTokens.RemoveRange(await db.MyResetTokens.Where(t => t.MyAppUserId == user.Id).ToListAsync(cancellationToken));
            await RevokeSessionsAsync(user, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            NotificationHub.DisconnectUser(user.Id.ToString());

            return "Password changed successfully!";
        }

        private const string DeletedUserName = "Deleted User";

        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await db.MyAppUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            if (identityService.GetCurrentUserIdNullable() == id)
            {
                throw new UserException("Ne možete obrisati vlastiti nalog.");
            }

            var isAdminUser = await db.UserRoles.AnyAsync(
                ur => ur.UserId == id && ur.IsActive && ur.Role!.Name == AppRoles.Admin, cancellationToken);
            if (isAdminUser)
            {
                var otherActiveAdmins = await db.UserRoles.CountAsync(
                    ur => ur.UserId != id && ur.IsActive && ur.Role!.Name == AppRoles.Admin && ur.User!.IsActive && !ur.User.IsDeleted,
                    cancellationToken);
                if (otherActiveAdmins == 0)
                {
                    throw new UserException("Posljednji administrator se ne može obrisati.");
                }
            }

            var originalUsername = user.Username;
            var originalEmail = user.Email;
            var followNotice = $"%\"userId\":\"{id}\"%";
            var commentNotice = $"{originalUsername} je komentarisao/la%";
            var idText = id.ToString();

            var existingTransaction = db.Database.CurrentTransaction;
            await using var transaction = existingTransaction == null
                ? await db.Database.BeginTransactionAsync(cancellationToken)
                : null;
            try
            {
                var imagePath = ProfileImages.ToRelativePath(await db.UserProfiles
                    .Where(p => p.UserId == id).Select(p => p.ProfileImageUrl).FirstOrDefaultAsync(cancellationToken));

                await db.MyRefreshTokens.Where(t => t.MyAppUserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.MyResetTokens.Where(t => t.MyAppUserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserDeviceTokens.Where(t => t.UserId == id).ExecuteDeleteAsync(cancellationToken);

                await db.UserProfiles.Where(p => p.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserRoles.Where(r => r.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.Follows.Where(f => f.FollowerId == id || f.FollowingId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserNotifications.Where(n => n.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.SwipeHistories.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.Reminders.Where(r => r.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.WatchlistItems.Where(w => w.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserGenrePreferences.Where(p => p.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.Streaks.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserListItems.Where(i => i.UserList.UserId == id).ExecuteDeleteAsync(cancellationToken);
                await db.UserLists.Where(l => l.UserId == id).ExecuteDeleteAsync(cancellationToken);

                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    DELETE un FROM UserNotifications un
                    INNER JOIN Notifications n ON n.Id = un.NotificationId
                    WHERE n.Data LIKE '%""type"":""follow""%' AND n.Data LIKE {followNotice}", cancellationToken);
                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    DELETE FROM Notifications
                    WHERE Data LIKE '%""type"":""follow""%' AND Data LIKE {followNotice}", cancellationToken);
                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE Notifications SET Body = STUFF(Body, 1, LEN({originalUsername}), {DeletedUserName})
                    WHERE Data LIKE '%""type"":""comment""%' AND Body LIKE {commentNotice}", cancellationToken);

                user.FirstName = "Deleted";
                user.LastName = "User";
                user.Username = DeletedUserName;
                user.Email = $"deleted-{id}@deleted.invalid";
                (user.PasswordHash, user.PasswordSalt) = passwordHasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
                user.LastActivity = null;
                user.IsActive = false;
                user.IsDeleted = true;
                user.SecurityStamp++;

                fileCleanup.Enqueue(imagePath);
                await db.SaveChangesAsync(cancellationToken);

                await db.AuditLogs
                    .Where(a => a.EntityType == nameof(MyAppUser) && a.EntityId == idText)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.OldValuesJson, (string?)null).SetProperty(a => a.NewValuesJson, (string?)null), cancellationToken);
                var emailFragment = "\"Email\":" + JsonSerializer.Serialize(originalEmail);
                var usernameFragment = "\"Username\":" + JsonSerializer.Serialize(originalUsername);
                await db.AuditLogs
                    .Where(a => a.EntityType == nameof(MyAppUser) && a.EntityId.StartsWith("-")
                        && a.NewValuesJson != null && (a.NewValuesJson.Contains(emailFragment) || a.NewValuesJson.Contains(usernameFragment)))
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.OldValuesJson, (string?)null).SetProperty(a => a.NewValuesJson, (string?)null), cancellationToken);
                await db.AuditLogs
                    .Where(a => a.PerformedByUserId == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.PerformedByUsername, DeletedUserName)
                        .SetProperty(a => a.IpAddress, (string?)null)
                        .SetProperty(a => a.UserAgent, (string?)null), cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }

            NotificationHub.DisconnectUser(idText);

            try
            {
                await fileCleanup.ProcessPendingAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Profile picture of deleted user {UserId} will be removed by the background cleanup.", id);
            }

            return true;
        }

        public async Task RegisterDeviceToken(RegisterDeviceTokenRequest request, CancellationToken cancellationToken = default)
        {
            int userId = identityService.GetCurrentUserId();

            var deviceToken = request.DeviceToken?.Trim() ?? string.Empty;
            if (deviceToken.Length is < 20 or > 4096)
            {
                throw new UserException("Neispravan token uređaja.");
            }

            request.DeviceToken = deviceToken;

            await db.UserDeviceTokens.Where(dt => dt.Token == deviceToken && dt.UserId != userId).ExecuteDeleteAsync(cancellationToken);

            var token = await db.UserDeviceTokens.Where(dt => dt.Token == request.DeviceToken && dt.UserId == userId).FirstOrDefaultAsync(cancellationToken);

            if(token != null)
            {
                token.LastLoggedOn = DateTime.UtcNow;
            }
            else
            {
                db.UserDeviceTokens.Add(new UserDeviceToken { 
                    DeviceType = request.DeviceType,
                    Token = request.DeviceToken,
                    LastLoggedOn = DateTime.UtcNow,
                    UserId = userId
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}




