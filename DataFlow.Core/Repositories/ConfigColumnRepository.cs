using DataFlow.Core.Data;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Core.Repositories
{
    public class ConfigColumnRepository_ : EFRepository<ConfigColumn>, IConfigColumnRepository
    {
        public ConfigColumnRepository_(AppDbContext context) : base(context)
        {}

        public async Task<IReadOnlyList<ConfigColumn>> GetByConfigTemplateIdAsync(
            int configTemplateId, CancellationToken cancellationToken)
        {
            return await DbSet
                .AsNoTracking()
                .Where(c => c.ConfigTemplateId == configTemplateId)
                .Include(t => t.DataType)
                .Include(t => t.ColumnType)
                .Include(t => t.Ranges)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConfigColumn?> GetWithDetailAsync(int id, CancellationToken cancellationToken = default)
        {
            return await DbSet
                //.AsNoTracking()
                .Include(t=>t.DataType)
                .Include(t=>t.ColumnType)
                .Include(t => t.Ranges)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
    }
}
