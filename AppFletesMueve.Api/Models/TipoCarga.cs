namespace AppFletesMueve.Api.Models
{
    public class TipoCarga
    {
        public int TipoCargaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public double PesoEstimadoKg { get; set; }

        public double VolumenEstimadoM3 { get; set; }
    }
}
