using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace DataFlow.UI.Pages.Dialogs
{
    public partial class InputParametroDialog : Window, INotifyPropertyChanged
    {
        private string _parametroKey = string.Empty;
        private string _name= string.Empty;
        private string _parametroValue = string.Empty;
        private string _description = string.Empty;
        private bool _disableKeyEdit = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ParametroKey
        {
            get => _parametroKey;
            set => SetProperty(ref _parametroKey, value);
        }
        public string ParameterName
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string ParametroValue
        {
            get => _parametroValue;
            set => SetProperty(ref _parametroValue, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool DisableKeyEdit
        {
            get => _disableKeyEdit;
            set
            {
                if (SetProperty(ref _disableKeyEdit, value))
                {
                    if (NameTextBox != null)
                    {
                        NameTextBox.IsEnabled = !value;
                    }
                }
            }
        }

        public InputParametroDialog()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (NameTextBox != null)
                {
                    NameTextBox.IsEnabled = !DisableKeyEdit;
                }
            };
        }

        public void ClearInputs()
        {
            ParametroKey = string.Empty;
            Name = string.Empty;
            ParametroValue = string.Empty;
            Description = string.Empty;
            DisableKeyEdit = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ParametroKey))
            {
                MessageBox.Show("La clave del parámetro es obligatoria.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(ParametroValue))
            {
                MessageBox.Show("El valor del parámetro es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
