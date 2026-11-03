using CafeteriaPOS.Models;
using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CafeteriaPOS.Views
{
    public sealed partial class SalesPage : Page
    {
        public SalesViewModel ViewModel { get; } = new();

        public SalesPage()
        {
            this.InitializeComponent();
        }

        private void Producto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Producto p)
                ViewModel.AgregarAlCarritoCommand.Execute(p);
        }

        private void Aumentar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CarritoItem item)
                ViewModel.AumentarCommand.Execute(item);
        }

        private void Disminuir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CarritoItem item)
                ViewModel.DisminuirCommand.Execute(item);
        }
    }
}