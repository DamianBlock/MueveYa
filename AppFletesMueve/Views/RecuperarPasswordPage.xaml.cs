using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public partial class RecuperarPasswordPage : ContentPage
{
    private readonly UsuarioService _usuarioService;

    public RecuperarPasswordPage(UsuarioService usuarioService)
    {
        InitializeComponent();
        _usuarioService = usuarioService;
    }

    private async void EnviarCodigo_Clicked(object? sender, EventArgs e)
    {
        var resultado = await _usuarioService.SolicitarRecuperacionPasswordAsync(txtEmail.Text);
        await DisplayAlertAsync(resultado.Success ? "Revisá tu correo" : "No se pudo enviar", resultado.Message, "Aceptar");
    }

    private async void RestablecerPassword_Clicked(object? sender, EventArgs e)
    {
        if (txtPasswordNueva.Text != txtConfirmarPassword.Text)
        {
            await DisplayAlertAsync("Contraseñas distintas", "La nueva contraseña y su confirmación deben coincidir.", "Aceptar");
            return;
        }

        var resultado = await _usuarioService.RestablecerPasswordAsync(
            txtEmail.Text,
            txtCodigo.Text,
            txtPasswordNueva.Text);

        await DisplayAlertAsync(resultado.Success ? "Contraseña actualizada" : "No se pudo actualizar", resultado.Message, "Aceptar");
        if (resultado.Success)
            await Navigation.PopAsync();
    }

    private void MostrarPasswordNueva_Clicked(object? sender, EventArgs e)
    {
        txtPasswordNueva.IsPassword = !txtPasswordNueva.IsPassword;
        btnMostrarPasswordNueva.Text = txtPasswordNueva.IsPassword ? "👁" : "🙈";
        SemanticProperties.SetHint(btnMostrarPasswordNueva, txtPasswordNueva.IsPassword
            ? "Mostrar contraseña nueva"
            : "Ocultar contraseña nueva");
    }

    private void MostrarConfirmarPassword_Clicked(object? sender, EventArgs e)
    {
        txtConfirmarPassword.IsPassword = !txtConfirmarPassword.IsPassword;
        btnMostrarConfirmarPassword.Text = txtConfirmarPassword.IsPassword ? "👁" : "🙈";
        SemanticProperties.SetHint(btnMostrarConfirmarPassword, txtConfirmarPassword.IsPassword
            ? "Mostrar contraseña"
            : "Ocultar contraseña");
    }
}
