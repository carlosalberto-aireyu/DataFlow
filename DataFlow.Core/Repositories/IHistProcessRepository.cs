using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public interface IHistProcessRepository : IRepository<HistProcess>
    {
        Task<IReadOnlyList<HistProcess>> GetByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken = default);
    }
}
