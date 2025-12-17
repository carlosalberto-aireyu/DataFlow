using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataFlow.Core.Data;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Core.Repositories
{
    public class EFConfigTemplateRepository : EFRepository<ConfigTemplate>, IConfigTemplateRepository
    {
        public EFConfigTemplateRepository(AppDbContext context) : base(context)
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

        public async Task<ConfigTemplate?> GetWithDetailAsync(int id, CancellationToken cancellationToken=default)
        {
            return await DbSet
                .Include(t => t.ConfigColumns)
                .ThenInclude(c => c.Ranges)
                .Include(t => t.HistProcess)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
    }
}
