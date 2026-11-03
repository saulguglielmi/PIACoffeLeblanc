using CafeteriaPOS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CafeteriaPOS.Services
{
    public class ProductoService
    {
        public List<Producto> GetAll() => JsonDatabase.Leer().Productos;

        public void SaveAll(IEnumerable<Producto> productos)
        {
            var lista = productos.ToList();
            JsonDatabase.Actualizar(db => db.Productos = lista);
        }

        public int SiguienteId() =>
            GetAll().Select(p => p.Id).DefaultIfEmpty(0).Max() + 1;

        /// <summary>Descuenta stock de una lista de productos ya vendidos.</summary>
        public void DescontarStock(IEnumerable<DetalleVenta> detalles)
        {
            var lista = detalles.ToList();
            JsonDatabase.Actualizar(db =>
            {
                foreach (var d in lista)
                {
                    var p = db.Productos.FirstOrDefault(x => x.Id == d.IdProducto);
                    if (p is not null) p.Stock -= d.Cantidad;
                }
            });
        }
    }
}
