using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CafeteriaPOS.Helpers
{
    /// <summary>
    /// coloca el icono de la app en la barra de título y en la barra de tareas.
    /// </summary>
    public static class WindowIcon
    {
        private const uint WM_SETICON = 0x0080;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x0010;

        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int ICON_SMALL2 = 2;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(
            IntPtr hinst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static void Aplicar(Window ventana, string archivo = "AppIcon.ico")
        {
            try
            {
                var ruta = Path.Combine(AppContext.BaseDirectory, "Assets", archivo);
                if (!File.Exists(ruta)) return;

                // icono de la ventana (barra de tareas y alt+tab)
                ventana.AppWindow.SetIcon(ruta);

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(ventana);

                // la barra de título usa el icono pequeño; se asignan los dos
                // tamaños para que también se vea nítido con escalado alto.
                var pequeno = LoadImage(IntPtr.Zero, ruta, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
                var grande = LoadImage(IntPtr.Zero, ruta, IMAGE_ICON, 64, 64, LR_LOADFROMFILE);

                if (pequeno != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, pequeno);
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL2, pequeno);
                }

                if (grande != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, grande);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
