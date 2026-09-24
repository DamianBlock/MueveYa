using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppFletesMueve.Models;

namespace AppFletesMueve.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Lista de vehículos
        public ObservableCollection<VehiculoModel> Vehiculos { get; }

        // Vehículo seleccionado
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

        // Texto dinámico del botón
        public string TextoBotonConfirmar
        {
            get
            {
                if (VehiculoSeleccionado != null)
                {
                    return $"Confirmar Flete - ${VehiculoSeleccionado.Precio} MXN";
                }

                return "Selecciona un vehículo";
            }
        }

        // Comando para seleccionar vehículo
        public ICommand SeleccionarVehiculoCommand { get; }

        // Comando para confirmar flete
        public ICommand ConfirmarFleteCommand { get; }

        public MainViewModel()
        {
            // Vehículos disponibles
            Vehiculos = new ObservableCollection<VehiculoModel>
            {
                new VehiculoModel
                {
                    Id = 1,
                    Nombre = "Camioneta Pick-up",
                    Imagen = "pickup_truck.png",
                    Precio = 350,
                    Info = "★ 4.8  ⏱ 7 min"
                },

                new VehiculoModel
                {
                    Id = 2,
                    Nombre = "Camión 3.5 Ton",
                    Imagen = "medium_truck.png",
                    Precio = 690,
                    Info = "★ 4.9  ⏱ 12 min"
                },

                new VehiculoModel
                {
                    Id = 3,
                    Nombre = "Mudanza Completa",
                    Imagen = "large_truck.png",
                    Precio = 1200,
                    Info = "★ 4.7  ⏱ 18 min"
                }
            };

            // Seleccionamos el primer vehículo
            VehiculoSeleccionado = Vehiculos[0];

            // Comando selección
            SeleccionarVehiculoCommand =
                new Command<VehiculoModel>(SeleccionarVehiculo);

            // Comando confirmar
            ConfirmarFleteCommand =
                new Command(ConfirmarFlete);
        }

        private void SeleccionarVehiculo(VehiculoModel? vehiculo)
        {
            if (vehiculo == null)
                return;

            VehiculoSeleccionado = vehiculo;
        }

        private async void ConfirmarFlete()
        {
            if (VehiculoSeleccionado == null)
                return;

            await Shell.Current.DisplayAlert(
                "MUEVE",
                $"Buscando {VehiculoSeleccionado.Nombre} cercana...",
                "OK");
        }

        // Implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}