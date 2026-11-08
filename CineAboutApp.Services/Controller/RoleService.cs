using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Model;
using CineAboutApp.Models.Requests.Role;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class RoleService(ApplicationDbContext db, IMapper mapper)
        : BaseCRUDService<RoleResponse, RoleSearchObject, RoleInsertOrUpdateRequest, RoleInsertOrUpdateRequest, Role>(db, mapper), IRoleService
    {
        public async Task AddToRole(int userId, int roleId)
        {
            var userRole = await db.UserRoles.Where(ur => ur.UserId == userId && ur.RoleId == roleId)
                .FirstOrDefaultAsync();

            if (userRole == null)
            {
                userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                db.UserRoles.Add(userRole);

                var role = await db.Roles.FindAsync(roleId);
                if (role != null && role.Name == AppRoles.User)
                {
                    if (!(await db.UserProfiles.Where(up => up.UserId == userId).AnyAsync()))
                    {
                        db.UserProfiles.Add(new UserProfile { UserId = userId });
                    }
                }
            }
            else if (!userRole.IsActive)
            {
                userRole.IsActive = true;
                userRole.UpdatedAt = DateTime.UtcNow;
            }
        }

        public async Task RemoveFromRole(int userId, int roleId)
        {
            var userRole = await db.UserRoles.Where(ur => ur.UserId == userId && ur.RoleId == roleId)
                        .FirstOrDefaultAsync();

            if (userRole != null)
            {
                userRole.IsActive = false;
                userRole.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static bool IsBuiltIn(string name) =>
            string.Equals(name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || string.Equals(name, AppRoles.User, StringComparison.OrdinalIgnoreCase);

        protected override async Task BeforeCreate(Role entity, RoleInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            request.Name = ReferenceNameGuard.Normalize(request.Name, "Uloga");
            await EnsureUniqueAsync(null, request.Name, cancellationToken);
            entity.Name = request.Name;
        }

        protected override async Task BeforeUpdate(Role entity, RoleInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (IsBuiltIn(entity.Name))
                throw new UserException($"Uloga '{entity.Name}' je ugrađena (koriste je ovlaštenja aplikacije) i ne može se mijenjati.");

            request.Name = ReferenceNameGuard.Normalize(request.Name, "Uloga");
            await EnsureUniqueAsync(entity.Id, request.Name, cancellationToken);
        }

        protected override async Task BeforeDelete(Role entity, CancellationToken cancellationToken = default)
        {
            if (IsBuiltIn(entity.Name))
                throw new UserException($"Uloga '{entity.Name}' je ugrađena (koriste je ovlaštenja aplikacije) i ne može se obrisati.");

            var assigned = await db.UserRoles.CountAsync(ur => ur.RoleId == entity.Id && ur.IsActive, cancellationToken);
            if (assigned > 0)
                throw new UserException($"Uloga '{entity.Name}' je dodijeljena {assigned} korisnika i ne može biti obrisana.");
        }

        private async Task EnsureUniqueAsync(int? ignoreId, string name, CancellationToken cancellationToken)
        {
            if (await db.Roles.AnyAsync(r => r.Name == name && r.Id != ignoreId, cancellationToken))
                throw new UserException($"Uloga '{name}' već postoji.");
        }

        protected override IQueryable<Role> ApplyFilters(IQueryable<Role> query, RoleSearchObject search)
        {
            if(search.NameFTS != null)
            {
                query = query.Where(r => r.Name.ToLower().Contains(search.NameFTS.ToLower()));
            }

            if(search.IncludeUsers)
            {
                query = query.Include(r => r.Users);
            }

            return query;
        }
    }
}



