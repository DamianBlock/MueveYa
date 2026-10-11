using AppFletesMueve.Models;
using AppFletesMueve.Services;


namespace AppFletesMueve.Views
{
    public partial class RegistroPage : ContentPage
    {
        private readonly UsuarioService _usuarioService;

        public RegistroPage(UsuarioService usuarioService)
        {
            InitializeComponent();

            _usuarioService = usuarioService;
        }

        private async void CrearCuenta_Clicked(object? sender, EventArgs e)
        {
            var usuario = new Usuario
            {
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                Dni = txtDni.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Password = txtPassword.Text,
                TipoUsuario = pickerTipoUsuario.SelectedItem?.ToString() ?? ""
            };

            var (registrado, error) = await _usuarioService.RegistrarUsuario(usuario);

            if (registrado)
            {
                await DisplayAlertAsync(
                    "MUEVE",
                    "Usuario registrado correctamente en el API.",
                    "Aceptar");
                txtNombre.Text = "";
                txtApellido.Text = "";
                txtDni.Text = "";
                txtTelefono.Text = "";
                txtEmail.Text = "";
                txtPassword.Text = "";
                pickerTipoUsuario.SelectedItem = null;

                await Navigation.PopAsync();
            }
            else
            {
                var mensaje = !string.IsNullOrWhiteSpace(error) ? error : "No se pudo conectar con el API.";
                await DisplayAlertAsync(
                    "MUEVE",
                    mensaje,
                    "Aceptar");
                lblMensaje.Text = mensaje;
            }
        }
    }
}