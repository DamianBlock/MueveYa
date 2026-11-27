using System.Globalization;
using AppFletesMueve.Models;
using AppFletesMueve.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AppFletesMueve.Views;

public class BilleteraPage : ContentPage
{
    private readonly bool _esConductor;
    private readonly TransporteService _transporteService;
    private readonly VerticalStackLayout _contenido = new() { Spacing = 14, Padding = new Thickness(16) };
    private static readonly CultureInfo MonedaArgentina = CultureInfo.GetCultureInfo("es-AR");

    public BilleteraPage(bool esConductor, TransporteService? transporteService = null)
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
        raiz.Add(SeccionesUi.Cabecera(_esConductor ? "Billetera" : "Pagos"), 0, 0);
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
        _contenido.Add(new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#E65100"), HorizontalOptions = LayoutOptions.Center });

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

        var completados = viajes.Where(v => v.Estado == "Completada").ToList();
        var total = completados.Sum(v => ImporteParaResumen(v));
        _contenido.Clear();

        _contenido.Add(CrearResumen(total, completados.Count));
        _contenido.Add(new Label
        {
            Text = _esConductor
                ? "Ganancias brutas estimadas según viajes completados. Los cobros y retiros todavía no se procesan desde MUEVE."
                : "Resumen informativo de los importes de tus viajes completados. MUEVE todavía no procesa tarjetas ni transferencias desde la app.",
            TextColor = Color.FromArgb("#616161"),
            FontSize = 13
        });

        if (completados.Count == 0)
        {
            _contenido.Add(new Label
            {
                Text = "Todavía no hay viajes completados para resumir.",
                TextColor = Color.FromArgb("#757575"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 24, 0, 0)
            });
            return;
        }

        _contenido.Add(new Label
        {
            Text = "Últimos viajes completados",
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#212121"),
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var viaje in completados.Take(10))
            _contenido.Add(CrearMovimiento(viaje));
    }

    private Border CrearResumen(decimal total, int cantidad) => new()
    {
        BackgroundColor = Color.FromArgb("#E65100"),
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
        Padding = 18,
        Content = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = _esConductor ? "GANANCIAS BRUTAS ESTIMADAS" : "IMPORTE DE VIAJES COMPLETADOS", TextColor = Colors.White, FontSize = 12, FontAttributes = FontAttributes.Bold },
                new Label { Text = total.ToString("C0", MonedaArgentina), TextColor = Colors.White, FontSize = 30, FontAttributes = FontAttributes.Bold },
                new Label { Text = $"{cantidad} viaje{(cantidad == 1 ? string.Empty : "s")} completado{(cantidad == 1 ? string.Empty : "s")}", TextColor = Color.FromArgb("#FFF3E0"), FontSize = 13 }
            }
        }
    };

    private Border CrearMovimiento(SolicitudFleteDto viaje)
    {
        var fila = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }
        };
        fila.Add(new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label { Text = viaje.DireccionDestino, TextColor = Color.FromArgb("#212121"), FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = viaje.FechaSolicitud.ToLocalTime().ToString("dd/MM/yyyy"), TextColor = Color.FromArgb("#757575"), FontSize = 13 }
            }
        }, 0, 0);
        fila.Add(new Label
        {
            Text = ImporteParaResumen(viaje).ToString("C0", MonedaArgentina),
            TextColor = Color.FromArgb("#2E7D32"),
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E0E0E0"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = 14,
            Content = fila
        };
    }

    private decimal ImporteParaResumen(SolicitudFleteDto viaje) =>
        _esConductor || viaje.PrecioCliente <= 0 ? viaje.Precio : viaje.PrecioCliente;
}
