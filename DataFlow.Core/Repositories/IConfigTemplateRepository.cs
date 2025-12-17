using DataFlow.Core.Models;

namespace DataFlow.Core.Repositories
{
    public interface IConfigTemplateRepository : IRepository<ConfigTemplate>
    {
        Task<ConfigTemplate?> GetWithDetailAsync(int id, CancellationToken cancellationToken=default);
        Task<IReadOnlyList<ConfigTemplate>> GetAllWithDetailAsync(CancellationToken cancellationToken = default);
    }
}
