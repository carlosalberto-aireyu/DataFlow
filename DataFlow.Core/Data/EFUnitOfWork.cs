using DataFlow.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Data
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IConfigTemplateRepository ConfigTemplates { get; }
        public IConfigColumnRepository ConfigColumns { get; }
        public IColumnRangeRepository ColumnRanges { get; }

        public EFUnitOfWork(
            AppDbContext context,
            IConfigTemplateRepository configTemplates,
            IConfigColumnRepository configColumns,
            IColumnRangeRepository columnRanges)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            ConfigTemplates = configTemplates ?? throw new ArgumentNullException(nameof(configTemplates));
            ConfigColumns = configColumns ?? throw new ArgumentNullException(nameof(configColumns));
            ColumnRanges = columnRanges ?? throw new ArgumentNullException(nameof(columnRanges));
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}
