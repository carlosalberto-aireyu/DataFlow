
using DataFlow.Core.Common;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Models;
using DataFlow.UI.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DataFlow.UI.Services
{
    public interface IConfigTemplateManager : INotifyPropertyChanged
    {
        ObservableCollection<ConfigTemplateItemViewModel> Items { get; }
        bool IsBusy { get; }
        string? ErrorMessage { get; }
        Task<Result<ConfigTemplate>> LoadByIdAsync(int id, CancellationToken cancellationToken = default);
        Task RefreshAllAsync(CancellationToken cancellationToken = default);
        Task<Result<ConfigTemplate>> CreateAsync(CreateConfigTemplateCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<ConfigTemplate>> UpdateAsync(UpdateConfigTemplateCommand cmd, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(DeleteConfigTemplateCommand cmd, CancellationToken cancellationToken = default);
    }
}
