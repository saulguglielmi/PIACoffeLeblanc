using CafeteriaPOS.Helpers;
using CafeteriaPOS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CafeteriaPOS.Services
{
    public enum MotivoInvalido
    {
        Ninguno,
        ComodinDesconocido,
        ProductoNoExiste,
        FaltaProducto,
        MontoInvalido
    }

    public sealed class ResultadoResolucion
    {
        public bool Ok { get; init; }
        public string Texto { get; init; } = "";
        public MotivoInvalido Motivo { get; init; }
        public string Detalle { get; init; } = "";

        public static ResultadoResolucion Valido(string texto) =>
            new() { Ok = true, Texto = texto };

        public static ResultadoResolucion Invalido(MotivoInvalido motivo, string detalle) =>
            new() { Ok = false, Motivo = motivo, Detalle = detalle };
    }

    /// <summary>
    /// Sustituye los comodines del modelo por datos reales del POS.
    /// Si un comodín no está en la lista blanca, la respuesta completa se
    /// marca como inválida: nunca se muestra un nombre inventado.
    /// </summary>
    public static class ComodinResolver
    {
        public const string SinInformacion =
            "De eso no tengo información, humano. No me subestimes: solo llevo menú, stock, ventas, propinas y horario.";

        public const string ProductoNoExiste =
            "Ese producto no está en el menú, humano. ¿Te digo qué sí hay?";

        public const string FaltaProducto =
            "¿De qué producto, humano? Dime cuál y te digo el dato.";

        private static readonly Regex Patron = new(@"«([^»]+)»", RegexOptions.Compiled);
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-MX");

        private static readonly HashSet<string> Permitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "PRECIO", "STOCK", "PRESUPUESTO", "MENU",
            "VENTAS_HOY", "TICKETS_HOY", "TICKET_PROMEDIO",
            "VENTAS_EFECTIVO", "VENTAS_TARJETA",
            "VENTAS_HISTORICO", "TICKETS_HISTORICO",
            "PROPINAS_HOY", "PROPINAS_EFECTIVO", "PROPINAS_TARJETA",
            "PROPINAS_POR_EMPLEADO", "STOCK_BAJO", "TOP_PRODUCTOS",
            "HORARIO", "PAGOS"
        };

        public static bool EsComodinPermitido(string nombre) =>
            Permitidos.Contains(nombre.Trim().ToUpperInvariant());

        private static readonly (string Alias, string Categoria)[] AliasCategorias =
        {
            ("bebida", "bebidas"),
            ("beber", "bebidas"),
            ("cafe", "bebidas"),
            ("comer", "comida"),
            ("postre", "postres"),
            ("juguete", "juguetes"),
            ("pan", "panaderia")
        };

        public static ResultadoResolucion Resolver(string texto, string? pregunta = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return ResultadoResolucion.Invalido(MotivoInvalido.ComodinDesconocido, "respuesta vacía");

            // Si la pregunta pide el acumulado, los comodines de "hoy" se
            // interpretan como histórico. Así el modelo puede seguir usando
            // VENTAS_HOY sin que la app muestre el dato del día.
            var historico = !string.IsNullOrWhiteSpace(pregunta) && EsConsultaHistorica(pregunta);

            var productos = new Lazy<List<Producto>>(() => new ProductoService().GetAll());
            var ventas = new Lazy<List<Venta>>(() => new VentaService().GetAll());
            var detalles = new Lazy<List<DetalleVenta>>(() => new VentaService().GetDetalles());
            var config = new Lazy<Configuracion>(LeerConfiguracion);

            var sb = new StringBuilder();
            var ultimo = 0;

            foreach (Match m in Patron.Matches(texto))
            {
                sb.Append(texto, ultimo, m.Index - ultimo);
                ultimo = m.Index + m.Length;

                var contenido = m.Groups[1].Value.Trim();
                var partes = contenido.Split(':', 2);
                var nombre = partes[0].Trim().ToUpperInvariant();
                var argumento = partes.Length > 1 ? partes[1].Trim() : "";

                if (!Permitidos.Contains(nombre))
                    return ResultadoResolucion.Invalido(MotivoInvalido.ComodinDesconocido, contenido);

                switch (nombre)
                {
                    case "PRECIO":
                    {
                        var p = BuscarProducto(productos.Value, argumento);
                        if (p is null) return FaltaOProducto(argumento);
                        sb.Append(p.Precio.ToString("C2", Cultura));
                        break;
                    }

                    case "STOCK":
                    {
                        var p = BuscarProducto(productos.Value, argumento);
                        if (p is null) return FaltaOProducto(argumento);
                        sb.Append(p.Stock <= 0 ? "no queda nada"
                                : p.Stock == 1 ? "queda 1"
                                : $"quedan {p.Stock}");
                        break;
                    }

                    case "PRESUPUESTO":
                    {
                        if (!TryParseMonto(argumento, out var monto))
                            return ResultadoResolucion.Invalido(MotivoInvalido.MontoInvalido, argumento);
                        sb.Append(DescribirPresupuesto(productos.Value, monto));
                        break;
                    }

                    case "MENU":
                        sb.Append(DescribirMenu(productos.Value, argumento));
                        break;

                    case "VENTAS_HOY":
                        sb.Append(historico
                            ? ventas.Value.Sum(v => v.Total).ToString("C2", Cultura)
                            : SumaHoy(ventas.Value, null).ToString("C2", Cultura));
                        break;

                    case "TICKETS_HOY":
                        sb.Append(historico
                            ? ventas.Value.Count
                            : ventas.Value.Count(v => v.FechaHora.Date == DateTime.Today));
                        break;

                    case "TICKET_PROMEDIO":
                    {
                        var hoy = ventas.Value.Where(v => v.FechaHora.Date == DateTime.Today).ToList();
                        var promedio = hoy.Count == 0 ? 0m : hoy.Sum(v => v.Total) / hoy.Count;
                        sb.Append(promedio.ToString("C2", Cultura));
                        break;
                    }

                    case "VENTAS_EFECTIVO":
                        sb.Append(SumaHoy(ventas.Value, "efectivo").ToString("C2", Cultura));
                        break;

                    case "VENTAS_TARJETA":
                        sb.Append(SumaHoy(ventas.Value, "tarjeta").ToString("C2", Cultura));
                        break;

                    case "VENTAS_HISTORICO":
                        sb.Append(ventas.Value.Sum(v => v.Total).ToString("C2", Cultura));
                        break;

                    case "TICKETS_HISTORICO":
                        sb.Append(ventas.Value.Count);
                        break;

                    case "PROPINAS_HOY":
                        sb.Append(ventas.Value
                            .Where(v => v.FechaHora.Date == DateTime.Today)
                            .Sum(v => v.Propina)
                            .ToString("C2", Cultura));
                        break;

                    case "PROPINAS_EFECTIVO":
                        sb.Append(SumaPropinasHoy(ventas.Value, "efectivo").ToString("C2", Cultura));
                        break;

                    case "PROPINAS_TARJETA":
                        sb.Append(SumaPropinasHoy(ventas.Value, "tarjeta").ToString("C2", Cultura));
                        break;

                    case "PROPINAS_POR_EMPLEADO":
                        sb.Append(DescribirPropinasPorEmpleado(
                            ventas.Value,
                            string.IsNullOrWhiteSpace(pregunta) ? null : EmpleadoMencionado(pregunta)));
                        break;

                    case "STOCK_BAJO":
                        sb.Append(DescribirStockBajo(productos.Value));
                        break;

                    case "TOP_PRODUCTOS":
                        sb.Append(DescribirTop(detalles.Value, ventas.Value));
                        break;

                    case "HORARIO":
                        sb.Append(config.Value.Horario);
                        break;

                    case "PAGOS":
                        sb.Append(config.Value.Pagos);
                        break;
                }
            }

            sb.Append(texto, ultimo, texto.Length - ultimo);
            var limpio = Limpiar(sb.ToString());

            if (string.IsNullOrWhiteSpace(limpio))
                return ResultadoResolucion.Invalido(MotivoInvalido.ComodinDesconocido, "respuesta vacía");

            if (limpio.Contains('«') || limpio.Contains('»'))
                return ResultadoResolucion.Invalido(MotivoInvalido.ComodinDesconocido, "comodín mal formado");

            return ResultadoResolucion.Valido(limpio);
        }

        private static ResultadoResolucion FaltaOProducto(string argumento) =>
            string.IsNullOrWhiteSpace(argumento)
                ? ResultadoResolucion.Invalido(MotivoInvalido.FaltaProducto, "sin producto")
                : ResultadoResolucion.Invalido(MotivoInvalido.ProductoNoExiste, argumento);

        private static decimal SumaHoy(List<Venta> ventas, string? metodo) =>
            ventas
                .Where(v => v.FechaHora.Date == DateTime.Today &&
                            (metodo is null ||
                             string.Equals(v.MetodoPago, metodo, StringComparison.OrdinalIgnoreCase)))
                .Sum(v => v.Total);

        private static decimal SumaPropinasHoy(List<Venta> ventas, string metodo) =>
            ventas
                .Where(v => v.FechaHora.Date == DateTime.Today &&
                            string.Equals(v.MetodoPago, metodo, StringComparison.OrdinalIgnoreCase))
                .Sum(v => v.Propina);

        private static string DescribirPropinasPorEmpleado(List<Venta> ventas, string? empleado = null)
        {
            var hoy = ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                var total = hoy
                    .Where(v => string.Equals(v.NombreEmpleado, empleado, StringComparison.OrdinalIgnoreCase))
                    .Sum(v => v.Propina);

                return $"{empleado} {total.ToString("C2", Cultura)}";
            }

            if (hoy.Count == 0) return "todavía no hay propinas";

            var grupos = hoy
                .GroupBy(v => string.IsNullOrWhiteSpace(v.NombreEmpleado) ? "sin nombre" : v.NombreEmpleado)
                .Select(g => $"{g.Key} {g.Sum(v => v.Propina).ToString("C2", Cultura)}")
                .ToList();

            return string.Join(", ", grupos);
        }

        public static bool EsConsultaHistorica(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            // Las propinas tienen su propio camino; "en total" en una pregunta
            // de propinas no debe convertirla en una consulta de ventas.
            if (q.Contains("propina")) return false;

            // "en total" vale, pero no si la pregunta acota a hoy/ayer/semana/mes.
            if (Regex.IsMatch(q, @"\b(hoy|ayer|semana|mes|ano)\b") ||
                q.Contains("del dia") ||
                q.Contains("de hoy") ||
                q.Contains("en el dia") ||
                q.Contains("turno"))
                return false;

            return q.Contains("historico") ||
                   q.Contains("historica") ||
                   q.Contains("historial") ||
                   q.Contains("historicamente") ||
                   q.Contains("desde siempre") ||
                   q.Contains("desde el inicio") ||
                   q.Contains("desde el principio") ||
                   q.Contains("todos los tiempos") ||
                   q.Contains("en total") ||
                   q.Contains("total general") ||
                   q.Contains("total historico") ||
                   q.Contains("acumulado") ||
                   q.Contains("acumulada") ||
                   q.Contains("acumuladas") ||
                   q.Contains("ventas totales") ||
                   q.Contains("total de ventas") ||
                   q.Contains("total vendido") ||
                   Regex.IsMatch(q, @"\bhemos vendido\b") ||
                   Regex.IsMatch(q, @"\bse ha vendido\b") ||
                   Regex.IsMatch(q, @"\bhan vendido\b");
        }

        public static bool EsConsultaPropinasPorEmpleado(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (!q.Contains("propina")) return false;

            if (q.Contains("por empleado") ||
                q.Contains("por empleados") ||
                q.Contains("entre los empleados") ||
                q.Contains("entre empleados") ||
                q.Contains("cada empleado") ||
                q.Contains("cada quien") ||
                q.Contains("me toca") ||
                q.Contains("le toca") ||
                q.Contains("repart") ||
                q.Contains("divid") ||
                q.Contains("quien") ||
                q.Contains("que empleado") ||
                q.Contains("cual empleado"))
                return true;

            return EmpleadoMencionado(pregunta) is not null;
        }

        /// <summary>Cualquier pregunta sobre propinas (total o desglose).</summary>
        public static bool EsConsultaPropinas(string pregunta) =>
            Normalizar(pregunta).Contains("propina");

        public static bool EsConsultaPagos(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            var menciona = q.Contains("pago") ||
                           q.Contains("pagar") ||
                           q.Contains("tarjeta") ||
                           q.Contains("efectivo");
            if (!menciona) return false;

            var pideForma = q.Contains("forma") ||
                            q.Contains("metodo") ||
                            q.Contains("acept") ||
                            q.Contains("puedo pagar") ||
                            q.Contains("se puede pagar") ||
                            q.Contains("opcion") ||
                            q.Contains("opciones");

            if (pideForma) return true;

            // "¿Cuánto se pagó con tarjeta?" o "¿cuánto hay en pagos?" son
            // consultas de ventas, no de métodos de pago.
            if (Regex.IsMatch(q, @"\bcuanto\b"))
                return false;

            return q.Contains("pago") || q.Contains("pagar");
        }

        public static bool EsConsultaStockBajo(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            // "¿Cuánto stock queda de X?" o "¿cuál es el stock de X?" son
            // consultas de un producto concreto, no del inventario bajo.
            if (q.Contains("stock queda") ||
                q.Contains("stock tiene") ||
                q.Contains("stock de ") ||
                Regex.IsMatch(q, @"\bcuanto stock\b"))
                return false;

            return q.Contains("stock bajo") ||
                   q.Contains("stock bajos") ||
                   q.Contains("bajo stock") ||
                   q.Contains("poco stock") ||
                   q.Contains("stock minimo") ||
                   q.Contains("por agotarse") ||
                   q.Contains("se esta agotando") ||
                   q.Contains("se estan agotando") ||
                   Regex.IsMatch(q, @"\bagotad[oa]s?\b");
        }

        public static bool EsConsultaTopProductos(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            return q.Contains("producto mas vendido") ||
                   q.Contains("productos mas vendidos") ||
                   q.Contains("mas vendido") ||
                   q.Contains("mas vendidos") ||
                   q.Contains("top productos") ||
                   q.Contains("top de productos") ||
                   q.Contains("top ventas") ||
                   q.Contains("producto estrella") ||
                   q.Contains("se vende mas") ||
                   q.Contains("se venden mas") ||
                   q.Contains("mas se vende") ||
                   q.Contains("mas se venden");
        }

        public static bool EsConsultaCantidadVentas(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            return Regex.IsMatch(q, @"\bcuantas?\s+(ventas?|tickets?)\b") ||
                   Regex.IsMatch(q, @"\bcuantos?\s+(ventas?|tickets?)\b") ||
                   q.Contains("numero de ventas") ||
                   q.Contains("numero de tickets") ||
                   q.Contains("cantidad de ventas") ||
                   q.Contains("cantidad de tickets");
        }

        /// <summary>
        /// El menú (completo o por categoría) se responde con el catálogo real:
        /// un modelo pequeño es poco fiable enumerando productos y precios.
        /// </summary>
        public static bool EsConsultaMenu(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            if (EsConsultaTopProductos(pregunta) ||
                EsConsultaStockBajo(pregunta) ||
                EsConsultaCantidadVentas(pregunta) ||
                EsConsultaPagos(pregunta))
                return false;

            // Un producto concreto no es el menú completo.
            if (Regex.IsMatch(q, @"\b(cuanto cuesta|cuanto sale|cuanto esta|cuanto vale|precio del|precio de la|precio de los|precio de las)\b"))
                return false;

            return Regex.IsMatch(q, @"\bmenu\b") ||
                   Regex.IsMatch(q, @"\bcarta\b") ||
                   q.Contains("lista de precios") ||
                   q.Contains("los precios") ||
                   q.Contains("que tienen") ||
                   q.Contains("que venden") ||
                   q.Contains("que ofrecen") ||
                   q.Contains("que hay de comer") ||
                   q.Contains("que hay de beber") ||
                   q.Contains("que se puede pedir") ||
                   q.Contains("productos disponibles") ||
                   q.Contains("catalogo");
        }

        /// <summary>
        /// Preguntas de dinero del turno ("¿cuánto se ha recaudado hoy?").
        /// Nunca deben caer en el acumulado histórico.
        /// </summary>
        public static bool EsConsultaRecaudadoHoy(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return false;

            if (q.Contains("propina")) return false;
            if (q.Contains("stock") || q.Contains("quedan") || q.Contains("queda")) return false;
            if (EsConsultaHistorica(pregunta)) return false;
            if (EsConsultaCantidadVentas(pregunta)) return false;
            if (EsConsultaTopProductos(pregunta)) return false;
            if (EsConsultaPagos(pregunta)) return false;

            // "recaudado ayer" o "de la semana pasada" no es de hoy.
            if (Regex.IsMatch(q, @"\b(ayer|semana|mes|ano|manana)\b")) return false;

            return q.Contains("recaud") ||
                   q.Contains("recaudo") ||
                   q.Contains("cobrad") ||
                   q.Contains("cobramos") ||
                   q.Contains("ingresos") ||
                   q.Contains("facturado") ||
                   q.Contains("facturacion") ||
                   (q.Contains("caja") &&
                    (q.Contains("cuanto") || q.Contains("como va") || q.Contains("como vamos"))) ||
                   q.Contains("cuanto llevamos") ||
                   q.Contains("como va el dia") ||
                   q.Contains("como va hoy") ||
                   q.Contains("cuanto vendimos hoy") ||
                   q.Contains("cuanto se vendio hoy") ||
                   q.Contains("ventas de hoy") ||
                   q.Contains("ventas del dia");
        }

        public static bool TieneComodin(string texto, string nombre)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;

            return Regex.IsMatch(
                texto,
                @"«\s*" + Regex.Escape(nombre) + @"\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public static bool MencionaVentas(string texto) =>
            TieneComodin(texto, "VENTAS_HOY") ||
            TieneComodin(texto, "VENTAS_HISTORICO") ||
            TieneComodin(texto, "TICKETS_HOY") ||
            TieneComodin(texto, "TICKETS_HISTORICO");

        public static string? EmpleadoMencionado(string pregunta)
        {
            var q = Normalizar(pregunta);
            if (q.Length == 0) return null;

            var empleados = new EmpleadoService().GetAll();
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in empleados)
                if (!string.IsNullOrWhiteSpace(e.Nombre)) nombres.Add(e.Nombre);

            foreach (var v in new VentaService().GetAll()
                         .Where(v => v.FechaHora.Date == DateTime.Today))
                if (!string.IsNullOrWhiteSpace(v.NombreEmpleado)) nombres.Add(v.NombreEmpleado);

            string? mejor = null;
            var mejorLargo = 0;

            foreach (var nombre in nombres)
            {
                var n = Normalizar(nombre);
                if (n.Length >= 3 && Regex.IsMatch(q, $@"\b{Regex.Escape(n)}\b"))
                {
                    if (n.Length > mejorLargo)
                    {
                        mejor = nombre;
                        mejorLargo = n.Length;
                    }
                    continue;
                }

                foreach (var parte in n.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (parte.Length < 3) continue;
                    if (Regex.IsMatch(q, $@"\b{Regex.Escape(parte)}\b") && parte.Length > mejorLargo)
                    {
                        mejor = nombre;
                        mejorLargo = parte.Length;
                    }
                }
            }

            if (mejor is not null) return mejor;

            // "el cajero" / "el admin" sin nombre propio.
            var puesto = q.Contains("cajero") ? "Cajero"
                       : Regex.IsMatch(q, @"\badmin\b") || q.Contains("administrador") ? "Admin"
                       : null;

            if (puesto is not null)
            {
                var emp = empleados.FirstOrDefault(e =>
                    string.Equals(e.Puesto, puesto, StringComparison.OrdinalIgnoreCase));
                if (emp is not null) return emp.Nombre;
            }

            return null;
        }

        public static string RespuestaHistorica()
        {
            var ventas = new VentaService().GetAll();
            if (ventas.Count == 0)
                return "Todavía no hay ventas registradas, humano.";

            var total = ventas.Sum(v => v.Total).ToString("C2", Cultura);
            var tickets = ventas.Count == 1 ? "1 ticket" : $"{ventas.Count} tickets";
            return $"En total llevamos {total} en {tickets}, humano.";
        }

        public static string RespuestaPropinas(string pregunta)
        {
            if (EsConsultaPropinasPorEmpleado(pregunta))
                return RespuestaPropinasPorEmpleado(pregunta);

            var ventas = new VentaService().GetAll();
            var hoy = ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();
            var q = Normalizar(pregunta);

            if (hoy.Count == 0)
                return "Hoy todavía no hay propinas, humano.";

            var efectivo = hoy
                .Where(v => string.Equals(v.MetodoPago, "efectivo", StringComparison.OrdinalIgnoreCase))
                .Sum(v => v.Propina);
            var tarjeta = hoy
                .Where(v => string.Equals(v.MetodoPago, "tarjeta", StringComparison.OrdinalIgnoreCase))
                .Sum(v => v.Propina);
            var total = hoy.Sum(v => v.Propina);

            if (q.Contains("efectivo") && !q.Contains("tarjeta"))
                return $"De propinas en efectivo llevamos {efectivo.ToString("C2", Cultura)}, humano.";

            if (q.Contains("tarjeta") && !q.Contains("efectivo"))
                return $"De propinas con tarjeta llevamos {tarjeta.ToString("C2", Cultura)}, humano.";

            return $"De propinas llevamos {total.ToString("C2", Cultura)}: " +
                   $"{efectivo.ToString("C2", Cultura)} en efectivo y {tarjeta.ToString("C2", Cultura)} con tarjeta, humano.";
        }

        public static string RespuestaPropinasPorEmpleado(string pregunta)
        {
            var ventas = new VentaService().GetAll();
            var hoy = ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();
            var empleado = EmpleadoMencionado(pregunta);

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                var total = hoy
                    .Where(v => string.Equals(v.NombreEmpleado, empleado, StringComparison.OrdinalIgnoreCase))
                    .Sum(v => v.Propina);

                return total <= 0
                    ? $"{empleado} no registra propinas hoy, humano."
                    : $"{empleado} lleva {total.ToString("C2", Cultura)} en propinas hoy, humano.";
            }

            if (hoy.Count == 0)
                return "Todavía no hay propinas hoy, humano.";

            var grupos = hoy
                .GroupBy(v => string.IsNullOrWhiteSpace(v.NombreEmpleado) ? "sin nombre" : v.NombreEmpleado)
                .Select(g => $"{g.Key} {g.Sum(v => v.Propina).ToString("C2", Cultura)}")
                .ToList();

            return "Hoy las propinas están así, humano: " + string.Join(", ", grupos) + ".";
        }

        public static string RespuestaPagos()
        {
            var config = LeerConfiguracion();
            return $"Se puede pagar con {config.Pagos}, humano.";
        }

        public static string RespuestaStockBajo()
        {
            var productos = new ProductoService().GetAll();
            var bajos = productos
                .Where(p => p.Stock <= p.StockMinimo)
                .Select(p => p.Nombre)
                .ToList();

            return bajos.Count == 0
                ? "Todo el stock está por encima del mínimo, humano."
                : $"Con stock bajo están: {UnirNombres(bajos)}, humano.";
        }

        public static string RespuestaCantidadVentas(string pregunta)
        {
            var ventas = new VentaService().GetAll();
            var historico = EsConsultaHistorica(pregunta);
            var seleccion = historico
                ? ventas
                : ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();

            if (seleccion.Count == 0)
                return historico
                    ? "Todavía no hay ventas registradas, humano."
                    : "Hoy todavía no hay ventas registradas, humano.";

            var tickets = seleccion.Count == 1 ? "1 ticket" : $"{seleccion.Count} tickets";
            var total = seleccion.Sum(v => v.Total).ToString("C2", Cultura);

            return historico
                ? $"En total llevamos {tickets} y {total}, humano."
                : $"Hoy llevamos {tickets} y {total}, humano.";
        }

        /// <summary>Menú completo o de una categoría, con precios reales y stock disponible.</summary>
        public static string RespuestaMenu(string? pregunta = null)
        {
            var productos = new ProductoService().GetAll();
            var disponibles = productos.Where(p => p.Stock > 0).ToList();

            if (disponibles.Count == 0)
                return "Ahora mismo no hay nada disponible en el menú, humano.";

            var q = string.IsNullOrWhiteSpace(pregunta) ? "" : Normalizar(pregunta);

            var categoria = disponibles
                .Select(p => p.Categoria)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(c => Regex.IsMatch(q, $@"\b{Regex.Escape(Normalizar(c))}\b"));

            if (categoria is null)
            {
                foreach (var (alias, destino) in AliasCategorias)
                {
                    if (!Regex.IsMatch(q, $@"\b{alias}\b")) continue;

                    categoria = disponibles
                        .Select(p => p.Categoria)
                        .FirstOrDefault(c => Normalizar(c) == destino);
                    if (categoria is not null) break;
                }
            }

            if (categoria is not null)
            {
                var deCategoria = disponibles
                    .Where(p => string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return $"De {categoria.ToLowerInvariant()} tenemos {ListaConPrecios(deCategoria)}, humano.";
            }

            var grupos = disponibles
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Categoria) ? "Otros" : p.Categoria)
                .OrderBy(g => OrdenCategoria(g.Key))
                .Select(g => $"{g.Key}: {ListaConPrecios(g.ToList())}");

            return "Este es el menú de hoy, humano: " + string.Join("; ", grupos) + ".";
        }

        /// <summary>Total recaudado hoy (subtotal + propina), sin tocar el acumulado.</summary>
        public static string RespuestaRecaudado(string pregunta)
        {
            if (EsConsultaHistorica(pregunta))
                return RespuestaHistorica();

            var ventas = new VentaService().GetAll();
            var hoy = ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();

            if (hoy.Count == 0)
                return "Hoy todavía no se ha recaudado nada, humano.";

            var total = hoy.Sum(v => v.Total).ToString("C2", Cultura);
            var tickets = hoy.Count == 1 ? "1 ticket" : $"{hoy.Count} tickets";

            return $"Hoy se ha recaudado {total} en {tickets}, humano. Eso incluye las propinas.";
        }

        public static string RespuestaTopProductos(string? pregunta = null)
        {
            var ventas = new VentaService().GetAll();
            var detalles = new VentaService().GetDetalles();
            var q = string.IsNullOrWhiteSpace(pregunta) ? "" : Normalizar(pregunta);

            var pideSemana = q.Contains("semana") ||
                             q.Contains("siete") ||
                             q.Contains("7 dias") ||
                             q.Contains("ultimos dias");
            var pideHistorico = q.Contains("historico") ||
                                q.Contains("historial") ||
                                q.Contains("siempre") ||
                                q.Contains("todos los tiempos") ||
                                q.Contains("en total") ||
                                q.Contains("total general");

            var plural = q.Contains("productos mas vendidos") ||
                         q.Contains("cuales son los mas vendidos") ||
                         q.Contains("cuales son los productos") ||
                         q.Contains("top productos") ||
                         q.Contains("top de productos") ||
                         q.Contains("top ventas") ||
                         q.Contains("productos estrella");

            List<Venta> seleccion;
            string inicio;
            if (pideHistorico)
            {
                seleccion = ventas;
                inicio = "En total, ";
            }
            else if (pideSemana)
            {
                seleccion = ventas
                    .Where(v => v.FechaHora.Date >= DateTime.Today.AddDays(-6))
                    .ToList();
                inicio = "En los últimos siete días, ";
            }
            else
            {
                seleccion = ventas.Where(v => v.FechaHora.Date == DateTime.Today).ToList();
                inicio = "Hoy, ";
            }

            var ids = seleccion.Select(v => v.IdVenta).ToHashSet();
            var top = detalles
                .Where(d => ids.Contains(d.IdVenta))
                .GroupBy(d => d.NombreProducto)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.Nombre)
                .Take(plural ? 3 : 1)
                .Select(x => x.Nombre)
                .ToList();

            if (top.Count == 0)
                return inicio.TrimEnd(' ', ',') + " no hay ventas registradas, humano.";

            var nombres = UnirNombres(top);
            return top.Count == 1
                ? $"{inicio}lo más vendido es {nombres}, humano."
                : $"{inicio}los más vendidos son: {nombres}, humano.";
        }

        private static string DescribirStockBajo(List<Producto> productos)
        {
            var bajos = productos
                .Where(p => p.Stock <= p.StockMinimo)
                .Select(p => p.Nombre)
                .ToList();

            return bajos.Count == 0
                ? "todo el stock está por encima del mínimo"
                : UnirNombres(bajos);
        }

        private static string DescribirTop(List<DetalleVenta> detalles, List<Venta> ventas)
        {
            var hace7 = DateTime.Today.AddDays(-6);
            var ids = ventas
                .Where(v => v.FechaHora.Date >= hace7)
                .Select(v => v.IdVenta)
                .ToHashSet();

            var top = detalles
                .Where(d => ids.Contains(d.IdVenta))
                .GroupBy(d => d.NombreProducto)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .Take(3)
                .Select(x => x.Nombre)
                .ToList();

            return top.Count == 0 ? "todavía no hay ventas" : UnirNombres(top);
        }

        /// <summary>
        /// Arma el menú con los productos que sí tienen stock. Sin argumento
        /// devuelve todas las categorías; con argumento filtra una ("bebidas").
        /// </summary>
        private static string DescribirMenu(List<Producto> productos, string categoria)
        {
            var disponibles = productos
                .Where(p => p.Stock > 0)
                .ToList();

            if (disponibles.Count == 0)
                return "no hay nada disponible en el menú";

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var buscada = Normalizar(categoria);
                var deCategoria = disponibles
                    .Where(p => Normalizar(p.Categoria) == buscada ||
                                Normalizar(p.Categoria).Contains(buscada))
                    .ToList();

                return deCategoria.Count == 0
                    ? "no hay nada de esa categoría en el menú"
                    : ListaConPrecios(deCategoria);
            }

            var grupos = disponibles
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Categoria) ? "Otros" : p.Categoria)
                .OrderBy(g => OrdenCategoria(g.Key))
                .Select(g => $"{g.Key}: {ListaConPrecios(g.ToList())}");

            return string.Join("; ", grupos);
        }

        private static string ListaConPrecios(List<Producto> productos)
        {
            var partes = productos
                .OrderBy(p => p.Precio)
                .Select(p => $"{p.Nombre.ToLowerInvariant()} {p.Precio.ToString("C2", Cultura)}")
                .ToList();

            return UnirNombres(partes);
        }

        private static int OrdenCategoria(string categoria) =>
            Normalizar(categoria) switch
            {
                "bebidas" => 0,
                "panaderia" => 1,
                "comida" => 2,
                "especiales" => 3,
                "postres" => 4,
                "merch" => 5,
                "juguetes" => 6,
                _ => 7
            };

        private static string DescribirPresupuesto(List<Producto> productos, decimal monto)
        {
            var disponibles = productos
                .Where(p => p.Stock > 0)
                .OrderBy(p => p.Precio)
                .ToList();

            if (disponibles.Count == 0) return "no queda nada en el menú";

            var mejor = new List<Producto>();
            var mejorTotal = -1m;

            void Considerar(List<Producto> combo, decimal total)
            {
                if (total > monto) return;
                if (total > mejorTotal || (total == mejorTotal && combo.Count > mejor.Count))
                {
                    mejorTotal = total;
                    mejor = combo;
                }
            }

            for (var i = 0; i < disponibles.Count; i++)
            {
                Considerar(new List<Producto> { disponibles[i] }, disponibles[i].Precio);

                for (var j = i + 1; j < disponibles.Count; j++)
                {
                    Considerar(
                        new List<Producto> { disponibles[i], disponibles[j] },
                        disponibles[i].Precio + disponibles[j].Precio);

                    for (var k = j + 1; k < disponibles.Count; k++)
                    {
                        Considerar(
                            new List<Producto> { disponibles[i], disponibles[j], disponibles[k] },
                            disponibles[i].Precio + disponibles[j].Precio + disponibles[k].Precio);
                    }
                }
            }

            return mejor.Count == 0
                ? "no te alcanza para nada del menú"
                : "te alcanza para " + UnirNombres(mejor.Select(p => p.Nombre).ToList());
        }

        private static string UnirNombres(List<string> nombres)
        {
            if (nombres.Count == 0) return "";
            if (nombres.Count == 1) return nombres[0];
            if (nombres.Count == 2) return $"{nombres[0]} y {nombres[1]}";

            return string.Join(", ", nombres.Take(nombres.Count - 1)) + " y " + nombres[^1];
        }

        private static Producto? BuscarProducto(List<Producto> productos, string consulta)
        {
            if (string.IsNullOrWhiteSpace(consulta)) return null;

            var q = Normalizar(consulta);
            if (q.Length == 0) return null;

            var qc = Compacto(q);

            var exacto = productos.FirstOrDefault(p => Normalizar(p.Nombre) == q);
            if (exacto is not null) return exacto;

            // El modelo a veces escribe "matchalatte" en vez de "Matcha Latte".
            var exactoCompacto = productos.FirstOrDefault(p => Compacto(Normalizar(p.Nombre)) == qc);
            if (exactoCompacto is not null) return exactoCompacto;

            var empieza = productos.FirstOrDefault(p => Normalizar(p.Nombre).StartsWith(q + " ", StringComparison.Ordinal));
            if (empieza is not null) return empieza;

            if (qc.Length >= 4)
            {
                var empiezaCompacto = productos.FirstOrDefault(p =>
                    Compacto(Normalizar(p.Nombre)).StartsWith(qc, StringComparison.Ordinal));
                if (empiezaCompacto is not null) return empiezaCompacto;
            }

            // El modelo a veces antepone cantidades ("10 jugo de naranja").
            var sinNumero = Regex.Replace(q, @"^\d+\s+", "");
            var porNombre = productos.FirstOrDefault(p =>
                sinNumero.Contains(Normalizar(p.Nombre), StringComparison.Ordinal));
            if (porNombre is not null) return porNombre;

            var sinNumeroCompacto = Compacto(sinNumero);
            return productos.FirstOrDefault(p =>
                sinNumeroCompacto.Contains(Compacto(Normalizar(p.Nombre)), StringComparison.Ordinal));
        }

        private static string Compacto(string texto) => texto.Replace(" ", "");

        private static string Normalizar(string texto)
        {
            var sinAcentos = new string(
                texto.Normalize(NormalizationForm.FormD)
                     .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                     .ToArray());

            // El modelo a veces une palabras con "_" o "-" (matcha_latte).
            // Se tratan igual que un espacio para buscar el producto real.
            return Regex.Replace(sinAcentos.ToLowerInvariant().Trim(), @"[\s_\-]+", " ");
        }

        private static string Limpiar(string texto)
        {
            texto = Regex.Replace(texto, @"\$\s*\$", "$$");

            // El modelo a veces empieza con "PAGOS:" antes del comodín.
            // La etiqueta sobra y dejarla produce "PAGOS: efectivo y tarjeta".
            var etiqueta = Regex.Match(texto, @"^\s*PAGOS\s*:\s*", RegexOptions.IgnoreCase);
            if (etiqueta.Success)
            {
                texto = texto[etiqueta.Length..];
                if (texto.Length > 0 && char.IsLower(texto[0]))
                    texto = char.ToUpper(texto[0], Cultura) + texto[1..];
            }

            texto = Regex.Replace(texto, @"[ \t]{2,}", " ");
            return texto.Trim();
        }

        private static bool TryParseMonto(string texto, out decimal monto)
        {
            monto = 0m;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            var limpio = texto.Replace("$", "").Replace(",", "").Trim();
            return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out monto) &&
                   monto > 0m;
        }

        private static Configuracion LeerConfiguracion()
        {
            try
            {
                if (!File.Exists(AppPaths.Configuracion)) return new Configuracion();

                using var doc = JsonDocument.Parse(File.ReadAllText(AppPaths.Configuracion));
                var raiz = doc.RootElement;

                return new Configuracion
                {
                    Horario = raiz.TryGetProperty("horario", out var h) && h.ValueKind == JsonValueKind.String
                        ? h.GetString() ?? "8:00 a 20:00"
                        : "8:00 a 20:00",
                    Pagos = raiz.TryGetProperty("pagos", out var p) && p.ValueKind == JsonValueKind.String
                        ? p.GetString() ?? "efectivo y tarjeta"
                        : "efectivo y tarjeta"
                };
            }
            catch
            {
                return new Configuracion();
            }
        }

        private sealed class Configuracion
        {
            public string Horario { get; init; } = "8:00 am a 6:00 pm";
            public string Pagos { get; init; } = "efectivo y tarjeta";
        }
    }
}
