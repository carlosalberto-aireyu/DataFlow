using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Necesario para VisualTreeHelper
using DataFlow.UI.ViewModels;
using DataFlow.UI.Pages.Dialogs; // Asumiendo que InputParametroDialog está en esta ruta

namespace DataFlow.UI.Pages
{
    
    public partial class Opciones : Page
    {
        private readonly ParametrosViewModel _viewModel;

        public Opciones(ParametrosViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            Loaded += Opciones_Loaded;
        }

        private void Opciones_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.ItemCount <= 0)
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando los parámetros: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PonerNull(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedItem = null;

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputParametroDialog
            {
                Owner = Window.GetWindow(this),
                Title = "Agregar Parámetro",
                ParametroKey = string.Empty,
                ParametroValue = string.Empty,
                Description = string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var name = dialog.ParametroKey;
                    var value = dialog.ParametroValue;
                    var description = dialog.Description;

                    if (!string.IsNullOrWhiteSpace(name) && _viewModel.AddParametroCommand.CanExecute((name, value, description)))
                    {
                        _viewModel.AddParametroCommand.Execute((name, value, description));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error agregando el parámetro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un parámetro para editar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new InputParametroDialog
            {
                Owner = Window.GetWindow(this),
                Title = $"Editar Parámetro: {_viewModel.SelectedItem.Name}",
                ParametroKey = _viewModel.SelectedItem.ParametroKey,
                ParameterName = _viewModel.SelectedItem.Name,
                ParametroValue = _viewModel.SelectedItem.ParametroValue,
                Description = _viewModel.SelectedItem.Description ?? string.Empty,
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _viewModel.SelectedItem.ParametroKey = dialog.ParametroKey;
                    _viewModel.SelectedItem.ParametroValue= dialog.ParametroValue;
                    _viewModel.SelectedItem.Description = dialog.Description;

                    if (_viewModel.EditParametroCommand.CanExecute(_viewModel.SelectedItem))
                    {
                        _viewModel.EditParametroCommand.Execute(_viewModel.SelectedItem);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editando el parámetro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un parámetro para eliminar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar el parámetro '{_viewModel.SelectedItem.ParametroKey}'?",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_viewModel.DeleteParametroCommand.CanExecute(_viewModel.SelectedItem))
                    {
                        _viewModel.DeleteParametroCommand.Execute(_viewModel.SelectedItem);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error eliminando el parámetro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}