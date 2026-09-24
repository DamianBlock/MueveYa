using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class Conductor
    {
        public int ConductorId { get; set; }

        public int UsuarioId { get; set; }

        public string Licencia { get; set; } = string.Empty;

        public bool Disponible { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }
    }
}