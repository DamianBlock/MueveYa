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
    private CancellationTokenSource? _pollingCts;
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
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        DetenerPolling();

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

            if (_hub.State == HubConnectionState.Disconnected)
                await _hub.StartAsync();

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

    // ===================== ESTADO =====================

    private async Task RecargarAsync()
    {
        var solicitud = await _transporteService.ObtenerSolicitudAsync(_solicitudId);
        if (solicitud == null)
            return;

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
            case "EnCurso":
                lblEstado.Text = s.Estado == "EnCurso" ? "Viaje en curso" : "Tu conductor está en camino";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Podés seguir su ubicación en el mapa y escribirle por el chat.";
                lblConductor.Text = s.ConductorNombre ?? "Conductor asignado";
                lblVehiculo.Text = $"{s.VehiculoDescripcion} · {s.Patente}";
                panelConductor.IsVisible = true;
                ConfigurarBotones(chat: true, textoCancelar: "Cancelar viaje", volver: false);
                break;

            case "Completada":
                lblEstado.Text = "Viaje finalizado";
                lblEstado.TextColor = Color.FromArgb("#2E7D32");
                lblDetalle.Text = "Gracias por viajar con MUEVE.";
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

        _chatAbierto = true;

        var group = $"solicitud-{_solicitudId}";
        var user = Preferences.Get("Nombre", SesionUsuario.Nombre ?? "Cliente");
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
            await DisplayAlertAsync("MUEVE", "No se pudo cancelar. Probá de nuevo.", "Aceptar");
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