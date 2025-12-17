using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public interface ILookupRepository
    {
        Task<bool> DataTypeExistsAsync(int id, CancellationToken ct);
        Task<bool> ColumnTypeExistsAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ColumnTypeLookup>> GetAllColumnTypesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DataTypeLookup>> GetAllDataTypesAsync(CancellationToken cancellationToken = default);
    }
}
