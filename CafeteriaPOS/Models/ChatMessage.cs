using CommunityToolkit.Mvvm.ComponentModel;

namespace CafeteriaPOS.Models
{
    public enum Remitente
    {
        Usuario,
        Asistente,
        Sistema
    }

    public partial class ChatMessage : ObservableObject
    {
        public Remitente Remitente { get; set; }
        public string Texto { get; set; } = "";
        public System.DateTime Fecha { get; set; } = System.DateTime.Now;

        public bool EsUsuario => Remitente == Remitente.Usuario;

        public Microsoft.UI.Xaml.Media.Brush ColorFondo =>
            Remitente == Remitente.Usuario
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 213, 207, 186))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 59, 42, 25));

        public Microsoft.UI.Xaml.Media.Brush ColorTexto =>
            Remitente == Remitente.Usuario
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 56, 36, 14))
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 233, 218));

        public Microsoft.UI.Xaml.HorizontalAlignment Alineacion =>
            Remitente == Remitente.Usuario
                ? Microsoft.UI.Xaml.HorizontalAlignment.Right
                : Microsoft.UI.Xaml.HorizontalAlignment.Left;
    }
}