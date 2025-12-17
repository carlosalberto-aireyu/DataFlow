using DataFlow.Core.Models;
using DataFlow.UI.Services;
using DataFlow.UI.ViewModels;
using System; 
using System.Collections.Generic; 
using System.Linq; 
using System.Windows;
using System.Windows.Controls;

namespace DataFlow.UI.Pages
{
    public partial class EditarColumnaDialog : Window
    {
        private readonly ConfigColumnItemViewModel _columnViewModel;
        private readonly ILookupService _lookupService;

        public EditarColumnaDialog(ConfigColumnItemViewModel columnViewModel, ILookupService lookupService)
        {
            InitializeComponent();

            _columnViewModel = columnViewModel ?? throw new ArgumentNullException(nameof(columnViewModel));
            _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));

            
            DataContext = _columnViewModel;

            
            var dataTypes = _lookupService.GetDataTypesAsync().GetAwaiter().GetResult();
            var columnTypes = _lookupService.GetColumnTypesAsync().GetAwaiter().GetResult();

            DataTypeComboBox.ItemsSource = dataTypes;
            ColumnTypeComboBox.ItemsSource = columnTypes;

            

            
            IndexColumnTextBox.Text = _columnViewModel.IndexColumn.ToString();
            NombreTextBox.Text = _columnViewModel.Name;
            DisplayNameTextBox.Text = _columnViewModel.NameDisplay;
            DescripcionTextBox.Text = _columnViewModel.Description;
            DefaultValueTextBox.Text = _columnViewModel.DefaultValue;

            NombreTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            if (!int.TryParse(IndexColumnTextBox.Text?.Trim(), out int indexColumn) || indexColumn < 0)
            {
                ErrorMessage.Text = "El índice debe ser un número entero no negativo.";
                ErrorMessage.Visibility = Visibility.Visible;
                IndexColumnTextBox.Focus();
                return;
            }

            var nombre = NombreTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 2)
            {
                ErrorMessage.Text = "El nombre debe tener al menos 2 caracteres.";
                ErrorMessage.Visibility = Visibility.Visible;
                NombreTextBox.Focus();
                return;
            }

            var selectedDataTypeId = DataTypeComboBox.SelectedValue as int?;
            if (selectedDataTypeId == null)
            {
                ErrorMessage.Text = "Debe seleccionar un tipo de dato.";
                ErrorMessage.Visibility = Visibility.Visible;
                DataTypeComboBox.Focus();
                return;
            }

            var selectedColumnTypeId = ColumnTypeComboBox.SelectedValue as int?;
            if (selectedColumnTypeId == null)
            {
                ErrorMessage.Text = "Debe seleccionar un tipo de columna.";
                ErrorMessage.Visibility = Visibility.Visible;
                ColumnTypeComboBox.Focus();
                return;
            }

            _columnViewModel.IndexColumn = indexColumn;
            _columnViewModel.Name = nombre;
            _columnViewModel.NameDisplay = DisplayNameTextBox.Text?.Trim() ?? nombre;
            _columnViewModel.DataTypeId = selectedDataTypeId.Value;
            _columnViewModel.ColumnTypeId = selectedColumnTypeId.Value;
            _columnViewModel.DefaultValue = DefaultValueTextBox.Text?.Trim() ?? string.Empty;
            _columnViewModel.Description = DescripcionTextBox.Text?.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}