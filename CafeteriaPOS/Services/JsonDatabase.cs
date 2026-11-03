using CafeteriaPOS.Helpers;
using CafeteriaPOS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CafeteriaPOS.Services
{
    /// <summary>Contenido completo de Data\db.json.</summary>
    public sealed class BaseDatos
    {
        public List<Producto> Productos { get; set; } = new();
        public List<string> Categorias { get; set; } = new();
        public List<Empleado> Empleados { get; set; } = new();
        public List<Venta> Ventas { get; set; } = new();
        public List<DetalleVenta> DetalleVentas { get; set; } = new();
    }

    /// <summary>
    /// Persistencia principal en un solo archivo JSON.
    /// Las lecturas y escrituras se serializan con un candado para que dos
    /// ventas simultáneas no se pisen, y cada escritura es atómica.
    /// </summary>
    public static class JsonDatabase
    {
        private static readonly object Candado = new();

        private static readonly JsonSerializerOptions Opciones = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static BaseDatos Leer()
        {
            lock (Candado)
            {
                return LeerSinBloqueo();
            }
        }

        public static void Guardar(BaseDatos db)
        {
            lock (Candado)
            {
                EscribirSinBloqueo(db);
            }
        }

        /// <summary>
        /// Lee, modifica y guarda dentro del mismo candado. Úsalo para ventas,
        /// stock y cualquier cambio que deba quedar completo o no quedar.
        /// </summary>
        public static void Actualizar(Action<BaseDatos> cambio)
        {
            lock (Candado)
            {
                var db = LeerSinBloqueo();
                cambio(db);
                EscribirSinBloqueo(db);
            }
        }

        /// <summary>Crea la base con datos iniciales solo si todavía no existe.</summary>
        public static void AsegurarBase()
        {
            lock (Candado)
            {
                if (File.Exists(AppPaths.BaseDatos)) return;

                EscribirSinBloqueo(new BaseDatos
                {
                    Empleados = new List<Empleado>
                    {
                        new() { Id = 1, Nombre = "Administrador", Usuario = "admin", Password = "1234", Puesto = "Admin" }
                    },
                    Productos = new List<Producto>
                    {
                        new() { Id = 1, Nombre = "Café Americano", Categoria = "Bebidas", Precio = 25.00m, Stock = 20, StockMinimo = 5 },
                        new() { Id = 2, Nombre = "Capuchino", Categoria = "Bebidas", Precio = 40.00m, Stock = 15, StockMinimo = 5 },
                        new() { Id = 3, Nombre = "Croissant", Categoria = "Panadería", Precio = 30.00m, Stock = 8, StockMinimo = 3 }
                    }
                });
            }
        }

        private static BaseDatos LeerSinBloqueo()
        {
            if (!File.Exists(AppPaths.BaseDatos)) return new BaseDatos();

            var json = File.ReadAllText(AppPaths.BaseDatos, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return new BaseDatos();

            return JsonSerializer.Deserialize<BaseDatos>(json, Opciones) ?? new BaseDatos();
        }

        private static void EscribirSinBloqueo(BaseDatos db)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.BaseDatos)!);

            var json = JsonSerializer.Serialize(db, Opciones);
            var temporal = AppPaths.BaseDatos + ".tmp";
            var respaldo = AppPaths.BaseDatos + ".bak";

            File.WriteAllText(temporal, json, new UTF8Encoding(false));

            // Conserva la versión anterior por si hubiera que recuperarla.
            if (File.Exists(AppPaths.BaseDatos))
                File.Copy(AppPaths.BaseDatos, respaldo, overwrite: true);

            File.Move(temporal, AppPaths.BaseDatos, overwrite: true);
        }
    }
}
