using System.Globalization;
using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AppFletesMueve.Views;

public class ViajesPage : ContentPage
{
    private readonly bool _esConductor;
    private readonly TransporteService _transporteService;
    private readonly VerticalStackLayout _lista = new() { Spacing = 12, Padding = new Thickness(16) };

    public ViajesPage(bool esConductor, TransporteService? transporteService = null)
    {
        _esConductor = esConductor;
        _transporteService = transporteService ?? new TransporteService();
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F5F5");

        var raiz = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        raiz.Add(SeccionesUi.Cabecera("Mis viajes"), 0, 0);
        raiz.Add(new ScrollView { Content = _lista }, 0, 1);
        Content = raiz;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _lista.Clear();
        _lista.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#E65100"), HorizontalOptions = LayoutOptions.Center });

        List<SolicitudFleteDto> viajes;
        if (_esConductor)
        {
            var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
            viajes = conductor is null
                ? new List<SolicitudFleteDto>()
                : await _transporteService.ObtenerHistorialConductorAsync(conductor.ConductorId);
        }
        else
        {
            viajes = await _transporteService.ObtenerHistorialClienteAsync(SesionUsuario.UsuarioId);
        }

        _lista.Clear();
        if (viajes.Count == 0)
        {
            _lista.Add(new Label
            {
                Text = "Todavía no tenés viajes para mostrar.",
                TextColor = Color.FromArgb("#757575"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            });
            return;
        }

        foreach (var viaje in viajes)
            _lista.Add(CrearTarjeta(viaje, _esConductor));
    }

    private static Border CrearTarjeta(SolicitudFleteDto viaje, bool esConductor)
    {
        var colorEstado = viaje.Estado switch
        {
            "Completada" => "#2E7D32",
            "Cancelada" => "#C62828",
            _ => "#E65100"
        };
        var rol = viaje.ConductorNombre ?? "Esperando conductor";
        var fecha = viaje.FechaSolicitud.ToLocalTime().ToString("dd/MM/yyyy · HH:mm");
        var importe = esConductor || viaje.PrecioCliente <= 0 ? viaje.Precio : viaje.PrecioCliente;
        var precio = importe.ToString("C0", CultureInfo.GetCultureInfo("es-AR"));

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    CrearFila(
                        new Label { Text = fecha, TextColor = Color.FromArgb("#757575"), FontSize = 13 },
                        new Label { Text = viaje.Estado, TextColor = Color.FromArgb(colorEstado), FontAttributes = FontAttributes.Bold }),
                    new Label { Text = viaje.DireccionOrigen, TextColor = Color.FromArgb("#212121"), FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation },
                    new Label { Text = $"Hasta: {viaje.DireccionDestino}", TextColor = Color.FromArgb("#555555"), LineBreakMode = LineBreakMode.TailTruncation },
                    CrearFila(
                        new Label { Text = rol, TextColor = Color.FromArgb("#757575"), FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation },
                        new Label { Text = precio, TextColor = Color.FromArgb("#212121"), FontAttributes = FontAttributes.Bold })
                }
            }
        };
    }

    private static Grid CrearFila(View izquierda, View derecha)
    {
        var fila = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        fila.Add(izquierda, 0, 0);
        fila.Add(derecha, 1, 0);
        return fila;
    }
}
