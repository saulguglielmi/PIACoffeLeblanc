using CafeteriaPOS.Models;
using CafeteriaPOS.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CafeteriaPOS.ViewModels
{
    public partial class EmployeesViewModel : ObservableObject
    {
        private readonly EmpleadoService _empleadoService = new();

        // ───────── Lista ─────────
        private ObservableCollection<Empleado> _todos { get; } = new();
        public ObservableCollection<Empleado> EmpleadosFiltrados { get; } = new();

        [ObservableProperty] private string textoBusqueda = "";
        [ObservableProperty] private Empleado? empleadoSeleccionado;

        // ───────── Formulario ─────────
        [ObservableProperty] private string formNombre = "";
        [ObservableProperty] private string formUsuario = "";
        [ObservableProperty] private string formPassword = "";
        [ObservableProperty] private string formPuesto = "Cajero"; // Admin | Cajero
        [ObservableProperty] private string mensajeEstado = "";
        [ObservableProperty] private bool modoEdicion = false;

        // ───────── Títulos dinámicos ─────────
        public string TituloFormulario => ModoEdicion ? "✏️ Editar empleado" : "➕ Nuevo empleado";
        public string TextoBotonGuardar => ModoEdicion ? "Actualizar" : "Guardar";
        public bool HaySeleccion => EmpleadoSeleccionado is not null;

        // ───────── Opciones del ComboBox ─────────
        public string[] PuestosDisponibles { get; } = { "Admin", "Cajero" };

        // ───────── Hint de contraseña ─────────
        public string HintPassword => ModoEdicion
            ? "Deja vacío para no cambiarla"
            : "Contraseña obligatoria";

        public EmployeesViewModel()
        {
            CargarEmpleados();
        }

        // ───────── Carga y filtro ─────────
        public void CargarEmpleados()
        {
            _todos.Clear();
            foreach (var e in _empleadoService.GetAll())
                _todos.Add(e);

            AplicarFiltro();
        }

        partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();

        private void AplicarFiltro()
        {
            EmpleadosFiltrados.Clear();
            var query = _todos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
                query = query.Where(e =>
                    e.Nombre.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                    e.Usuario.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase));

            foreach (var e in query)
                EmpleadosFiltrados.Add(e);
        }

        // ───────── Al seleccionar ─────────
        partial void OnEmpleadoSeleccionadoChanged(Empleado? value)
        {
            OnPropertyChanged(nameof(HaySeleccion));

            if (value is null) return;

            FormNombre = value.Nombre;
            FormUsuario = value.Usuario;
            FormPassword = "";   // Por seguridad: no mostramos la contraseña
            FormPuesto = value.Puesto;
            ModoEdicion = true;
            MensajeEstado = "";
        }

        partial void OnModoEdicionChanged(bool value)
        {
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(TextoBotonGuardar));
            OnPropertyChanged(nameof(HintPassword));
        }

        // ───────── Comandos ─────────
        [RelayCommand]
        private void Nuevo()
        {
            LimpiarFormulario();
            ModoEdicion = false;
        }

        [RelayCommand]
        private void Guardar()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(FormNombre))
            {
                MensajeEstado = "⚠️ El nombre es obligatorio.";
                return;
            }
            if (FormNombre.Length > 30)
            {
                MensajeEstado = "⚠️ El nombre no puede superar los 30 caracteres.";
                return;
            }
            if (!FormNombre.All(c => char.IsLetter(c) || c == ' '))
            {
                MensajeEstado = "⚠️ El nombre solo acepta letras y espacios.";
                return;
            }
            if (string.IsNullOrWhiteSpace(FormUsuario))
            {
                MensajeEstado = "⚠️ El usuario es obligatorio.";
                return;
            }
            if (FormUsuario.Length > 20)
            {
                MensajeEstado = "⚠️ El usuario no puede superar los 20 caracteres.";
                return;
            }

            // Seguridad adicional: solo letras, sin números ni signos ni emojis
            if (!FormUsuario.All(char.IsLetter))
            {
                MensajeEstado = "⚠️ El usuario solo acepta letras.";
                return;
            }
            if (string.IsNullOrWhiteSpace(FormPuesto))
            {
                MensajeEstado = "⚠️ Debes elegir un puesto.";
                return;
            }

            // Validar contraseña según el modo
            if (!ModoEdicion && string.IsNullOrWhiteSpace(FormPassword))
            {
                MensajeEstado = "⚠️ La contraseña es obligatoria para nuevos empleados.";
                return;
            }
            if (!string.IsNullOrEmpty(FormPassword) &&
                (FormPassword.Length > 4 || !FormPassword.All(char.IsDigit)))
            {
                MensajeEstado = "⚠️ La contraseña debe tener máximo 4 dígitos.";
                return;
            }

            // Validar usuario único
            int exceptoId = ModoEdicion && EmpleadoSeleccionado is not null
                ? EmpleadoSeleccionado.Id
                : -1;

            if (_empleadoService.ExisteUsuario(FormUsuario.Trim(), exceptoId))
            {
                MensajeEstado = "⚠️ Ese nombre de usuario ya existe.";
                return;
            }

            var empleados = _empleadoService.GetAll();

            if (ModoEdicion && EmpleadoSeleccionado is not null)
            {
                var e = empleados.FirstOrDefault(x => x.Id == EmpleadoSeleccionado.Id);
                if (e is not null)
                {
                    e.Nombre = FormNombre.Trim();
                    e.Usuario = FormUsuario.Trim();
                    e.Puesto = FormPuesto;

                    // Solo actualizamos contraseña si el admin escribió una nueva
                    if (!string.IsNullOrWhiteSpace(FormPassword))
                        e.Password = FormPassword;
                }
                MensajeEstado = "✅ Empleado actualizado.";
            }
            else
            {
                empleados.Add(new Empleado
                {
                    Id = _empleadoService.SiguienteId(),
                    Nombre = FormNombre.Trim(),
                    Usuario = FormUsuario.Trim(),
                    Password = FormPassword,
                    Puesto = FormPuesto
                });
                MensajeEstado = "✅ Empleado agregado.";
            }

            _empleadoService.SaveAll(empleados);
            CargarEmpleados();
            LimpiarFormulario();
            ModoEdicion = false;
        }

        [RelayCommand]
        private void Eliminar()
        {
            if (EmpleadoSeleccionado is null) return;

            // No permitir auto-eliminarse
            if (SessionService.UsuarioActual is not null &&
                EmpleadoSeleccionado.Id == SessionService.UsuarioActual.Id)
            {
                MensajeEstado = "⚠️ No puedes eliminar tu propio usuario.";
                return;
            }

            var empleados = _empleadoService.GetAll();
            empleados.RemoveAll(e => e.Id == EmpleadoSeleccionado.Id);
            _empleadoService.SaveAll(empleados);
            CargarEmpleados();
            LimpiarFormulario();
            ModoEdicion = false;
            MensajeEstado = "🗑️ Empleado eliminado.";
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
            FormUsuario = "";
            FormPassword = "";
            FormPuesto = "Cajero";
            EmpleadoSeleccionado = null;
        }
    }
}