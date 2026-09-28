namespace AppFletesMueve.Api.Models
{
    public class Conductor
    {
        public int ConductorId { get; set; }

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public string Licencia { get; set; } = string.Empty;

        public bool Disponible { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}
