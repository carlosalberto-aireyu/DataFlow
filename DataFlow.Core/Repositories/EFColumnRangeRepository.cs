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
    public class EFColumnRangeRepository : EFRepository<ColumnRange>, IColumnRangeRepository
    {
        private readonly DbSet<ColumnRange> _dbSet;

        public EFColumnRangeRepository(AppDbContext context) : base(context)
        {
            _dbSet = context.Set<ColumnRange>();
        }

        public async Task<IReadOnlyList<ColumnRange>> GetByConfigColumnIdAsync(int configColumnId, CancellationToken cancellationToken = default)
        {
            if(configColumnId <= 0)
            {
                throw new ArgumentException(nameof(configColumnId), "ConfigColumnId no es valido.");
            }

            return await _dbSet
                .AsNoTracking()
                .Where(r => r.ConfigColumnId == configColumnId)
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
