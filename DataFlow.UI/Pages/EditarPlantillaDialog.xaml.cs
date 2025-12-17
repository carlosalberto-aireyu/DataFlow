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
    public partial class EditarPlantillaDialog : Window
    {
        public string? Descripcion { get; private set; }
        public EditarPlantillaDialog()
        {
            InitializeComponent();
        }
        public void SetDescripcion(string? descripcion)
        {
            DescripcionTextBox.Text = descripcion;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var descripcion = DescripcionTextBox.Text;
            if(string.IsNullOrWhiteSpace(descripcion))
            {
                ErrorMessage.Text = "La descripción es requerida.";
                ErrorMessage.Visibility = Visibility.Visible;
                DescripcionTextBox.Focus();
                return;
            }
            if(descripcion.Length < 3)
            { 
                ErrorMessage.Text = "La descripción debe tener al menos 3 caracteres.";
                ErrorMessage.Visibility = Visibility.Visible;
                DescripcionTextBox.Focus();
                return;
            }
            Descripcion = descripcion;
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
