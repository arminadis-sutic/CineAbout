using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;

namespace CineAboutApp.Services.Controller
{
    public class BaseCRUDService<T, TSearch, TInsert, TUpdate, TEntity> 
        : BaseReadService<T, TSearch, TEntity>, ICRUDService<T, TSearch, TInsert, TUpdate>
        where TSearch : BaseSearchObject
        where TInsert : class
        where TUpdate : class
        where TEntity : class
        where T : class
    {
        public BaseCRUDService(ApplicationDbContext db, IMapper mapper) : base(db, mapper)
        {
        }
        protected virtual Task BeforeCreate(TEntity entity, TInsert request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<T> CreateAsync(TInsert request, CancellationToken cancellationToken = default)
        {
            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;

            try
            {
                var entity = MapInsertToEntity(request);

                await BeforeCreate(entity, request, cancellationToken);

                db.Set<TEntity>().Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return MapToResponse(entity);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
        }

        protected virtual Task BeforeDelete(TEntity entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;

            try
            {
                var entity = await db.Set<TEntity>().FindAsync(id, cancellationToken);

                if (entity == null)
                {
                    return false;
                }

                await BeforeDelete(entity, cancellationToken);

                db.Set<TEntity>().Remove(entity);
                await db.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return true;
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
        }

        protected virtual Task BeforeUpdate(TEntity entity, TUpdate request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<T?> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default)
        {
            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;

            try
            {
                var entity = await db.Set<TEntity>().FindAsync(id, cancellationToken);

                if (entity == null)
                {
                    return null;
                }

                await BeforeUpdate(entity, request, cancellationToken);

                MapUpdateToEntity(request, entity);
                await db.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                return MapToResponse(entity);
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
        }

        protected virtual TEntity MapInsertToEntity(TInsert request)
        {
            return mapper.Map<TEntity>(request);
        }

        protected virtual TEntity MapUpdateToEntity(TUpdate request, TEntity entity)
        {
            return mapper.Map(request, entity);
        }
    }
}




