namespace AppFletesMueve.Api.Models
{
    public class Vehiculo
    {
        public int VehiculoId { get; set; }

        public int ConductorId { get; set; }
        public Conductor Conductor { get; set; } = null!;

        public string Patente { get; set; } = string.Empty;

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Anio { get; set; }

        public TipoVehiculo TipoVehiculo { get; set; }

        public double CapacidadKg { get; set; }

        public double VolumenM3 { get; set; }

        public bool Disponible { get; set; }

        public string? Imagen { get; set; }
    }
}
