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
        var usuario = await _usuarioService.Login(
            txtEmail.Text,
            txtPassword.Text);

        if (usuario == null)
        {
            await DisplayAlert(
                "Error",
                "Usuario o contraseña incorrectos",
                "Aceptar");

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
            if (usuario.TipoUsuario == "CLIENTE")
            {
                app.Windows[0].Page = new NavigationPage(new MainPage());
            }
            else
            {
                app.Windows[0].Page = new NavigationPage(new HomeConductor());
            }
        }
        else
        {
            // Fallback: mantiene compatibilidad si no hay ventanas (comportamiento heredado)
            if (usuario.TipoUsuario == "CLIENTE")
            {
                app.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                app.MainPage = new NavigationPage(new HomeConductor());
            }
        }
    }

    private async void Registro_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistroPage(_usuarioService));
    }
}