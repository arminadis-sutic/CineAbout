using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IReadService<T,TSearch> 
        where TSearch : BaseSearchObject
        where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<MyPagedList<T>> GetAsync(TSearch search, CancellationToken cancellation = default);
    }
}

