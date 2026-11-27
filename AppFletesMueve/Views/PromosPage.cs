using System.Globalization;
using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AppFletesMueve.Views;

public class PromosPage : ContentPage
{
    private readonly bool _esConductor;
    private readonly TransporteService _transporteService;
    private readonly VerticalStackLayout _contenido = new() { Spacing = 14, Padding = new Thickness(16) };

    public PromosPage(bool esConductor, TransporteService? transporteService = null)
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
        raiz.Add(SeccionesUi.Cabecera("Promos"), 0, 0);
        raiz.Add(new ScrollView { Content = _contenido }, 0, 1);
        Content = raiz;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _contenido.Clear();
        if (_esConductor)
        {
            _contenido.Add(Tarjeta("Próximamente para conductores",
                "Los bonos e incentivos para conductores se comunicarán desde esta sección.", "#757575"));
            return;
        }

        _contenido.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#E65100"), HorizontalOptions = LayoutOptions.Center });
        var promo = await _transporteService.ObtenerPromocionPrimerViajeAsync(SesionUsuario.UsuarioId);
        _contenido.Clear();

        if (promo is null)
        {
            _contenido.Add(Tarjeta("No se pudo consultar promociones",
                "Verificá tu conexión e intentá nuevamente.", "#C62828"));
            return;
        }

        var titulo = promo.Disponible ? "50% en tu primer viaje" : "Promo de primer viaje";
        var color = promo.Disponible ? "#E65100" : "#757575";
        var detalle = promo.Mensaje;
        var vence = promo.VenceUtc;
        if (promo.Disponible && vence.HasValue)
        {
            detalle += $"\n\nVálida hasta el {vence.Value.ToLocalTime():dd/MM/yyyy}. Tope de descuento: " +
                       promo.TopeDescuento.ToString("C0", CultureInfo.GetCultureInfo("es-AR")) +
                       ". No acumulable con otras promociones.";
        }
        _contenido.Add(Tarjeta(titulo, detalle, color));
        _contenido.Add(new Label
        {
            Text = "La tarifa completa del viaje se mantiene para el conductor; MUEVE asume el valor del descuento.",
            TextColor = Color.FromArgb("#616161"),
            FontSize = 13
        });
    }

    private static Border Tarjeta(string titulo, string detalle, string color) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb(color),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
        Padding = 18,
        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = titulo, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb(color) },
                new Label { Text = detalle, FontSize = 14, TextColor = Color.FromArgb("#424242") }
            }
        }
    };
}
