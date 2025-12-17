using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DataFlow.UI.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si no hay selección → Collapsed
            if (value == null) return Visibility.Collapsed;
            if (parameter == null) return Visibility.Collapsed;

            var valueStr = value.ToString();
            var paramStr = parameter.ToString();


            System.Diagnostics.Debug.WriteLine($"Comparando: '{valueStr}' == '{paramStr}'");

            // Compara como string (porque ParametroKey es string)
            return valueStr == paramStr
                 ? Visibility.Visible
                 : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
