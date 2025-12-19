using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;

namespace DataFlow.UI.Converters
{
    public class JsonPrettyPrintConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return "(sin datos)";

            try
            {
                using var doc = JsonDocument.Parse(s);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                // No es JSON válido: retornar texto tal cual
                return s;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}