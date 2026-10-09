using AppFletesMueve.Services;
using AppFletesMueve.ViewModels;
using AppFletesMueve.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Graphics;

namespace AppFletesMueve.Views
{
    public partial class HomeCliente : ContentPage
    {
        private ToolbarItem? _chatToolbarItem;

        private readonly IDirectionsService _directionsService;
        private readonly TransporteService _transporteService;
        private readonly PlacesService _placesService;
        private Location? _ultimoDestino;
        private Location? _ultimoOrigen;
        private List<ItemCargaRequest> _cargaSeleccionada = new();
        private CancellationTokenSource? _debounceCtsOrigen;
        private CancellationTokenSource? _debounceCtsDestino;
        private bool _eligiendoOrigenEnMapa;
        private bool _eligiendoDestinoEnMapa;
        private string? _origenDescripcionSeleccionada;
        private string? _destinoDescripcionSeleccionada;
        private HubConnection? _hubCliente;
        private readonly IChatPageFactory _chatPageFactory;
        private int? _solicitudActivaId;
        private Pin? _pinConductor;

        public HomeCliente(IDirectionsService directionsService, TransporteService transporteService, PlacesService placesService, IChatPageFactory chatPageFactory)
        {
            _directionsService = directionsService ?? throw new ArgumentNullException(nameof(directionsService));
            _transporteService = transporteService ?? throw new ArgumentNullException(nameof(transporteService));
            _placesService = placesService ?? throw new ArgumentNullException(nameof(placesService));
            _chatPageFactory = chatPageFactory ?? throw new ArgumentNullException(nameof(chatPageFactory));

            InitializeComponent();
            BindingContext = new MainViewModel();
            string nombre = Preferences.Get("Nombre", "Cliente");

            lblBienvenida.Text = $"Hola, {nombre}";

            // Añadir botón de Chat en la barra de la página
            _chatToolbarItem = new ToolbarItem
            {
                Text = "Chat",
                Order = ToolbarItemOrder.Primary
            };
            _chatToolbarItem.Clicked += ChatItem_Clicked;
            ToolbarItems.Add(_chatToolbarItem);
        }

        // Inicializa la conexión SignalR y registra handlers de forma centralizada
        private async Task InitializeHubAsync()
        {
            if (_hubCliente != null) return;

            _hubCliente = new HubConnectionBuilder()
                .WithUrl(TransporteService.HubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubCliente.On<SolicitudFleteDto>("SolicitudAceptada", async solicitud =>
            {
                if (solicitud.ClienteId != SesionUsuario.UsuarioId) return;
                await MainThread.InvokeOnMainThreadAsync(async () => await ShowSolicitudRouteOnMap(solicitud));
            });

            try
            {
                await _hubCliente.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hub start failed: {ex}");
            }
        }

        // Muestra en el mapa la solicitud aceptada: pins, ruta (o fallback) y notificación al usuario
        private async Task ShowSolicitudRouteOnMap(SolicitudFleteDto solicitud)
        {
            _solicitudActivaId = solicitud.SolicitudFleteId;
            try { await DisplayAlert("MUEVE", "Tu viaje fue aceptado. El conductor está en camino.", "Aceptar"); } catch { }

            mapCliente.Pins.Clear();
            mapCliente.MapElements.Clear();

            var origen = new Location(solicitud.LatitudOrigen, solicitud.LongitudOrigen);
            var destino = new Location(solicitud.LatitudDestino, solicitud.LongitudDestino);
            mapCliente.Pins.Add(new Pin { Label = "Origen: " + solicitud.DireccionOrigen, Location = origen });
            mapCliente.Pins.Add(new Pin { Label = "Destino: " + solicitud.DireccionDestino, Location = destino });

            try
            {
                var puntos = await _directionsService.GetRoutePointsAsync(solicitud.LatitudOrigen, solicitud.LongitudOrigen, solicitud.LatitudDestino, solicitud.LongitudDestino);
                var linea = new Polyline { StrokeColor = Colors.Orange, StrokeWidth = 8 };
                if (puntos != null && puntos.Count > 0)
                {
                    foreach (var p in puntos) linea.Geopath.Add(p);
                }
                else
                {
                    linea.Geopath.Add(origen);
                    linea.Geopath.Add(destino);
                }

                mapCliente.MapElements.Add(linea);
                mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(origen, Distance.FromKilometers(2)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando ruta en mapa: {ex}");
                try { await DisplayAlert("Ruta", "No se pudo calcular la ruta; se mostrará la ubicación sin ruta.", "Aceptar"); } catch { }
                var fallback = new Polyline { StrokeColor = Colors.Gray, StrokeWidth = 4 };
                fallback.Geopath.Add(origen);
                fallback.Geopath.Add(destino);
                mapCliente.MapElements.Add(fallback);
                mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(origen, Distance.FromKilometers(2)));
            }
        }

        private async void ChatItem_Clicked(object? sender, EventArgs e)
        {
            if (_solicitudActivaId == null)
            {
                await DisplayAlert("Chat", "No hay una solicitud activa para chatear.", "Aceptar");
                return;
            }

            if (_hubCliente == null)
            {
                await DisplayAlert("Chat", "No hay conexión al servidor de mensajes.", "Aceptar");
                return;
            }

            var group = $"solicitud-{_solicitudActivaId}";
            var user = Preferences.Get("Nombre", "Cliente");
            var chatPage = _chatPageFactory.Create(_hubCliente, group, user);
            await Navigation.PushAsync(chatPage);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                try
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    var location = await Geolocation.Default.GetLocationAsync(request);
                    if (location != null)
                    {
                        Preferences.Set("UltimaLat", location.Latitude);
                        Preferences.Set("UltimaLon", location.Longitude);
                        mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(location.Latitude, location.Longitude), Distance.FromKilometers(5)));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo obtener ubicación: {ex}");
                }
            }

            // Inicializar conexión SignalR para recibir actualizaciones del conductor
            try
            {
                await InitializeHubAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SignalR cliente: {ex}");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (_hubCliente != null)
                {
                    // dejar el grupo de la solicitud activa antes de desconectar
                    try
                    {
                        if (_solicitudActivaId != null && _hubCliente.State == HubConnectionState.Connected)
                        {
                            await _hubCliente.SendAsync("LeaveGroup", $"solicitud-{_solicitudActivaId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al salir del grupo: {ex}");
                    }

                    await _hubCliente.StopAsync();
                    await _hubCliente.DisposeAsync();
                    _hubCliente = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping hub cliente: {ex}");
            }
        }

        // Badge de chat gestionado por SignalR (UnreadCountUpdated)

        private void Vehiculos_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (BindingContext is ViewModels.MainViewModel vm)
            {
                vm.VehiculoSeleccionado = e.CurrentSelection?.FirstOrDefault() as Models.VehiculoModel;
            }
        }

        private async void AbrirRegistro_Clicked(object? sender, EventArgs e)
        {
            if (Handler?.MauiContext is not { } mauiContext)
                return;

            var usuarioService = mauiContext.Services.GetRequiredService<UsuarioService>();

            await Navigation.PushAsync(new RegistroPage(usuarioService));
        }
        private async void VerMapa_Clicked(object? sender, EventArgs e)
        {
            await DisplayAlertAsync(
                "MUEVE",
                "Aquí se abrirá el mapa interactivo.",
                "Aceptar");
        }
        private async void BuscarDestino_Clicked(object? sender, EventArgs e)
        {
            var texto = txtDestino.Text?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                await DisplayAlertAsync("MUEVE", "Ingrese un destino.", "Aceptar");
                return;
            }

            listaSugerencias.IsVisible = false;

            try
            {
                var locations = await Geocoding.Default.GetLocationsAsync(texto);
                var loc = locations?.FirstOrDefault();
                if (loc == null)
                {
                    await DisplayAlertAsync("MUEVE", "No se encontró la ubicación.", "Aceptar");
                    return;
                }

                await ActualizarDestinoAsync(new Location(loc.Latitude, loc.Longitude), texto);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("MUEVE", "Error buscando destino.", "Aceptar");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void BuscarOrigen_Clicked(object? sender, EventArgs e)
        {
            var texto = txtOrigen.Text?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                await DisplayAlertAsync("MUEVE", "Ingrese un origen.", "Aceptar");
                return;
            }

            listaSugerenciasOrigen.IsVisible = false;

            try
            {
                var locations = await Geocoding.Default.GetLocationsAsync(texto);
                var loc = locations?.FirstOrDefault();
                if (loc == null)
                {
                    await DisplayAlertAsync("MUEVE", "No se encontró la ubicación.", "Aceptar");
                    return;
                }

                _ultimoOrigen = new Location(loc.Latitude, loc.Longitude);
                lblOrigenResumen.Text = texto;
                ActualizarTrayecto();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("MUEVE", "Error buscando origen.", "Aceptar");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void CerrarSesion_Tapped(
            object? sender,
            TappedEventArgs e)
        {
            bool salir = await DisplayAlertAsync(
                "Cerrar sesión",
                "¿Deseas cerrar sesión?",
                "Sí",
                "No");

            if (!salir)
                return;

            if (Application.Current is { } app && app.Windows.Count > 0)
            {
                app.Windows[0].Page = new NavigationPage(new LoginPage());
            }
        }
        private void AbrirMenu_Tapped(object? sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = true;
            panelMenu.IsVisible = true;
        }

        private void CerrarMenu_Tapped(object? sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = false;
            panelMenu.IsVisible = false;
        }

        private async void Viajes_Tapped(object? sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlertAsync("MUEVE", "Sección de viajes en construcción.", "Aceptar");
        }

        private async void Promos_Tapped(object? sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlertAsync("MUEVE", "Sección de promos en construcción.", "Aceptar");
        }

        private async Task ActualizarDestinoAsync(Location destino, string etiqueta)
        {
            _ultimoDestino = destino;
            lblDestinoResumen.Text = etiqueta;
            ActualizarTrayecto();

            try
            {
                var origenLat = _ultimoOrigen?.Latitude ?? Preferences.Get("UltimaLat", 0.0);
                var origenLon = _ultimoOrigen?.Longitude ?? Preferences.Get("UltimaLon", 0.0);

                var opciones = await _transporteService.ObtenerOpcionesVehiculoAsync(origenLat, origenLon, destino.Latitude, destino.Longitude);

                // Garantizar que la modificación de la colección se ejecute en el Hilo Principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (BindingContext is MainViewModel vm)
                    {
                        vm.Vehiculos.Clear();
                        foreach (var o in opciones)
                        {
                            vm.Vehiculos.Add(new Models.VehiculoModel
                            {
                                Id = o.VehiculoId,
                                Nombre = o.Nombre,
                                Imagen = o.Imagen ?? "pickup_truck.png",
                                Precio = Convert.ToDecimal(o.Precio),
                                Info = "Opción"
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando opciones de vehículo: {ex}");
                await DisplayAlertAsync("MUEVE", "No se pudieron obtener opciones de vehículo.", "Aceptar");
            }
        }

        private async void TxtOrigen_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (e.NewTextValue == _origenDescripcionSeleccionada)
                return;

            _debounceCtsOrigen?.Cancel();
            _debounceCtsOrigen = new CancellationTokenSource();
            var token = _debounceCtsOrigen.Token;
            var texto = e.NewTextValue;

            try { await Task.Delay(400, token); }
            catch (TaskCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            var sugerencias = await _placesService.BuscarSugerenciasAsync(texto ?? string.Empty);
            if (token.IsCancellationRequested) return;

            listaSugerenciasOrigen.ItemsSource = sugerencias;
            listaSugerenciasOrigen.IsVisible = sugerencias.Count > 0;
        }

        private async void SugerenciaOrigen_Tapped(object? sender, TappedEventArgs e)
        {

            if (sender is not Grid grid || grid.BindingContext is not PlaceSuggestion sugerencia)
                return;

            _debounceCtsOrigen?.Cancel();
            listaSugerenciasOrigen.IsVisible = false;
            _origenDescripcionSeleccionada = sugerencia.Description;
            txtOrigen.Text = sugerencia.Description;

            var detalle = await _placesService.ObtenerDetalleAsync(sugerencia.PlaceId);
            if (detalle is null)
            {
                await DisplayAlertAsync("MUEVE", "No se pudo obtener la ubicación seleccionada.", "Aceptar");
                return;
            }

            _ultimoOrigen = new Location(detalle.Latitud, detalle.Longitud);
            lblOrigenResumen.Text = sugerencia.Description;
            ActualizarTrayecto();
        }

        private async void ElegirCarga_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SeleccionarCargaPage(cargas =>
            {
                _cargaSeleccionada = cargas;
            }));
        }
        private async void TxtDestino_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (e.NewTextValue == _destinoDescripcionSeleccionada)
                return;

            _debounceCtsDestino?.Cancel();
            _debounceCtsDestino = new CancellationTokenSource();
            var token = _debounceCtsDestino.Token;
            var texto = e.NewTextValue;

            try { await Task.Delay(400, token); }
            catch (TaskCanceledException) { return; }

            if (token.IsCancellationRequested) return;

            var sugerencias = await _placesService.BuscarSugerenciasAsync(texto ?? string.Empty);
            if (token.IsCancellationRequested) return;

            listaSugerencias.ItemsSource = sugerencias;
            listaSugerencias.IsVisible = sugerencias.Count > 0;
        }

        private async void SugerenciaDestino_Tapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Grid grid || grid.BindingContext is not PlaceSuggestion sugerencia)
                return;

            _debounceCtsDestino?.Cancel();
            listaSugerencias.IsVisible = false;
            _destinoDescripcionSeleccionada = sugerencia.Description;
            txtDestino.Text = sugerencia.Description;

            var detalle = await _placesService.ObtenerDetalleAsync(sugerencia.PlaceId);
            if (detalle is null)
            {
                await DisplayAlertAsync("MUEVE", "No se pudo obtener la ubicación seleccionada.", "Aceptar");
                return;
            }

            await ActualizarDestinoAsync(new Location(detalle.Latitud, detalle.Longitud), sugerencia.Description);
        }
        private async void Solicitar_Clicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.VehiculoModel vm)
            {
                if (_ultimoDestino == null)
                {
                    await DisplayAlertAsync("MUEVE", "Primero buscá y seleccioná un destino.", "Aceptar");
                    return;
                }

                if (_cargaSeleccionada.Count == 0)
                {
                    await DisplayAlertAsync("MUEVE", "Primero indicá qué vas a trasladar.", "Aceptar");
                    return;
                }
                bool confirmar = await DisplayAlertAsync("Confirmar", $"Solicitar {vm.Nombre} por ${vm.Precio:0.00}?", "Sí", "No");
                if (!confirmar) return;

                var origenLat = _ultimoOrigen?.Latitude ?? Preferences.Get("UltimaLat", 0.0);
                var origenLon = _ultimoOrigen?.Longitude ?? Preferences.Get("UltimaLon", 0.0);

                var request = new CrearSolicitudFleteRequest
                {
                    ClienteId = SesionUsuario.UsuarioId,
                    DireccionOrigen = !string.IsNullOrWhiteSpace(lblOrigenResumen.Text) ? lblOrigenResumen.Text : "Mi ubicación",
                    LatitudOrigen = origenLat,
                    LongitudOrigen = origenLon,
                    DireccionDestino = !string.IsNullOrWhiteSpace(lblDestinoResumen.Text) ? lblDestinoResumen.Text : string.Empty,
                    LatitudDestino = _ultimoDestino.Latitude,
                    LongitudDestino = _ultimoDestino.Longitude,
                    DistanciaKm = TransporteService.HaversineDistanceKm(origenLat, origenLon, _ultimoDestino.Latitude, _ultimoDestino.Longitude),
                    Cargas = _cargaSeleccionada
                };

                try
                {
                    var resultado = await _transporteService.CrearSolicitud(request);
                    if (resultado != null)
                    {
                        _solicitudActivaId = resultado.SolicitudFleteId;
                        await DisplayAlertAsync("MUEVE", $"Solicitud creada. Precio estimado: ${resultado.Precio:0.00}", "Aceptar");
                        // Unirse al grupo de la solicitud para recibir actualizaciones del conductor
                        if (_hubCliente != null && _hubCliente.State == HubConnectionState.Connected)
                        {
                            try
                            {
                                await _hubCliente.SendAsync("JoinGroup", $"solicitud-{_solicitudActivaId}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"No se pudo unir al grupo de la solicitud: {ex}");
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlertAsync("MUEVE", "No se pudo crear la solicitud.", "Aceptar");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("MUEVE", "Error al crear la solicitud.", "Aceptar");
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
        private void ElegirOrigenEnMapa_Clicked(object? sender, EventArgs e)
        {
            _eligiendoOrigenEnMapa = true;
            _eligiendoDestinoEnMapa = false;
            pinCentral.IsVisible = true;
            btnConfirmarUbicacionMapa.IsVisible = true;
        }

        private void ElegirDestinoEnMapa_Clicked(object? sender, EventArgs e)
        {
            _eligiendoDestinoEnMapa = true;
            _eligiendoOrigenEnMapa = false;
            pinCentral.IsVisible = true;
            btnConfirmarUbicacionMapa.IsVisible = true;
        }

        private async void ConfirmarUbicacionMapa_Clicked(object? sender, EventArgs e)
        {
            var centro = mapCliente.VisibleRegion?.Center;            
            if (centro is null)
            {
                await DisplayAlertAsync("MUEVE", "Mové el mapa un poco e intentá de nuevo.", "Aceptar");
                return;
            }

            string etiqueta = $"Lat {centro.Latitude:0.0000}, Lon {centro.Longitude:0.0000}";
            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(centro);
                var lugar = placemarks?.FirstOrDefault();
                if (lugar != null)
                {
                    etiqueta = $"{lugar.Thoroughfare} {lugar.SubThoroughfare}, {lugar.Locality}".Trim(' ', ',');
                    if (string.IsNullOrWhiteSpace(etiqueta)) etiqueta = $"Lat {centro.Latitude:0.0000}, Lon {centro.Longitude:0.0000}";
                }
            }
            catch { /* si falla la geocodificación inversa, usamos las coordenadas igual */ }

            if (_eligiendoOrigenEnMapa)
            {
                _ultimoOrigen = centro;
                lblOrigenResumen.Text = etiqueta;
                _origenDescripcionSeleccionada = etiqueta;
                txtOrigen.Text = etiqueta;
                ActualizarTrayecto();
            }
            else if (_eligiendoDestinoEnMapa)
            {
                _destinoDescripcionSeleccionada = etiqueta;
                txtDestino.Text = etiqueta;
                await ActualizarDestinoAsync(centro, etiqueta);
            }

            pinCentral.IsVisible = false;
            btnConfirmarUbicacionMapa.IsVisible = false;
            _eligiendoOrigenEnMapa = false;
            _eligiendoDestinoEnMapa = false;
        }
        private void ActualizarTrayecto()
        {
            var origen = _ultimoOrigen;
            if (origen == null)
            {
                var lat = Preferences.Get("UltimaLat", 0.0);
                var lon = Preferences.Get("UltimaLon", 0.0);
                if (lat != 0 || lon != 0)
                {
                    origen = new Location(lat, lon);
                    if (string.IsNullOrWhiteSpace(lblOrigenResumen.Text))
                        lblOrigenResumen.Text = "Mi ubicación actual";
                }
            }

            mapCliente.Pins.Clear();
            mapCliente.MapElements.Clear();

            if (origen != null)
                mapCliente.Pins.Add(new Pin { Label = "Origen: " + lblOrigenResumen.Text, Location = origen });

            if (_ultimoDestino != null)
                mapCliente.Pins.Add(new Pin { Label = "Destino: " + lblDestinoResumen.Text, Location = _ultimoDestino });

            // Si todavía falta uno de los dos puntos, solo centramos en el que haya
            if (origen == null || _ultimoDestino == null)
            {
                var unico = _ultimoDestino ?? origen;
                if (unico != null)
                    mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(unico, Distance.FromKilometers(5)));
                return;
            }

            var linea = new Polyline { StrokeColor = Colors.Orange, StrokeWidth = 8 };
            linea.Geopath.Add(origen);
            linea.Geopath.Add(_ultimoDestino);
            mapCliente.MapElements.Add(linea);

            var distanciaKm = TransporteService.HaversineDistanceKm(
                origen.Latitude, origen.Longitude,
                _ultimoDestino.Latitude, _ultimoDestino.Longitude);

            var centro = new Location(
                (origen.Latitude + _ultimoDestino.Latitude) / 2,
                (origen.Longitude + _ultimoDestino.Longitude) / 2);

            var radioKm = Math.Max(distanciaKm * 0.75, 0.5);
            mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(centro, Distance.FromKilometers(radioKm)));
        }
    }
}