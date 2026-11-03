using CafeteriaPOS.Helpers;
using CafeteriaPOS.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CafeteriaPOS.Services
{
    public class TicketService
    {
        private const int Ancho = 42;

        public string GenerarTicket(Venta venta, List<DetalleVenta> detalles, decimal pagoCliente = 0m)
        {
            var sb = new StringBuilder();

            // Encabezado
            sb.AppendLine(Centrar("☕ CAFETERÍA LEBLANC"));
            sb.AppendLine(Centrar("Ticket de venta"));
            sb.AppendLine(new string('=', Ancho));
            sb.AppendLine($"Ticket:  {venta.IdVenta}");
            sb.AppendLine($"Fecha:   {venta.FechaHora:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Cajero:  {venta.NombreEmpleado}");
            sb.AppendLine(new string('-', Ancho));

            // Detalle
            sb.AppendLine($"{"Cant",-5} {"Producto",-20} {"Importe",10}");
            sb.AppendLine(new string('-', Ancho));
            foreach (var d in detalles)
            {
                string nombre = d.NombreProducto.Length > 20
                    ? d.NombreProducto[..20]
                    : d.NombreProducto;

                sb.AppendLine($"{d.Cantidad,-5} {nombre,-20} {d.Subtotal,10:0.00}");
            }

            sb.AppendLine(new string('-', Ancho));
            sb.AppendLine($"{"Subtotal:",-30} {venta.Subtotal,10:0.00}");
            sb.AppendLine($"{"Propina:",-30} {venta.Propina,10:0.00}");
            sb.AppendLine($"{"TOTAL:",-30} {venta.Total,10:0.00}");
            sb.AppendLine($"Método:  {venta.MetodoPago}");

            if (venta.MetodoPago == "Efectivo" && pagoCliente > 0)
            {
                sb.AppendLine($"Paga con:{pagoCliente,26:0.00}");
                sb.AppendLine($"Cambio:  {(pagoCliente - venta.Total),26:0.00}");
            }

            sb.AppendLine(new string('=', Ancho));
            sb.AppendLine(Centrar("Gracias por su compra!"));
            sb.AppendLine();

            string ruta = Path.Combine(AppPaths.TicketsFolder, $"{venta.IdVenta}.txt");
            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
            return ruta;
        }

        private static string Centrar(string texto)
        {
            if (texto.Length >= Ancho) return texto;
            int margen = (Ancho - texto.Length) / 2;
            return new string(' ', margen) + texto;
        }
    }
}