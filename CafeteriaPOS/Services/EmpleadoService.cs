using CafeteriaPOS.Models;
using System.Collections.Generic;
using System.Linq;

namespace CafeteriaPOS.Services
{
    public class EmpleadoService
    {
        // ───────── login ─────────
        public Empleado? ValidarCredenciales(string usuario, string password)
        {
            return GetAll().FirstOrDefault(e =>
                e.Usuario.Equals(usuario, System.StringComparison.OrdinalIgnoreCase) &&
                e.Password == password);
        }

        // ───────── CRUD ─────────
        public List<Empleado> GetAll() => JsonDatabase.Leer().Empleados;

        public void SaveAll(IEnumerable<Empleado> empleados)
        {
            var lista = empleados.ToList();
            JsonDatabase.Actualizar(db => db.Empleados = lista);
        }

        public int SiguienteId() =>
            GetAll().Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;

        public bool ExisteUsuario(string usuario, int exceptoId = -1)
        {
            return GetAll().Any(e =>
                e.Id != exceptoId &&
                e.Usuario.Equals(usuario, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
