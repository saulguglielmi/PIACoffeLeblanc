using System.ComponentModel;
using System.Linq;
using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CafeteriaPOS.Views
{
    public sealed partial class EmployeesPage : Page
    {
        private bool _corrigiendoBusqueda;
        private bool _corrigiendoNombre;
        private bool _corrigiendoPassword;

        public EmployeesViewModel ViewModel { get; } = new();

        public EmployeesPage()
        {
            this.InitializeComponent();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// El PasswordBox no se puede bindear: cuando el ViewModel limpia la
        /// contraseña (al guardar, cancelar o cambiar de empleado), también
        /// hay que borrar los puntos que quedaron visibles en la caja.
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(EmployeesViewModel.FormPassword)) return;
            if (!string.IsNullOrEmpty(ViewModel.FormPassword)) return;
            if (FormPasswordBox.Password.Length == 0) return;

            _corrigiendoPassword = true;
            FormPasswordBox.Password = "";
            _corrigiendoPassword = false;
        }

        /// <summary>
        /// El buscador solo acepta letras y espacios, con un máximo de 30.
        /// También limpia lo que se pegue.
        /// </summary>
        private void Busqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_corrigiendoBusqueda) return;
            if (sender is not TextBox caja) return;

            string actual = caja.Text ?? "";
            string limpio = new string(actual
                .Where(c => char.IsLetter(c) || c == ' ')
                .Take(30)
                .ToArray());

            if (limpio == actual) return;

            _corrigiendoBusqueda = true;
            caja.Text = limpio;
            caja.SelectionStart = limpio.Length;
            _corrigiendoBusqueda = false;
        }

        /// <summary>
        /// El nombre completo solo acepta letras y espacios, con un máximo de 30.
        /// </summary>
        private void FormNombre_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_corrigiendoNombre) return;
            if (sender is not TextBox caja) return;

            string actual = caja.Text ?? "";
            string limpio = new string(actual
                .Where(c => char.IsLetter(c) || c == ' ')
                .Take(30)
                .ToArray());

            if (limpio == actual) return;

            _corrigiendoNombre = true;
            caja.Text = limpio;
            caja.SelectionStart = limpio.Length;
            _corrigiendoNombre = false;
        }

        /// <summary>
        /// PasswordBox no permite binding, así que sincronizamos manualmente.
        /// La contraseña es un PIN de máximo 4 dígitos.
        /// </summary>
        private void FormPasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is not PasswordBox pb) return;
            if (_corrigiendoPassword) return;

            string actual = pb.Password ?? "";
            string limpio = new string(actual
                .Where(c => c >= '0' && c <= '9')
                .Take(4)
                .ToArray());

            if (limpio != actual)
            {
                _corrigiendoPassword = true;
                pb.Password = limpio;
                _corrigiendoPassword = false;
            }

            ViewModel.FormPassword = limpio;
        }

        /// <summary>
        /// Solo se aceptan caracteres de texto (letras), sin números ni caracteres especiales ni emojis.
        /// Se revisa el texto completo antes de que se guarde, y funciona incluso con IME activo.
        /// </summary>
        private void FormUserTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs e)
        {
            var text = e.NewText;
            if (!string.IsNullOrEmpty(text) && !text.All(char.IsLetter))
            {
                e.Cancel = true;
            }
        }
    }
}