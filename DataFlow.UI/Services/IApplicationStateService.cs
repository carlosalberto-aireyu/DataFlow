using DataFlow.BL.Services;
using DataFlow.Core.Constants;
using DataFlow.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public interface IApplicationStateService : INotifyPropertyChanged
    {
        string? ExcelFilePath { get; set; }
        List<ProcessNotification> NotificationsProcess { get; }
        ConfigTemplateItemViewModel? SelectedTemplate { get; set; }
        public Task RefreshParametros(CancellationToken cancellationToken = default);
        string? GetParametroValue(ParametroKey key);

    }
}
