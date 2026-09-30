using System.Net.Http.Json;

namespace AppFletesMueve.Services
{
    public class VehiculoDisponibleDto
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
        public string? Imagen { get; set; }
    }

    public class ItemCargaRequest
    {
        public int TipoCargaId { get; set; }
        public int Cantidad { get; set; } = 1;
    }

    public class CrearSolicitudFleteRequest
    {
        public int ClienteId { get; set; }
        public string TipoServicio { get; set; } = "Inmediato";
        public DateTime? FechaProgramada { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public double LatitudOrigen { get; set; }
        public double LongitudOrigen { get; set; }
        public string DireccionDestino { get; set; } = string.Empty;
        public double LatitudDestino { get; set; }
        public double LongitudDestino { get; set; }
        public double DistanciaKm { get; set; }
        public List<ItemCargaRequest> Cargas { get; set; } = new();
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
        public string TipoServicio { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public double DistanciaKm { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<CargaDto> Cargas { get; set; } = new();
    }

    public class TipoCargaDto
    {
        public int TipoCargaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public double PesoEstimadoKg { get; set; }
        public double VolumenEstimadoM3 { get; set; }
    }

    public class ConductorDto
    {
        public int ConductorId { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Disponible { get; set; }
    }

    public class TransporteService
    {
        private readonly HttpClient _httpClient;

#if DEBUG
        private const string ApiUrl = "http://10.0.2.2:5051/api/";
#else
        private const string ApiUrl = "https://mueveya.onrender.com/api/";
#endif

        public TransporteService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<TipoCargaDto>> ObtenerTiposCarga()
        {
            var resultado = await _httpClient
                .GetFromJsonAsync<List<TipoCargaDto>>(ApiUrl + "TiposCarga");
            return resultado ?? new List<TipoCargaDto>();
        }

        public async Task<List<VehiculoDisponibleDto>> ObtenerVehiculosDisponibles()
        {
            var resultado = await _httpClient
                .GetFromJsonAsync<List<VehiculoDisponibleDto>>(ApiUrl + "Vehiculos/disponibles");
            return resultado ?? new List<VehiculoDisponibleDto>();
        }

        public async Task<SolicitudFleteDto?> CrearSolicitud(CrearSolicitudFleteRequest request)
        {
            var response = await _httpClient
                .PostAsJsonAsync(ApiUrl + "SolicitudesFlete", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SolicitudFleteDto>();
        }

        public async Task<List<SolicitudFleteDto>> ObtenerSolicitudesPendientes()
        {
            var resultado = await _httpClient
                .GetFromJsonAsync<List<SolicitudFleteDto>>(ApiUrl + "SolicitudesFlete/pendientes");
            return resultado ?? new List<SolicitudFleteDto>();
        }

        public async Task<SolicitudFleteDto?> AceptarSolicitud(
            int solicitudId, int conductorId, int vehiculoId)
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"{ApiUrl}SolicitudesFlete/{solicitudId}/aceptar",
                new { ConductorId = conductorId, VehiculoId = vehiculoId });

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SolicitudFleteDto>();
        }

        public async Task<ConductorDto?> ObtenerConductorPorUsuario(int usuarioId)
        {
            var response = await _httpClient.GetAsync(ApiUrl + $"Conductores/por-usuario/{usuarioId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ConductorDto>();
        }

        public async Task<List<VehiculoDisponibleDto>> ObtenerVehiculosDeConductor(int conductorId)
        {
            var resultado = await _httpClient
                .GetFromJsonAsync<List<VehiculoDisponibleDto>>(ApiUrl + $"Vehiculos/conductor/{conductorId}");
            return resultado ?? new List<VehiculoDisponibleDto>();
        }
    }
}