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

        if (usuario.TipoUsuario == "CLIENTE")
        {
            Application.Current.MainPage =
                new NavigationPage(
                    new MainPage());
        }
        else
        {
            Application.Current.MainPage =
                new NavigationPage(
                    new HomeConductor());
        }
    }

    private async void Registro_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistroPage(_usuarioService));
    }
}