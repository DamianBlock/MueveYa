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
    private readonly TransporteService _transporteService;
    private readonly Action<List<ItemCargaRequest>> _onConfirmar;
    private ObservableCollection<CargaItemModel> _items = new();

    public SeleccionarCargaPage(Action<List<ItemCargaRequest>> onConfirmar, TransporteService? transporteService = null)
    {
        _transporteService = transporteService ?? new TransporteService();
        _onConfirmar = onConfirmar;
        InitializeComponent();
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
        }
    }

    private List<ItemCargaRequest> ArmarLista(params (string nombre, int cantidad)[] seleccion)
    {
        var resultado = new List<ItemCargaRequest>();
        foreach (var (nombre, cantidad) in seleccion)
        {
            var item = _items.FirstOrDefault(i => i.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
            if (item != null)
                resultado.Add(new ItemCargaRequest { TipoCargaId = item.TipoCargaId, Cantidad = cantidad });
        }
        return resultado;
    }

    private async void PresetPocasCosas_Tapped(object? sender, TappedEventArgs e)
        => await ConfirmarYVolver(ArmarLista(("Cajas y bultos", 3)));

    private async void PresetMudanzaChica_Tapped(object? sender, TappedEventArgs e)
        => await ConfirmarYVolver(ArmarLista(("Muebles", 3), ("Cajas y bultos", 5)));

    private async void PresetMudanzaGrande_Tapped(object? sender, TappedEventArgs e)
        => await ConfirmarYVolver(ArmarLista(("Muebles", 8), ("Electrodomésticos", 3), ("Cajas y bultos", 10)));

    private async void PresetMateriales_Tapped(object? sender, TappedEventArgs e)
        => await ConfirmarYVolver(ArmarLista(("Materiales de construcción", 1)));

    private void ToggleDetalle_Tapped(object? sender, TappedEventArgs e)
        => panelDetalle.IsVisible = !panelDetalle.IsVisible;

    private void Sumar_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CargaItemModel item)
            item.Cantidad++;
    }

    private void Restar_Clicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CargaItemModel item && item.Cantidad > 0)
            item.Cantidad--;
    }

    private async void ConfirmarDetalle_Clicked(object? sender, EventArgs e)
    {
        var elegidos = _items
            .Where(i => i.Cantidad > 0)
            .Select(i => new ItemCargaRequest { TipoCargaId = i.TipoCargaId, Cantidad = i.Cantidad })
            .ToList();

        if (elegidos.Count == 0)
        {
            await DisplayAlert("MUEVE", "Elegí al menos un ítem, o usá una de las opciones rápidas de arriba.", "Aceptar");
            return;
        }

        await ConfirmarYVolver(elegidos);
    }

    private async Task ConfirmarYVolver(List<ItemCargaRequest> cargas)
    {
        if (cargas.Count == 0)
        {
            await DisplayAlert("MUEVE", "No se pudo preparar la selección. Probá de nuevo.", "Aceptar");
            return;
        }

        _onConfirmar(cargas);
        await Navigation.PopAsync();
    }
}