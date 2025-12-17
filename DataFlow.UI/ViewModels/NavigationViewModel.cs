using DataFlow.UI.Commands;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataFlow.UI.ViewModels
{
    public class NavigationViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Frame _navFrame;
        private Type? _currentPageType;

        public ICommand PlantillaCommand { get; }
        public ICommand ColumnasCommand { get; }
        public ICommand OpcionesCommand { get; }
        public ICommand HistorialCommand { get; }
        public ICommand ProcesoCommand { get; }

        public NavigationViewModel(IServiceProvider serviceProvider, Frame navFrame)
        {
            _serviceProvider = serviceProvider;
            _navFrame = navFrame;
            PlantillaCommand = new RelayCommand(_ => Navigate(typeof(Pages.Plantilla)));
            ColumnasCommand = new RelayCommand(_ => Navigate(typeof(Pages.Columnas)));
            HistorialCommand = new RelayCommand(_ => Navigate(typeof(Pages.Historial)));
            OpcionesCommand = new RelayCommand(_ => Navigate(typeof(Pages.Opciones)));
            ProcesoCommand = new RelayCommand(_ => Navigate(typeof(Pages.Proceso)));
        }

        private void Navigate(Type pageType)
        {
            if (_currentPageType == typeof(Pages.Columnas) && _navFrame.Content is Pages.Columnas columnasPage)
            {
                System.Diagnostics.Debug.WriteLine("Descartando ediciones pendientes en Columnas antes de navegar");

                columnasPage.CancelOrCommitPendingEdits(preferCommit: false);
            }

            if (pageType == typeof(Pages.Columnas))
            {
                var templatesViewModel = _serviceProvider.GetRequiredService<ConfigTemplatesViewModel>();
                if (templatesViewModel.SelectedItem == null || templatesViewModel.SelectedItem.Id == 0)
                {
                    MessageBox.Show(
                        "Debe seleccionar una plantilla primero para acceder a las columnas.",
                        "Acceso Denegado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                var pageObj = _serviceProvider.GetRequiredService(pageType);
                if (pageObj is Page page)
                {
                    if (pageType == typeof(Pages.Columnas) && pageObj is Pages.Columnas columnasPage2)
                    {
                        var templatesViewModel = _serviceProvider.GetRequiredService<ConfigTemplatesViewModel>();
                        columnasPage2.SetSelectedTemplateId(templatesViewModel.SelectedItem?.Id ?? 0);
                    }
                    _navFrame.Navigate(page);
                    _currentPageType = pageType;
                    OnPropertyChanged(nameof(IsPlantillaSelected));
                    OnPropertyChanged(nameof(IsColumnasSelected));
                    OnPropertyChanged(nameof(IsHistorialSelected));
                    OnPropertyChanged(nameof(IsOpcionesSelected));
                    OnPropertyChanged(nameof(IsProcesoSelected));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en navegación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanNavigateToColumnas()
        {
            var templatesViewModel = _serviceProvider.GetRequiredService<ConfigTemplatesViewModel>();
            return templatesViewModel.SelectedItem != null && templatesViewModel.SelectedItem.Id != 0;
        }

        public bool IsPlantillaSelected => _currentPageType == typeof(Pages.Plantilla);
        public bool IsColumnasSelected => _currentPageType == typeof(Pages.Columnas);
        public bool IsHistorialSelected => _currentPageType == typeof(Pages.Historial);
        public bool IsOpcionesSelected => _currentPageType == typeof(Pages.Opciones);
        public bool IsProcesoSelected => _currentPageType == typeof(Pages.Proceso);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}