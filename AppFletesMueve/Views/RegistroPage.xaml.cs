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
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtDni.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                pickerTipoUsuario.SelectedItem is null)
            {
                const string mensajeCampos = "Completá todos los campos para crear tu cuenta.";
                await DisplayAlertAsync("MUEVE", mensajeCampos, "Aceptar");
                lblMensaje.Text = mensajeCampos;
                return;
            }

            var usuario = new Usuario
            {
                Nombre = txtNombre.Text?.Trim() ?? string.Empty,
                Apellido = txtApellido.Text?.Trim() ?? string.Empty,
                Dni = txtDni.Text?.Trim() ?? string.Empty,
                Telefono = txtTelefono.Text?.Trim() ?? string.Empty,
                Email = txtEmail.Text?.Trim() ?? string.Empty,
                Password = txtPassword.Text ?? string.Empty,
                TipoUsuario = pickerTipoUsuario.SelectedItem?.ToString() ?? ""
            };

            var (registrado, error) = await _usuarioService.RegistrarUsuario(usuario);

            if (registrado)
            {
                await DisplayAlertAsync(
                    "MUEVE",
                    "Tu cuenta se creó correctamente. Ya podés iniciar sesión.",
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

        private void MostrarPassword_Clicked(object? sender, EventArgs e)
        {
            txtPassword.IsPassword = !txtPassword.IsPassword;
            btnMostrarPassword.Text = txtPassword.IsPassword ? "👁" : "🙈";
            SemanticProperties.SetHint(btnMostrarPassword, txtPassword.IsPassword
                ? "Mostrar contraseña"
                : "Ocultar contraseña");
        }
    }
}
