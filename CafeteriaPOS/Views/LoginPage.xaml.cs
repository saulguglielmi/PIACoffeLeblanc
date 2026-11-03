using System.Linq;
using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CafeteriaPOS.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginViewModel ViewModel { get; } = new();

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IntentarLogin(PassBox.Password))
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        private void PassBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                LoginButton_Click(sender, e);
        }

        /// <summary>
        /// Solo se aceptan caracteres de texto (letras), sin números ni caracteres especiales ni emojis.
        /// Se revisa el texto completo antes de que se guarde, y funciona incluso con IME activo.
        /// </summary>
        private void UserTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs e)
        {
            var text = e.NewText;
            if (!string.IsNullOrEmpty(text) && !text.All(char.IsLetter))
            {
                e.Cancel = true;
            }
        }
    }
}