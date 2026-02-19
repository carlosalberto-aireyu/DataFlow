using System;
using System.Globalization;
using System.Windows.Data;

namespace DataFlow.UI.Converters
{
    /// <summary>
    /// Convertidor que extrae solo la carpeta de una ruta completa de archivo.
    /// </summary>
    public class FilePathToDirectoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && !string.IsNullOrWhiteSpace(filePath))
            {
                string? directory = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    directory +=directory + "\\";
                return directory ?? filePath;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}