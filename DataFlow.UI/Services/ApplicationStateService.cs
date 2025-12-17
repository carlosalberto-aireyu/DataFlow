using DataFlow.BL.Services;
using DataFlow.Core.Constants;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public class ApplicationStateService : IApplicationStateService
    {
        private readonly IParametroManager _parametroManager;
        private readonly IConfigTemplateManager _templateManager;
        private string? _excelFilePath;
        private List<ParametroItemViewModel> _parametros;
        private ConfigTemplateItemViewModel? _selectedTemplate;
        private List<ProcessNotification> _notificationsProcess;

        private readonly ILogger<ApplicationStateService> _logger;

        public ApplicationStateService(ILogger<ApplicationStateService> logger,
            IParametroManager parametroManager,
            IConfigTemplateManager templateManager
            )
        {
            _logger = logger;
            _parametroManager = parametroManager;
            _parametros = new List<ParametroItemViewModel>();
            _templateManager = templateManager;
            _notificationsProcess = new List<ProcessNotification>();

        }
        public List<ProcessNotification> NotificationsProcess
        {
            get { return _notificationsProcess; } 
        }
        public List<ParametroItemViewModel> Parametros{ 
            get => _parametros; 
            set {
                if(_parametros != value)
                {
                    _parametros = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parametros)));
                }
            }
        }

        public ConfigTemplateItemViewModel? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (_selectedTemplate != value)
                {
                    _selectedTemplate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTemplate)));
                }
            }
        }

        public string? ExcelFilePath
        {
            get => _excelFilePath;
            set
            {
                if (_excelFilePath != value)
                {
                    _excelFilePath = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExcelFilePath)));
                    _logger.LogInformation("ExcelFilePath actualizado a: {Path}", value ?? "(nulo)");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task RefreshParametros(CancellationToken cancellationToken = default)
        {
            var result = await _parametroManager.LoadAllAsync(cancellationToken);
            if (result.IsSuccess && result.Value != null)
            {
                Parametros = result.Value;
            }
            else
            {
                _logger.LogError("Error al cargar los parámetros: {Error}", result.Error);
            }
        }
        public string? GetParametroValue(ParametroKey key)
        {
            return _parametros.FirstOrDefault(p => p.ParametroKey == key.ToString())?.ParametroValue;
        }
    }
}
