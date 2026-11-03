using CafeteriaPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CafeteriaPOS.Services
{
    public class VentaService
    {
        /// <summary>
        /// Registra una venta completa en una sola operación atómica:
        /// cabecera, detalle y descuento de stock. Devuelve el id generado.
        /// </summary>
        public string RegistrarVenta(Venta venta, List<DetalleVenta> detalles)
        {
            // 1. Generar id único si no lo trae
            if (string.IsNullOrWhiteSpace(venta.IdVenta))
                venta.IdVenta = GenerarIdVenta();

            // 2. El detalle hereda el id de la venta (antes quedaba vacío en CSV)
            foreach (var d in detalles)
                d.IdVenta = venta.IdVenta;

            // 3. Guardar venta, detalle y stock juntos
            JsonDatabase.Actualizar(db =>
            {
                db.Ventas.Add(venta);
                db.DetalleVentas.AddRange(detalles);

                foreach (var d in detalles)
                {
                    var p = db.Productos.FirstOrDefault(x => x.Id == d.IdProducto);
                    if (p is not null) p.Stock -= d.Cantidad;
                }
            });

            return venta.IdVenta;
        }

        /// <summary>Formato: V-YYYYMMDD-HHmmss-XXX (los XXX son aleatorios para evitar colisiones).</summary>
        private string GenerarIdVenta() =>
            $"V-{DateTime.Now:yyyyMMdd-HHmmss}-{Random.Shared.Next(100, 999)}";

        public List<Venta> GetAll() => JsonDatabase.Leer().Ventas;

        public List<DetalleVenta> GetDetalles() => JsonDatabase.Leer().DetalleVentas;
    }
}
