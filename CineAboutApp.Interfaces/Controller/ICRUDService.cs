using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface ICRUDService<T, TSearch, TInsert, TUpdate> : IReadService<T, TSearch>
        where TSearch : BaseSearchObject
        where TInsert : class
        where TUpdate : class
        where T : class
    {
        Task<T> CreateAsync(TInsert request, CancellationToken cancellationToken = default);
        Task<T?> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
