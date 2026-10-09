namespace AppFletesMueve.Models
{
    public class CotizacionVehiculo : ObservableBase
    {
        private bool _esSeleccionado;

        public string TipoVehiculo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public double CapacidadKg { get; set; }
        public double CapacidadM3 { get; set; }
        public bool Entra { get; set; }
        public bool EsSugerido { get; set; }

        public bool EsSeleccionado
        {
            get => _esSeleccionado;
            set
            {
                if (SetProperty(ref _esSeleccionado, value))
                {
                    OnPropertyChanged(nameof(ColorBorde));
                    OnPropertyChanged(nameof(ColorFondo));
                }
            }
        }

        // Imágenes que ya usás en la app
        public string Imagen => TipoVehiculo switch
        {
            "CamionGrande" => "large_truck.png",
            "CamionMediano" or "CamionChico" => "medium_truck.png",
            _ => "pickup_truck.png"
        };

        public string PrecioTexto => $"$ {Precio:N0}";

        public string Detalle => $"Hasta {CapacidadKg:N0} kg · {CapacidadM3:0.#} m³";

        public string Etiqueta =>
            !Entra ? "No alcanza para tu carga"
            : EsSugerido ? "Recomendado"
            : string.Empty;

        public bool TieneEtiqueta => !string.IsNullOrEmpty(Etiqueta);

        public double Opacidad => Entra ? 1.0 : 0.45;

        public Color ColorBorde =>
            EsSeleccionado ? Color.FromArgb("#E65100") : Color.FromArgb("#E0E0E0");

        public Color ColorFondo =>
            EsSeleccionado ? Color.FromArgb("#FFF3E0") : Colors.White;
        public Color ColorEtiqueta =>
            EsSugerido && Entra ? Color.FromArgb("#2E7D32") : Color.FromArgb("#757575");
    }
}