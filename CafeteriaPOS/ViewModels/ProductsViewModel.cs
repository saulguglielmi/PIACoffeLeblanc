using CafeteriaPOS.Models;
using CafeteriaPOS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CafeteriaPOS.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        private const string CategoriaTodas = "Categoría";

        private readonly ProductoService _productoService = new();
        private readonly CategoriaService _categoriaService = new();

        // ───────── Lista de productos ─────────
        private ObservableCollection<Producto> _todosLosProductos { get; } = new();
        public ObservableCollection<Producto> ProductosFiltrados { get; } = new();

        // ───────── Categorías ─────────
        // Categorias: opciones del filtro, con "Categoría" como comodín.
        // CategoriasDisponibles: categorías reales para el formulario y el CRUD.
        public ObservableCollection<string> Categorias { get; } = new();
        public ObservableCollection<string> CategoriasDisponibles { get; } = new();

        [ObservableProperty] private string textoBusqueda = "";
        [ObservableProperty] private string categoriaFiltro = CategoriaTodas;
        [ObservableProperty] private Producto? productoSeleccionado;

        // ───────── CRUD de categorías ─────────
        [ObservableProperty] private string nuevaCategoria = "";
        [ObservableProperty] private string? categoriaEditadaSeleccionada;
        [ObservableProperty] private string nombreCategoriaEditado = "";
        [ObservableProperty] private string mensajeCategoria = "";

        public bool HayCategoriaSeleccionada => !string.IsNullOrWhiteSpace(CategoriaEditadaSeleccionada);

        // ───────── Campos del formulario ─────────
        [ObservableProperty] private string formNombre = "";
        [ObservableProperty] private string formCategoria = "";
        [ObservableProperty] private double formPrecio = 0;
        [ObservableProperty] private int formStock = 0;
        [ObservableProperty] private int formStockMinimo = 0;
        [ObservableProperty] private string mensajeEstado = "";
        [ObservableProperty] private bool modoEdicion = false;

        // ───────── Títulos dinámicos ─────────
        public string TituloFormulario => ModoEdicion ? "✏️ Editar producto" : "➕ Nuevo producto";
        public string TextoBotonGuardar => ModoEdicion ? "Actualizar" : "Guardar";
        public bool HaySeleccion => ProductoSeleccionado is not null;

        public ProductsViewModel()
        {
            CargarProductos();
            CargarCategorias();
        }

        // ───────── Carga y filtro ─────────
        public void CargarProductos()
        {
            _todosLosProductos.Clear();
            foreach (var p in _productoService.GetAll())
                _todosLosProductos.Add(p);

            AplicarFiltro();
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();
        partial void OnCategoriaFiltroChanged(string value) => AplicarFiltro();

        private void AplicarFiltro()
        {
            ProductosFiltrados.Clear();
            var query = _todosLosProductos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                query = query.Where(p => p.Nombre.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(CategoriaFiltro) && CategoriaFiltro != CategoriaTodas)
                query = query.Where(p => string.Equals(p.Categoria, CategoriaFiltro, StringComparison.OrdinalIgnoreCase));

            foreach (var p in query)
                ProductosFiltrados.Add(p);
        }

        // ───────── Categorías: carga y sincronización ─────────
        private void CargarCategorias()
        {
            var guardadas = _categoriaService.GetAll();
            var combinadas = new List<string>(guardadas);
            bool cambiaron = false;

            // Al abrir una base antigua (sin lista de categorías), se recuperan
            // las que ya usan los productos para no perder ninguna.
            foreach (var categoria in _todosLosProductos.Select(p => p.Categoria))
            {
                if (string.IsNullOrWhiteSpace(categoria)) continue;
                if (combinadas.Contains(categoria, StringComparer.OrdinalIgnoreCase)) continue;

                combinadas.Add(categoria.Trim());
                cambiaron = true;
            }

            combinadas = combinadas
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cambiaron || combinadas.Count != guardadas.Count)
                _categoriaService.SaveAll(combinadas);

            CategoriasDisponibles.Clear();
            foreach (var categoria in combinadas)
                CategoriasDisponibles.Add(categoria);

            Categorias.Clear();
            Categorias.Add(CategoriaTodas);
            foreach (var categoria in combinadas)
                Categorias.Add(categoria);

            if (string.IsNullOrWhiteSpace(CategoriaFiltro) ||
                !Categorias.Contains(CategoriaFiltro, StringComparer.OrdinalIgnoreCase))
            {
                CategoriaFiltro = CategoriaTodas;
            }
            else
            {
                AplicarFiltro();
            }

            if (CategoriaEditadaSeleccionada is not null &&
                !CategoriasDisponibles.Contains(CategoriaEditadaSeleccionada, StringComparer.OrdinalIgnoreCase))
            {
                CategoriaEditadaSeleccionada = null;
            }

            if (!string.IsNullOrWhiteSpace(FormCategoria) &&
                !CategoriasDisponibles.Contains(FormCategoria, StringComparer.OrdinalIgnoreCase))
            {
                FormCategoria = "";
            }
        }

        partial void OnCategoriaEditadaSeleccionadaChanged(string? value)
        {
            NombreCategoriaEditado = value ?? "";
            MensajeCategoria = "";
            OnPropertyChanged(nameof(HayCategoriaSeleccionada));
        }

        // ───────── CRUD de categorías ─────────

        private static bool NombreCategoriaValido(string nombre) =>
            nombre.Length <= 15 && nombre.All(char.IsLetter);

        [RelayCommand]
        private void AgregarCategoria()
        {
            string nombre = NuevaCategoria.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MensajeCategoria = "⚠️ Escribe el nombre de la categoría.";
                return;
            }

            if (!NombreCategoriaValido(nombre))
            {
                MensajeCategoria = "⚠️ Usa solo letras, sin números ni símbolos (máx. 15).";
                return;
            }

            if (CategoriasDisponibles.Contains(nombre, StringComparer.OrdinalIgnoreCase))
            {
                MensajeCategoria = $"⚠️ La categoría «{nombre}» ya existe.";
                return;
            }

            _categoriaService.SaveAll(CategoriasDisponibles.Append(nombre));

            NuevaCategoria = "";
            CargarCategorias();
            CategoriaEditadaSeleccionada = CategoriasDisponibles
                .FirstOrDefault(c => string.Equals(c, nombre, StringComparison.OrdinalIgnoreCase));

            MensajeCategoria = $"✅ Categoría «{nombre}» agregada.";
        }

        [RelayCommand]
        private void ActualizarCategoria()
        {
            if (string.IsNullOrWhiteSpace(CategoriaEditadaSeleccionada))
            {
                MensajeCategoria = "⚠️ Selecciona una categoría para modificarla.";
                return;
            }

            string anterior = CategoriaEditadaSeleccionada;
            string nuevo = NombreCategoriaEditado.Trim();

            if (string.IsNullOrWhiteSpace(nuevo))
            {
                MensajeCategoria = "⚠️ El nombre de la categoría es obligatorio.";
                return;
            }

            if (!NombreCategoriaValido(nuevo))
            {
                MensajeCategoria = "⚠️ Usa solo letras, sin números ni símbolos (máx. 15).";
                return;
            }

            if (!string.Equals(anterior, nuevo, StringComparison.OrdinalIgnoreCase) &&
                CategoriasDisponibles.Contains(nuevo, StringComparer.OrdinalIgnoreCase))
            {
                MensajeCategoria = $"⚠️ La categoría «{nuevo}» ya existe.";
                return;
            }

            bool eraFiltro = string.Equals(CategoriaFiltro, anterior, StringComparison.OrdinalIgnoreCase);
            bool eraFormulario = string.Equals(FormCategoria, anterior, StringComparison.OrdinalIgnoreCase);

            _categoriaService.Renombrar(anterior, nuevo);

            CargarProductos();
            CargarCategorias();

            if (eraFiltro)
            {
                CategoriaFiltro = CategoriasDisponibles
                    .FirstOrDefault(c => string.Equals(c, nuevo, StringComparison.OrdinalIgnoreCase))
                    ?? CategoriaTodas;
            }

            if (eraFormulario)
                FormCategoria = nuevo;

            CategoriaEditadaSeleccionada = CategoriasDisponibles
                .FirstOrDefault(c => string.Equals(c, nuevo, StringComparison.OrdinalIgnoreCase));

            MensajeCategoria = $"✅ Categoría actualizada a «{nuevo}».";
        }

        [RelayCommand]
        private void EliminarCategoria()
        {
            if (string.IsNullOrWhiteSpace(CategoriaEditadaSeleccionada))
            {
                MensajeCategoria = "⚠️ Selecciona una categoría para eliminarla.";
                return;
            }

            string nombre = CategoriaEditadaSeleccionada;
            int enUso = _productoService.GetAll()
                .Count(p => string.Equals(p.Categoria, nombre, StringComparison.OrdinalIgnoreCase));

            if (enUso > 0)
            {
                MensajeCategoria = $"⚠️ No se puede eliminar «{nombre}»: está en uso por {enUso} producto(s).";
                return;
            }

            _categoriaService.Eliminar(nombre);

            CategoriaEditadaSeleccionada = null;
            CargarCategorias();
            MensajeCategoria = $"🗑️ Categoría «{nombre}» eliminada.";
        }

        // ───────── Al seleccionar un producto, llenamos el formulario ─────────
        partial void OnProductoSeleccionadoChanged(Producto? value)
        {
            OnPropertyChanged(nameof(HaySeleccion));

            if (value is null) return;

            FormNombre = value.Nombre;
            FormCategoria = value.Categoria;
            FormPrecio = (double)value.Precio;
            FormStock = value.Stock;
            FormStockMinimo = value.StockMinimo;
            ModoEdicion = true;
            MensajeEstado = "";
        }

        partial void OnModoEdicionChanged(bool value)
        {
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(TextoBotonGuardar));
        }

        // ───────── Comandos de producto ─────────

        [RelayCommand]
        private void Nuevo()
        {
            LimpiarFormulario();
            ModoEdicion = false;
        }

        [RelayCommand]
        private void Guardar()
        {
            // Validación
            if (string.IsNullOrWhiteSpace(FormNombre))
            {
                MensajeEstado = "⚠️ El nombre es obligatorio.";
                return;
            }
            if (string.IsNullOrWhiteSpace(FormCategoria))
            {
                MensajeEstado = "⚠️ La categoría es obligatoria. Créala en «Administrar categorías».";
                return;
            }
            if (!double.IsFinite(FormPrecio) || FormPrecio <= 0)
            {
                MensajeEstado = "⚠️ El precio debe ser mayor que cero.";
                return;
            }
            if (FormStock < 0 || FormStockMinimo < 0)
            {
                MensajeEstado = "⚠️ El stock no puede ser negativo.";
                return;
            }

            var productos = _productoService.GetAll();

            if (ModoEdicion && ProductoSeleccionado is not null)
            {
                // Actualizar existente
                var p = productos.FirstOrDefault(x => x.Id == ProductoSeleccionado.Id);
                if (p is not null)
                {
                    p.Nombre = FormNombre.Trim();
                    p.Categoria = FormCategoria.Trim();
                    p.Precio = ADecimal(FormPrecio);
                    p.Stock = FormStock;
                    p.StockMinimo = FormStockMinimo;
                }
                MensajeEstado = "✅ Producto actualizado.";
            }
            else
            {
                // Crear nuevo
                productos.Add(new Producto
                {
                    Id = _productoService.SiguienteId(),
                    Nombre = FormNombre.Trim(),
                    Categoria = FormCategoria.Trim(),
                    Precio = ADecimal(FormPrecio),
                    Stock = FormStock,
                    StockMinimo = FormStockMinimo
                });
                MensajeEstado = "✅ Producto agregado.";
            }

            _productoService.SaveAll(productos);
            CargarProductos();
            CargarCategorias();
            LimpiarFormulario();
            ModoEdicion = false;
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (ProductoSeleccionado is null) return;

            var productos = _productoService.GetAll();
            productos.RemoveAll(p => p.Id == ProductoSeleccionado.Id);
            _productoService.SaveAll(productos);
            CargarProductos();
            CargarCategorias();
            LimpiarFormulario();
            ModoEdicion = false;
            MensajeEstado = "🗑️ Producto eliminado.";
        }

        [RelayCommand]
        private void Cancelar()
        {
            LimpiarFormulario();
            ModoEdicion = false;
        }

        private void LimpiarFormulario()
        {
            FormNombre = "";
            FormCategoria = "";
            FormPrecio = 0;
            FormStock = 0;
            FormStockMinimo = 0;
            ProductoSeleccionado = null;
        }

        /// <summary>
        /// NumberBox devuelve NaN cuando el campo está vacío; convertirlo a
        /// decimal lanza OverflowException, así que lo tratamos como cero.
        /// </summary>
        private static decimal ADecimal(double valor) =>
            double.IsFinite(valor) ? (decimal)valor : 0m;
    }
}
