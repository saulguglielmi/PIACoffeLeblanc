using System;
using System.Collections.Generic;
using System.Linq;

namespace CafeteriaPOS.Services
{
    /// <summary>
    /// Categorías independientes de los productos, para que una categoría
    /// pueda existir aunque todavía no tenga productos asignados.
    /// </summary>
    public class CategoriaService
    {
        public List<string> GetAll() => JsonDatabase.Leer().Categorias;

        public void SaveAll(IEnumerable<string> categorias)
        {
            var lista = categorias
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            JsonDatabase.Actualizar(db => db.Categorias = lista);
        }

        /// <summary>
        /// Cambia el nombre de una categoría y actualiza todos los productos
        /// que la usaban, dentro de la misma escritura.
        /// </summary>
        public void Renombrar(string anterior, string nuevo)
        {
            string limpio = nuevo.Trim();

            JsonDatabase.Actualizar(db =>
            {
                int indice = db.Categorias.FindIndex(c =>
                    string.Equals(c, anterior, StringComparison.OrdinalIgnoreCase));

                if (indice >= 0)
                    db.Categorias[indice] = limpio;
                else if (!db.Categorias.Contains(limpio, StringComparer.OrdinalIgnoreCase))
                    db.Categorias.Add(limpio);

                foreach (var producto in db.Productos)
                {
                    if (string.Equals(producto.Categoria, anterior, StringComparison.OrdinalIgnoreCase))
                        producto.Categoria = limpio;
                }

                db.Categorias = db.Categorias
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            });
        }

        public void Eliminar(string nombre)
        {
            JsonDatabase.Actualizar(db =>
                db.Categorias.RemoveAll(c =>
                    string.Equals(c, nombre, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
