using CafeteriaPOS.Helpers;
using CafeteriaPOS.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace CafeteriaPOS
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public MainWindow()
        {
            this.InitializeComponent();

            AppWindow.Title = "Café Leblanc";
            WindowIcon.Aplicar(this);
            AplicarColoresTitulo();
            AjustarTamanoInicial();

            RootFrame.Navigate(typeof(LoginPage));
        }

        /// <summary>
        /// Abre la ventana con un tamaño cómodo para el POS. Sin esto, WinUI
        /// arranca en 960x545 y el buscador de Punto de Venta se queda sin
        /// espacio cuando el menú lateral está expandido.
        /// </summary>
        private void AjustarTamanoInicial()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                uint dpi = GetDpiForWindow(hwnd);
                double escala = dpi > 0 ? dpi / 96.0 : 1.0;

                var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
                var trabajo = area.WorkArea;
                int margen = (int)Math.Round(32 * escala);

                int anchoMax = Math.Max(1, trabajo.Width - margen);
                int altoMax = Math.Max(1, trabajo.Height - margen);

                int ancho = Math.Min((int)Math.Round(1180 * escala), anchoMax);
                int alto = Math.Min((int)Math.Round(720 * escala), altoMax);

                int anchoMin = Math.Min((int)Math.Round(900 * escala), anchoMax);
                int altoMin = Math.Min((int)Math.Round(600 * escala), altoMax);

                ancho = Math.Max(ancho, anchoMin);
                alto = Math.Max(alto, altoMin);

                int x = trabajo.X + Math.Max(0, (trabajo.Width - ancho) / 2);
                int y = trabajo.Y + Math.Max(0, (trabajo.Height - alto) / 2);

                AppWindow.MoveAndResize(new RectInt32(x, y, ancho, alto));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// pinta la barra de título con el marrón de la app para que la
        /// ventana combine con la temática.
        /// </summary>
        private void AplicarColoresTitulo()
        {
            try
            {
                var barra = AppWindow.TitleBar;

                var fondo = Windows.UI.Color.FromArgb(255, 56, 36, 14);
                var fondoInactivo = Windows.UI.Color.FromArgb(255, 46, 30, 12);
                var texto = Windows.UI.Color.FromArgb(255, 243, 238, 226);
                var textoInactivo = Windows.UI.Color.FromArgb(255, 199, 183, 156);
                var hover = Windows.UI.Color.FromArgb(255, 76, 57, 34);
                var pulsado = Windows.UI.Color.FromArgb(255, 90, 70, 46);

                barra.BackgroundColor = fondo;
                barra.InactiveBackgroundColor = fondoInactivo;
                barra.ForegroundColor = texto;
                barra.InactiveForegroundColor = textoInactivo;

                barra.ButtonBackgroundColor = fondo;
                barra.ButtonInactiveBackgroundColor = fondoInactivo;
                barra.ButtonForegroundColor = texto;
                barra.ButtonInactiveForegroundColor = textoInactivo;
                barra.ButtonHoverBackgroundColor = hover;
                barra.ButtonHoverForegroundColor = texto;
                barra.ButtonPressedBackgroundColor = pulsado;
                barra.ButtonPressedForegroundColor = texto;
            }
            catch (Exception ex)
            {
                // en versiones de Windows sin personalización de la barra
                // se queda el color del sistema.
                Debug.WriteLine(ex);
            }
        }
    }
}
