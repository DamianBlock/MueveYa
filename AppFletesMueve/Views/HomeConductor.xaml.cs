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
    private HubConnection? _hub;
    private readonly IChatPageFactory _chatPageFactory;

    private int? _conductorId;
    private int? _vehiculoIdPropio;
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
        // Añadir botón de Chat en la barra
        _chatToolbarItem = new ToolbarItem { Text = "Chat", Order = ToolbarItemOrder.Primary };
        _chatToolbarItem.Clicked += ChatItem_Clicked;
        ToolbarItems.Add(_chatToolbarItem);
    }

        private async void ChatItem_Clicked(object? sender, EventArgs e)
        {
            if (_solicitudEnCurso == null && _solicitudPendiente == null)
            {
                await DisplayAlert("Chat", "No hay solicitud activa o pendiente para chatear.", "Aceptar");
                return;
            }

            var solicitudId = _solicitudEnCurso?.SolicitudFleteId ?? _solicitudPendiente?.SolicitudFleteId;
            if (solicitudId == null)
            {
                await DisplayAlert("Chat", "No se pudo determinar la solicitud.", "Aceptar");
                return;
            }

            var group = $"solicitud-{solicitudId}";
            var user = Preferences.Get("Nombre", SesionUsuario.Nombre ?? "Conductor");
            if (_hub == null)
            {
                await DisplayAlert("Chat", "No hay conexión al servidor de mensajes.", "Aceptar");
                return;
            }

            var chatPage = _chatPageFactory.Create(_hub, group, user);
            await Navigation.PushAsync(chatPage);
        }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEstadoAsync();

        // Inicializar SignalR para recibir solicitudes en tiempo real
        try
        {
                if (_hub == null)
                {
                    _hub = new HubConnectionBuilder()
                        .WithUrl(TransporteService.HubUrl)
                        .WithAutomaticReconnect()
                        .Build();

                _hub.On<SolicitudFleteDto>("NuevaSolicitud", solicitud =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            var pin = new Pin
                            {
                                Label = $"${solicitud.Precio:0.00} - {solicitud.DireccionOrigen}",
                                Location = new Location(solicitud.LatitudOrigen, solicitud.LongitudOrigen)
                            };

                            // Almacenar la referencia a la solicitud
                            _pinToSolicitud[pin] = solicitud;

                            mapConductor.Pins.Add(pin);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex);
                        }
                    });
                });

                // Suscribirse también al evento local para pruebas sin servidor
                // Re-join drivers group automatically after reconnect
                _hub.Reconnected += async (string connectionId) =>
                {
                    try
                    {
                        await _hub.SendAsync("JoinGroup", "drivers");
                        // Si ya tengo un viaje en curso, volver a unirme a su grupo para recibir/emitir actualizaciones
                        if (_solicitudEnCurso != null)
                        {
                            try
                            {
                                await _hub.SendAsync("JoinGroup", $"solicitud-{_solicitudEnCurso.SolicitudFleteId}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error re-joining solicitud group: {ex}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error re-joining drivers group: {ex}");
                    }
                };

                // No special action needed on reconnecting, keep handler for completeness
                _hub.Reconnecting += (exception) =>
                {
                    return Task.CompletedTask;
                };

                // Escuchar actualizaciones de conteo de mensajes no leídos via SignalR
                _hub.On<int>("UnreadCountUpdated", count =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_chatToolbarItem != null)
                            _chatToolbarItem.Text = count > 0 ? $"Chat ({count})" : "Chat";
                    });
                });

                TransporteService.SolicitudCreada += OnSolicitudCreadaLocal;
            }

            await _hub.StartAsync();
            try
            {
                await _hub.SendAsync("JoinGroup", "drivers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo unir al grupo drivers: {ex}");
            }
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
            // Polling eliminado: badge actualizado por SignalR (UnreadCountUpdated)
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping hub: {ex}");
        }
    }

    private CancellationTokenSource? _chatBadgeCts;
    private ToolbarItem? _chatToolbarItem;

    private void StartChatBadgePolling()
    {
        try
        {
            if (_chatToolbarItem == null)
            {
                _chatToolbarItem = new ToolbarItem { Text = "Chat", Order = ToolbarItemOrder.Primary };
                _chatToolbarItem.Clicked += ChatItem_Clicked;
                ToolbarItems.Add(_chatToolbarItem);
            }

            _chatBadgeCts?.Cancel();
            _chatBadgeCts = new CancellationTokenSource();
            var token = _chatBadgeCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var solicitudId = _solicitudEnCurso?.SolicitudFleteId ?? _solicitudPendiente?.SolicitudFleteId;
                        if (solicitudId != null && _chatToolbarItem != null)
                        {
                            var user = Preferences.Get("Nombre", SesionUsuario.Nombre ?? "Conductor");
#if DEBUG
                            var apiBase = "http://10.0.2.2:5051/api/";
#else
                            var apiBase = "https://mueveya.onrender.com/api/";
#endif
                            var http = new System.Net.Http.HttpClient();
                            var url = apiBase + $"Chat/unread-count/{solicitudId}?forUser={System.Net.WebUtility.UrlEncode(user)}";
                            var resp = await http.GetAsync(url, token);
                            if (resp.IsSuccessStatusCode)
                            {
                                var json = await resp.Content.ReadAsStringAsync(token);
                                using var doc = System.Text.Json.JsonDocument.Parse(json);
                                var count = doc.RootElement.GetProperty("count").GetInt32();
                                MainThread.BeginInvokeOnMainThread(() => _chatToolbarItem.Text = count > 0 ? $"Chat ({count})" : "Chat");
                            }
                        }
                    }
                    catch { }
                    await Task.Delay(10000, token);
                }
            }, token);
        }
        catch { }
    }

    // Polling eliminado: badge actualizado por SignalR (UnreadCountUpdated)

    private void OnSolicitudCreadaLocal(SolicitudFleteDto solicitud)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Renderizar la solicitud en el mapa (pines + trazado)
                RenderSolicitudOnMap(solicitud);

                // Almacenar referencia por seguridad (puede no usarse actualmente)
                // crear un pin de referencia para el diccionario
                var refPin = new Pin { Label = $"${solicitud.Precio:0.00} - {solicitud.ClienteNombre}", Location = new Location(solicitud.LatitudOrigen, solicitud.LongitudOrigen) };
                _pinToSolicitud[refPin] = solicitud;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        });
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
        // Mostrar la ubicación del cliente en el mapa y centrar
        try
        {
            RenderSolicitudOnMap(s);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renderizando solicitud en mapa: {ex}");
        }
    }

    // MostrarViajeEnCurso está definido más abajo; se usa la implementación completa.

    private async void RenderSolicitudOnMap(SolicitudFleteDto s)
    {
        if (s == null) return;

        mapConductor.Pins.Clear();
        mapConductor.MapElements.Clear();

        var origen = new Location(s.LatitudOrigen, s.LongitudOrigen);
        var destino = new Location(s.LatitudDestino, s.LongitudDestino);

        var pinOrigen = new Pin { Label = $"Origen: {s.DireccionOrigen}", Location = origen };
        var pinDestino = new Pin { Label = $"Destino: {s.DireccionDestino}", Location = destino };

        mapConductor.Pins.Add(pinOrigen);
        mapConductor.Pins.Add(pinDestino);

        // Intentar obtener ruta real via OSRM, con fallback a línea directa
        try
        {
            var puntos = await _directionsService.GetRoutePointsAsync(origen.Latitude, origen.Longitude, destino.Latitude, destino.Longitude);
            var poly = new Polyline
            {
                StrokeColor = Color.FromArgb("#E65100"),
                StrokeWidth = 6
            };

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

            mapConductor.MapElements.Add(poly);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo ruta conductor: {ex}");
            var poly = new Polyline
            {
                StrokeColor = Color.FromArgb("#E65100"),
                StrokeWidth = 6
            };
            poly.Geopath.Add(origen);
            poly.Geopath.Add(destino);
            mapConductor.MapElements.Add(poly);
        }

        // Centrar en el origen con un radio basado en la distancia
        var distanciaKm = TransporteService.HaversineDistanceKm(s.LatitudOrigen, s.LongitudOrigen, s.LatitudDestino, s.LongitudDestino);
        var radioKm = Math.Max(1, distanciaKm / 2.0);
        mapConductor.MoveToRegion(MapSpan.FromCenterAndRadius(origen, Distance.FromKilometers(radioKm)));
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
        try
        {
            // Renderizar la ruta y empezar a enviar ubicación periódicamente
            // Guardar el viaje en curso antes de renderizar para que cualquier handler pueda consultarlo
            _solicitudEnCurso = s;
            RenderSolicitudOnMap(s);
            StartUbicacionUpdates(s.SolicitudFleteId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error iniciando viaje en curso: {ex}");
        }
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

                await Task.Delay(5000, token);
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
                // Detener envío de ubicación antes de finalizar
                StopUbicacionUpdates();
                var resultado = await _transporteService.CompletarSolicitud(_solicitudEnCurso.SolicitudFleteId);
                await DisplayAlertAsync("MUEVE",
                    resultado != null ? "Viaje finalizado correctamente." : "No se pudo finalizar el viaje.",
                    "Aceptar");
            }
            else if (_solicitudPendiente != null && _conductorId.HasValue && _vehiculoIdPropio.HasValue)
            {
                var ok = await _transporteService.AceptarSolicitudAsync(
                    _solicitudPendiente.SolicitudFleteId, _conductorId.Value, _vehiculoIdPropio.Value);

                await DisplayAlertAsync("MUEVE",
                    ok ? "Viaje aceptado correctamente." : "No se pudo aceptar. Puede que otro conductor ya lo haya tomado.",
                    "Aceptar");
                if (ok && _hub != null && _hub.State == HubConnectionState.Connected)
                {
                    try
                    {
                        // Unirse al grupo de la solicitud aceptada para enviar/recibir actualizaciones
                        await _hub.SendAsync("JoinGroup", $"solicitud-{_solicitudPendiente.SolicitudFleteId}");
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
             bool salir = await DisplayAlertAsync("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");
             if (!salir) return;

             Preferences.Clear();
             var app = Application.Current;
             if (app?.Windows?.Count > 0)
                 app.Windows[0].Page = new NavigationPage(new LoginPage());
         }

         private void OnMapPinClicked(object? sender, EventArgs e)
         {
             // Método vacío: el evento PinClicked no existe en MAUI Maps. Si desea manejar taps en pines,
             // suscriba al evento adecuado o gestione el clic en la vista del pin.
         }
    }