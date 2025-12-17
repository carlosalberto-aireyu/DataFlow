using DataFlow.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DataFlow.UI
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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