namespace CafeteriaPOS.Models
{
    public class ResumenDia
    {
        public string Fecha { get; set; } = "";
        public int CantidadVentas { get; set; }
        public decimal TotalSinPropinas { get; set; }
        public decimal TotalConPropinas { get; set; }
        public decimal PromedioVenta { get; set; }
    }

    public class ProductoTop
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int CantidadTotal { get; set; }
        public decimal MontoTotal { get; set; }
    }

    /// <summary>propinas del empleado.</summary>
    public class PropinasEmpleado
    {
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = "";
        public int CantidadVentas { get; set; }
        public decimal PropinasEfectivo { get; set; }
        public decimal PropinasTarjeta { get; set; }
        public decimal TotalPropinas => PropinasEfectivo + PropinasTarjeta;
    }
}