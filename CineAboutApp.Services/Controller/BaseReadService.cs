using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CineAboutApp.Services.Controller
{
    public class BaseReadService<T, TSearch, TEntity> : IReadService<T, TSearch>
        where TSearch : BaseSearchObject
        where T : class
        where TEntity : class
    {
        protected readonly ApplicationDbContext db;
        protected readonly IMapper mapper;

        public BaseReadService(ApplicationDbContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }
        public virtual async Task<MyPagedList<T>> GetAsync(TSearch search, CancellationToken cancellation = default)
        {
            var query = db.Set<TEntity>().AsNoTracking().AsQueryable();
            query = GetIncludeQuery(query);
            query = ApplyFilters(query, search);
            query = ApplySorting(query, search);

            int totalCount;
            List<TEntity> items;
            
            int pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            int pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(20), 1, 100);

            if (query.Provider is IAsyncQueryProvider)
            {
                totalCount = await query.CountAsync(cancellation);
                items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellation);
            }
            else
            {
                totalCount = query.Count();
                items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }

            return new MyPagedList<T>(items.Select(MapToResponse).ToList(), totalCount, pageNumber, pageSize);
        }

        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, TSearch search)
        {
            var createdAtProperty = typeof(TEntity).GetProperty("CreatedAt");
            if (createdAtProperty != null)
            {
                return query.OrderByDescending(x => EF.Property<DateTime>(x, "CreatedAt"));
            }

            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty != null)
            {
                return query.OrderByDescending(x => EF.Property<int>(x, "Id"));
            }

            return query;
        }

        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var query = db.Set<TEntity>().AsNoTracking().AsQueryable();
            query = GetIncludeQuery(query);
            
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                var fallback = await db.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
                return fallback == null ? null : MapToResponse(fallback);
            }

            var entity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, cancellationToken);
            if (entity == null) return null;
            return MapToResponse(entity);
        }

        protected virtual IQueryable<TEntity> GetIncludeQuery(IQueryable<TEntity> query)
        {
            return query;
        }

        protected virtual IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, TSearch search)
        {
            if (search.IsDeleted.HasValue && typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
            {
                query = query.Where(x => ((ISoftDeletable)x).IsDeleted == search.IsDeleted.Value);
            }
            return query;
        }

        protected virtual T MapToResponse(TEntity entity)
        {
            return mapper.Map<T>(entity);
        }
    }
}




