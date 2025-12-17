using DataFlow.UI.Services;
using System.Windows;

namespace DataFlow.UI.Pages
{
    public partial class AgregarColumnaDialog : Window
    {
        private readonly AgregarColumnaDialogViewModel _viewModel;

        public int IndexColumn => _viewModel.IndexColumn;
        public string? ColumnName => _viewModel.ColumnName;
        public string? DisplayName => _viewModel.DisplayName;
        public string? Description => _viewModel.Description;
        public int DataTypeId => _viewModel.SelectedDataType?.Id ?? default;
        public string? DefaultValue => _viewModel.DefaultValue;
        public int ColumnTypeId => _viewModel.SelectedColumnType?.Id ?? default;

        public AgregarColumnaDialog(int initialIndex, ILookupService lookupService)
        {
            InitializeComponent();
            _viewModel = new AgregarColumnaDialogViewModel(lookupService);
            _viewModel.IndexColumn = initialIndex;
            DataContext = _viewModel;
            NombreTextBox.Focus();

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var columnName = _viewModel.ColumnName?.Trim();
            if (_viewModel.IndexColumn < 0)
            {
                ErrorMessage.Text = "El valor de indice de la columna no es valido.";
                ErrorMessage.Visibility = Visibility.Visible;
                NombreTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                ErrorMessage.Text = "El nombre de la columna no puede estar vacío.";
                ErrorMessage.Visibility = Visibility.Visible;
                NombreTextBox.Focus();
                return;
            }

            if (columnName.Length < 2)
            {
                ErrorMessage.Text = "El nombre debe tener al menos 2 caracteres.";
                ErrorMessage.Visibility = Visibility.Visible;
                NombreTextBox.Focus();
                return;
            }

            if (_viewModel.SelectedDataType is null) {
                ErrorMessage.Text = "El tipo de dato de la columna es requerido.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
                if (_viewModel.SelectedColumnType is null)
            {
                ErrorMessage.Text = "El tipo de columna es requerido.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

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