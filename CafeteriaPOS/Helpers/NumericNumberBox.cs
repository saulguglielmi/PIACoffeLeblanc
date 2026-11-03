using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CafeteriaPOS.Helpers
{
    /// <summary>
    /// NumberBox que garantiza que el filtro numérico se aplique siempre.
    /// El control interno (TextBox) puede ser reemplazado por WinUI sin que
    /// se vuelva a aplicar la plantilla ni se pierda el foco; por eso se
    /// revisa en OnApplyTemplate, Loaded, GotFocus y LayoutUpdated.
    /// </summary>
    public sealed class NumericNumberBox : NumberBox
    {
        private TextBox? _textoActual;

        public NumericNumberBox()
        {
            Loaded += (_, _) => Enganchar();
            GotFocus += (_, _) => Enganchar();
            LayoutUpdated += (_, _) => Enganchar();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Enganchar();
        }

        private void Enganchar()
        {
            var texto = GetTemplateChild("InputBox") as TextBox;
            if (texto is null) return;
            if (ReferenceEquals(texto, _textoActual)) return;

            _textoActual = texto;
            NumericInput.Conectar(texto, this);
        }
    }
}
