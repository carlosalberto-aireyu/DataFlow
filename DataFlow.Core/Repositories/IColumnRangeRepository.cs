using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public interface IColumnRangeRepository : IRepository<ColumnRange>
    {
        Task<IReadOnlyList<ColumnRange>> GetByConfigColumnIdAsync(int configColumnId, CancellationToken cancellationToken = default);
    }
}
