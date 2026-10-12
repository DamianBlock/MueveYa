using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;

namespace AppFletesMueve.Views;

public partial class ViajeClientePage : ContentPage
{
    private readonly TransporteService _transporteService = new();
    private readonly int _solicitudId;

    private HubConnection? _hub;
    private Pin? _pinConductor;
    private SolicitudFleteDto? _solicitud;
    private CancellationTokenSource? _pollingCts;
    private IDispatcherTimer? _cronometro;
    private TimeSpan _desfaseServidor = TimeSpan.Zero;
    private bool _rutaDibujada;
    private bool _chatAbierto;

    public ViajeClientePage(int solicitudId)
    {
        InitializeComponent();
        _solicitudId = solicitudId;
    }

    // ===================== CICLO DE VIDA =====================

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _chatAbierto = false;

        await RecargarAsync();
        await ConectarHubAsync();
        IniciarPolling();
        IniciarCronometro();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        DetenerPolling();
        DetenerCronometro();

        // Si se abrió el chat encima, se mantiene la conexión porque el chat la usa
        if (_chatAbierto)
            return;

        await CerrarHubAsync();
    }

    // ===================== SIGNALR =====================

    private async Task ConectarHubAsync()
    {
        try
        {
            if (_hub == null)
            {
                _hub = new HubConnectionBuilder()
                    .WithUrl(TransporteService.HubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hub.On<SolicitudFleteDto>("SolicitudAceptada", _ => RecargarEnUi());
                _hub.On<SolicitudFleteDto>("EtapaActualizada", _ => RecargarEnUi());
                _hub.On<SolicitudFleteDto>("SolicitudCompletada", _ => RecargarEnUi());
                _hub.On<SolicitudFleteDto>("SolicitudCancelada", _ => RecargarEnUi());

                _hub.On<int, double, double>("UbicacionConductor", (solicitudId, lat, lon) =>
                {
                    if (solicitudId != _solicitudId) return;
                    MainThread.BeginInvokeOnMainThread(() => MoverConductor(lat, lon));
                });

                _hub.On<int>("UnreadCountUpdated", count =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        btnChat.Text = count > 0 ? $"Chat con el conductor ({count})" : "Chat con el conductor");
                });

                _hub.Reconnected += async (string? _) => await UnirseGrupoAsync();
            }

            // Si volvemos del chat la conexión ya está activa
            if (_hub.State == HubConnectionState.Disconnected)
                await _hub.StartAsync();

            // El chat sale del grupo al cerrarse: volver a unirse
            await UnirseGrupoAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error SignalR (viaje cliente): {ex}");
        }
    }

    private async Task UnirseGrupoAsync()
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hub.SendAsync("JoinGroup", $"solicitud-{_solicitudId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"No se pudo unir al grupo de la solicitud: {ex}");
        }
    }

    private async Task CerrarHubAsync()
    {
        try
        {
            if (_hub != null)
            {
                var hub = _hub;
                _hub = null;

                if (hub.State == HubConnectionState.Connected)
                    await hub.SendAsync("LeaveGroup", $"solicitud-{_solicitudId}");

                await hub.StopAsync();
                await hub.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cerrando el hub (viaje cliente): {ex}");
        }
    }

    private void RecargarEnUi()
    {
        MainThread.BeginInvokeOnMainThread(async () => await RecargarAsync());
    }

    // ===================== RESPALDO: CONSULTA PERIÓDICA =====================

    private void IniciarPolling()
    {
        DetenerPolling();
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(8000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                RecargarEnUi();
            }
        }, token);
    }

    private void DetenerPolling()
    {
        try
        {
            _pollingCts?.Cancel();
            _pollingCts = null;
        }
        catch { }
    }

    // ===================== CONTADOR DE ESPERA =====================

    private void IniciarCronometro()
    {
        DetenerCronometro();
        _cronometro = Dispatcher.CreateTimer();
        _cronometro.Interval = TimeSpan.FromSeconds(1);
        _cronometro.Tick += (_, _) => ActualizarContador();
        _cronometro.Start();
    }

    private void DetenerCronometro()
    {
        if (_cronometro == null)
            return;

        _cronometro.Stop();
        _cronometro = null;
    }

    private void ActualizarContador()
    {
        var s = _solicitud;

        DateTime? desde = s?.Estado switch
        {
            "EnOrigen" => s.LlegadaOrigenUtc,
            "EnDestino" => s.LlegadaDestinoUtc,
            _ => null
        };

        if (s == null || desde == null)
        {
            lblContador.IsVisible = false;
            return;
        }

        var minutos = s.Estado == "EnOrigen" ? s.MinutosEsperaCarga : s.MinutosEsperaDescarga;
        var etiqueta = s.Estado == "EnOrigen" ? "Tiempo de carga" : "Tiempo de descarga";

        var ahoraServidor = DateTime.UtcNow + _desfaseServidor;
        var restante = TimeSpan.FromMinutes(minutos) - (ahoraServidor - desde.Value);

        lblContador.IsVisible = true;

        if (restante >= TimeSpan.Zero)
        {
            lblContador.Text = $"{etiqueta} incluido: {(int)restante.TotalMinutes:00}:{restante.Seconds:00}";
            lblContador.TextColor = Color.FromArgb("#2E7D32");
        }
        else
        {
            var extra = -restante;
            lblContador.Text = $"{etiqueta}: tiempo extra +{(int)extra.TotalMinutes:00}:{extra.Seconds:00}";
            lblContador.TextColor = Color.FromArgb("#C62828");
        }
    }

    // ===================== ESTADO =====================

    private async Task RecargarAsync()
    {
        var solicitud = await _transporteService.ObtenerSolicitudAsync(_solicitudId);
        if (solicitud == null)
            return;

        _solicitud = solicitud;

        // Diferencia entre el reloj del servidor y el del teléfono
        _desfaseServidor = solicitud.AhoraUtc == default ? TimeSpan.Zero : solicitud.AhoraUtc - DateTime.UtcNow;

        AplicarEstado(solicitud);
    }

    private void AplicarEstado(SolicitudFleteDto s)
    {
        lblOrigen.Text = s.DireccionOrigen;
        lblDestino.Text = s.DireccionDestino;
        lblPrecio.Text = $"$ {s.Precio:N0}";

        DibujarRuta(s);

        switch (s.Estado)
        {
            case "Pendiente":
                lblEstado.Text = "Buscando conductor...";
                lblEstado.TextColor = Color.FromArgb("#E65100");
                lblDetalle.Text = "Estamos avisando a los conductores disponibles. Podés cancelar cuando quieras.";
                panelConductor.IsVisible = false;
                ConfigurarBotones(chat: false, textoCancelar: "Cancelar solicitud", volver: false);
                break;

            case "Aceptada":
                lblEstado.Text = "Tu conductor está en camino";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Podés seguir su ubicación en el mapa y escribirle por el chat.";
                MostrarConductor(s);
                ConfigurarBotones(chat: true, textoCancelar: "Cancelar viaje", volver: false);
                break;

            case "EnOrigen":
                lblEstado.Text = "Tu conductor llegó";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Es el momento de cargar. El viaje empieza cuando el conductor lo inicia.";
                MostrarConductor(s);
                ConfigurarBotones(chat: true, textoCancelar: "Cancelar viaje", volver: false);
                break;

            case "EnCurso":
                lblEstado.Text = "Viaje en curso";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Van camino al destino. Podés seguir el recorrido en el mapa.";
                MostrarConductor(s);
                ConfigurarBotones(chat: true, textoCancelar: null, volver: false);
                break;

            case "EnDestino":
                lblEstado.Text = "Llegaron al destino";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Es el momento de descargar. El viaje termina cuando el conductor lo finaliza.";
                MostrarConductor(s);
                ConfigurarBotones(chat: true, textoCancelar: null, volver: false);
                break;

            case "Completada":
                lblEstado.Text = "Viaje finalizado";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Gracias por viajar con MUEVE.";
                MostrarConductor(s);
                ConfigurarBotones(chat: false, textoCancelar: null, volver: true);
                DetenerPolling();
                break;

            case "Cancelada":
                lblEstado.Text = "Viaje cancelado";
                lblEstado.TextColor = Color.FromArgb("#C62828");
                lblDetalle.Text = "Podés pedir un nuevo flete cuando quieras.";
                panelConductor.IsVisible = false;
                ConfigurarBotones(chat: false, textoCancelar: null, volver: true);
                DetenerPolling();
                break;

            default:
                lblEstado.Text = s.Estado;
                break;
        }

        ActualizarContador();
    }

    private void MostrarConductor(SolicitudFleteDto s)
    {
        lblConductor.Text = s.ConductorNombre ?? "Conductor asignado";
        lblVehiculo.Text = $"{s.VehiculoDescripcion} · {s.Patente}";
        panelConductor.IsVisible = true;
    }

    private void ConfigurarBotones(bool chat, string? textoCancelar, bool volver)
    {
        btnChat.IsVisible = chat;
        btnCancelar.IsVisible = textoCancelar != null;
        if (textoCancelar != null)
            btnCancelar.Text = textoCancelar;
        btnVolver.IsVisible = volver;
    }

    // ===================== MAPA =====================

    private void DibujarRuta(SolicitudFleteDto s)
    {
        if (_rutaDibujada)
            return;

        _rutaDibujada = true;

        var origen = new Location(s.LatitudOrigen, s.LongitudOrigen);
        var destino = new Location(s.LatitudDestino, s.LongitudDestino);

        mapViaje.Pins.Add(new Pin { Label = "Origen: " + s.DireccionOrigen, Location = origen });
        mapViaje.Pins.Add(new Pin { Label = "Destino: " + s.DireccionDestino, Location = destino });

        var linea = new Polyline { StrokeColor = Colors.Orange, StrokeWidth = 8 };
        linea.Geopath.Add(origen);
        linea.Geopath.Add(destino);
        mapViaje.MapElements.Add(linea);

        var distanciaKm = TransporteService.HaversineDistanceKm(
            origen.Latitude, origen.Longitude, destino.Latitude, destino.Longitude);

        var centro = new Location(
            (origen.Latitude + destino.Latitude) / 2,
            (origen.Longitude + destino.Longitude) / 2);

        mapViaje.MoveToRegion(MapSpan.FromCenterAndRadius(
            centro, Distance.FromKilometers(Math.Max(distanciaKm * 0.75, 0.5))));
    }

    private void MoverConductor(double lat, double lon)
    {
        var ubicacion = new Location(lat, lon);

        if (_pinConductor != null)
            mapViaje.Pins.Remove(_pinConductor);

        _pinConductor = new Pin { Label = "Conductor", Location = ubicacion };
        mapViaje.Pins.Add(_pinConductor);
    }

    // ===================== ACCIONES =====================

    private async void Chat_Clicked(object? sender, EventArgs e)
    {
        if (_hub == null || _hub.State != HubConnectionState.Connected)
        {
            await DisplayAlertAsync("Chat", "No hay conexión al servidor de mensajes.", "Aceptar");
            return;
        }

        var group = $"solicitud-{_solicitudId}";
        var user = Preferences.Get("Nombre", SesionUsuario.Nombre ?? "Cliente");

        _chatAbierto = true;
        await Navigation.PushAsync(new ChatPage(_hub, group, user));
    }

    private async void Cancelar_Clicked(object? sender, EventArgs e)
    {
        bool confirmar = await DisplayAlertAsync("MUEVE", "¿Querés cancelar este viaje?", "Sí, cancelar", "No");
        if (!confirmar)
            return;

        var ok = await _transporteService.CancelarSolicitudAsync(_solicitudId);
        if (!ok)
        {
            await DisplayAlertAsync("MUEVE", "No se pudo cancelar. Si el viaje ya está en marcha, no se puede cancelar.", "Aceptar");
            await RecargarAsync();
            return;
        }

        await RecargarAsync();
    }

    private async void VolverInicio_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void Volver_Tapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}