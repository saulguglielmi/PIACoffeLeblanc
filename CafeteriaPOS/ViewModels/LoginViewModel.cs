using CafeteriaPOS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;

namespace CafeteriaPOS.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly EmpleadoService _empleadoService = new();

        [ObservableProperty] private string usuario = "";
        [ObservableProperty] private string mensajeError = "";
        [ObservableProperty] private bool hayError;

        /// <summary>
        /// Devuelve true si el login fue exitoso. El PasswordBox no se puede
        /// bindear, así que lo recibimos como parámetro desde el code-behind.
        /// </summary>
        public bool IntentarLogin(string password)
        {
            HayError = false;
            MensajeError = "";

            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(password))
            {
                HayError = true;
                MensajeError = "Ingresa usuario y contraseña.";
                return false;
            }

            // Seguridad adicional: rechazar si el usuario tiene caracteres no-letras
            if (!Usuario.All(char.IsLetter))
            {
                HayError = true;
                MensajeError = "El campo de usuario solo acepta letras.";
                return false;
            }

            var empleado = _empleadoService.ValidarCredenciales(Usuario.Trim(), password);
            if (empleado is null)
            {
                HayError = true;
                MensajeError = "Usuario o contraseña incorrectos.";
                return false;
            }

            SessionService.IniciarSesion(empleado);
            return true;
        }
    }

}