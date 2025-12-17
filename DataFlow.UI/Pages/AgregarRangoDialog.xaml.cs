using System.Windows;

namespace DataFlow.UI.Pages
{
    public partial class AgregarRangoDialog : Window
    {
        public string? RangeFrom { get; private set; }
        public string? RangeTo { get; private set; }
        public string? DefaultValue { get; set; }

        public AgregarRangoDialog()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var desde = DesdeTextBox.Text?.Trim().ToUpperInvariant();
            var hasta = HastaTextBox.Text?.Trim().ToUpperInvariant();
            var defaultValue = DefaultValueTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(desde) && string.IsNullOrWhiteSpace(hasta))
            {
                ErrorMessage.Text = "Debe ingresar al menos un valor (Desde o Hasta).";
                ErrorMessage.Visibility = Visibility.Visible;
                DesdeTextBox.Focus();
                return;
            }

            RangeFrom = desde;
            RangeTo = hasta;
            DefaultValue = defaultValue;
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