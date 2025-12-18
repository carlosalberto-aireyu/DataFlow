using DataFlow.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Data
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IConfigTemplateRepository ConfigTemplates { get; }
        IConfigColumnRepository ConfigColumns { get; }
        IColumnRangeRepository ColumnRanges { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
