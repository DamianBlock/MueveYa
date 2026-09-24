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

        private async void AbrirRegistro_Clicked(object sender, EventArgs e)
        {
            var usuarioService =
                Handler.MauiContext.Services.GetRequiredService<UsuarioService>();

            await Navigation.PushAsync(
                new RegistroPage(usuarioService));
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

            Application.Current.Windows[0].Page =
                new NavigationPage(
                    new LoginPage());
        }
    }
}