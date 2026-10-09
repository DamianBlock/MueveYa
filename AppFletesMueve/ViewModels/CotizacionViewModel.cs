using System.Collections.ObjectModel;
using AppFletesMueve.Models;
using AppFletesMueve.Services;

namespace AppFletesMueve.ViewModels
{
    public class CotizacionViewModel : ObservableBase
    {
        private CotizacionVehiculo? _seleccionado;
        private string _resumenViaje = string.Empty;
        private bool _hayOpciones;
        private bool _estaProcesando;

        public ObservableCollection<CotizacionVehiculo> Opciones { get; } = new();

        public double DistanciaKm { get; private set; }

        public CotizacionVehiculo? Seleccionado
        {
            get => _seleccionado;
            private set
            {
                if (SetProperty(ref _seleccionado, value))
                {
                    OnPropertyChanged(nameof(PuedeConfirmar));
                    OnPropertyChanged(nameof(TextoConfirmar));
                }
            }
        }

        public bool EstaProcesando
        {
            get => _estaProcesando;
            set
            {
                if (SetProperty(ref _estaProcesando, value))
                {
                    OnPropertyChanged(nameof(PuedeConfirmar));
                    OnPropertyChanged(nameof(TextoConfirmar));
                }
            }
        }

        public string ResumenViaje
        {
            get => _resumenViaje;
            private set => SetProperty(ref _resumenViaje, value);
        }

        public bool HayOpciones
        {
            get => _hayOpciones;
            private set
            {
                if (SetProperty(ref _hayOpciones, value))
                    OnPropertyChanged(nameof(SinOpciones));
            }
        }

        public bool SinOpciones => !HayOpciones;

        public string MensajeSinOpciones =>
            "Tu carga supera la capacidad de nuestros vehículos. Probá dividirla en más de un viaje.";

        public bool PuedeConfirmar => Seleccionado != null && !EstaProcesando;

        public string TextoConfirmar =>
            EstaProcesando ? "Enviando solicitud..."
            : Seleccionado == null ? "Seleccioná un vehículo"
            : $"Confirmar flete - {Seleccionado.PrecioTexto}";

        public void Cargar(CotizacionDto cotizacion, double distanciaKm)
        {
            DistanciaKm = distanciaKm;
            Opciones.Clear();

            foreach (var o in cotizacion.Opciones)
            {
                Opciones.Add(new CotizacionVehiculo
                {
                    TipoVehiculo = o.TipoVehiculo,
                    Nombre = o.Nombre,
                    Precio = o.Precio,
                    CapacidadKg = o.CapacidadKg,
                    CapacidadM3 = o.CapacidadM3,
                    Entra = o.Entra,
                    EsSugerido = o.Sugerido
                });
            }

            ResumenViaje =
                $"{distanciaKm:0.#} km · {cotizacion.PesoTotalKg:N0} kg · " +
                $"incluye {cotizacion.MinutosEspera} min de espera (carga y descarga)";

            HayOpciones = Opciones.Any(o => o.Entra);

            Seleccionar(Opciones.FirstOrDefault(o => o.EsSugerido));
        }

        public void Seleccionar(CotizacionVehiculo? opcion)
        {
            if (opcion == null)
            {
                foreach (var o in Opciones)
                    o.EsSeleccionado = false;

                Seleccionado = null;
                return;
            }

            if (!opcion.Entra)
                return;

            foreach (var o in Opciones)
                o.EsSeleccionado = ReferenceEquals(o, opcion);

            Seleccionado = opcion;
        }
    }
}