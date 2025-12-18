using DataFlow.UI.Services;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DataFlow.UI.Pages
{
    public partial class Plantilla : Page
    {
        private readonly ConfigTemplatesViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationStateService _appStateService;

        public Plantilla(ConfigTemplatesViewModel viewModel, 
            IServiceProvider serviceProvider,
            IApplicationStateService appStateService)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            DataContext = _viewModel;

            Loaded += Plantilla_Loaded;
            _appStateService = appStateService;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            //_appStateService.PropertyChanged += ApplicationStateService_PropertyChanged;

        }
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.SelectedItem))
            {
                UpdateSelectedTemplateInApplicationState();
            }
        }
        private void UpdateSelectedTemplateInApplicationState()
        {
            _appStateService.SelectedTemplate = _viewModel.SelectedItem;
        }

        private void Plantilla_Loaded(object? sender, RoutedEventArgs e)
        {
            Button? clearButton = FindVisualChild<Button>(this, "ClearButton");
            if (clearButton != null)
            {
                clearButton.Click += ClearButton_Click;
            }

            try
            {
                if (_viewModel.RefreshCommand.CanExecute(null))
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Cargando los datos de las plantillass: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            FilterInput.Text = string.Empty;

        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AgregarPlantillaDialog
            {
                Owner = Window.GetWindow(this)
            };
            if(dlg.ShowDialog() == true)
            {
                try
                {
                    var descripcion = dlg.Descripcion;
                    if(!string.IsNullOrWhiteSpace(descripcion))
                    {
                        if(_viewModel.CreateCommand.CanExecute(descripcion))
                        {
                            _viewModel.CreateCommand.Execute(descripcion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error agregando la plantilla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if(_viewModel.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar alguna plantilla para eliminar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar la plantilla '{_viewModel.SelectedItem.Description}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if(result == MessageBoxResult.Yes) {
                try
                {
                    if(_viewModel.DeleteCommand.CanExecute(null))
                    {
                        _viewModel.DeleteCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error eliminando la plantilla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if(_viewModel.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar alguna plantilla para editar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var dlg = new EditarPlantillaDialog
            {
                Owner = Window.GetWindow(this)
            };
            dlg.SetDescripcion(_viewModel.SelectedItem.Description);
            if(dlg.ShowDialog() == true)
            {
                try
                {
                    var descripcion = dlg.Descripcion?.Trim();
                    if(!string.IsNullOrWhiteSpace(descripcion))
                    {
                        _viewModel.SelectedItem.Description = descripcion;
                        if (_viewModel.EditCommand.CanExecute(null))
                        {
                            _viewModel.EditCommand.Execute(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editando la plantilla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
                
        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.RefreshCommand.CanExecute(null))
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error actualizando las plantillas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static T? FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child != null)
                {
                    if (child is T childT &&
                        child.GetValue(FrameworkElement.NameProperty) as string == childName)
                    {
                        return (T)child;
                    }
                    else
                    {
                        T? foundChild = FindVisualChild<T>(child, childName);
                        if (foundChild != null)
                            return foundChild;
                    }
                }
            }
            return null;
        }
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NavigateToColumnas();
        }
        private void NavigateToColumnas()
        {
            try
            {
                if (_viewModel.SelectedItem == null || _viewModel.SelectedItem.Id == 0)
                {
                    MessageBox.Show("Debe seleccionar una plantilla válida.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null && mainWindow.DataContext is NavigationViewModel navViewModel) { 
                
                    if (mainWindow.navFrame?.Content is Columnas columnasPage)
                    {
                        System.Diagnostics.Debug.WriteLine("Descartando ediciones pendientes antes de cambiar de plantilla");
                        columnasPage.CancelOrCommitPendingEdits(preferCommit: false);
                    }

                    navViewModel.ColumnasCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navegando a Columnas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
       
       
    }
}
