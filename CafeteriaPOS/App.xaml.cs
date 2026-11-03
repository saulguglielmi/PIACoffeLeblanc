using CafeteriaPOS.Helpers;
using CafeteriaPOS.Services;
using Microsoft.UI.Xaml;

namespace CafeteriaPOS
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            AppPaths.EnsureFolders();
            JsonDatabase.AsegurarBase();

            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}