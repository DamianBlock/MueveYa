using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public partial class HomeConductor : ContentPage
{
    private readonly TransporteService _transporteService = new();

    private int? _conductorId;
    private int? _vehiculoIdPropio;
    private List<VehiculoDisponibleDto> _misVehiculos = new();

    private SolicitudFleteDto? _solicitudPendiente;
    private SolicitudFleteDto? _solicitudEnCurso;

    public string Saludo { get; set; } = string.Empty;

    public HomeConductor()
    {
        InitializeComponent();
        lblSaludo.Text = $"Hola, {SesionUsuario.Nombre}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEstadoAsync();
    }

    private async Task CargarEstadoAsync()
    {
        if (_conductorId is null)
        {
            var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
            if (conductor is null)
            {
                MostrarSinViajes("Todavía no tenés un perfil de conductor cargado.");
                btnCompletarPerfil.IsVisible = true;
                return;
            }

            _conductorId = conductor.ConductorId;
            _misVehiculos = await _transporteService.ObtenerVehiculosDeConductor(conductor.ConductorId);
            _vehiculoIdPropio = _misVehiculos.FirstOrDefault()?.VehiculoId;
        }

        // Prioridad 1: ¿ya tengo un viaje en curso?
        _solicitudEnCurso = await _transporteService.ObtenerViajeActivoDeConductor(_conductorId.Value);
        if (_solicitudEnCurso != null)
        {
            MostrarViajeEnCurso(_solicitudEnCurso);
            return;
        }

        // Prioridad 2: ¿hay algún viaje pendiente para tomar?
        var pendientes = await _transporteService.ObtenerSolicitudesPendientes();
        _solicitudPendiente = pendientes.FirstOrDefault();

        if (_solicitudPendiente != null)
        {
            MostrarViajePendiente(_solicitudPendiente);
        }
        else
        {
            MostrarSinViajes("Buscando viajes disponibles...");
        }
    }

    private void MostrarSinViajes(string mensaje)
    {
        contenedorViaje.IsVisible = false;
        lblSinViajes.IsVisible = true;
        lblSinViajes.Text = mensaje;
        // btnCompletarPerfil se activa explícitamente solo cuando falta el perfil
    }

    private void MostrarViajePendiente(SolicitudFleteDto s)
    {
        btnCompletarPerfil.IsVisible = false;
        lblSinViajes.IsVisible = false;
        contenedorViaje.IsVisible = true;

        lblTituloViaje.Text = "Solicitud de Viaje";
        lblCliente.Text = $"Cliente: {s.ClienteNombre}";
        lblOrigen.Text = $"Origen: {s.DireccionOrigen}";
        lblDestino.Text = $"Destino: {s.DireccionDestino}";
        lblVehiculo.Text = "Vehículo: el tuyo";
        lblPrecio.Text = $"${s.Precio:0.00} MXN";

        btnAccion.Text = "ACEPTAR VIAJE";
    }

    private void MostrarViajeEnCurso(SolicitudFleteDto s)
    {
        btnCompletarPerfil.IsVisible = false;
        lblSinViajes.IsVisible = false;
        contenedorViaje.IsVisible = true;

        var vehiculo = _misVehiculos.FirstOrDefault(v => v.VehiculoId == s.VehiculoId);

        lblTituloViaje.Text = "Viaje en curso";
        lblCliente.Text = $"Cliente: {s.ClienteNombre}";
        lblOrigen.Text = $"Origen: {s.DireccionOrigen}";
        lblDestino.Text = $"Destino: {s.DireccionDestino}";
        lblVehiculo.Text = vehiculo != null
            ? $"Vehículo: {vehiculo.Marca} {vehiculo.Modelo} ({vehiculo.Patente})"
            : "Vehículo asignado";
        lblPrecio.Text = $"${s.Precio:0.00} MXN";

        btnAccion.Text = "FINALIZAR VIAJE";
    }
    private async void CompletarPerfil_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CompletarPerfilConductorPage());
    }
    private async void AceptarViaje_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_solicitudEnCurso != null)
            {
                // El botón dice "FINALIZAR VIAJE"
                var resultado = await _transporteService.CompletarSolicitud(_solicitudEnCurso.SolicitudFleteId);
                await DisplayAlert("MUEVE",
                    resultado != null ? "Viaje finalizado correctamente." : "No se pudo finalizar el viaje.",
                    "Aceptar");
            }
            else if (_solicitudPendiente != null && _conductorId.HasValue && _vehiculoIdPropio.HasValue)
            {
                // El botón dice "ACEPTAR VIAJE"
                var resultado = await _transporteService.AceptarSolicitud(
                    _solicitudPendiente.SolicitudFleteId, _conductorId.Value, _vehiculoIdPropio.Value);

                await DisplayAlert("MUEVE",
                    resultado != null ? "Viaje aceptado correctamente." : "No se pudo aceptar. Puede que otro conductor ya lo haya tomado.",
                    "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("MUEVE", "No se pudo conectar con el servidor.", "Aceptar");
            System.Diagnostics.Debug.WriteLine($"Error en acción de viaje: {ex}");
        }

        _solicitudPendiente = null;
        _solicitudEnCurso = null;
        await CargarEstadoAsync();
    }

    private async void CerrarSesion_Tapped(object sender, TappedEventArgs e)
    {
        bool salir = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (!salir) return;

        Preferences.Clear();
        var app = Application.Current;
        if (app?.Windows?.Count > 0)
            app.Windows[0].Page = new NavigationPage(new LoginPage());
    }
}