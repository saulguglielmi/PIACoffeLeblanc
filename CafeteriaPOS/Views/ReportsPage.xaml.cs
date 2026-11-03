using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

 //aqui exportamos en json y en csv

namespace CafeteriaPOS.Views
{
    public sealed partial class ReportsPage : Page
    {
        public ReportsViewModel ViewModel { get; } = new();

        public ReportsPage()
        {
            this.InitializeComponent();
        }

        private async void Exportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ruta = await ViewModel.ExportarUltimos7DiasAsync();

                if (ruta is null)
                {
                    ViewModel.MensajeEstado = "❌ Exportación cancelada.";
                    return;
                }

                ViewModel.MensajeEstado = $"✅ Exportado a: {ruta}";
            }
            catch (System.Exception ex)
            {
                ViewModel.MensajeEstado = $"❌ Error al exportar: {ex.Message}";
            }
        }

        private async void ExportarJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ruta = await ViewModel.ExportarJsonAsync();

                if (ruta is null)
                {
                    ViewModel.MensajeEstado = "❌ Exportación cancelada.";
                    return;
                }

                ViewModel.MensajeEstado = $"✅ Exportado a: {ruta}";
            }
            catch (System.Exception ex)
            {
                ViewModel.MensajeEstado = $"❌ Error al exportar: {ex.Message}";
            }
        }
    }
}