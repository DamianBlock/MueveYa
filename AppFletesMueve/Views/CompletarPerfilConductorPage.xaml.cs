using System.Globalization;
using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.Views;

public partial class CompletarPerfilConductorPage : ContentPage
{
    private readonly TransporteService _transporteService = new();

    public CompletarPerfilConductorPage()
    {
        InitializeComponent();
    }

    private async void Guardar_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtLicencia.Text) ||
            string.IsNullOrWhiteSpace(txtPatente.Text) ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            pickerTipoVehiculo.SelectedItem is null ||
            !int.TryParse(txtAnio.Text, out var anio) ||
            !double.TryParse(txtCapacidadKg.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var capacidad) ||
            !double.TryParse(txtVolumenM3.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var volumen))
        {
            await DisplayAlert("MUEVE", "Completá todos los campos correctamente.", "Aceptar");
            return;
        }

        try
        {
            var (okConductor, errorConductor) = await _transporteService.CrearConductor(
                SesionUsuario.UsuarioId, txtLicencia.Text.Trim());

            if (!okConductor)
            {
                await DisplayAlert("MUEVE", errorConductor ?? "No se pudo crear el perfil de conductor.", "Aceptar");
                return;
            }

            var conductor = await _transporteService.ObtenerConductorPorUsuario(SesionUsuario.UsuarioId);
            if (conductor is null)
            {
                await DisplayAlert("MUEVE", "No se pudo recuperar el perfil creado.", "Aceptar");
                return;
            }

            var (okVehiculo, errorVehiculo) = await _transporteService.CrearVehiculo(new CrearVehiculoRequest
            {
                ConductorId = conductor.ConductorId,
                Patente = txtPatente.Text.Trim(),
                Marca = txtMarca.Text.Trim(),
                Modelo = txtModelo.Text.Trim(),
                Anio = anio,
                TipoVehiculo = pickerTipoVehiculo.SelectedItem.ToString()!,
                CapacidadKg = capacidad,
                VolumenM3 = volumen
            });

            if (!okVehiculo)
            {
                await DisplayAlert("MUEVE", errorVehiculo ?? "No se pudo registrar el vehículo.", "Aceptar");
                return;
            }

            await DisplayAlert("MUEVE", "Perfil de conductor creado correctamente.", "Aceptar");

            var app = Application.Current;
            if (app?.Windows?.Count > 0)
                app.Windows[0].Page = new NavigationPage(new HomeConductor());
        }
        catch (Exception ex)
        {
            await DisplayAlert("MUEVE", "No se pudo conectar con el servidor.", "Aceptar");
            System.Diagnostics.Debug.WriteLine($"Error al completar perfil: {ex}");
        }
    }
}