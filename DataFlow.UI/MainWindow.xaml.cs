using DataFlow.UI.Services;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataFlow.UI
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.Title = $"{AlphaVersionService.GetVersionInfo()}";
            DisclaimerTextBlock.Text = $"{AlphaVersionService.GetVersionInfo()} | Importante: Este software se encuentra en fase ALPHA y puede contener errores. Los datos obtenidos deben ser revisados por el usuario. Este programa es propiedad exclusiva de Repsol y está destinado únicamente para uso interno. Este software no debe ser distribuido, copiado, modificado o utilizado fuera del ámbito corporativo © Repsol";
            _serviceProvider = serviceProvider;
            Loaded += MainWindow_Loaded;
            
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var navViewModel = new NavigationViewModel(_serviceProvider, navFrame);
            DataContext = navViewModel;
            navViewModel.PlantillaCommand.Execute(null);
        }
    }
}