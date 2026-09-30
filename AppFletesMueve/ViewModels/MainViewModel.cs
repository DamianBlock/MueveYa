using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TransporteService _transporteService;

        public ObservableCollection<VehiculoModel> Vehiculos { get; } = new();

        private VehiculoModel? _vehiculoSeleccionado;
        public VehiculoModel? VehiculoSeleccionado
        {
            get => _vehiculoSeleccionado;
            set
            {
                if (_vehiculoSeleccionado != value)
                {
                    _vehiculoSeleccionado = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextoBotonConfirmar));
                }
            }
        }

        public string TextoBotonConfirmar =>
            VehiculoSeleccionado != null
                ? $"Confirmar Flete - {VehiculoSeleccionado.Info}"
                : "Selecciona un vehículo";

        public ICommand SeleccionarVehiculoCommand { get; }
        public ICommand ConfirmarFleteCommand { get; }

        // Usado por el diseñador XAML / sin DI
        public MainViewModel() : this(new TransporteService()) { }

        public MainViewModel(TransporteService transporteService)
        {
            _transporteService = transporteService;

            SeleccionarVehiculoCommand = new Command<VehiculoModel>(SeleccionarVehiculo);
            ConfirmarFleteCommand = new Command(async () => await ConfirmarFleteAsync());

            _ = CargarVehiculosAsync();
        }

        public async Task CargarVehiculosAsync()
        {
            try
            {
                var disponibles = await _transporteService.ObtenerVehiculosDisponibles();

                Vehiculos.Clear();
                foreach (var v in disponibles)
                {
                    Vehiculos.Add(new VehiculoModel
                    {
                        Id = v.VehiculoId,
                        Nombre = $"{v.Marca} {v.Modelo}",
                        Imagen = "pickup_truck.png", // placeholder hasta tener imágenes por tipo
                        Precio = 0, // el precio real se calcula al crear la solicitud
                        Info = $"{v.TipoVehiculo} · hasta {v.CapacidadKg:0} kg"
                    });
                }

                VehiculoSeleccionado = Vehiculos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando vehículos: {ex}");
            }
        }

        private void SeleccionarVehiculo(VehiculoModel? vehiculo)
        {
            if (vehiculo == null) return;
            VehiculoSeleccionado = vehiculo;
        }

        private async Task ConfirmarFleteAsync()
        {
            if (VehiculoSeleccionado == null || SesionUsuario.UsuarioId == 0)
                return;

            // Ruta de ejemplo fija hasta integrar mapa/geolocalización
            var request = new CrearSolicitudFleteRequest
            {
                ClienteId = SesionUsuario.UsuarioId,
                TipoServicio = "Inmediato",
                DireccionOrigen = "Av. Siempre Viva 123",
                LatitudOrigen = -26.83,
                LongitudOrigen = -65.20,
                DireccionDestino = "San Miguel de Tucumán Centro",
                LatitudDestino = -26.82,
                LongitudDestino = -65.21,
                DistanciaKm = 5.5,
                Cargas = new List<ItemCargaRequest>
                {
                    new ItemCargaRequest { TipoCargaId = 1, Cantidad = 1 }
                }
            };

            var resultado = await _transporteService.CrearSolicitud(request);

            var mensaje = resultado != null
                ? $"Solicitud creada. Precio estimado: ${resultado.Precio:0.00}. Buscando conductor..."
                : "No se pudo crear la solicitud. Intentá de nuevo.";

            await Shell.Current.DisplayAlert("MUEVE", mensaje, "OK");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}