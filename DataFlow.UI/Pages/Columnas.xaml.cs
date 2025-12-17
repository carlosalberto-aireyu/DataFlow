using DataFlow.Core.Constants;
using DataFlow.UI.Commands;
using DataFlow.UI.Helpers;
using DataFlow.UI.Services;
using DataFlow.UI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataFlow.UI.Pages
{
    public partial class Columnas : Page
    {
        private readonly ConfigColumnsViewModel _viewModel;
        private bool _isClosing = false;
        private readonly ILookupService _lookupService;
        private readonly LookupIds _lookupIds; 

        public Columnas(
            ConfigColumnsViewModel viewModel,
            ILookupService lookupService,
            LookupIds lookupIds) 
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
            _lookupIds = lookupIds ?? throw new ArgumentNullException(nameof(lookupIds)); 

            DataContext = _viewModel;

            Loaded += Columnas_Loaded;
            Unloaded += Columnas_Unloaded;
        }

        public void SetSelectedTemplateId(int templateId)
        {
            _viewModel.SelectedTemplateId = templateId;
        }

        
        private bool IsSelectedColumnDimension()
        {
            return _viewModel.SelectedColumn?.ColumnTypeId == _lookupIds.Dimension;
        }

        
        private void UpdateRangesDimensionFlag()
        {
            if (_viewModel.SelectedColumn?.Ranges == null) return;

            bool isDimension = IsSelectedColumnDimension();
            foreach (var range in _viewModel.SelectedColumn.Ranges)
            {
                range.IsDimensionColumn = isDimension;
            }
        }

        public bool CancelOrCommitPendingEdits(bool preferCommit = false)
        {
            try
            {
             
                CancelCollectionTransactions();

             
                Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

             
                bool success = EditTransactionHelper.TryCommitOrCancelMultiple(
                    preferCommit,
                    ColumnsDataGrid,
                    RangesDataGrid);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("Ediciones pendientes canceladas exitosamente");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Hubo problemas al cancelar ediciones");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CancelOrCommitPendingEdits: {ex.Message}");
                return false;
            }
        }

        
        private void CancelCollectionTransactions()
        {
            try
            {
                // Cancelar transacciones en Ranges
                if (_viewModel.SelectedColumn?.Ranges != null)
                {
                    var rangesView = System.Windows.Data.CollectionViewSource.GetDefaultView(_viewModel.SelectedColumn.Ranges);
                    if (rangesView is System.ComponentModel.IEditableCollectionView editableView)
                    {
                        if (editableView.IsAddingNew)
                        {
                            editableView.CancelNew();
                        }
                        if (editableView.IsEditingItem)
                        {
                            editableView.CancelEdit();
                        }
                    }
                }

                // Cancelar transacciones en Columns
                if (_viewModel.Columns != null)
                {
                    var columnsView = System.Windows.Data.CollectionViewSource.GetDefaultView(_viewModel.Columns);
                    if (columnsView is System.ComponentModel.IEditableCollectionView editableColumnsView)
                    {
                        if (editableColumnsView.IsAddingNew)
                        {
                            editableColumnsView.CancelNew();
                        }
                        if (editableColumnsView.IsEditingItem)
                        {
                            editableColumnsView.CancelEdit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cancelando transacciones de colección: {ex.Message}");
            }
        }

        private void Columnas_Loaded(object? sender, RoutedEventArgs e)
        {
            Button? clearButton = FindVisualChild<Button>(this, "ClearButton");
            if (clearButton != null)
            {
                clearButton.Click += ClearButton_Click;
            }

            if (RangesDataGrid != null)
            {
                RangesDataGrid.PreviewKeyDown += RangesDataGrid_PreviewKeyDown;
            }

            try
            {
                if (_viewModel.SelectedTemplateId <= 0)
                {
                    MessageBox.Show(
                        "No se ha seleccionado una plantilla válida.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                if (_viewModel.RefreshCommand.CanExecute(null))
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando los datos de las columnas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Columnas_Unloaded(object? sender, RoutedEventArgs e)
        {
            _isClosing = true;

            CancelOrCommitPendingEdits(preferCommit: false);

            if (RangesDataGrid != null)
            {
                RangesDataGrid.PreviewKeyDown -= RangesDataGrid_PreviewKeyDown;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditColumnButton_Click(sender, e);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            FilterInput.Text = string.Empty;
        }

        private async void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AgregarColumnaDialog(_viewModel.Columns.Count + 1, _lookupService)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var columnData = new
                    {
                        IndexColumn = dialog.IndexColumn,
                        ColumnName = dialog.ColumnName,
                        DisplayName = dialog.DisplayName,
                        Description = dialog.Description,
                        DataTypeId = dialog.DataTypeId,
                        DefaultValue = dialog.DefaultValue,
                        ColumnTypeId = dialog.ColumnTypeId
                    };
                    if (!string.IsNullOrWhiteSpace(columnData.ColumnName))
                    {
                        if (_viewModel.CreateCommand.CanExecute(columnData))
                        {
                            if (_viewModel.CreateCommand is IAsyncCommand<dynamic> asyncCmd)
                            {
                                await asyncCmd.ExecuteAsync(columnData);
                            }
                            else
                            {
                                _viewModel.CreateCommand.Execute(columnData);
                            }

                            await Task.Delay(100);
                        }

                        if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
                        {
                            MessageBox.Show(
                                _viewModel.ErrorMessage,
                                "Error al crear columna",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error agregando la columna: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedColumn == null)
            {
                MessageBox.Show("Debe seleccionar una columna para editar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditarColumnaDialog(_viewModel.SelectedColumn, _lookupService)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_viewModel.EditCommand.CanExecute(null))
                    {
                        if (_viewModel.EditCommand is IAsyncCommand asyncCmd)
                        {
                            await asyncCmd.ExecuteAsync(null);
                        }
                        else
                        {
                            _viewModel.EditCommand.Execute(null);
                        }

                        await Task.Delay(100);
                    }


                    UpdateRangesDimensionFlag();

                    if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
                    {
                        MessageBox.Show(
                            _viewModel.ErrorMessage,
                            "Error al editar columna",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editando la columna: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedColumn == null)
            {
                MessageBox.Show("Debe seleccionar una columna para eliminar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar la columna '{_viewModel.SelectedColumn.DisplayName}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_viewModel.DeleteCommand.CanExecute(null))
                    {
                        _viewModel.DeleteCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error eliminando la columna: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedColumn == null)
            {
                MessageBox.Show("Debe seleccionar una columna primero.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            if (IsSelectedColumnDimension())
            {
                var incompleteRanges = _viewModel.SelectedColumn.Ranges?
                    .Where(r => r.IsNewRow || string.IsNullOrWhiteSpace(r.DefaultValue))
                    .ToList();

                if (incompleteRanges != null && incompleteRanges.Any())
                {
                    MessageBox.Show(
                        "Para columnas de tipo Dimensión, debe completar todos los rangos existentes (incluyendo el Valor por Defecto) antes de agregar uno nuevo.",
                        "Validación requerida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var dialog = new AgregarRangoDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (IsSelectedColumnDimension() && string.IsNullOrWhiteSpace(dialog.DefaultValue))
                    {
                        MessageBox.Show(
                            "Para columnas de tipo Dimensión, el Valor por Defecto es obligatorio.",
                            "Validación requerida",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var rangeData = (dialog.RangeFrom, dialog.RangeTo, dialog.DefaultValue);
                    if (!string.IsNullOrWhiteSpace(rangeData.RangeFrom) || !string.IsNullOrWhiteSpace(rangeData.RangeTo))
                    {
                        if (_viewModel.CreateRangeCommand.CanExecute(rangeData))
                        {
                            _viewModel.CreateRangeCommand.Execute(rangeData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error agregando el rango: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRange == null)
            {
                MessageBox.Show("Debe seleccionar un rango para editar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditarRangoDialog(_viewModel.SelectedRange)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (_viewModel.EditRangeCommand.CanExecute(null))
                    {
                        _viewModel.EditRangeCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editando el rango: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedRange == null)
            {
                MessageBox.Show("Debe seleccionar un rango para eliminar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar el rango '{_viewModel.SelectedRange.DisplayName}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_viewModel.DeleteRangeCommand.CanExecute(null))
                    {
                        _viewModel.DeleteRangeCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error eliminando el rango: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        
        private void RangesDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
         
            if (IsSelectedColumnDimension())
            {
                var incompleteRanges = _viewModel.SelectedColumn?.Ranges?
                    .Where(r => r.Id == 0 || string.IsNullOrWhiteSpace(r.DefaultValue) || !r.IsValid)
                    .ToList();

                if (incompleteRanges != null && incompleteRanges.Any())
                {
                    MessageBox.Show(
                        "Para columnas de tipo Dimensión, debe completar todos los rangos existentes (incluyendo el Valor por Defecto) antes de agregar uno nuevo.",
                        "Validación requerida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    e.NewItem = null;
                    return;
                }
            }

            var newRange = new ColumnRangeItemViewModel
            {
                Id = 0,
                ConfigColumnId = _viewModel?.SelectedColumn?.Id ?? 0,
                IsDimensionColumn = IsSelectedColumnDimension() // ✅ Establecer el flag
            };

            e.NewItem = newRange;
        }

        
        private async void RangesDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (_isClosing || _viewModel?.SelectedColumn?.Ranges == null)
            {
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit && e.Row.DataContext is ColumnRangeItemViewModel range)
            {

                range.IsDimensionColumn = IsSelectedColumnDimension();

                _viewModel.SelectedRange = range;

                if (range.IsNewRow)
                {

                    if (!range.IsValid)
                    {
                        e.Cancel = true;
                        MessageBox.Show(
                            range.ValidationMessage,
                            "Validación requerida",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        if (range.RFrom is null || range.RTo is null)
                        {
                            e.Cancel = true;
                            MessageBox.Show(
                                "Los valores de rango no pueden ser nulos.",
                                "Error de validación",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }


                        if (IsSelectedColumnDimension() && string.IsNullOrWhiteSpace(range.DefaultValue))
                        {
                            e.Cancel = true;
                            MessageBox.Show(
                                "Para columnas de tipo Dimensión, el Valor por Defecto es obligatorio.",
                                "Error de validación",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        var rangeData = (range.RFrom, range.RTo, range.DefaultValue);

                        if (_viewModel.CreateRangeCommand.CanExecute(rangeData))
                        {
                            if (_viewModel.CreateRangeCommand is IAsyncCommand<(string, string, string?)> asyncCmd)
                            {
                                await asyncCmd.ExecuteAsync(rangeData);
                            }
                            else
                            {
                                _viewModel.CreateRangeCommand.Execute(rangeData);
                            }
                            await Task.Delay(200);
                        }

                        if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
                        {
                            e.Cancel = true;
                            MessageBox.Show(
                                _viewModel.ErrorMessage,
                                "Error al guardar rango",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        e.Cancel = true;
                        MessageBox.Show(
                            $"Error guardando el rango: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                else if (range.Id > 0)
                {
                    try
                    {
                        if (!range.IsValid)
                        {
                            e.Cancel = true;
                            MessageBox.Show(
                                range.ValidationMessage,
                                "Error de validación",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        if (_viewModel.EditRangeCommand.CanExecute(null))
                        {
                            if (_viewModel.EditRangeCommand is IAsyncCommand asyncCmd)
                            {
                                await asyncCmd.ExecuteAsync(null);
                            }
                            else
                            {
                                _viewModel.EditRangeCommand.Execute(null);
                            }

                            await Task.Delay(200);
                        }

                        if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
                        {
                            e.Cancel = true;
                            MessageBox.Show(
                                _viewModel.ErrorMessage,
                                "Error al actualizar rango",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        e.Cancel = true;
                        MessageBox.Show(
                            $"Error actualizando el rango: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RangesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab) return;
            if (!(sender is DataGrid dg)) return;

            var current = dg.CurrentCell;
            if (current.Column == null) return;

            var currentItem = current.Item as ColumnRangeItemViewModel;

            int currentDisplay = current.Column.DisplayIndex;
            int direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
            int colCount = dg.Columns.Count;
            int rowIndex = dg.Items.IndexOf(current.Item);

            int nextDisplay = currentDisplay + direction;
            int nextRowIndex = rowIndex;
            if (nextDisplay >= colCount)
            {
                nextDisplay = 0;
                nextRowIndex = rowIndex + 1;
            }
            else if (nextDisplay < 0)
            {
                nextDisplay = colCount - 1;
                nextRowIndex = rowIndex - 1;
            }

            if (nextRowIndex < 0 || nextRowIndex >= dg.Items.Count)
            {
                e.Handled = true;
                return;
            }

            dg.CommitEdit(DataGridEditingUnit.Cell, true);

            bool movingToDifferentRow = nextRowIndex != rowIndex;
            if (movingToDifferentRow)
            {
                if (currentItem != null && currentItem.IsNewRow && !currentItem.IsValid)
                {
                    MessageBox.Show(
                        currentItem.ValidationMessage,
                        "Validación requerida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    dg.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        dg.CurrentCell = new DataGridCellInfo(dg.Items[rowIndex], current.Column);
                        dg.BeginEdit();
                        var cell = GetCell(dg, rowIndex, current.Column);
                        cell?.Focus();
                    }), DispatcherPriority.Background);

                    e.Handled = true;
                    return;
                }

                bool rowCommitted = dg.CommitEdit(DataGridEditingUnit.Row, true);
                if (!rowCommitted)
                {
                    dg.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        dg.CurrentCell = new DataGridCellInfo(dg.Items[rowIndex], current.Column);
                        dg.BeginEdit();
                        var cell = GetCell(dg, rowIndex, current.Column);
                        cell?.Focus();
                    }), DispatcherPriority.Background);

                    e.Handled = true;
                    return;
                }
            }

            var nextColumn = dg.Columns.FirstOrDefault(c => c.DisplayIndex == nextDisplay);
            if (nextColumn == null)
            {
                e.Handled = true;
                return;
            }

            dg.CurrentCell = new DataGridCellInfo(dg.Items[nextRowIndex], nextColumn);

            dg.Dispatcher.BeginInvoke((Action)(() =>
            {
                dg.ScrollIntoView(dg.Items[nextRowIndex], nextColumn);
                dg.BeginEdit();

                var cell = GetCell(dg, nextRowIndex, nextColumn);
                if (cell != null)
                {
                    cell.Focus();

                    var content = nextColumn.GetCellContent(dg.Items[nextRowIndex]) as FrameworkElement;
                    if (content != null)
                    {
                        var tb = FindVisualChild<TextBox>(content);
                        if (tb != null)
                        {
                            tb.Focus();
                            Keyboard.Focus(tb);
                            tb.SelectAll();
                        }
                        else
                        {
                            content.Focus();
                            Keyboard.Focus(content);
                        }
                    }
                }
            }), DispatcherPriority.Background);

            e.Handled = true;
        }

        private DataGridCell? GetCell(DataGrid grid, int rowIndex, DataGridColumn column)
        {
            var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
            if (row == null) return null;
            var presenter = FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null)
            {
                grid.ScrollIntoView(row.Item);
                presenter = FindVisualChild<DataGridCellsPresenter>(row);
                if (presenter == null) return null;
            }
            int cellIndex = column.DisplayIndex;
            var cell = (DataGridCell?)presenter.ItemContainerGenerator.ContainerFromIndex(cellIndex);
            return cell;
        }

        private static T? FindVisualChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child != null)
                {
                    if (child is T childT &&
                        (childName == null || child.GetValue(FrameworkElement.NameProperty) as string == childName))
                    {
                        return childT;
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

        private void RangesDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.DataContext is ColumnRangeItemViewModel range && range.IsNewRow)
            {
             
                range.IsDimensionColumn = IsSelectedColumnDimension();

                if (string.IsNullOrEmpty(range.RFrom) && string.IsNullOrEmpty(range.RTo))
                {
                    range.RFrom = null;
                    range.RTo = null;
                    range.DefaultValue = null;
                    range.Id = 0;
                }
            }
        }
    }
}