using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using System;

namespace AppFletesMueve.Views;

public partial class LoginPage : ContentPage
{
    private readonly UsuarioService _usuarioService;

    public LoginPage()
    {
        InitializeComponent();
        _usuarioService = new UsuarioService();
    }

    private async void Ingresar_Clicked(object? sender, EventArgs e)
    {
        ResultadoLogin resultado;
        try
        {
            resultado = await _usuarioService.Login(txtEmail.Text, txtPassword.Text);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error de conexión",
                "No se pudo conectar con el servidor. Verificá tu conexión e intentá de nuevo.",
                "Aceptar");
            System.Diagnostics.Debug.WriteLine($"Error de login: {ex}");
            return;
        }

        if (resultado.RequiereConfirmacionSesion)
        {
            var continuar = await DisplayAlertAsync(
                "Sesión activa",
                "Este usuario ya tiene una sesión iniciada en otro dispositivo. Si continuás, se cerrará aquella sesión.",
                "Continuar",
                "Cancelar");
            if (!continuar)
                return;

            try
            {
                resultado = await _usuarioService.Login(txtEmail.Text, txtPassword.Text, confirmarReemplazoSesion: true);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error de conexión", "No se pudo confirmar el inicio de sesión. Intentá de nuevo.", "Aceptar");
                System.Diagnostics.Debug.WriteLine($"Error confirmando login: {ex}");
                return;
            }
        }

        var usuario = resultado.Usuario;
        if (usuario == null)
        {
            await DisplayAlertAsync("Error", "No se pudo iniciar sesión. Verificá tus datos e intentá de nuevo.", "Aceptar");
            return;
        }

        SesionUsuario.UsuarioId = usuario.UsuarioId;
        SesionUsuario.Nombre = usuario.Nombre;
        SesionUsuario.TipoUsuario = usuario.TipoUsuario;
        SesionUsuario.TokenSesion = usuario.TokenSesion;
        Preferences.Set("TokenSesion", usuario.TokenSesion ?? string.Empty);
        Preferences.Set("UsuarioId", usuario.UsuarioId);
        Preferences.Set("Nombre", usuario.Nombre);
        Preferences.Set("Apellido", usuario.Apellido);
        Preferences.Set("Email", usuario.Email);
        Preferences.Set("TipoUsuario", usuario.TipoUsuario);

        var app = Application.Current;
        if (app?.Windows?.Count > 0)
        {
            // Resolver páginas desde el contenedor DI si está disponible
            var services = Application.Current?.Handler?.MauiContext?.Services;
            Page targetPage;
            if (usuario.TipoUsuario == "CLIENTE")
            {
                var home = services?.GetService<HomeCliente>()
                           ?? new HomeCliente(
                                DirectionsService.Instance,
                                services?.GetService<TransporteService>() ?? new TransporteService(),
                                services?.GetService<PlacesService>() ?? new PlacesService(),
                                services?.GetService<IChatPageFactory>() ?? new ChatPageFactory());
                targetPage = new MenuClientePage(home);
            }
            else
            {
                var conductor = services?.GetService<HomeConductor>() ?? new HomeConductor();
                targetPage = new MenuConductorPage(conductor);
            }
            app.Windows[0].Page = targetPage;
        }
    }

    private async void Registro_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistroPage(_usuarioService));
    }
}
