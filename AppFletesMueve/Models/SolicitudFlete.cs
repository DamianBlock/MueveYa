using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class SolicitudFlete
    {
        public int SolicitudFleteId { get; set; }

        public int ClienteId { get; set; }

        public int? ConductorId { get; set; }

        public int? VehiculoId { get; set; }

        // INMEDIATO o PROGRAMADO
        public string TipoServicio { get; set; } = string.Empty;

        public DateTime FechaSolicitud { get; set; }

        public DateTime? FechaProgramada { get; set; }

        // ORIGEN
        public string DireccionOrigen { get; set; } = string.Empty;

        public double LatitudOrigen { get; set; }

        public double LongitudOrigen { get; set; }

        // DESTINO
        public string DireccionDestino { get; set; } = string.Empty;

        public double LatitudDestino { get; set; }

        public double LongitudDestino { get; set; }

        public double DistanciaKm { get; set; }

        public double Precio { get; set; }

        // PENDIENTE, BUSCANDO_CONDUCTOR, etc.
        public string Estado { get; set; } = "PENDIENTE";
    }
}
