using DataFlow.Core.Constants;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public interface ILookupService
    {
        Task<IReadOnlyList<ColumnTypeLookup>> GetColumnTypesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DataTypeLookup>> GetDataTypesAsync(CancellationToken cancellationToken = default);

        

    }
}
