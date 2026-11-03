using CommunityToolkit.Mvvm.ComponentModel;

namespace CafeteriaPOS.Models
{
    public partial class CarritoItem : ObservableObject
    {
        public Producto Producto { get; }

        [ObservableProperty]
        private int cantidad;

        public decimal Subtotal => Producto.Precio * Cantidad;

        public CarritoItem(Producto producto, int cantidad = 1)
        {
            Producto = producto;
            Cantidad = cantidad;
        }

        // cuando cambia la cantidad, notificamos también el subtotal.
        partial void OnCantidadChanged(int value)
        {
            OnPropertyChanged(nameof(Subtotal));
        }
    }
}