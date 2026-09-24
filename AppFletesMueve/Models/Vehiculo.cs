using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class Vehiculo
    {
        public int VehiculoId { get; set; }

        public int ConductorId { get; set; }

        public string Patente { get; set; } = string.Empty;

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public int Anio { get; set; }

        public string TipoVehiculo { get; set; } = string.Empty;

        public double CapacidadKg { get; set; }

        public double VolumenM3 { get; set; }

        public bool Disponible { get; set; }

        public string Imagen { get; set; } = string.Empty;
    }
}