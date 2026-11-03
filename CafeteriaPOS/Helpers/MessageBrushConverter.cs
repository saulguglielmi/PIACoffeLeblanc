using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace CafeteriaPOS.Helpers
{
    /// <summary>
    /// Pinta en verde los mensajes de éxito (prefijo ✅ o 🗑️) y en rojo
    /// las advertencias y los errores.
    /// </summary>
    public sealed class MessageBrushConverter : IValueConverter
    {
        private static readonly Brush Exito =
            new SolidColorBrush(Color.FromArgb(255, 0, 122, 94));

        private static readonly Brush Error =
            new SolidColorBrush(Color.FromArgb(255, 198, 40, 40));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string texto = value as string ?? "";
            bool esExito = texto.StartsWith("✅", StringComparison.Ordinal) ||
                           texto.StartsWith("🗑", StringComparison.Ordinal);
            return esExito ? Exito : Error;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            throw new NotSupportedException();
    }
}
