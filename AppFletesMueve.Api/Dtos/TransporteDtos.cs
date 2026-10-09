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

    public class ItemCargaDto
    {
        [Range(1, int.MaxValue)]
        public int TipoCargaId { get; set; }

        [Range(1, 1000)]
        public int Cantidad { get; set; } = 1;
    }

    public class CrearSolicitudFleteDto
    {
        [Range(1, int.MaxValue)]
        public int ClienteId { get; set; }

        [EnumDataType(typeof(TipoServicio))]
        public TipoServicio TipoServicio { get; set; }

        public DateTime? FechaProgramada { get; set; }

        [Required, StringLength(200)]
        public string DireccionOrigen { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double LatitudOrigen { get; set; }

        [Range(-180, 180)]
        public double LongitudOrigen { get; set; }

        [Required, StringLength(200)]
        public string DireccionDestino { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double LatitudDestino { get; set; }

        [Range(-180, 180)]
        public double LongitudDestino { get; set; }

        [Range(0.1, 2000)]
        public double DistanciaKm { get; set; }

        [MinLength(1)]
        public List<ItemCargaDto> Cargas { get; set; } = new();

        [EnumDataType(typeof(TipoVehiculo))]
        public TipoVehiculo TipoVehiculo { get; set; }
    }

    public class AceptarSolicitudDto
    {
        [Range(1, int.MaxValue)]
        public int ConductorId { get; set; }

        [Range(1, int.MaxValue)]
        public int VehiculoId { get; set; }
    }

    public class CargaDto
    {
        public int TipoCargaId { get; set; }
        public string TipoCargaNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double PesoKg { get; set; }
        public double VolumenM3 { get; set; }
    }

    public class SolicitudFleteDto
    {
        public int SolicitudFleteId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public int? ConductorId { get; set; }
        public int? VehiculoId { get; set; }
        public TipoServicio TipoServicio { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public double LatitudOrigen { get; set; }
        public double LongitudOrigen { get; set; }
        public string DireccionDestino { get; set; } = string.Empty;
        public double LatitudDestino { get; set; }
        public double LongitudDestino { get; set; }
        public double DistanciaKm { get; set; }
        public decimal Precio { get; set; }
        public EstadoSolicitud Estado { get; set; }
        public List<CargaDto> Cargas { get; set; } = new();
        public TipoVehiculo TipoVehiculo { get; set; }
    }

    public class CotizarTarifaDto
    {
        [EnumDataType(typeof(TipoServicio))]
        public TipoServicio TipoServicio { get; set; }

        [Range(0.1, 2000)]
        public double DistanciaKm { get; set; }

        [MinLength(1)]
        public List<ItemCargaDto> Cargas { get; set; } = new();
    }

    public class OpcionTarifaDto
    {
        public TipoVehiculo TipoVehiculo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public double CapacidadKg { get; set; }
        public double CapacidadM3 { get; set; }
        public bool Entra { get; set; }
        public bool Sugerido { get; set; }
    }

    public class CotizacionDto
    {
        public double PesoTotalKg { get; set; }
        public double VolumenTotalM3 { get; set; }
        public int MinutosEspera { get; set; }
        public List<OpcionTarifaDto> Opciones { get; set; } = new();
    }

    public class ActivarConductorDto
    {
        public int VehiculoId { get; set; }

        [Range(-90, 90)]
        public double? Latitud { get; set; }

        [Range(-180, 180)]
        public double? Longitud { get; set; }
    }
}