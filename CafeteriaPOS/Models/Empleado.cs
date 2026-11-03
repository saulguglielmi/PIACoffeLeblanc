using System.Text.Json.Serialization;

namespace CafeteriaPOS.Models
{
    public class Empleado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Password { get; set; } = "";
        public string Puesto { get; set; } = "";
        [JsonIgnore]
        public Microsoft.UI.Xaml.Media.Brush ColorPuesto =>
            Puesto == "Admin"
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 108, 92, 231))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 122, 97, 71));
    }

}