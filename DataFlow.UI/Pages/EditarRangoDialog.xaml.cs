using DataFlow.UI.ViewModels;
using System.Windows;

namespace DataFlow.UI.Pages
{
    public partial class EditarRangoDialog : Window
    {
        private readonly ColumnRangeItemViewModel _rangeViewModel;

        public EditarRangoDialog(ColumnRangeItemViewModel rangeViewModel)
        {
            InitializeComponent();
            _rangeViewModel = rangeViewModel ?? throw new ArgumentNullException(nameof(rangeViewModel));

            // Cargar datos actuales
            DesdeTextBox.Text = _rangeViewModel.RFrom;
            HastaTextBox.Text = _rangeViewModel.RTo;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var desde = DesdeTextBox.Text?.Trim();
            var hasta = HastaTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(desde) && string.IsNullOrWhiteSpace(hasta))
            {
                ErrorMessage.Text = "Debe ingresar al menos un valor (Desde o Hasta).";
                ErrorMessage.Visibility = Visibility.Visible;
                DesdeTextBox.Focus();
                return;
            }

            // Actualizar el ViewModel
            _rangeViewModel.RFrom = desde;
            _rangeViewModel.RTo = hasta;

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