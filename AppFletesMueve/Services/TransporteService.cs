using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AppFletesMueve.Services
{
    public class CrearConductorRequest { public int UsuarioId { get; set; } public string Licencia { get; set; } = string.Empty; }
    public class CrearVehiculoRequest { public int ConductorId { get; set; } public string Patente { get; set; } = string.Empty; public string Marca { get; set; } = string.Empty; public string Modelo { get; set; } = string.Empty; public int Anio { get; set; } public string TipoVehiculo { get; set; } = string.Empty; public double CapacidadKg { get; set; } public double VolumenM3 { get; set; } }

    public class VehiculoDisponibleDto { public int VehiculoId { get; set; } public int ConductorId { get; set; } public string Patente { get; set; } = string.Empty; public string Marca { get; set; } = string.Empty; public string Modelo { get; set; } = string.Empty; public int Anio { get; set; } public string TipoVehiculo { get; set; } = string.Empty; public double CapacidadKg { get; set; } public double VolumenM3 { get; set; } public bool Disponible { get; set; } public string? Imagen { get; set; } }

    public class ItemCargaRequest { public int TipoCargaId { get; set; } public int Cantidad { get; set; } = 1; }

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

    public class CargaDto { public int TipoCargaId { get; set; } public string TipoCargaNombre { get; set; } = string.Empty; public int Cantidad { get; set; } public double PesoKg { get; set; } public double VolumenM3 { get; set; } }

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
        public double LatitudOrigen { get; set; }
        public double LongitudOrigen { get; set; }
        public double LatitudDestino { get; set; }
        public double LongitudDestino { get; set; }
        public double DistanciaKm { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<CargaDto> Cargas { get; set; } = new();
    }

    public class TipoCargaDto { public int TipoCargaId { get; set; } public string Nombre { get; set; } = string.Empty; public double PesoEstimadoKg { get; set; } public double VolumenEstimadoM3 { get; set; } }

    public class ConductorDto { public int ConductorId { get; set; } public int UsuarioId { get; set; } public string Nombre { get; set; } = string.Empty; public bool Disponible { get; set; } }

    public class VehiculoOpcionDto { public int VehiculoId { get; set; } public int ConductorId { get; set; } public string Nombre { get; set; } = string.Empty; public double Precio { get; set; } public string? Imagen { get; set; } }

    public class TransporteService
    {
        private readonly HttpClient _httpClient;
        public static event Action<SolicitudFleteDto>? SolicitudCreada;

#if DEBUG
        private const string ApiUrl = "http://10.0.2.2:5051/api/";
#else
        private const string ApiUrl = "https://mueveya.onrender.com/api/";
#endif

        public TransporteService() { _httpClient = new HttpClient(); }

        private static async Task<string> LeerMensajeError(HttpResponseMessage response)
        {
            try
            {
                var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                if (json != null && json.TryGetValue("mensaje", out var m)) return m?.ToString() ?? "Ocurrió un error.";
            }
            catch { }
            return "Ocurrió un error.";
        }

        public async Task<List<VehiculoDisponibleDto>> ObtenerVehiculosDisponibles()
        {
            try { var resultado = await _httpClient.GetFromJsonAsync<List<VehiculoDisponibleDto>>(ApiUrl + "Vehiculos/disponibles"); return resultado ?? new List<VehiculoDisponibleDto>(); }
            catch { return new List<VehiculoDisponibleDto>(); }
        }

        public async Task<ConductorDto?> ObtenerConductorPorUsuario(int usuarioId)
        {
            try { return await _httpClient.GetFromJsonAsync<ConductorDto?>(ApiUrl + $"Conductores/por-usuario/{usuarioId}"); }
            catch { return null; }
        }


        public async Task<List<VehiculoDisponibleDto>> ObtenerVehiculosDeConductor(int conductorId)
        {
            try { var r = await _httpClient.GetFromJsonAsync<List<VehiculoDisponibleDto>>(ApiUrl + $"Vehiculos/conductor/{conductorId}"); return r ?? new List<VehiculoDisponibleDto>(); }
            catch { return new List<VehiculoDisponibleDto>(); }
        }

        public async Task<SolicitudFleteDto?> ObtenerViajeActivoDeConductor(int conductorId)
        {
            try { return await _httpClient.GetFromJsonAsync<SolicitudFleteDto?>(ApiUrl + $"SolicitudesFlete/conductor/{conductorId}/activa"); }
            catch { return null; }
        }
        public async Task<List<SolicitudFleteDto>> ObtenerSolicitudesPendientes()
        {
            try { var r = await _httpClient.GetFromJsonAsync<List<SolicitudFleteDto>>(ApiUrl + "SolicitudesFlete/pendientes"); return r ?? new List<SolicitudFleteDto>(); }
            catch { return new List<SolicitudFleteDto>(); }
        }

        public async Task<SolicitudFleteDto?> CrearSolicitud(CrearSolicitudFleteRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiUrl + "SolicitudesFlete", request);
                if (!response.IsSuccessStatusCode) return null;

                var dto = await response.Content.ReadFromJsonAsync<SolicitudFleteDto>();
                if (dto != null) SolicitudCreada?.Invoke(dto);
                return dto;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear solicitud: {ex}");
                return null;
            }
        }

        public async Task<SolicitudFleteDto?> AceptarSolicitud(int solicitudId, int conductorId, int vehiculoId)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(ApiUrl + $"SolicitudesFlete/{solicitudId}/aceptar",
                    new { ConductorId = conductorId, VehiculoId = vehiculoId });
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<SolicitudFleteDto>();
            }
            catch { return null; }
        }

        public async Task<bool> AceptarSolicitudAsync(int solicitudId, int conductorId, int vehiculoId)
        {
            var r = await AceptarSolicitud(solicitudId, conductorId, vehiculoId);
            return r != null;
        }

        public async Task<SolicitudFleteDto?> CompletarSolicitud(int solicitudId)
        {
            try
            {
                var response = await _httpClient.PutAsync(ApiUrl + $"SolicitudesFlete/{solicitudId}/completar", null);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<SolicitudFleteDto>();
            }
            catch { return null; }
        }

        public async Task<List<VehiculoOpcionDto>> ObtenerOpcionesVehiculoAsync(double origenLat, double origenLon, double destinoLat, double destinoLon)
        {
            try
            {
                var resultado = await _httpClient.GetFromJsonAsync<List<VehiculoOpcionDto>>(ApiUrl + $"Tarifas/opciones?origenLat={origenLat}&origenLon={origenLon}&destinoLat={destinoLat}&destinoLon={destinoLon}");
                if (resultado != null) return resultado;
            }
            catch { }

            var vehiculos = await ObtenerVehiculosDisponibles();
            var distancia = HaversineDistanceKm(origenLat, origenLon, destinoLat, destinoLon);
            var lista = new List<VehiculoOpcionDto>();
            foreach (var v in vehiculos.Where(x => x.Disponible))
            {
                var precio = CalcularPrecioEstimado(distancia, v);
                lista.Add(new VehiculoOpcionDto { VehiculoId = v.VehiculoId, ConductorId = v.ConductorId, Nombre = $"{v.Marca} {v.Modelo}", Precio = precio, Imagen = v.Imagen });
            }
            return lista;
        }

        public static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * (Math.PI / 180);

        private static double CalcularPrecioEstimado(double distanciaKm, VehiculoDisponibleDto v)
        {
            double baseFare = 50;
            double perKm = 12;
            if (v.TipoVehiculo?.ToLower().Contains("caja") == true) perKm = 18;
            return Math.Round(baseFare + perKm * distanciaKm, 2);
        }

        public async Task<(bool, string?)> CrearConductor(int usuarioId, string licencia)
        {
            try
            {
                var request = new CrearConductorRequest { UsuarioId = usuarioId, Licencia = licencia };
                var response = await _httpClient.PostAsJsonAsync(ApiUrl + "Conductores", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await LeerMensajeError(response);
                    return (false, errorMsg);
                }

                var conductor = await response.Content.ReadFromJsonAsync<ConductorDto>();
                return (conductor != null, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool, string?)> CrearVehiculo(CrearVehiculoRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiUrl + "Vehiculos", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await LeerMensajeError(response);
                    return (false, errorMsg);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public async Task<List<TipoCargaDto>> ObtenerTiposCarga()
        {
            try { var r = await _httpClient.GetFromJsonAsync<List<TipoCargaDto>>(ApiUrl + "TiposCarga"); return r ?? new List<TipoCargaDto>(); }
            catch { return new List<TipoCargaDto>(); }
        }
    }
}