using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public class CargaItemModel : INotifyPropertyChanged
{
    public int TipoCargaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    private int _cantidad;
    public int Cantidad
    {
        get => _cantidad;
        set { _cantidad = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class SeleccionarCargaPage : ContentPage
{
    // Cantidades por defecto de cada opción rápida
    // (los nombres tienen que coincidir con los de la tabla TiposCarga)
    private static readonly Dictionary<string, (string nombre, int cantidad)[]> Presets = new()
    {
        ["PocasCosas"] = new[] { ("Cajas y bultos", 3) },
        ["MudanzaChica"] = new[] { ("Muebles", 3), ("Cajas y bultos", 5) },
        ["MudanzaGrande"] = new[] { ("Muebles", 8), ("Electrodomésticos", 3), ("Cajas y bultos", 10) },
        ["Materiales"] = new[] { ("Materiales de construcción", 1) }
    };

    private readonly TransporteService _transporteService = new();
    private readonly Action<List<ItemCargaRequest>> _onConfirmar;
    private readonly List<ItemCargaRequest> _cargaInicial;
    private ObservableCollection<CargaItemModel> _items = new();

    public SeleccionarCargaPage(Action<List<ItemCargaRequest>> onConfirmar, List<ItemCargaRequest>? cargaActual = null)
    {
        InitializeComponent();
        _onConfirmar = onConfirmar;
        _cargaInicial = cargaActual ?? new List<ItemCargaRequest>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_items.Count == 0)
        {
            var tipos = await _transporteService.ObtenerTiposCarga();
            _items = new ObservableCollection<CargaItemModel>(
                tipos.Select(t => new CargaItemModel { TipoCargaId = t.TipoCargaId, Nombre = t.Nombre, Cantidad = 0 }));
            listaTiposCarga.ItemsSource = _items;

            // Si ya había una carga elegida, restaurar las cantidades para poder editarla
            foreach (var previa in _cargaInicial)
            {
                var item = _items.FirstOrDefault(i => i.TipoCargaId == previa.TipoCargaId);
                if (item != null)
                    item.Cantidad = previa.Cantidad;
            }

            panelDetalle.IsVisible = _cargaInicial.Count > 0;
            ActualizarPresetMarcado();
        }
    }

    private void AplicarPreset(string clave)
    {
        if (_items.Count == 0)
            return;

        foreach (var item in _items)
            item.Cantidad = 0;

        foreach (var (nombre, cantidad) in Presets[clave])
        {
            var item = _items.FirstOrDefault(i => i.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            if (item != null)
                item.Cantidad = cantidad;
        }

        panelDetalle.IsVisible = true;
        ActualizarPresetMarcado();
    }

    private bool CoincideConPreset(string clave)
    {
        var esperado = Presets[clave];
        var elegidos = _items.Where(i => i.Cantidad > 0).ToList();

        if (elegidos.Count != esperado.Length)
            return false;

        return esperado.All(p =>
            elegidos.Any(i => i.Nombre.Equals(p.nombre, StringComparison.OrdinalIgnoreCase) && i.Cantidad == p.cantidad));
    }

    private static void MarcarPreset(Border borde, bool marcado)
    {
        borde.Stroke = marcado ? Color.FromArgb("#E65100") : Color.FromArgb("#E0E0E0");
        borde.StrokeThickness = marcado ? 3 : 1;
        borde.BackgroundColor = marcado ? Color.FromArgb("#FFF3E0") : Colors.White;
    }

    private void ActualizarPresetMarcado()
    {
        MarcarPreset(borderPocasCosas, CoincideConPreset("PocasCosas"));
        MarcarPreset(borderMudanzaChica, CoincideConPreset("MudanzaChica"));
        MarcarPreset(borderMudanzaGrande, CoincideConPreset("MudanzaGrande"));
        MarcarPreset(borderMateriales, CoincideConPreset("Materiales"));
    }

    private void PresetPocasCosas_Tapped(object? sender, TappedEventArgs e) => AplicarPreset("PocasCosas");

    private void PresetMudanzaChica_Tapped(object? sender, TappedEventArgs e) => AplicarPreset("MudanzaChica");

    private void PresetMudanzaGrande_Tapped(object? sender, TappedEventArgs e) => AplicarPreset("MudanzaGrande");

    private void PresetMateriales_Tapped(object? sender, TappedEventArgs e) => AplicarPreset("Materiales");

    private void ToggleDetalle_Tapped(object? sender, TappedEventArgs e)
        => panelDetalle.IsVisible = !panelDetalle.IsVisible;

    private void Sumar_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CargaItemModel item)
        {
            item.Cantidad++;
            ActualizarPresetMarcado();
        }
    }

    private void Restar_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CargaItemModel item && item.Cantidad > 0)
        {
            item.Cantidad--;
            ActualizarPresetMarcado();
        }
    }

    private async void ConfirmarDetalle_Clicked(object? sender, EventArgs e)
    {
        var elegidos = _items
            .Where(i => i.Cantidad > 0)
            .Select(i => new ItemCargaRequest { TipoCargaId = i.TipoCargaId, Cantidad = i.Cantidad })
            .ToList();

        if (elegidos.Count == 0)
        {
            await DisplayAlertAsync("MUEVE", "Elegí al menos un ítem, o usá una de las opciones rápidas de arriba.", "Aceptar");
            return;
        }

        await ConfirmarYVolver(elegidos);
    }

    private async Task ConfirmarYVolver(List<ItemCargaRequest> cargas)
    {
        if (cargas.Count == 0)
        {
            await DisplayAlertAsync("MUEVE", "No se pudo preparar la selección. Probá de nuevo.", "Aceptar");
            return;
        }

        _onConfirmar(cargas);
        await Navigation.PopAsync();
    }
}