using DataFlow.BL.Constants;
using DataFlow.BL.Services;
using DataFlow.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataFlow.UI.Controls
{
    public partial class ProcessMonitorControl : UserControl
    {
        private ObservableCollection<ProcessNotificationViewModel> _notifications;
        private int _infoCount = 0;
        private int _warningCount = 0;
        private int _errorCount = 0;
        private IApplicationStateService _applicationStateService;

        public ProcessMonitorControl(IApplicationStateService applicationStateService)
        {
            InitializeComponent();
            _notifications = new ObservableCollection<ProcessNotificationViewModel>();
            NotificationsItemsControl.ItemsSource = _notifications;
            _applicationStateService = applicationStateService;
        }

        public void AddNotification(ProcessNotification notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new ProcessNotificationViewModel
                {
                    Level = notification.Level,
                    Message = notification.Message,
                    Details = notification.Details,
                    Timestamp = notification.Timestamp,
                    HasDetails = !string.IsNullOrEmpty(notification.Details)
                };

                _notifications.Add(viewModel);

                // Actualizar contadores
                switch (notification.Level)
                {
                    case ProcessNotificationLevel.Info:
                        _infoCount++;
                        InfoCountTextBlock.Text = _infoCount.ToString();
                        StatusTextBlock.Text = "Procesando...";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        break;
                    case ProcessNotificationLevel.Warning:
                        _warningCount++;
                        WarningCountTextBlock.Text = _warningCount.ToString();
                        break;
                    case ProcessNotificationLevel.Error:
                        _errorCount++;
                        ErrorCountTextBlock.Text = _errorCount.ToString();
                        StatusTextBlock.Text = "Error";
                        StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        break;
                }

                // Auto-scroll al final
                NotificationScrollViewer.ScrollToBottom();

                // Limitar a las últimas 500 notificaciones
                if (_notifications.Count > 500)
                {
                    _notifications.RemoveAt(0);
                }
            });
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _notifications.Clear();
                _infoCount = 0;
                _warningCount = 0;
                _errorCount = 0;
                InfoCountTextBlock.Text = "0";
                WarningCountTextBlock.Text = "0";
                ErrorCountTextBlock.Text = "0";
                StatusTextBlock.Text = "Listo";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            });
            _applicationStateService.NotificationsProcess.Clear();
        }

        public void MarkAsCompleted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_errorCount > 0)
                {
                    StatusTextBlock.Text = $"Completado con {_errorCount} error(es)";
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
                else if (_warningCount > 0)
                {
                    StatusTextBlock.Text = $"Completado con {_warningCount} advertencia(s)";
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                }
                else
                {
                    StatusTextBlock.Text = "✓ Completado exitosamente";
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
            });
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }
    }

    public class ProcessNotificationViewModel
    {
        public ProcessNotificationLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public bool HasDetails { get; set; }
    }
}
