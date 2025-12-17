using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataFlow.Core.Models;

namespace DataFlow.Core.Repositories
{
    public interface IParametroRepository : IRepository<Parametro>
    {
        Task<Parametro?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        

    }
}
