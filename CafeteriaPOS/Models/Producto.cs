using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CafeteriaPOS.Models
{
    public partial class Producto : ObservableObject
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public int StockMinimo { get; set; }

        [JsonIgnore]
        public bool StockBajo => Stock <= StockMinimo;

        [JsonIgnore]
        public bool Disponible => Stock > 0;

        [JsonIgnore]
        public string TextoStock => Disponible
            ? $"Stock: {Stock} • Mín: {StockMinimo}"
            : "Agotado";

        [JsonIgnore]
        public Microsoft.UI.Xaml.Media.Brush RellenoFondo =>
            !Disponible
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(45, 120, 120, 120))
                : StockBajo
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 200, 0))
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

        [JsonIgnore]
        public Microsoft.UI.Xaml.Media.Brush RellenoBorde =>
            !Disponible
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160))
                : StockBajo
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 180, 0))
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255));

        [JsonIgnore]
        public Microsoft.UI.Xaml.Media.Brush ColorStock =>
            !Disponible
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 190, 60, 60))
                : StockBajo
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100))
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 122, 97, 71));
    }
}