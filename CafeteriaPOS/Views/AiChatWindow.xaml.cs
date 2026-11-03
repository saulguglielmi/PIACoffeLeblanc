using CafeteriaPOS.Helpers;
using CafeteriaPOS.Models;
using CafeteriaPOS.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics;

namespace CafeteriaPOS.Views
{
    public sealed partial class AiChatWindow : Window
    {
        public ObservableCollection<ChatMessage> Mensajes { get; } = new();

        private bool _ocupado;

        public AiChatWindow()
        {
            InitializeComponent();

            AppWindow.Title = "Morgana · Asistente virtual";
            WindowIcon.Aplicar(this);
            CentrarVentana(480, 640);
            AplicarColoresTitulo();

            MostrarBienvenida();

            // Carga el modelo en segundo plano: solo la primera vez tarda.
            _ = CalentarAsync();
        }

        /// <summary>
        /// abre la ventana centrada en la pantalla actual, para que nunca
        /// aparezca fuera del área visible.
        /// </summary>
        private void CentrarVentana(int ancho, int alto)
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            var trabajo = area.WorkArea;

            var x = trabajo.X + Math.Max(0, (trabajo.Width - ancho) / 2);
            var y = trabajo.Y + Math.Max(0, (trabajo.Height - alto) / 2);

            AppWindow.MoveAndResize(new RectInt32(x, y, ancho, alto));
        }

        /// <summary>
        /// tiñe la barra de título con el marrón de la app para que la ventana
        /// combine con la temática del resto del sistema.
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

        private void MostrarBienvenida()
        {
            Mensajes.Add(new ChatMessage
            {
                Remitente = Remitente.Asistente,
                Texto = "¡Miau! Soy Morgana, la asistente de Café Leblanc.\n\n" +
                        "Puedo consultarte:\n" +
                        "• Precios y stock del menú\n" +
                        "• Ventas y tickets de hoy\n" +
                        "• Propinas por empleado\n" +
                        "• Formas de pago y horario\n" +
                        "• Qué alcanza con un presupuesto\n\n" +
                        "¿Qué necesitas, humano?"
            });
        }

        private static async Task CalentarAsync()
        {
            try
            {
                await MorganaService.Instance.PrecalentarAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void LimpiarChat_Click(object sender, RoutedEventArgs e)
        {
            Mensajes.Clear();
            MostrarBienvenida();
            PreguntaBox.Focus(FocusState.Programmatic);
        }

        private void PreguntaBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = EnviarAsync();
            }
        }

        private void Enviar_Click(object sender, RoutedEventArgs e)
        {
            _ = EnviarAsync();
        }

        private async Task EnviarAsync()
        {
            if (_ocupado) return;

            var pregunta = PreguntaBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(pregunta)) return;

            PreguntaBox.Text = "";
            _ocupado = true;
            EnviarBtn.IsEnabled = false;

            Mensajes.Add(new ChatMessage
            {
                Remitente = Remitente.Usuario,
                Texto = pregunta
            });

            var pensando = new ChatMessage
            {
                Remitente = Remitente.Asistente,
                Texto = "💭 Pensando..."
            };
            Mensajes.Add(pensando);
            ScrollAlFinal();

            string respuesta;
            try
            {
                // Solo el texto del usuario entra al modelo.
                respuesta = await MorganaService.Instance.PreguntarAsync(pregunta);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                respuesta = "No pude responder, humano. Intenta otra vez.";
            }
            finally
            {
                Mensajes.Remove(pensando);
                _ocupado = false;
                EnviarBtn.IsEnabled = true;
            }

            Mensajes.Add(new ChatMessage
            {
                Remitente = Remitente.Asistente,
                Texto = respuesta
            });

            ScrollAlFinal();
            PreguntaBox.Focus(FocusState.Programmatic);
        }

        private void ScrollAlFinal()
        {
            // pequeño delay para que la UI se actualice antes de hacer scroll
            DispatcherQueue.TryEnqueue(() =>
            {
                ChatScroll.ChangeView(null, ChatScroll.ScrollableHeight, null);
            });
        }
    }
}
