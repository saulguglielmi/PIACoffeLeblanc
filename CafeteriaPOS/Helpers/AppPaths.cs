using System;
using System.IO;

namespace CafeteriaPOS.Helpers
{
    public static class AppPaths
    {
        public static string BaseFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "CafeteriaPOS");

        public static string DataFolder => Path.Combine(BaseFolder, "Data");
        public static string TicketsFolder => Path.Combine(BaseFolder, "Tickets");
        public static string ModelosFolder => Path.Combine(BaseFolder, "Modelos");

        public static string BaseDatos => Path.Combine(DataFolder, "db.json");
        public static string Configuracion => Path.Combine(DataFolder, "configuracion.json");
        public static string Modelo => Path.Combine(ModelosFolder, "morgana.gguf");

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(TicketsFolder);
            Directory.CreateDirectory(ModelosFolder);
        }
    }
}