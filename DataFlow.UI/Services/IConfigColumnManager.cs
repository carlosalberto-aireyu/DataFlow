using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataFlow.Core.Common;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Models;
using DataFlow.UI.ViewModels;

namespace DataFlow.UI.Services
{
    public interface IConfigColumnManager : INotifyPropertyChanged
    {
        ObservableCollection<ConfigColumnItemViewModel> Items { get; }
        bool IsBusy { get; }
        string? ErrorMessage { get; }

        Task LoadByIdAsync(int id, CancellationToken cancellationToken = default);
        Task RefreshAllAsync(int configTemplateId, CancellationToken cancellationToken = default);
        Task<Result<ConfigColumn>> CreateAsync(CreateConfigColumnCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<ConfigColumn>> UpdateAsync(UpdateConfigColumnCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(DeleteConfigColumnCommand cmd, CancellationToken cancellationToken = default);

        Task<Result<ColumnRange>> CreateRangeAsync(CreateColumnRangeCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<ColumnRange>> UpdateRangeAsync(UpdateColumnRangeCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteRangeAsync(DeleteColumnRangeCommand cmd, CancellationToken cancellationToken = default);
    }
}
