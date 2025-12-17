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
    /// Lógica de interacción para AgregarPlantillaDialog.xaml
    /// </summary>
    public partial class AgregarPlantillaDialog : Window
    {
        public string? Descripcion { get; private set; }
        public AgregarPlantillaDialog()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var descripcion = DescripcionTextBox.Text?.Trim();
            if(string.IsNullOrWhiteSpace(descripcion))
            {
                ErrorMessage.Text = "La descripción no puede estar vacía.";
                ErrorMessage.Visibility = Visibility.Visible;
                DescripcionTextBox.Focus();
                return;
            }
            if(descripcion.Length < 3)
            {
                ErrorMessage.Text = "La descripción es muy corta.";
                ErrorMessage.Visibility = Visibility.Visible;
                DescripcionTextBox.Focus();
                return;
            }
            Descripcion = descripcion;
            this.DialogResult = true;
            this.Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
