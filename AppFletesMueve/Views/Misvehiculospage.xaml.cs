using System.Globalization;
using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public class VehiculoItem
{
    public int VehiculoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

public partial class MisVehiculosPage : ContentPage
{
    // Nombre para mostrar, valor que entiende la API y capacidades de referencia
    private static readonly (string Nombre, string Valor, double Kg, double M3)[] Tipos =
    {
        ("Utilitario", "Utilitario", 500, 2),
        ("Camioneta / Pick-up", "Camioneta", 1000, 5),
        ("Camión chico", "CamionChico", 2500, 12),
        ("Camión mediano", "CamionMediano", 5000, 25),
        ("Camión grande / Mudanza", "CamionGrande", 10000, 45)
    };

    private readonly TransporteService _transporteService = new();
    private int? _conductorId;

    public MisVehiculosPage()
    {
        InitializeComponent();
        pickerTipo.ItemsSource = Tipos.Select(t => t.Nombre).ToList();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    // ===================== LISTA =====================

    private async Task CargarAsync()
    {
        if (_conductorId is null)
        {
            var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
            if (conductor is null)
            {
                await DisplayAlertAsync("MUEVE", "Todavía no tenés un perfil de conductor.", "Aceptar");
                await Navigation.PopAsync();
                return;
            }

            _conductorId = conductor.ConductorId;
        }

        var vehiculos = await _transporteService.ObtenerVehiculosDeConductor(_conductorId.Value);

        var items = vehiculos.Select(v => new VehiculoItem
        {
            VehiculoId = v.VehiculoId,
            Titulo = $"{v.Marca} {v.Modelo} ({v.Anio})",
            Subtitulo = $"{v.Patente} · {NombreTipo(v.TipoVehiculo)}",
            Detalle = $"Hasta {v.CapacidadKg:N0} kg · {v.VolumenM3:0.#} m³"
        }).ToList();

        BindableLayout.SetItemsSource(listaVehiculos, items);
        lblSinVehiculos.IsVisible = items.Count == 0;
    }

    private static string NombreTipo(string valor)
    {
        foreach (var t in Tipos)
        {
            if (t.Valor == valor)
                return t.Nombre;
        }

        return valor;
    }

    // ===================== ALTA =====================

    private void Agregar_Clicked(object? sender, EventArgs e)
    {
        panelFormulario.IsVisible = true;
        btnAgregar.IsVisible = false;
    }

    private void CancelarFormulario_Clicked(object? sender, EventArgs e)
    {
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        txtPatente.Text = string.Empty;
        txtMarca.Text = string.Empty;
        txtModelo.Text = string.Empty;
        txtAnio.Text = string.Empty;
        txtCapacidadKg.Text = string.Empty;
        txtVolumenM3.Text = string.Empty;
        pickerTipo.SelectedIndex = -1;

        panelFormulario.IsVisible = false;
        btnAgregar.IsVisible = true;
    }

    private void Tipo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var indice = pickerTipo.SelectedIndex;
        if (indice < 0)
            return;

        // Valores de referencia del tipo elegido: se pueden modificar
        if (string.IsNullOrWhiteSpace(txtCapacidadKg.Text))
            txtCapacidadKg.Text = Tipos[indice].Kg.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(txtVolumenM3.Text))
            txtVolumenM3.Text = Tipos[indice].M3.ToString(CultureInfo.InvariantCulture);
    }

    private async void Guardar_Clicked(object? sender, EventArgs e)
    {
        if (_conductorId is not { } conductorId)
            return;

        var patente = (txtPatente.Text ?? string.Empty).Replace(" ", "").ToUpperInvariant();

        if (patente.Length < 6 || patente.Length > 10 ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            pickerTipo.SelectedIndex < 0 ||
            !int.TryParse(txtAnio.Text, out var anio) ||
            !double.TryParse(txtCapacidadKg.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var capacidad) ||
            !double.TryParse(txtVolumenM3.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var volumen))
        {
            await DisplayAlertAsync("MUEVE", "Completá todos los campos correctamente.", "Aceptar");
            return;
        }

        if (anio < 1980 || anio > 2100 || capacidad < 1 || volumen < 0)
        {
            await DisplayAlertAsync("MUEVE", "Revisá el año, la capacidad y el volumen.", "Aceptar");
            return;
        }

        btnGuardar.IsEnabled = false;
        try
        {
            var (ok, error) = await _transporteService.CrearVehiculo(new CrearVehiculoRequest
            {
                ConductorId = conductorId,
                Patente = patente,
                Marca = txtMarca.Text!.Trim(),
                Modelo = txtModelo.Text!.Trim(),
                Anio = anio,
                TipoVehiculo = Tipos[pickerTipo.SelectedIndex].Valor,
                CapacidadKg = capacidad,
                VolumenM3 = volumen
            });

            if (!ok)
            {
                await DisplayAlertAsync("MUEVE", error ?? "No se pudo registrar el vehículo.", "Aceptar");
                return;
            }

            LimpiarFormulario();
            await CargarAsync();
            await DisplayAlertAsync("MUEVE", "Vehículo agregado. Ya podés elegirlo cuando inicies el servicio.", "Aceptar");
        }
        finally
        {
            btnGuardar.IsEnabled = true;
        }
    }

    // ===================== BAJA =====================

    private async void DarDeBaja_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button boton || boton.CommandParameter is not int vehiculoId)
            return;

        bool confirmar = await DisplayAlertAsync(
            "MUEVE",
            "¿Querés dar de baja este vehículo? Ya no vas a poder usarlo para trabajar.",
            "Dar de baja",
            "Cancelar");

        if (!confirmar)
            return;

        var (ok, error) = await _transporteService.DarDeBajaVehiculoAsync(vehiculoId);
        if (!ok)
        {
            await DisplayAlertAsync("MUEVE", error ?? "No se pudo dar de baja el vehículo.", "Aceptar");
            return;
        }

        await CargarAsync();
    }

    private async void Volver_Tapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}