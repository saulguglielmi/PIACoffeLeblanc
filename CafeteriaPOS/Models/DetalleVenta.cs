using System;

namespace CafeteriaPOS.Models
{
    public class DetalleVenta
    {
        public string IdVenta { get; set; } = "";
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}