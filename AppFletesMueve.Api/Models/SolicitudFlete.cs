namespace AppFletesMueve.Api.Models
{
    public class SolicitudFlete
    {
        public int SolicitudFleteId { get; set; }

        // Usuario que pide el flete
        public int ClienteId { get; set; }
        public Usuario Cliente { get; set; } = null!;

        // Se completan cuando un conductor acepta
        public int? ConductorId { get; set; }
        public Conductor? Conductor { get; set; }

        public int? VehiculoId { get; set; }
        public Vehiculo? Vehiculo { get; set; }

        public TipoServicio TipoServicio { get; set; }

        // Tipo de vehículo que pidió el cliente
        public TipoVehiculo TipoVehiculo { get; set; }

        // Siempre en UTC
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        public DateTime? FechaProgramada { get; set; }

        // Origen
        public string DireccionOrigen { get; set; } = string.Empty;
        public double LatitudOrigen { get; set; }
        public double LongitudOrigen { get; set; }

        // Destino
        public string DireccionDestino { get; set; } = string.Empty;
        public double LatitudDestino { get; set; }
        public double LongitudDestino { get; set; }

        public double DistanciaKm { get; set; }

        public decimal Precio { get; set; }

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        public ICollection<SolicitudCarga> Cargas { get; set; } = new List<SolicitudCarga>();
    }
}
