using DataFlow.Core.Common;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Models;
using DataFlow.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public interface IHistProcessManager
    {
        bool IsBusy { get; }
        string? ErrorMessage { get; }

        Task<Result<IReadOnlyList<HistProcess>>> LoadByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken = default);

        Task<Result<HistProcess>> LoadByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Result<HistProcess>> CreateAsync(CreateHistProcessCommand cmd, CancellationToken cancellationToken = default);
        //Task<Result<ConfigColumn>> UpdateAsync( cmd, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(DeleteHistProcessCommand cmd, CancellationToken cancellationToken = default);
    }
}
