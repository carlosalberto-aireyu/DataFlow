using DataFlow.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Constants
{
    public class LookupIdProvider
    {
        private readonly ILookupRepository _lookupRepository;

        public LookupIdProvider(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository;
        }

        public async Task<LookupIds> LoadAsync(CancellationToken ct = default)
        {
            var columnTypes = await _lookupRepository.GetAllColumnTypesAsync(ct);
            var dataTypes = await _lookupRepository.GetAllDataTypesAsync(ct);

            return new LookupIds
            {
                Constante = columnTypes.First(x => x.Code == "Constante").Id,
                Valor = columnTypes.First(x => x.Code == "Valor").Id,
                Dimension = columnTypes.First(x => x.Code == "Dimension").Id,

                Texto = dataTypes.First(x => x.Code== "Texto").Id,
                Numerico = dataTypes.First(x => x.Code == "Numerico").Id,
                Fecha = dataTypes.First(x => x.Code  == "Fecha").Id
            };
        }
    }

}
