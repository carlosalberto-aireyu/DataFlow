
using DataFlow.Core.Data;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public class EFHistProcessRepository : EFRepository<HistProcess>, IHistProcessRepository
    {
        public EFHistProcessRepository(AppDbContext context) : base(context)
        {
        }
        
        public async Task<IReadOnlyList<HistProcess>> GetByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken)
        {
            return await DbSet.Where(c => c.ConfigTemplateId == configTemplateId)
                .ToListAsync(cancellationToken);
        }
    }
}
