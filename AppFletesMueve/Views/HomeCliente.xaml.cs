using AppFletesMueve.Services;
using AppFletesMueve.ViewModels;
using AppFletesMueve.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Linq;

namespace AppFletesMueve.Views
{
    public partial class HomeCliente : ContentPage
    {
        private readonly TransporteService _transporteService = new();
        private Location? _ultimoDestino;

        public HomeCliente()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
            string nombre = Preferences.Get("Nombre", "Cliente");

            lblBienvenida.Text = $"Hola, {nombre}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Intentar obtener ubicación actual y guardarla en Preferences
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);
                if (location != null)
                {
                    Preferences.Set("UltimaLat", location.Latitude);
                    Preferences.Set("UltimaLon", location.Longitude);
                    // centrar mapa en ubicación actual la primera vez
                    mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(location.Latitude, location.Longitude), Distance.FromKilometers(5)));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo obtener ubicación: {ex}");
            }
        }

        private void Vehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BindingContext is ViewModels.MainViewModel vm)
            {
                vm.VehiculoSeleccionado = e.CurrentSelection?.FirstOrDefault() as Models.VehiculoModel;
            }
        }

        private async void AbrirRegistro_Clicked(object sender, EventArgs e)
        {
            if (Handler?.MauiContext is not { } mauiContext)
                return;

            var usuarioService = mauiContext.Services.GetRequiredService<UsuarioService>();

            await Navigation.PushAsync(new RegistroPage(usuarioService));
        }
        private async void VerMapa_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert(
                "MUEVE",
                "Aquí se abrirá el mapa interactivo.",
                "Aceptar");
        }

        private async void BuscarDestino_Clicked(object sender, EventArgs e)
        {
            var texto = txtDestino.Text?.Trim();
            if (string.IsNullOrEmpty(texto))
            {
                await DisplayAlert("MUEVE", "Ingrese un destino.", "Aceptar");
                return;
            }

            try
            {
                var locations = await Geocoding.Default.GetLocationsAsync(texto);
                var loc = locations?.FirstOrDefault();
                if (loc == null)
                {
                    await DisplayAlert("MUEVE", "No se encontró la ubicación.", "Aceptar");
                    return;
                }

                mapCliente.Pins.Clear();
                _ultimoDestino = new Location(loc.Latitude, loc.Longitude);
                mapCliente.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(loc.Latitude, loc.Longitude), Distance.FromKilometers(5)));

                // Intentar obtener opciones de vehículos y tarifas desde el servicio
                try
                {
                    var origenLat = Preferences.Get("UltimaLat", 0.0);
                    var origenLon = Preferences.Get("UltimaLon", 0.0);

                    var opciones = await _transporteService.ObtenerOpcionesVehiculoAsync(origenLat, origenLon, loc.Latitude, loc.Longitude);
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
                    else
                    {
                        var vmnew = new MainViewModel(_transporteService);
                        foreach (var o in opciones)
                        {
                            vmnew.Vehiculos.Add(new Models.VehiculoModel
                            {
                                Id = o.VehiculoId,
                                Nombre = o.Nombre,
                                Imagen = o.Imagen ?? "pickup_truck.png",
                                Precio = Convert.ToDecimal(o.Precio),
                                Info = "Opción"
                            });
                        }
                        BindingContext = vmnew;
                    }
                }
                catch (Exception)
                {
                    // Servicio no disponible o método no implementado
                    await DisplayAlert("MUEVE", "No se pudieron obtener opciones de vehículo.", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("MUEVE", "Error buscando destino.", "Aceptar");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
        private async void CerrarSesion_Tapped(
    object sender,
    TappedEventArgs e)
        {
            bool salir = await DisplayAlert(
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
        private void AbrirMenu_Tapped(object sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = true;
            panelMenu.IsVisible = true;
        }

        private void CerrarMenu_Tapped(object sender, TappedEventArgs e)
        {
            fondoOscuro.IsVisible = false;
            panelMenu.IsVisible = false;
        }

        private async void Viajes_Tapped(object sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlert("MUEVE", "Sección de viajes en construcción.", "Aceptar");
        }

        private async void Promos_Tapped(object sender, TappedEventArgs e)
        {
            CerrarMenu_Tapped(sender, e);
            await DisplayAlert("MUEVE", "Sección de promos en construcción.", "Aceptar");
        }

        private async void Solicitar_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Models.VehiculoModel vm)
            {
                if (_ultimoDestino == null)
                {
                    await DisplayAlert("MUEVE", "Primero buscá y seleccioná un destino.", "Aceptar");
                    return;
                }

                bool confirmar = await DisplayAlert("Confirmar", $"Solicitar {vm.Nombre} por ${vm.Precio:0.00}?", "Sí", "No");
                if (!confirmar) return;

                var origenLat = Preferences.Get("UltimaLat", 0.0);
                var origenLon = Preferences.Get("UltimaLon", 0.0);

                var request = new CrearSolicitudFleteRequest
                {
                    ClienteId = SesionUsuario.UsuarioId,
                    DireccionOrigen = "Mi ubicación",
                    LatitudOrigen = origenLat,
                    LongitudOrigen = origenLon,
                    DireccionDestino = _ultimoDestino.ToString(),
                    LatitudDestino = _ultimoDestino.Latitude,
                    LongitudDestino = _ultimoDestino.Longitude,
                    DistanciaKm = TransporteService.HaversineDistanceKm(origenLat, origenLon, _ultimoDestino.Latitude, _ultimoDestino.Longitude),
                    Cargas = new List<ItemCargaRequest> { new ItemCargaRequest { TipoCargaId = 1, Cantidad = 1 } }
                };

                try
                {
                    var resultado = await _transporteService.CrearSolicitud(request);
                    if (resultado != null)
                    {
                        await DisplayAlert("MUEVE", $"Solicitud creada. Precio estimado: ${resultado.Precio:0.00}", "Aceptar");
                    }
                    else
                    {
                        await DisplayAlert("MUEVE", "No se pudo crear la solicitud.", "Aceptar");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("MUEVE", "Error al crear la solicitud.", "Aceptar");
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}