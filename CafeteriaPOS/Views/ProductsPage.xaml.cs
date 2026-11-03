using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace CafeteriaPOS.Views
{
    public sealed partial class ProductsPage : Page
    {
        private bool _corrigiendoTexto;

        public ProductsViewModel ViewModel { get; } = new();

        public ProductsPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// los nombres de categoría solo aceptan letras (incluidas tildes y ñ)
        /// y un máximo de 15 caracteres. También limpia lo pegado.
        /// </summary>
        private void Categoria_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_corrigiendoTexto) return;
            if (sender is not TextBox caja) return;

            string actual = caja.Text ?? "";
            string limpio = new string(actual.Where(char.IsLetter).Take(15).ToArray());

            if (limpio == actual) return;

            _corrigiendoTexto = true;
            caja.Text = limpio;
            caja.SelectionStart = limpio.Length;
            _corrigiendoTexto = false;
        }
    }
}
