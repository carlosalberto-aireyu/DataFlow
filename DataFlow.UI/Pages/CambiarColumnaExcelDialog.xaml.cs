using DataFlow.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataFlow.UI.Pages
{
    /// <summary>
    /// Lógica de interacción para CambiarColumnaExcelDialog.xaml
    /// </summary>
    public partial class CambiarColumnaExcelDialog : Window
    {
        public string? NewColumnLetter { get; private set; }
        public CambiarColumnaExcelDialog()
        {
            InitializeComponent();
            NewColumnTextBox.Focus();
        }
        private void NewColumnTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateColumn();
        }
        private void ValidateColumn()
        {
            var input = NewColumnTextBox.Text?.Trim().ToUpperInvariant();

            OKButton.IsEnabled = false;
            ValidationMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));

            if (string.IsNullOrWhiteSpace(input))
            {
                ValidationMessage.Text = "Ingrese una columna (ej: A, B, AA)";
                return;
            }

            try
            {
                // Validar que sea una columna Excel válida
                ExcelAddressConverter.ColumnLettersToNumber(input);

                ValidationMessage.Text = $"✓ Columna '{input}' válida";
                ValidationMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                OKButton.IsEnabled = true;
            }
            catch (ArgumentException)
            {
                ValidationMessage.Text = $"✗ '{input}' no es una columna Excel válida. Use letras (A-Z, AA-ZZ, etc.)";
                ValidationMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var input = NewColumnTextBox.Text?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(input))
            {
                ValidationMessage.Text = "Debe ingresar una columna";
                ValidationMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                NewColumnTextBox.Focus();
                return;
            }

            try
            {
                ExcelAddressConverter.ColumnLettersToNumber(input);
                NewColumnLetter = input;
                DialogResult = true;
                Close();
            }
            catch (ArgumentException ex)
            {
                ValidationMessage.Text = $"Error: {ex.Message}";
                ValidationMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                NewColumnTextBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
