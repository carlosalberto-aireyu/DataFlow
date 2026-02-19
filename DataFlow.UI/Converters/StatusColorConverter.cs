using DataFlow.UI.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace DataFlow.UI.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is Services.ProcessQueueItemStatus status)
            {
                return status switch
                {
                    ProcessQueueItemStatus.Pending => new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)),      // Azul
                    ProcessQueueItemStatus.Processing => new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)), // Naranja
                    ProcessQueueItemStatus.Completed => new SolidColorBrush(Color.FromArgb(255, 0, 128, 0)),    // Verde
                    ProcessQueueItemStatus.Failed => new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),        // Rojo
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
