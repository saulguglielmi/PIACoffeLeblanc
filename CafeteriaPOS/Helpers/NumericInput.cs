using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.CompilerServices;
using System.Text;

namespace CafeteriaPOS.Helpers
{
    /// <summary>
    /// Restringe lo que se puede escribir dentro de un NumberBox:
    /// solo dígitos, sin signos ni caracteres especiales, con un máximo de
    /// dígitos enteros y, opcionalmente, decimales.
    /// Se usa como propiedad adjunta en el XAML:
    ///   helpers:NumericInput.MaxDigits="4"
    ///   helpers:NumericInput.AllowDecimal="True"
    /// </summary>
    public static class NumericInput
    {
        // Evita enganchar dos veces el mismo TextBox interno del NumberBox.
        private static readonly ConditionalWeakTable<TextBox, NumberBox> Enganchados = new();

        public static readonly DependencyProperty MaxDigitsProperty =
            DependencyProperty.RegisterAttached(
                "MaxDigits",
                typeof(int),
                typeof(NumericInput),
                // El valor predeterminado NO debe coincidir con el que se usa en
                // el XAML: WinUI no invoca el callback cuando se asigna el mismo
                // valor que ya tenía la propiedad, y el filtro no se engancharía.
                new PropertyMetadata(int.MaxValue, AlCambiarConfiguracion));

        public static void SetMaxDigits(DependencyObject elemento, int valor) =>
            elemento.SetValue(MaxDigitsProperty, valor);

        public static int GetMaxDigits(DependencyObject elemento) =>
            (int)elemento.GetValue(MaxDigitsProperty);

        public static readonly DependencyProperty AllowDecimalProperty =
            DependencyProperty.RegisterAttached(
                "AllowDecimal",
                typeof(bool),
                typeof(NumericInput),
                new PropertyMetadata(false, AlCambiarConfiguracion));

        public static void SetAllowDecimal(DependencyObject elemento, bool valor) =>
            elemento.SetValue(AllowDecimalProperty, valor);

        public static bool GetAllowDecimal(DependencyObject elemento) =>
            (bool)elemento.GetValue(AllowDecimalProperty);

        public static readonly DependencyProperty MaxDecimalsProperty =
            DependencyProperty.RegisterAttached(
                "MaxDecimals",
                typeof(int),
                typeof(NumericInput),
                new PropertyMetadata(2, AlCambiarConfiguracion));

        public static void SetMaxDecimals(DependencyObject elemento, int valor) =>
            elemento.SetValue(MaxDecimalsProperty, valor);

        public static int GetMaxDecimals(DependencyObject elemento) =>
            (int)elemento.GetValue(MaxDecimalsProperty);

        private static void AlCambiarConfiguracion(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not NumberBox caja) return;

            caja.Loaded -= Caja_Loaded;
            caja.Loaded += Caja_Loaded;
            caja.GotFocus -= Caja_GotFocus;
            caja.GotFocus += Caja_GotFocus;

            // Si la propiedad se aplica después de que la caja ya está cargada.
            if (caja.IsLoaded) Enganchar(caja);
        }

        private static void Caja_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox caja) Enganchar(caja);
        }

        private static void Caja_GotFocus(object sender, RoutedEventArgs e)
        {
            // Al entrar en foco la plantilla ya existe; si se reemplazó el
            // TextBox interno, aquí se vuelve a enganchar el actual.
            if (sender is NumberBox caja) Enganchar(caja);
        }

        /// <summary>
        /// Punto de entrada para NumericNumberBox.OnApplyTemplate: la plantilla
        /// acaba de crearse, así que el TextBox interno es el definitivo.
        /// </summary>
        internal static void EngancharPlantilla(NumberBox caja, TextBox? texto)
        {
            if (texto is not null)
            {
                Conectar(texto, caja);
                return;
            }

            var encontrado = BuscarTextBox(caja);
            if (encontrado is not null) Conectar(encontrado, caja);
        }

        private static void Enganchar(NumberBox caja)
        {
            caja.ApplyTemplate();

            var texto = BuscarTextBox(caja);
            if (texto is not null)
            {
                Conectar(texto, caja);
                return;
            }

            caja.DispatcherQueue.TryEnqueue(() =>
            {
                caja.ApplyTemplate();

                var interno = BuscarTextBox(caja);
                if (interno is not null) Conectar(interno, caja);
            });
        }

        internal static void Conectar(TextBox texto, NumberBox caja)
        {
            if (Enganchados.TryGetValue(texto, out _)) return;

            Enganchados.Add(texto, caja);
            texto.BeforeTextChanging += Texto_AntesDeCambiar;
            texto.TextChanged += Texto_DespuesDeCambiar;
            texto.PreviewKeyDown += Texto_AntesDeTeclear;
        }

        // Antes de escribir, se descartan las teclas de signo. Algunos
        // NumberBox interpretan "-" o "+" antes de que BeforeTextChanging
        // alcance a cancelar el cambio de texto.
        private static void Texto_AntesDeTeclear(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Add:
                case Windows.System.VirtualKey.Subtract:
                case (Windows.System.VirtualKey)187: // tecla OEM +
                case (Windows.System.VirtualKey)189: // tecla OEM -
                    e.Handled = true;
                    break;
            }
        }

        private static void Texto_AntesDeCambiar(TextBox sender, TextBoxBeforeTextChangingEventArgs e)
        {
            if (!Enganchados.TryGetValue(sender, out var caja)) return;
            if (!EsValido(e.NewText ?? "", caja)) e.Cancel = true;
        }

        // Segunda capa: si por alguna razón el cambio se coló (por ejemplo,
        // porque el NumberBox todavía no había enganchado su TextBox), se
        // recorta el texto inmediatamente a los límites configurados.
        private static void Texto_DespuesDeCambiar(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox texto) return;
            if (!Enganchados.TryGetValue(texto, out var caja)) return;

            string actual = texto.Text ?? "";
            string corregido = Corregir(actual, caja);
            if (corregido == actual) return;

            texto.Text = corregido;
            texto.SelectionStart = corregido.Length;
        }

        private static string Corregir(string texto, NumberBox caja)
        {
            int maxEnteros = GetMaxDigits(caja);
            bool aceptaDecimales = GetAllowDecimal(caja);
            int maxDecimales = GetMaxDecimals(caja);

            var salida = new StringBuilder(texto.Length);
            int enteros = 0;
            int decimales = 0;
            bool enDecimales = false;

            foreach (char c in texto)
            {
                if (c >= '0' && c <= '9')
                {
                    if (enDecimales)
                    {
                        if (decimales < maxDecimales)
                        {
                            salida.Append(c);
                            decimales++;
                        }
                    }
                    else if (enteros < maxEnteros)
                    {
                        salida.Append(c);
                        enteros++;
                    }
                }
                else if (c == '.' && aceptaDecimales && !enDecimales)
                {
                    salida.Append('.');
                    enDecimales = true;
                }
            }

            return salida.ToString();
        }

        private static bool EsValido(string texto, NumberBox caja)
        {
            if (string.IsNullOrEmpty(texto)) return true;

            int maxEnteros = GetMaxDigits(caja);
            bool aceptaDecimales = GetAllowDecimal(caja);
            int maxDecimales = GetMaxDecimals(caja);

            int enteros = 0;
            int decimales = 0;
            bool enDecimales = false;

            foreach (char c in texto)
            {
                if (c >= '0' && c <= '9')
                {
                    if (enDecimales)
                    {
                        decimales++;
                        if (decimales > maxDecimales) return false;
                    }
                    else
                    {
                        enteros++;
                        if (enteros > maxEnteros) return false;
                    }
                }
                else if (c == '.' && aceptaDecimales && !enDecimales)
                {
                    enDecimales = true;
                }
                else
                {
                    // Letras, signos (+/-), comas, espacios y cualquier otro carácter.
                    return false;
                }
            }

            return true;
        }

        private static TextBox? BuscarTextBox(DependencyObject raiz)
        {
            int hijos = VisualTreeHelper.GetChildrenCount(raiz);
            for (int i = 0; i < hijos; i++)
            {
                var hijo = VisualTreeHelper.GetChild(raiz, i);
                if (hijo is TextBox texto) return texto;

                var encontrado = BuscarTextBox(hijo);
                if (encontrado is not null) return encontrado;
            }

            return null;
        }
    }
}
