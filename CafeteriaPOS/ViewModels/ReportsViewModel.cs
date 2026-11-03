using CafeteriaPOS.Helpers;
using CafeteriaPOS.Models;
using CafeteriaPOS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CafeteriaPOS.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly VentaService _ventaService = new();

        // ───────── Métricas del día ─────────
        [ObservableProperty] private decimal totalVentasHoy;
        [ObservableProperty] private decimal totalVendidoHoy;
        [ObservableProperty] private int cantidadVentasHoy;
        [ObservableProperty] private decimal totalPropinasHoy;
        [ObservableProperty] private decimal promedioTicket;
        [ObservableProperty] private bool hayVentasHoy;

        // ───────── Listas ─────────
        public ObservableCollection<ProductoTop> TopProductos { get; } = new();
        public ObservableCollection<PropinasEmpleado> PropinasPorEmp { get; } = new();
        public ObservableCollection<ResumenDia> Ultimos7Dias { get; } = new();

        // ───────── Visibilidad de estados vacíos ─────────
        public Visibility VisibilidadListas =>
            HayVentasHoy ? Visibility.Visible : Visibility.Collapsed;

        public Visibility VisibilidadSinVentas =>
            HayVentasHoy ? Visibility.Collapsed : Visibility.Visible;

        partial void OnHayVentasHoyChanged(bool value)
        {
            OnPropertyChanged(nameof(VisibilidadListas));
            OnPropertyChanged(nameof(VisibilidadSinVentas));
        }

        // ───────── Estado ─────────
        [ObservableProperty] private string mensajeEstado = "";

        public ReportsViewModel()
        {
            CargarReportes();
        }

        [RelayCommand]
        public void CargarReportes()
        {
            var ventas = _ventaService.GetAll();
            var detalles = _ventaService.GetDetalles();
            var hoy = DateTime.Today;

            // ───── Ventas de hoy ─────
            var ventasHoy = ventas.Where(v => v.FechaHora.Date == hoy).ToList();

            TotalVentasHoy = ventasHoy.Sum(v => v.Total);
            TotalVendidoHoy = ventasHoy.Sum(v => v.Subtotal);
            CantidadVentasHoy = ventasHoy.Count();
            TotalPropinasHoy = ventasHoy.Sum(v => v.Propina);
            PromedioTicket = ventasHoy.Count > 0
                ? TotalVentasHoy / ventasHoy.Count
                : 0m;
            HayVentasHoy = ventasHoy.Count > 0;

            // ───── Top 10 productos ─────
            var idsVentasHoy = ventasHoy.Select(v => v.IdVenta).ToHashSet();

            var top = detalles
                .Where(d => idsVentasHoy.Contains(d.IdVenta))
                .GroupBy(d => new { d.IdProducto, d.NombreProducto })
                .Select(g => new ProductoTop
                {
                    IdProducto = g.Key.IdProducto,
                    NombreProducto = g.Key.NombreProducto,
                    CantidadTotal = g.Sum(x => x.Cantidad),
                    MontoTotal = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(p => p.CantidadTotal)
                .Take(10)
                .ToList();

            TopProductos.Clear();
            foreach (var t in top) TopProductos.Add(t);

            // ───── Propinas por empleado (hoy) ─────
            var propinas = ventasHoy
                .GroupBy(v => new { v.IdEmpleado, v.NombreEmpleado })
                .Select(g => new PropinasEmpleado
                {
                    IdEmpleado = g.Key.IdEmpleado,
                    NombreEmpleado = g.Key.NombreEmpleado,
                    CantidadVentas = g.Count(),
                    PropinasEfectivo = g.Where(v => v.MetodoPago == "Efectivo").Sum(v => v.Propina),
                    PropinasTarjeta = g.Where(v => v.MetodoPago == "Tarjeta").Sum(v => v.Propina)
                })
                .OrderByDescending(p => p.TotalPropinas)
                .ToList();

            PropinasPorEmp.Clear();
            foreach (var p in propinas) PropinasPorEmp.Add(p);

            // ───── Últimos 7 días (siempre 7 filas, con ceros si no hubo ventas) ─────
            var hace7 = hoy.AddDays(-6);
            var ventasPorDia = ventas
                .Where(v => v.FechaHora.Date >= hace7 && v.FechaHora.Date <= hoy)
                .GroupBy(v => v.FechaHora.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resumen = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(-6 + i))
                .OrderByDescending(fecha => fecha)
                .Select(fecha =>
                {
                    var delDia = ventasPorDia.TryGetValue(fecha, out var lista)
                        ? lista
                        : new List<Venta>();

                    return new ResumenDia
                    {
                        Fecha = fecha.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        CantidadVentas = delDia.Count,
                        TotalSinPropinas = delDia.Sum(v => v.Subtotal),
                        TotalConPropinas = delDia.Sum(v => v.Total),
                        PromedioVenta = delDia.Count > 0
                            ? delDia.Sum(v => v.Total) / delDia.Count
                            : 0m
                    };
                })
                .ToList();

            Ultimos7Dias.Clear();
            foreach (var r in resumen) Ultimos7Dias.Add(r);
        }

        /// <summary>
        /// Exporta el reporte de los últimos 7 días a un CSV UTF-8 con BOM.
        /// Devuelve la ruta del archivo guardado, o null si el usuario canceló.
        /// </summary>
        public async Task<string?> ExportarUltimos7DiasAsync()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();

            // Inicializar el picker con el HWND de la ventana principal
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Archivo CSV", new List<string> { ".csv" });
            picker.SuggestedFileName = $"Reporte_{DateTime.Now:yyyyMMdd_HHmm}";

            var file = await picker.PickSaveFileAsync();
            if (file is null) return null;

            // Escribir el CSV con UTF-8 + BOM
            var lineas = new List<string[]>
            {
                new[] { "Fecha", "Ventas", "TotalSinPropinas", "TotalConPropinas", "PromedioVenta" }
            };

            foreach (var r in Ultimos7Dias)
            {
                lineas.Add(new[]
                {
                    r.Fecha,
                    r.CantidadVentas.ToString(),
                    r.TotalSinPropinas.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    r.TotalConPropinas.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    r.PromedioVenta.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            // Sección de propinas por empleado
            lineas.Add(new string[0]);
            lineas.Add(new[] { "Empleado", "Ventas", "PropinasEfectivo", "PropinasTarjeta", "TotalPropinas" });
            foreach (var p in PropinasPorEmp)
            {
                lineas.Add(new[]
                {
                    p.NombreEmpleado,
                    p.CantidadVentas.ToString(),
                    p.PropinasEfectivo.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.PropinasTarjeta.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.TotalPropinas.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            CsvService.WriteAll(file.Path, lineas);

            return file.Path;
        }
        /// <summary>
        /// Exporta el mismo reporte (últimos 7 días + propinas) a formato JSON.
        /// Devuelve la ruta del archivo guardado, o null si el usuario canceló.
        /// </summary>
        public async Task<string?> ExportarJsonAsync()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Archivo JSON", new List<string> { ".json" });
            picker.SuggestedFileName = $"Reporte_{DateTime.Now:yyyyMMdd_HHmm}";

            var file = await picker.PickSaveFileAsync();
            if (file is null) return null;

            // Construir un objeto anónimo con toda la info del reporte
            var reporte = new
            {
                GeneradoEn = DateTime.Now,
                ResumenHoy = new
                {
                    TotalSinPropinas = TotalVendidoHoy,
                    TotalConPropinas = TotalVentasHoy,
                    CantidadVentas = CantidadVentasHoy,
                    TotalPropinas = TotalPropinasHoy,
                    TicketPromedio = PromedioTicket
                },
                TopProductos = TopProductos.Select(p => new
                {
                    p.IdProducto,
                    p.NombreProducto,
                    p.CantidadTotal,
                    p.MontoTotal
                }).ToList(),
                PropinasPorEmpleado = PropinasPorEmp.Select(p => new
                {
                    p.IdEmpleado,
                    p.NombreEmpleado,
                    p.CantidadVentas,
                    p.PropinasEfectivo,
                    p.PropinasTarjeta,
                    p.TotalPropinas
                }).ToList(),
                Ultimos7Dias = Ultimos7Dias.Select(r => new
                {
                    r.Fecha,
                    r.CantidadVentas,
                    r.TotalSinPropinas,
                    r.TotalConPropinas,
                    r.PromedioVenta
                }).ToList()
            };

            // Serializar con indentación (legible)
            var opciones = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = System.Text.Json.JsonSerializer.Serialize(reporte, opciones);

            // Guardar con UTF-8 (sin BOM para JSON, es el estándar)
            await System.IO.File.WriteAllTextAsync(file.Path, json, new System.Text.UTF8Encoding(false));

            return file.Path;
        }
    }
}