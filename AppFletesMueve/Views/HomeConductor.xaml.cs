using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public partial class HomeConductor : ContentPage
{
    private readonly TransporteService _transporteService = new();
    private SolicitudFleteDto? _solicitudActual;
    private int? _conductorId;
    private int? _vehiculoId;

    public string Saludo { get; set; }

    public HomeConductor()
    {
        InitializeComponent();
        lblSaludo.Text = $"Hola, {SesionUsuario.Nombre}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarSolicitudPendiente();
    }

    private async Task CargarSolicitudPendiente()
    {
        var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
        if (conductor is null)
        {
            await DisplayAlert("MUEVE", "Todavía no tenés un perfil de conductor cargado.", "OK");
            return;
        }

        _conductorId = conductor.ConductorId;

        var vehiculos = await _transporteService.ObtenerVehiculosDeConductor(conductor.ConductorId);
        _vehiculoId = vehiculos.FirstOrDefault()?.VehiculoId;

        var pendientes = await _transporteService.ObtenerSolicitudesPendientes();
        _solicitudActual = pendientes.FirstOrDefault();
    }

    private async void AceptarViaje_Clicked(object sender, EventArgs e)
    {
        if (_solicitudActual is null || _conductorId is null || _vehiculoId is null)
        {
            await DisplayAlert("MUEVE", "No hay ningún viaje disponible para aceptar.", "Aceptar");
            return;
        }

        var resultado = await _transporteService.AceptarSolicitud(
            _solicitudActual.SolicitudFleteId, _conductorId.Value, _vehiculoId.Value);

        var mensaje = resultado != null
            ? "Viaje aceptado correctamente."
            : "No se pudo aceptar el viaje. Puede que otro conductor ya lo haya tomado.";

        await DisplayAlert("MUEVE", mensaje, "Aceptar");

        _solicitudActual = null;
    }

    private async void CerrarSesion_Tapped(object sender, TappedEventArgs e)
    {
        bool salir = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (!salir) return;

        Preferences.Clear();
        Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage());
    }
}