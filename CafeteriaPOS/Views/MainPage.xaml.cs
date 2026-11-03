using CafeteriaPOS.Services;
using CafeteriaPOS.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CafeteriaPOS.Views
{
    public sealed partial class MainPage : Page
    {
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public MainViewModel ViewModel { get; } = new();

        public MainPage()
        {
            this.InitializeComponent();
            AplicarPermisosPorRol();

            // Si se cierra la ventana principal con el asistente abierto,
            // la ventana flotante también debe cerrarse.
            if (App.MainWindow is not null)
                App.MainWindow.Closed += MainWindow_Closed;

            Unloaded += MainPage_Unloaded;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            CerrarAsistente();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.MainWindow is not null)
                App.MainWindow.Closed -= MainWindow_Closed;

            Unloaded -= MainPage_Unloaded;
            CerrarAsistente();
        }

        private void AplicarPermisosPorRol()
        {
            if (SessionService.EsAdmin) return;

            foreach (var item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    var tag = navItem.Tag?.ToString();
                    if (tag is "Products" or "Employees" or "Reports")
                        navItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(SalesPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var tag = (args.InvokedItemContainer as NavigationViewItem)?.Tag?.ToString();

            switch (tag)
            {
                case "Sales":
                    ContentFrame.Navigate(typeof(SalesPage));
                    break;
                case "Products":
                    if (SessionService.EsAdmin) ContentFrame.Navigate(typeof(ProductsPage));
                    break;
                case "Employees":
                    if (SessionService.EsAdmin) ContentFrame.Navigate(typeof(EmployeesPage));
                    break;
                case "Reports":
                    if (SessionService.EsAdmin) ContentFrame.Navigate(typeof(ReportsPage));
                    break;
                case "Asistente":
                    AbrirAsistente();
                    break;
                case "Logout":
                    CerrarAsistente();
                    SessionService.CerrarSesion();
                    Frame.Navigate(typeof(LoginPage));
                    break;
            }
        }

        /// <summary>
        /// ASISTENTE IA
        /// referencia a la ventana flotante del asistente
        /// es null cuando la ventana está cerrada
        /// </summary>
        private AiChatWindow? _ventanaAsistente;

        /// <summary>
        /// Cierra la ventana del asistente si está abierta y libera la referencia.
        /// Se usa al cerrar sesión y al cerrar la ventana principal.
        /// </summary>
        private void CerrarAsistente()
        {
            var asistente = _ventanaAsistente;
            _ventanaAsistente = null;

            if (asistente is null) return;

            try
            {
                asistente.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"No se pudo cerrar el asistente: {ex.Message}");
            }
        }

        /// <summary>
        /// se ejecuta al elegir "Asistente virtual" en el menú lateral.
        /// abre la ventana del asistente o la trae al frente si ya está abierta.
        /// </summary>
        private void AbrirAsistente()
        {
            if (_ventanaAsistente is null)
            {
                _ventanaAsistente = new AiChatWindow();

                _ventanaAsistente.Closed += (s, args) => _ventanaAsistente = null;
            }

            var asistente = _ventanaAsistente;
            if (asistente is null) return;

            // Se hace después de que el menú termine de procesar el clic.
            // Si se activa de inmediato, la ventana principal recupera el
            // foco y el asistente parece que se esconde.
            DispatcherQueue.TryEnqueue(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () =>
                {
                    if (_ventanaAsistente != asistente) return;

                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(asistente);
                    ShowWindow(hwnd, SW_RESTORE);
                    asistente.Activate();
                    SetForegroundWindow(hwnd);
                });
        }
    }
}