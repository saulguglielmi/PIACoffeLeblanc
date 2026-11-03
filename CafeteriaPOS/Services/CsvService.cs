using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CafeteriaPOS.Services
{
    /// <summary>
    /// Servicio de exportación de reportes a CSV con codificación UTF-8 con BOM,
    /// asegurando compatibilidad con Excel y Power BI (tildes y ñ correctas).
    /// La base de datos principal ya no usa CSV: vive en Data\db.json.
    /// Maneja escape de comas, comillas y saltos de línea.
    /// </summary>
    public static class CsvService
    {
        // UTF-8 con BOM (Byte Order Mark)      excel lo reconoce.
        private static readonly UTF8Encoding Utf8ConBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // LECTURA

        /// <summary>
        /// lee el archivo csv y devuelve una lista de filas,
        /// donde cada fila es un arreglo de columnas
        /// si el archivo no existe hace una lista vacia
        /// </summary>
        public static List<string[]> ReadAll(string path)
        {
            var filas = new List<string[]>();
            if (!File.Exists(path)) return filas;

            var lineas = File.ReadAllLines(path, Encoding.UTF8);
            foreach (var linea in lineas)
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                filas.Add(ParseLine(linea));
            }
            return filas;
        }

        // ESCRITURA
        
        /// <summary>
        /// sobrescribe el archivo completo
        /// crea el directorio si no existe
        /// </summary>
        public static void WriteAll(string path, IEnumerable<string[]> filas)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var sb = new StringBuilder();
            foreach (var fila in filas)
            {
                sb.AppendLine(string.Join(",", fila.Select(Escape)));
            }

            File.WriteAllText(path, sb.ToString(), Utf8ConBom);
        }

        /// <summary>
        /// añade un append
        /// </summary>
        public static void AppendRow(string path, string[] fila)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // si el archivo no existe, streamWriter escribe el BOM automáticamente.
            using var writer = new StreamWriter(path, append: true, Utf8ConBom);
            writer.WriteLine(string.Join(",", fila.Select(Escape)));
        }

        // UTILIDADES DE ESCAPE / PARSEO
        /// <summary>
        /// escapa un valor si contiene coma, comilla o salto de linea
        /// Ej: Hola, "mundo" a "Hola, ""mundo"""
        /// </summary>
        public static string Escape(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return string.Empty;

            bool necesitaComillas = valor.Contains(',') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r');
            return necesitaComillas ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
        }

        /// <summary>
        /// Parsea una línea csv respetando comillas dobles escapadas ("").
        /// </summary>
        public static string[] ParseLine(string linea)
        {
            var resultado = new List<string>();
            var sb = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];

                if (enComillas)
                {
                    if (c == '"')
                    {
                        // comilla doble "" a comilla literal
                        if (i + 1 < linea.Length && linea[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            enComillas = false;
                        }
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == '"') enComillas = true;
                    else if (c == ',') { resultado.Add(sb.ToString()); sb.Clear(); }
                    else sb.Append(c);
                }
            }
            resultado.Add(sb.ToString());
            return resultado.ToArray();
        }
    }
}