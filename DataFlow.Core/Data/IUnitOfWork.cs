using DataFlow.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Data
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IConfigTemplateRepository ConfigTemplates { get; }
        IConfigColumnRepository ConfigColumns { get; }
        IColumnRangeRepository ColumnRanges { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
