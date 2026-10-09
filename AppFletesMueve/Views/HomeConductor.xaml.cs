using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.ApplicationModel;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace AppFletesMueve.Views;

public partial class HomeConductor : ContentPage
{
    private readonly TransporteService _transporteService;
    private readonly IDirectionsService? _directionsService;
    private readonly IChatPageFactory _chatPageFactory;
    private HubConnection? _hub;
    private ToolbarItem? _chatToolbarItem;

    private int? _conductorId;
    private bool _enServicio;
    private bool _cambiandoServicio;
    private VehiculoDisponibleDto? _vehiculoActivo;
    private List<VehiculoDisponibleDto> _misVehiculos = new();
    private Dictionary<Pin, SolicitudFleteDto> _pinToSolicitud = new();

    private SolicitudFleteDto? _solicitudPendiente;
    private SolicitudFleteDto? _solicitudEnCurso;
    private CancellationTokenSource? _ubicacionCts;

    public string Saludo { get; set; } = string.Empty;

    // Constructor compatible: permite inyección de servicios via DI o uso de instancias por defecto
    public HomeConductor(TransporteService? transporteService = null, IDirectionsService? directionsService = null, IChatPageFactory? chatPageFactory = null)
    {
        _transporteService = transporteService ?? new TransporteService();
        _directionsService = directionsService;
        _chatPageFactory = chatPageFactory ?? new ChatPageFactory();

        InitializeComponent();
        lblSaludo.Text = $"Hola, {SesionUsuario.Nombre}";

        // Botón de Chat en la barra
        _chatToolbarItem = new ToolbarItem { Text = "Chat", Order = ToolbarItemOrder.Primary };
        _chatToolbarItem.Clicked += ChatItem_Clicked;
        ToolbarItems.Add(_chatToolbarItem);
    }

    // ===================== CHAT =====================

    private async void ChatItem_Clicked(object? sender, EventArgs e)
    {
        var solicitudId = _solicitudEnCurso?.SolicitudFleteId ?? _solicitudPendiente?.SolicitudFleteId;
        if (solicitudId == null)
        {
            await DisplayAlertAsync("Chat", "No hay solicitud activa o pendiente para chatear.", "Aceptar");
            return;
        }

        if (_hub == null)
        {
            await DisplayAlertAsync("Chat", "No hay conexión al servidor de mensajes.", "Aceptar");
            return;
        }

        var group = $"solicitud-{solicitudId}";
        var user = Preferences.Get("Nombre", SesionUsuario.Nombre ?? "Conductor");

        var chatPage = _chatPageFactory.Create(_hub, group, user);
        await Navigation.PushAsync(chatPage);
    }

    // ===================== CICLO DE VIDA / SIGNALR =====================

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEstadoAsync();

        try
        {
            if (_hub == null)
            {
                _hub = new HubConnectionBuilder()
                    .WithUrl(TransporteService.HubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // Solo llegan solicitudes del tipo de vehículo con el que estoy en servicio
                _hub.On<SolicitudFleteDto>("NuevaSolicitud", solicitud =>
                {
                    MainThread.BeginInvokeOnMainThread(() => OnNuevaSolicitud(solicitud));
                });

                // Escuchar actualizaciones de conteo de mensajes no leídos via SignalR
                _hub.On<int>("UnreadCountUpdated", count =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_chatToolbarItem != null)
                            _chatToolbarItem.Text = count > 0 ? $"Chat ({count})" : "Chat";
                    });
                });

                // Volver a unirse a los grupos si la conexión se restablece
                _hub.Reconnected += async (string? connectionId) =>
                {
                    await UnirseGruposAsync();
                };

                // Evento local para pruebas sin servidor
                TransporteService.SolicitudCreada += OnSolicitudCreadaLocal;
            }

            await _hub.StartAsync();
            await UnirseGruposAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error SignalR: {ex}");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            if (_hub != null)
            {
                await _hub.StopAsync();
                await _hub.DisposeAsync();
                _hub = null;
            }
            TransporteService.SolicitudCreada -= OnSolicitudCreadaLocal;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping hub: {ex}");
        }
    }

    private static string GrupoServicio(VehiculoDisponibleDto vehiculo) => $"drivers-{vehiculo.TipoVehiculo}";

    private async Task UnirseGruposAsync()
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected)
            return;

        try
        {
            if (_enServicio && _vehiculoActivo != null)
                await _hub.SendAsync("JoinGroup", GrupoServicio(_vehiculoActivo));

            if (_solicitudEnCurso != null)
                await _hub.SendAsync("JoinGroup", $"solicitud-{_solicitudEnCurso.SolicitudFleteId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error uniéndose a los grupos: {ex}");
        }
    }

    private async Task SalirDeGrupoServicioAsync()
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected || _vehiculoActivo == null)
            return;

        try
        {
            await _hub.SendAsync("LeaveGroup", GrupoServicio(_vehiculoActivo));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saliendo del grupo de servicio: {ex}");
        }
    }

    private void OnNuevaSolicitud(SolicitudFleteDto solicitud)
    {
        try
        {
            if (!_enServicio || _solicitudEnCurso != null)
                return;

            // Si no hay otra solicitud en pantalla, mostrar esta
            if (_solicitudPendiente == null)
            {
                _solicitudPendiente = solicitud;
                MostrarViajePendiente(solicitud);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void OnSolicitudCreadaLocal(SolicitudFleteDto solicitud)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                RenderSolicitudOnMap(solicitud);

                var refPin = new Pin
                {
                    Label = $"${solicitud.Precio:N0} - {solicitud.ClienteNombre}",
                    Location = new Location(solicitud.LatitudOrigen, solicitud.LongitudOrigen)
                };
                _pinToSolicitud[refPin] = solicitud;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        });
    }

    // ===================== ESTADO =====================

    private async Task CargarEstadoAsync()
    {
        if (_conductorId is null)
        {
            var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
            if (conductor is null)
            {
                MostrarSinViajes("Todavía no tenés un perfil de conductor cargado.");
                btnCompletarPerfil.IsVisible = true;
                ActualizarUiServicio();
                return;
            }

            _conductorId = conductor.ConductorId;
            _enServicio = conductor.Disponible;
        }

        _misVehiculos = await _transporteService.ObtenerVehiculosDeConductor(_conductorId.Value);

        // Prioridad 1: ¿ya tengo un viaje en curso?
        _solicitudEnCurso = await _transporteService.ObtenerViajeActivoDeConductor(_conductorId.Value);
        if (_solicitudEnCurso != null)
        {
            _vehiculoActivo = _misVehiculos.FirstOrDefault(v => v.VehiculoId == _solicitudEnCurso.VehiculoId);
            ActualizarUiServicio();
            MostrarViajeEnCurso(_solicitudEnCurso);
            return;
        }

        // El vehículo en servicio es el único marcado como disponible
        var vehiculo = _enServicio ? _misVehiculos.FirstOrDefault(v => v.Disponible) : null;
        _vehiculoActivo = vehiculo;
        if (vehiculo == null)
            _enServicio = false;

        ActualizarUiServicio();

        if (!_enServicio || vehiculo == null)
        {
            _solicitudPendiente = null;
            MostrarSinViajes("Estás fuera de servicio. Tocá INICIAR para recibir viajes.");
            return;
        }

        // Prioridad 2: ¿hay algún viaje pendiente de mi tipo de vehículo?
        var pendientes = await _transporteService.ObtenerSolicitudesPendientes(vehiculo.TipoVehiculo);
        _solicitudPendiente = pendientes.FirstOrDefault();

        if (_solicitudPendiente != null)
            MostrarViajePendiente(_solicitudPendiente);
        else
            MostrarSinViajes("Buscando viajes disponibles...");
    }

    private void ActualizarUiServicio()
    {
        if (_enServicio && _vehiculoActivo != null)
        {
            lblEstadoServicio.Text = $"En servicio · {_vehiculoActivo.Marca} {_vehiculoActivo.Modelo} ({_vehiculoActivo.Patente})";
            lblEstadoServicio.TextColor = Color.FromArgb("#2E7D32");
            btnServicio.Text = "DETENER";
            btnServicio.BackgroundColor = Color.FromArgb("#C62828");
        }
        else
        {
            lblEstadoServicio.Text = "Fuera de servicio";
            lblEstadoServicio.TextColor = Color.FromArgb("#757575");
            btnServicio.Text = "INICIAR";
            btnServicio.BackgroundColor = Color.FromArgb("#2E7D32");
        }

        btnServicio.IsVisible = _conductorId != null;
    }

    private void MostrarSinViajes(string mensaje)
    {
        contenedorViaje.IsVisible = false;
        lblSinViajes.IsVisible = true;
        lblSinViajes.Text = mensaje;
    }

    private string TextoVehiculoActivo()
    {
        return _vehiculoActivo != null
            ? $"Vehículo: {_vehiculoActivo.Marca} {_vehiculoActivo.Modelo} ({_vehiculoActivo.Patente})"
            : "Vehículo: el tuyo";
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
        lblVehiculo.Text = TextoVehiculoActivo();
        lblPrecio.Text = $"$ {s.Precio:N0}";

        btnAccion.Text = "ACEPTAR VIAJE";

        try
        {
            RenderSolicitudOnMap(s);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renderizando solicitud en mapa: {ex}");
        }
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
        lblPrecio.Text = $"$ {s.Precio:N0}";

        btnAccion.Text = "FINALIZAR VIAJE";
        try
        {
            _solicitudEnCurso = s;
            RenderSolicitudOnMap(s);
            StartUbicacionUpdates(s.SolicitudFleteId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error iniciando viaje en curso: {ex}");
        }
    }

    // ===================== INICIAR / DETENER SERVICIO =====================

    private async void Servicio_Clicked(object? sender, EventArgs e)
    {
        if (_conductorId is not { } conductorId || _cambiandoServicio)
            return;

        _cambiandoServicio = true;
        btnServicio.IsEnabled = false;
        try
        {
            if (_enServicio)
                await DetenerServicioAsync(conductorId);
            else
                await IniciarServicioAsync(conductorId);
        }
        finally
        {
            btnServicio.IsEnabled = true;
            _cambiandoServicio = false;
        }
    }

    private async Task IniciarServicioAsync(int conductorId)
    {
        if (_misVehiculos.Count == 0)
            _misVehiculos = await _transporteService.ObtenerVehiculosDeConductor(conductorId);

        if (_misVehiculos.Count == 0)
        {
            await DisplayAlertAsync("MUEVE", "Todavía no tenés vehículos cargados. Completá tu perfil para poder trabajar.", "Aceptar");
            btnCompletarPerfil.IsVisible = true;
            return;
        }

        var elegido = await ElegirVehiculoAsync();
        if (elegido == null)
            return;

        double? lat = null;
        double? lon = null;
        try
        {
            var loc = await Geolocation.Default.GetLastKnownLocationAsync();
            if (loc != null)
            {
                lat = loc.Latitude;
                lon = loc.Longitude;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"No se pudo obtener la ubicación: {ex}");
        }

        var (ok, mensaje) = await _transporteService.ActivarConductorAsync(conductorId, elegido.VehiculoId, lat, lon);
        if (!ok)
        {
            await DisplayAlertAsync("MUEVE", mensaje ?? "No se pudo iniciar el servicio.", "Aceptar");
            return;
        }

        _enServicio = true;
        _vehiculoActivo = elegido;
        ActualizarUiServicio();
        await UnirseGruposAsync();
        await CargarEstadoAsync();
    }

    private async Task DetenerServicioAsync(int conductorId)
    {
        if (_solicitudEnCurso != null)
        {
            await DisplayAlertAsync("MUEVE", "Tenés un viaje en curso. Finalizalo antes de detener las solicitudes.", "Aceptar");
            return;
        }

        var (ok, mensaje) = await _transporteService.DesactivarConductorAsync(conductorId);
        if (!ok)
        {
            await DisplayAlertAsync("MUEVE", mensaje ?? "No se pudo detener el servicio.", "Aceptar");
            return;
        }

        await SalirDeGrupoServicioAsync();

        _enServicio = false;
        _vehiculoActivo = null;
        _solicitudPendiente = null;
        mapConductor.Pins.Clear();
        mapConductor.MapElements.Clear();

        ActualizarUiServicio();
        MostrarSinViajes("Estás fuera de servicio. Tocá INICIAR para recibir viajes.");
    }

    private async Task<VehiculoDisponibleDto?> ElegirVehiculoAsync()
    {
        if (_misVehiculos.Count == 1)
            return _misVehiculos[0];

        var opciones = _misVehiculos
            .Select(v => $"{v.Marca} {v.Modelo} · {v.Patente} ({NombreTipo(v.TipoVehiculo)})")
            .ToArray();

        var elegida = await DisplayActionSheetAsync("¿Con qué vehículo vas a trabajar?", "Cancelar", null, opciones);
        if (string.IsNullOrEmpty(elegida) || elegida == "Cancelar")
            return null;

        var indice = Array.IndexOf(opciones, elegida);
        return indice >= 0 ? _misVehiculos[indice] : null;
    }

    private static string NombreTipo(string tipo) => tipo switch
    {
        "CamionChico" => "Camión chico",
        "CamionMediano" => "Camión mediano",
        "CamionGrande" => "Camión grande",
        "Furgon" => "Furgón",
        _ => tipo
    };

    // ===================== MAPA =====================

    private async void RenderSolicitudOnMap(SolicitudFleteDto s)
    {
        if (s == null) return;

        mapConductor.Pins.Clear();
        mapConductor.MapElements.Clear();

        var origen = new Location(s.LatitudOrigen, s.LongitudOrigen);
        var destino = new Location(s.LatitudDestino, s.LongitudDestino);

        mapConductor.Pins.Add(new Pin { Label = $"Origen: {s.DireccionOrigen}", Location = origen });
        mapConductor.Pins.Add(new Pin { Label = $"Destino: {s.DireccionDestino}", Location = destino });

        var poly = new Polyline
        {
            StrokeColor = Color.FromArgb("#E65100"),
            StrokeWidth = 6
        };

        // Intentar obtener ruta real, con fallback a línea directa
        try
        {
            var puntos = _directionsService != null
                ? await _directionsService.GetRoutePointsAsync(origen.Latitude, origen.Longitude, destino.Latitude, destino.Longitude)
                : null;

            if (puntos != null && puntos.Count > 0)
            {
                foreach (var p in puntos)
                    poly.Geopath.Add(p);
            }
            else
            {
                poly.Geopath.Add(origen);
                poly.Geopath.Add(destino);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo ruta conductor: {ex}");
            poly.Geopath.Clear();
            poly.Geopath.Add(origen);
            poly.Geopath.Add(destino);
        }

        mapConductor.MapElements.Add(poly);

        var distanciaKm = TransporteService.HaversineDistanceKm(s.LatitudOrigen, s.LongitudOrigen, s.LatitudDestino, s.LongitudDestino);
        var radioKm = Math.Max(1, distanciaKm / 2.0);
        mapConductor.MoveToRegion(MapSpan.FromCenterAndRadius(origen, Distance.FromKilometers(radioKm)));
    }

    private void StartUbicacionUpdates(int solicitudId)
    {
        _ubicacionCts?.Cancel();
        _ubicacionCts = new CancellationTokenSource();
        var token = _ubicacionCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var loc = await Geolocation.Default.GetLocationAsync();
                    if (loc != null && _hub != null && _hub.State == HubConnectionState.Connected)
                    {
                        await _hub.SendAsync("ActualizarUbicacion", solicitudId, loc.Latitude, loc.Longitude);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error enviando ubicación: {ex}");
                }

                try
                {
                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopUbicacionUpdates()
    {
        try
        {
            _ubicacionCts?.Cancel();
            _ubicacionCts = null;
        }
        catch { }
    }

    // ===================== ACCIONES =====================

    private async void CompletarPerfil_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new CompletarPerfilConductorPage());
    }

    private async void AceptarViaje_Clicked(object? sender, EventArgs e)
    {
        try
        {
            if (_solicitudEnCurso != null)
            {
                StopUbicacionUpdates();
                var resultado = await _transporteService.CompletarSolicitud(_solicitudEnCurso.SolicitudFleteId);
                await DisplayAlertAsync("MUEVE",
                    resultado != null ? "Viaje finalizado correctamente." : "No se pudo finalizar el viaje.",
                    "Aceptar");
            }
            else if (_solicitudPendiente != null && _conductorId.HasValue && _vehiculoActivo != null)
            {
                var solicitudId = _solicitudPendiente.SolicitudFleteId;

                var ok = await _transporteService.AceptarSolicitudAsync(
                    solicitudId, _conductorId.Value, _vehiculoActivo.VehiculoId);

                await DisplayAlertAsync("MUEVE",
                    ok ? "Viaje aceptado correctamente." : "No se pudo aceptar. Puede que otro conductor ya lo haya tomado.",
                    "Aceptar");

                if (ok && _hub != null && _hub.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await _hub.SendAsync("JoinGroup", $"solicitud-{solicitudId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"No se pudo unir al grupo de la solicitud al aceptar: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("MUEVE", "No se pudo conectar con el servidor.", "Aceptar");
            System.Diagnostics.Debug.WriteLine($"Error en acción de viaje: {ex}");
        }

        _solicitudPendiente = null;
        _solicitudEnCurso = null;
        await CargarEstadoAsync();
    }

    private async void CerrarSesion_Tapped(object? sender, TappedEventArgs e)
    {
        if (_solicitudEnCurso != null)
        {
            await DisplayAlertAsync("MUEVE", "Tenés un viaje en curso. Finalizalo antes de cerrar sesión.", "Aceptar");
            return;
        }

        bool salir = await DisplayAlertAsync("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (!salir) return;

        // Dejar de recibir solicitudes antes de salir
        if (_enServicio && _conductorId is { } conductorId)
        {
            await SalirDeGrupoServicioAsync();
            await _transporteService.DesactivarConductorAsync(conductorId);
        }

        Preferences.Clear();
        var app = Application.Current;
        if (app?.Windows?.Count > 0)
            app.Windows[0].Page = new NavigationPage(new LoginPage());
    }

    private void OnMapPinClicked(object? sender, EventArgs e)
    {
        // Método vacío: el evento PinClicked no existe en MAUI Maps.
    }
}