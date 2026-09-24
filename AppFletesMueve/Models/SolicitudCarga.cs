using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class SolicitudCarga
    {
        public int SolicitudCargaId { get; set; }

        public int SolicitudFleteId { get; set; }

        public int TipoCargaId { get; set; }

        public int Cantidad { get; set; }

        public double PesoKg { get; set; }

        public double VolumenM3 { get; set; }
    }
}