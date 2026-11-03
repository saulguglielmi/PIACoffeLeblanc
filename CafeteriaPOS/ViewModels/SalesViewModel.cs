using CafeteriaPOS.Models;
using CafeteriaPOS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace CafeteriaPOS.ViewModels
{
    public partial class SalesViewModel : ObservableObject
    {
        private const string CategoriaTodas = "Categoría";

        private readonly ProductoService _productoService = new();
        private readonly VentaService _ventaService = new();
        private readonly TicketService _ticketService = new();

        private List<Producto> _todosLosProductos = new();
        public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
        public ObservableCollection<string> Categorias { get; } = new();

        [ObservableProperty] private string textoBusqueda = "";
        [ObservableProperty] private string categoriaSeleccionada = CategoriaTodas;

  
        public ObservableCollection<CarritoItem> Carrito { get; } = new();

        [ObservableProperty] private decimal subtotal;
        [ObservableProperty] private double propina;
        [ObservableProperty] private decimal total;

        [ObservableProperty] private string metodoPago = "Efectivo"; // "Efectivo" | "Tarjeta"
        [ObservableProperty] private double pagoCliente;
        [ObservableProperty] private decimal cambio;

        [ObservableProperty] private string mensajeEstado = "";

        public SalesViewModel()
        {
            CargarProductos();
        }

        public void CargarProductos()
        {
            _todosLosProductos = _productoService.GetAll();

            Categorias.Clear();
            Categorias.Add(CategoriaTodas);
            foreach (var c in _todosLosProductos.Select(p => p.Categoria).Distinct().OrderBy(c => c))
                Categorias.Add(c);

            AplicarFiltro();
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();
        partial void OnCategoriaSeleccionadaChanged(string value) => AplicarFiltro();

        private void AplicarFiltro()
        {
            var query = _todosLosProductos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                query = query.Where(p => p.Nombre.Contains(TextoBusqueda, System.StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(CategoriaSeleccionada) && CategoriaSeleccionada != CategoriaTodas)
                query = query.Where(p => p.Categoria == CategoriaSeleccionada);

            ProductosFiltrados.Clear();
            foreach (var p in query) ProductosFiltrados.Add(p);
        }

        [RelayCommand]
        private void AgregarAlCarrito(Producto producto)
        {
            if (producto is null) return;

            if (producto.Stock <= 0)
            {
                MensajeEstado = $"⚠️ {producto.Nombre} sin stock.";
                return;
            }

            var existente = Carrito.FirstOrDefault(c => c.Producto.Id == producto.Id);
            if (existente is not null)
            {
                if (existente.Cantidad >= producto.Stock)
                {
                    MensajeEstado = $"⚠️ Solo hay {producto.Stock} unidades de {producto.Nombre}.";
                    return;
                }
                existente.Cantidad++;
            }
            else
            {
                Carrito.Add(new CarritoItem(producto, 1));
            }

            MensajeEstado = "";
            RecalcularTotales();
        }

        [RelayCommand]
        private void Aumentar(CarritoItem item)
        {
            if (item is null) return;
            if (item.Cantidad >= item.Producto.Stock)
            {
                MensajeEstado = $"⚠️ Stock máximo alcanzado para {item.Producto.Nombre}.";
                return;
            }
            item.Cantidad++;
            RecalcularTotales();
        }

        [RelayCommand]
        private void Disminuir(CarritoItem item)
        {
            if (item is null) return;
            if (item.Cantidad <= 1) { Carrito.Remove(item); }
            else { item.Cantidad--; }
            RecalcularTotales();
        }

        [RelayCommand]
        private void Quitar(CarritoItem item)
        {
            if (item is null) return;
            Carrito.Remove(item);
            RecalcularTotales();
        }

        [RelayCommand]
        private void VaciarCarrito()
        {
            Carrito.Clear();
            RecalcularTotales();
        }
        private void RecalcularTotales()
        {
            Subtotal = Carrito.Sum(c => c.Subtotal);
            Total    = Subtotal + ADecimal(Propina);
            Cambio   = ADecimal(PagoCliente) > 0 ? ADecimal(PagoCliente) - Total : 0m;
            OnPropertyChanged(nameof(PuedeCobrar));
        }

        private static decimal ADecimal(double valor) =>
            double.IsFinite(valor) ? (decimal)valor : 0m;

        partial void OnPropinaChanged(double value)     => RecalcularTotales();
        partial void OnPagoClienteChanged(double value) => RecalcularTotales();

        [RelayCommand]
        private void AplicarPropinaPorcentaje(string porcentajeTexto)
        {
            if (decimal.TryParse(porcentajeTexto, out var pct))
            {
                Propina = (double)System.Math.Round(Subtotal * (pct / 100m), 2);
            }
        }

        [RelayCommand]
        private void Cobrar()
        {
            if (Carrito.Count == 0)
            {
                MensajeEstado = "⚠️ El carrito está vacío.";
                return;
            }

            if (MetodoPago == "Efectivo" && ADecimal(PagoCliente) < Total)
            {
                MensajeEstado = "⚠️ El pago del cliente es insuficiente.";
                return;
            }

            var usuario = SessionService.UsuarioActual!;

            var venta = new Venta
            {
                FechaHora      = System.DateTime.Now,
                IdEmpleado     = usuario.Id,
                NombreEmpleado = usuario.Nombre,
                Subtotal       = Subtotal,
                Propina        = ADecimal(Propina),
                Total          = Total,
                MetodoPago     = MetodoPago
            };

            var detalles = Carrito.Select(c => new DetalleVenta
            {
                IdProducto     = c.Producto.Id,
                NombreProducto = c.Producto.Nombre,
                Cantidad       = c.Cantidad,
                PrecioUnitario = c.Producto.Precio,
                Subtotal       = c.Subtotal
            }).ToList();

            string idVenta = _ventaService.RegistrarVenta(venta, detalles);

            var detallesConId = detalles.Select(d => { d.IdVenta = idVenta; return d; }).ToList();
            string rutaTicket = _ticketService.GenerarTicket(venta, detallesConId,
                MetodoPago == "Efectivo" ? ADecimal(PagoCliente) : 0m);

            MensajeEstado = $"✅ Venta {idVenta} registrada. Ticket: {rutaTicket}";

            Carrito.Clear();
            Propina = 0;
            PagoCliente = 0;
            RecalcularTotales();
            CargarProductos();
        }

        public bool PuedeCobrar => Carrito.Count > 0;

        public bool EsEfectivo
        {
            get => MetodoPago == "Efectivo";
            set { if (value) MetodoPago = "Efectivo"; }
        }
        public bool EsTarjeta
        {
            get => MetodoPago == "Tarjeta";
            set { if (value) MetodoPago = "Tarjeta"; }
        }
        partial void OnMetodoPagoChanged(string value)
        {
            OnPropertyChanged(nameof(EsEfectivo));
            OnPropertyChanged(nameof(EsTarjeta));
        }
    }
}