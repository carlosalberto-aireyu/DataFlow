using DataFlow.Core.Common; // Para Result<T>
using DataFlow.UI.ViewModels; // Para ParametroItemViewModel
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // Para Task<Result<List<ParametroItemViewModel>>>

namespace DataFlow.UI.Services
{
    public interface IParametroManager : INotifyPropertyChanged
    {
        ObservableCollection<ParametroItemViewModel> Items { get; }

        bool IsBusy { get; }
        string? ErrorMessage { get; }

        Task<Result<List<ParametroItemViewModel>>> LoadAllAsync(CancellationToken cancellationToken = default); 
        Task<Result<ParametroItemViewModel>> CreateAsync(string key, string name, string value, string? description = null, CancellationToken cancellationToken = default);
        Task<Result<ParametroItemViewModel>> UpdateAsync(ParametroItemViewModel parametro, CancellationToken cancellationToken = default);
        Task<Result<bool>> ExportarInformacion(ParametroItemViewModel parametro, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(ParametroItemViewModel parametro, CancellationToken cancellationToken = default);
        Task<string?> GetParametroValueByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<Result<bool>> SetParametroValueByKeyAsync(string key, string value, CancellationToken cancellationToken = default);
    }
}