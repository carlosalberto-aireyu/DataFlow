using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public class LookupService : ILookupService
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ILogger _logger;
        //private LookupIds? _cachedLookupIds;

        public LookupService(IQueryDispatcher queryDispatcher, ILogger<LookupService> logger)
        {
            _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<IReadOnlyList<ColumnTypeLookup>> GetColumnTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetColumnTypesQuery();
                var result = await _queryDispatcher
                    .DispatchAsync<GetColumnTypesQuery, Result<IReadOnlyList<ColumnTypeLookup>>>(query, cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsSuccess && result.Value != null)
                    return result.Value;

                _logger.LogWarning("Error al obtener los Tipos de Columnas: {Error}", result.Error);
                return Array.Empty<ColumnTypeLookup>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los Tipos de Columnas");
                return Array.Empty<ColumnTypeLookup>();
            }
        }

        public async Task<IReadOnlyList<DataTypeLookup>> GetDataTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetDataTypesQuery();
                var result = await _queryDispatcher
                    .DispatchAsync<GetDataTypesQuery, Result<IReadOnlyList<DataTypeLookup>>>(query, cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsSuccess && result.Value != null)
                    return result.Value;

                _logger.LogWarning("Error al obtener Los Tipos de Datos: {Error}", result.Error);
                return Array.Empty<DataTypeLookup>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener Los Tipos de Datos");
                return Array.Empty<DataTypeLookup>();
            }
        }
    }
}
