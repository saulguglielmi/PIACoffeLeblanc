using CafeteriaPOS.Services;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CafeteriaPOS.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public string NombreUsuario => SessionService.UsuarioActual?.Nombre ?? "";
        public string PuestoUsuario => SessionService.UsuarioActual?.Puesto ?? "";

        public Visibility AdminVisibility => SessionService.EsAdmin ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CajeroVisibility => SessionService.EsCajero ? Visibility.Visible : Visibility.Collapsed;
    }
}