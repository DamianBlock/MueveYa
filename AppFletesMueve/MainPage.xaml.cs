using AppFletesMueve.Services;
using AppFletesMueve.ViewModels;
using AppFletesMueve.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AppFletesMueve
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
            string nombre =
Preferences.Get("Nombre", "Cliente");

            lblBienvenida.Text = $"Hola, {nombre}";
        }

        private void Vehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BindingContext is ViewModels.MainViewModel vm)
            {
                vm.VehiculoSeleccionado = e.CurrentSelection?.FirstOrDefault() as Models.VehiculoModel;
            }
        }

        private async void AbrirRegistro_Clicked(object sender, EventArgs e)
        {
            if (Handler?.MauiContext is not { } mauiContext)
                return;

            var usuarioService = mauiContext.Services.GetRequiredService<UsuarioService>();

            await Navigation.PushAsync(new RegistroPage(usuarioService));
        }
        private async void VerMapa_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                "MUEVE",
                "Aquí se abrirá el mapa interactivo.",
                "Aceptar");
        }
        private async void CerrarSesion_Tapped(
    object sender,
    TappedEventArgs e)
        {
            bool salir = await DisplayAlert(
                "Cerrar sesión",
                "¿Deseas cerrar sesión?",
                "Sí",
                "No");

            if (!salir)
                return;

            if (Application.Current is { } app && app.Windows.Count > 0)
            {
                app.Windows[0].Page = new NavigationPage(new LoginPage());
            }
        }
        private void AbrirMenu_Tapped(object sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = true;
            panelMenu.IsVisible = true;
        }

        private void CerrarMenu_Tapped(object sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = false;
            panelMenu.IsVisible = false;
        }

        private async void Viajes_Tapped(object sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlert("MUEVE", "Sección de viajes en construcción.", "Aceptar");
        }

        private async void Promos_Tapped(object sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlert("MUEVE", "Sección de promos en construcción.", "Aceptar");
        }
    }
}