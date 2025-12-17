using DataFlow.Core.Data;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Core.Repository
{
    public class ConfigTemplateRepository : EFRepository<ConfigTemplate>, IConfigTemplateRepository
    {
        public ConfigTemplateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<ConfigTemplate>> GetAllWithDetailAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .Include(t => t.ConfigColumns)
                    .ThenInclude(c => c.Ranges)
                .Include(t => t.HistProcess)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConfigTemplate?> GetWithDetailAsync(int id, CancellationToken cancellationToken)
        {
            return await DbSet
                .Include(t => t.ConfigColumns)
                    .ThenInclude(c => c.Ranges)
                .Include(t => t.HistProcess)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
    }
}
