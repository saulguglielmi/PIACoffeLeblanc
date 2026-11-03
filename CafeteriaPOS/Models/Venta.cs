using System;

namespace CafeteriaPOS.Models
{
    public class Venta
    {
        public string IdVenta { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Propina { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = ""; // "efectivo" o "tarjeta", en forma visual en el xaml
    }
}