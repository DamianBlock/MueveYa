using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public partial class LoginPage : ContentPage
{
    private readonly UsuarioService _usuarioService;

    public LoginPage()
    {
        InitializeComponent();
        _usuarioService = new UsuarioService();
    }

    private async void Ingresar_Clicked(object sender, EventArgs e)
    {
        Usuario? usuario;
        try
        {
            usuario = await _usuarioService.Login(txtEmail.Text, txtPassword.Text);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error de conexión",
                "No se pudo conectar con el servidor. Verificá tu conexión e intentá de nuevo.",
                "Aceptar");
            System.Diagnostics.Debug.WriteLine($"Error de login: {ex}");
            return;
        }

        if (usuario == null)
        {
            await DisplayAlert("Error", "Usuario o contraseña incorrectos", "Aceptar");
            return;
        }

        SesionUsuario.UsuarioId = usuario.UsuarioId;
        SesionUsuario.Nombre = usuario.Nombre;
        SesionUsuario.TipoUsuario = usuario.TipoUsuario;

        Preferences.Set("UsuarioId", usuario.UsuarioId);
        Preferences.Set("Nombre", usuario.Nombre);
        Preferences.Set("Apellido", usuario.Apellido);
        Preferences.Set("Email", usuario.Email);
        Preferences.Set("TipoUsuario", usuario.TipoUsuario);

        var app = Application.Current;
        if (app?.Windows?.Count > 0)
        {
            app.Windows[0].Page = usuario.TipoUsuario == "CLIENTE"
                ? new NavigationPage(new MainPage())
                : new NavigationPage(new HomeConductor());
        }
    }

    private async void Registro_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistroPage(_usuarioService));
    }
}