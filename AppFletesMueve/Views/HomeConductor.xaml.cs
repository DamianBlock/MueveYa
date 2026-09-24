using AppFletesMueve.Models;

namespace AppFletesMueve.Views;

public partial class HomeConductor : ContentPage
{
    public string Saludo { get; set; }

    public HomeConductor()
    {
        InitializeComponent();

        lblSaludo.Text =
    $"Hola, {SesionUsuario.Nombre}";
    }

    private async void AceptarViaje_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "MUEVE",
            "Viaje aceptado correctamente.",
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

        Preferences.Clear();

        Application.Current.Windows[0].Page =
            new NavigationPage(
                new LoginPage());
    }
}