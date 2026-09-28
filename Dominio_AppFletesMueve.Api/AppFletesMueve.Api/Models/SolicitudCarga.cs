namespace AppFletesMueve.Api.Models
{
    public class SolicitudCarga
    {
        public int SolicitudCargaId { get; set; }

        public int SolicitudFleteId { get; set; }
        public SolicitudFlete SolicitudFlete { get; set; } = null!;

        public int TipoCargaId { get; set; }
        public TipoCarga TipoCarga { get; set; } = null!;

        public int Cantidad { get; set; }

        public double PesoKg { get; set; }

        public double VolumenM3 { get; set; }
    }
}
