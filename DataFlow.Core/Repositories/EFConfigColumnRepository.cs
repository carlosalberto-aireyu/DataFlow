using DataFlow.Core.Data;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public class EFConfigColumnRepository : EFRepository<ConfigColumn>, IConfigColumnRepository
    {
        private readonly ILogger<EFConfigColumnRepository> _logger;
        public EFConfigColumnRepository(AppDbContext context, ILogger<EFConfigColumnRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<ConfigColumn>> GetByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken = default)
        {
            if (configTemplateId <= 0)
                throw new ArgumentException("ConfigTemplateId no es válido.", nameof(configTemplateId));

            return await DbSet
            .Where(c => c.ConfigTemplateId == configTemplateId)
            .Include(c => c.Ranges)
            .Include(c => c.DataType)
            .Include(c => c.ColumnType)
            .OrderBy(c => c.IndexColumn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        }

        public async Task<ConfigColumn?> GetWithDetailAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                throw new ArgumentException(nameof(id), "ConfigColumnId no es valido.");
            }

            return await DbSet
                //.AsNoTracking()
                .Include(c => c.Ranges)
                .Include(c => c.DataType)
                .Include(c => c.ColumnType)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }
        
    }
}
    
