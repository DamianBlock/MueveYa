using System.ComponentModel.DataAnnotations;
using AppFletesMueve.Api.Models;

namespace AppFletesMueve.Api.Dtos
{
    public class TipoCargaDto
    {
        public int TipoCargaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public double PesoEstimadoKg { get; set; }
        public double VolumenEstimadoM3 { get; set; }
    }

    public class CrearConductorDto
    {
        [Range(1, int.MaxValue)]
        public int UsuarioId { get; set; }

        [Required, StringLength(30, MinimumLength = 4)]
        public string Licencia { get; set; } = string.Empty;
    }

    public class ActualizarEstadoConductorDto
    {
        public bool Disponible { get; set; }

        [Range(-90, 90)]
        public double? Latitud { get; set; }

        [Range(-180, 180)]
        public double? Longitud { get; set; }
    }

    public class ConductorDto
    {
        public int ConductorId { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Licencia { get; set; } = string.Empty;
        public bool Disponible { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public class CrearVehiculoDto
    {
        [Range(1, int.MaxValue)]
        public int ConductorId { get; set; }

        [Required, StringLength(10, MinimumLength = 6)]
        public string Patente { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [Range(1980, 2100)]
        public int Anio { get; set; }

        [EnumDataType(typeof(TipoVehiculo))]
        public TipoVehiculo TipoVehiculo { get; set; }

        [Range(1, 100000)]
        public double CapacidadKg { get; set; }

        [Range(0, 1000)]
        public double VolumenM3 { get; set; }

        [StringLength(500)]
        public string? Imagen { get; set; }
    }

    public class VehiculoDto
    {
        public int VehiculoId { get; set; }
        public int ConductorId { get; set; }
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