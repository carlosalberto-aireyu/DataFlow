using DataFlow.Core.Models;

namespace DataFlow.Core.Repositories
{
    public interface IConfigColumnRepository : IRepository<ConfigColumn>
    {
        Task<IReadOnlyList<ConfigColumn>> GetByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken = default);
        Task<ConfigColumn?> GetWithDetailAsync(int id, CancellationToken cancellationToken = default);
    }
}
