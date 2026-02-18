using DataFlow.UI.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DataFlow.UI.Converters
{
    /// <summary>
    /// Convertidor para traducir el estado de la cola.
    /// </summary>
    public class ProcessQueueStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is ProcessQueueItemStatus status)
            {
                return status switch
                {
                    ProcessQueueItemStatus.Pending => "Pendiente",
                    ProcessQueueItemStatus.Processing => "Procesado",
                    ProcessQueueItemStatus.Completed => "Completado",
                    ProcessQueueItemStatus.Failed => "Error",
                    _ => status.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
