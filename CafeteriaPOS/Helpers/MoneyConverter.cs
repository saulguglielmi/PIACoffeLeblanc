using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace CafeteriaPOS.Helpers
{
    /// <summary>
    /// Formatea un número como moneda para mostrarlo en la interfaz.
    /// Solo afecta la presentación: los datos siguen guardándose como decimal.
    /// </summary>
    public sealed class MoneyConverter : IValueConverter
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            decimal monto = value switch
            {
                decimal d => d,
                double db => (decimal)db,
                float f => (decimal)f,
                int i => i,
                long l => l,
                _ => 0m
            };

            return monto.ToString("C2", Cultura);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
