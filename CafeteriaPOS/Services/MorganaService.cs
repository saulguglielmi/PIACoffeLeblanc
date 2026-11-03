using CafeteriaPOS.Helpers;
using LLama;
using LLama.Common;
using LLama.Sampling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CafeteriaPOS.Services
{
    /// <summary>
    /// Carga el modelo afinado dentro del proceso (sin servidor ni API) y
    /// devuelve la respuesta de Morgana con los comodines ya resueltos.
    /// Solo el texto del usuario entra al modelo: no hay prompt de sistema.
    /// </summary>
    public sealed class MorganaService : IDisposable
    {
        private static readonly Lazy<MorganaService> Instancia =
            new(() => new MorganaService(), isThreadSafe: true);

        public static MorganaService Instance => Instancia.Value;

        private readonly SemaphoreSlim _candado = new(1, 1);

        private LLamaWeights? _pesos;
        private StatelessExecutor? _ejecutor;
        private bool _cargado;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static bool _nativosConfigurados;

        public bool Cargado => _cargado;
        public string RutaModelo => AppPaths.Modelo;

        private MorganaService() { }

        /// <summary>Carga el modelo en segundo plano para que la primera pregunta no espere.</summary>
        public async Task PrecalentarAsync(CancellationToken ct = default)
        {
            await _candado.WaitAsync(ct);
            try
            {
                await AsegurarCargadoAsync(ct);
            }
            finally
            {
                _candado.Release();
            }
        }

        public async Task<string> PreguntarAsync(string pregunta, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pregunta)) return ComodinResolver.SinInformacion;

            await _candado.WaitAsync(ct);
            try
            {
                try
                {
                    await AsegurarCargadoAsync(ct);
                }
                catch (FileNotFoundException)
                {
                    return "No encuentro el modelo entrenado, humano.\n\n" +
                           $"Cópialo en:\n{AppPaths.Modelo}";
                }
                catch (Exception ex)
                {
                    RegistrarError(ex);
                    return "No pude cargar el modelo, humano. Revisa que el archivo esté completo.";
                }

                var esHistorico = ComodinResolver.EsConsultaHistorica(pregunta);
                var esPropinasEmpleado = ComodinResolver.EsConsultaPropinasPorEmpleado(pregunta);
                var esPagos = ComodinResolver.EsConsultaPagos(pregunta);
                var esStockBajo = ComodinResolver.EsConsultaStockBajo(pregunta);
                var esTopProductos = ComodinResolver.EsConsultaTopProductos(pregunta);
                var esCantidadVentas = ComodinResolver.EsConsultaCantidadVentas(pregunta);
                var esMenu = ComodinResolver.EsConsultaMenu(pregunta);
                var esRecaudadoHoy = ComodinResolver.EsConsultaRecaudadoHoy(pregunta);
                var esPropinas = ComodinResolver.EsConsultaPropinas(pregunta);

                // Estas consultas son de datos puros y deben coincidir siempre
                // con el periodo preguntado; se responden sin pasar por el modelo.
                if (esMenu)
                    return ComodinResolver.RespuestaMenu(pregunta);

                if (esRecaudadoHoy)
                    return ComodinResolver.RespuestaRecaudado(pregunta);

                if (esPropinas)
                    return ComodinResolver.RespuestaPropinas(pregunta);

                if (esCantidadVentas)
                    return ComodinResolver.RespuestaCantidadVentas(pregunta);

                if (esTopProductos)
                    return ComodinResolver.RespuestaTopProductos(pregunta);

                ResultadoResolucion? ultimo = null;

                // Dos intentos: si el primero inventa un comodín, se vuelve a generar.
                for (var intento = 0; intento < 2; intento++)
                {
                    ct.ThrowIfCancellationRequested();

                    var salida = await GenerarAsync(pregunta, intento, ct);
                    if (string.IsNullOrWhiteSpace(salida))
                    {
                        ultimo = ResultadoResolucion.Invalido(MotivoInvalido.ComodinDesconocido, "respuesta vacía");
                        continue;
                    }

                    var resultado = ComodinResolver.Resolver(salida, pregunta);
                    if (resultado.Ok)
                    {
                        // La respuesta es válida, pero para estas preguntas
                        // debe traer el comodín correcto; si no, se reintenta.
                        if (esPropinasEmpleado && !ComodinResolver.TieneComodin(salida, "PROPINAS_POR_EMPLEADO"))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin propinas por empleado. Salida: {salida}");
                            continue;
                        }

                        if (esHistorico && !ComodinResolver.MencionaVentas(salida))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin ventas históricas. Salida: {salida}");
                            continue;
                        }

                        if (esPagos && !ComodinResolver.TieneComodin(salida, "PAGOS"))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin comodín de pagos. Salida: {salida}");
                            continue;
                        }

                        if (esStockBajo && !ComodinResolver.TieneComodin(salida, "STOCK_BAJO"))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin stock bajo. Salida: {salida}");
                            continue;
                        }

                        if (esMenu && !ComodinResolver.TieneComodin(salida, "MENU"))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin comodín de menú. Salida: {salida}");
                            continue;
                        }

                        if (esRecaudadoHoy && !ComodinResolver.MencionaVentas(salida))
                        {
                            ultimo = resultado;
                            Debug.WriteLine($"[Morgana] Intento {intento + 1} sin ventas de hoy. Salida: {salida}");
                            continue;
                        }

                        return resultado.Texto;
                    }

                    ultimo = resultado;
                    Debug.WriteLine($"[Morgana] Intento {intento + 1} inválido: {resultado.Motivo} ({resultado.Detalle}). Salida: {salida}");
                }

                // Fallback determinista solo para intenciones claras.
                if (esMenu)
                    return ComodinResolver.RespuestaMenu(pregunta);

                if (esRecaudadoHoy)
                    return ComodinResolver.RespuestaRecaudado(pregunta);

                if (esStockBajo)
                    return ComodinResolver.RespuestaStockBajo();

                if (esPropinasEmpleado)
                    return ComodinResolver.RespuestaPropinasPorEmpleado(pregunta);

                if (esHistorico)
                    return ComodinResolver.RespuestaHistorica();

                if (esPagos)
                    return ComodinResolver.RespuestaPagos();

                return ultimo?.Motivo == MotivoInvalido.ProductoNoExiste
                    ? ComodinResolver.ProductoNoExiste
                    : ComodinResolver.SinInformacion;
            }
            catch (OperationCanceledException)
            {
                return ComodinResolver.SinInformacion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return "No pude responder, humano. Intenta otra vez.";
            }
            finally
            {
                _candado.Release();
            }
        }

        private async Task AsegurarCargadoAsync(CancellationToken ct)
        {
            if (_cargado) return;

            if (!File.Exists(AppPaths.Modelo))
                throw new FileNotFoundException("Modelo no encontrado", AppPaths.Modelo);

            ConfigurarNativos();

            var parametros = new ModelParams(AppPaths.Modelo)
            {
                ContextSize = 2048,
                GpuLayerCount = 0,
                BatchSize = 512,
                Threads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8)
            };

            var pesos = await Task.Run(() => LLamaWeights.LoadFromFile(parametros), ct);

            _pesos = pesos;
            _ejecutor = new StatelessExecutor(pesos, parametros) { ApplyTemplate = false };
            _cargado = true;
        }

        private async Task<string> GenerarAsync(string pregunta, int intento, CancellationToken ct)
        {
            // Plantilla ChatML de Qwen, sin mensaje de sistema.
            var prompt = $"<|im_start|>user\n{pregunta.Trim()}<|im_end|>\n<|im_start|>assistant\n";

            var parametros = new InferenceParams
            {
                MaxTokens = 120,
                AntiPrompts = new List<string> { "<|im_end|>", "<|im_start|>" },
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = 0.3f,
                    TopP = 0.9f,
                    TopK = 40,
                    RepeatPenalty = 1.1f,
                    Seed = (uint)(42 + intento * 7919)
                }
            };

            var sb = new StringBuilder();
            await foreach (var token in _ejecutor!.InferAsync(prompt, parametros, ct))
                sb.Append(token);

            var salida = sb.ToString()
                .Replace("<|im_end|>", "")
                .Replace("<|im_start|>", "")
                .Trim();

            if (salida.StartsWith("assistant", StringComparison.OrdinalIgnoreCase))
                salida = salida[9..].TrimStart(':', ' ', '\n', '\r');

            return salida;
        }

        public void Dispose()
        {
            _ejecutor = null;
            _pesos?.Dispose();
            _pesos = null;
            _cargado = false;
            _candado.Dispose();
        }

        private static void ConfigurarNativos()
        {
            if (_nativosConfigurados) return;
            _nativosConfigurados = true;

            // En .NET 8, LLamaSharp usa su build netstandard2.0, que no busca
            // los nativos en runtimes\...\native\{cpu}. Se lo indicamos a Windows.
            var carpeta = Avx512F.IsSupported ? "avx512"
                        : Avx2.IsSupported ? "avx2"
                        : Avx.IsSupported ? "avx"
                        : "noavx";

            var ruta = Path.Combine(
                AppContext.BaseDirectory,
                "runtimes", "win-x64", "native", carpeta);

            if (Directory.Exists(ruta))
                SetDllDirectory(ruta);
        }

        private static void RegistrarError(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataFolder);
                File.AppendAllText(
                    Path.Combine(AppPaths.DataFolder, "morgana_error.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
            }
            catch
            {
                // si tampoco se puede escribir el log, no hay nada más que hacer
            }

            Debug.WriteLine(ex);
        }
    }
}
