using CafeteriaPOS.Models;



namespace CafeteriaPOS.Services
{
    /// <summary>
    /// Mantiene en memoria al usuario logueado durante la sesión activa.
    /// </summary>
    public static class SessionService
    {
        public static Empleado? UsuarioActual { get; private set; }

        public static bool EsAdmin => UsuarioActual?.Puesto == "Admin";
        public static bool EsCajero => UsuarioActual?.Puesto == "Cajero";

        public static void IniciarSesion(Empleado empleado) => UsuarioActual = empleado;
        public static void CerrarSesion() => UsuarioActual = null;
    }
}