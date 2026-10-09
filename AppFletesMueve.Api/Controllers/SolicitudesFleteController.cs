using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Models;
using AppFletesMueve.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolicitudesFleteController : ControllerBase
    {
    private readonly MueveDbContext _context;
    private readonly ICalculadoraTarifas _tarifas;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<AppFletesMueve.Api.Hubs.SolicitudesHub> _hub;
    private readonly ILogger<SolicitudesFleteController> _logger;

    public SolicitudesFleteController(MueveDbContext context, ICalculadoraTarifas tarifas,
        Microsoft.AspNetCore.SignalR.IHubContext<AppFletesMueve.Api.Hubs.SolicitudesHub> hub,
        ILogger<SolicitudesFleteController> logger)
        {
            _context = context;
            _tarifas = tarifas;
            _hub = hub;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<SolicitudFleteDto>> Crear(CrearSolicitudFleteDto dto)
        {
            var cliente = await _context.Usuarios.FindAsync(dto.ClienteId);
            if (cliente is null)
                return NotFound(new { mensaje = "El cliente no existe" });

            var tipoCargaIds = dto.Cargas.Select(c => c.TipoCargaId).ToList();
            var tiposCarga = await _context.TiposCarga
                .Where(t => tipoCargaIds.Contains(t.TipoCargaId))
                .ToDictionaryAsync(t => t.TipoCargaId);

            if (tiposCarga.Count != tipoCargaIds.Distinct().Count())
                return BadRequest(new { mensaje = "Hay un tipo de carga inválido" });

            var cargas = dto.Cargas.Select(c =>
            {
                var tipo = tiposCarga[c.TipoCargaId];
                return new SolicitudCarga
                {
                    TipoCargaId = c.TipoCargaId,
                    Cantidad = c.Cantidad,
                    PesoKg = tipo.PesoEstimadoKg * c.Cantidad,
                    VolumenM3 = tipo.VolumenEstimadoM3 * c.Cantidad
                };
            }).ToList();

            var pesoTotal = cargas.Sum(c => c.PesoKg);
            var volumenTotal = cargas.Sum(c => c.VolumenM3);

            var perfil = _tarifas.ObtenerPerfil(dto.TipoVehiculo);
            if (pesoTotal > perfil.CapacidadKg || volumenTotal > perfil.CapacidadM3)
                return BadRequest(new { mensaje = "La carga no entra en el vehículo elegido" });

            var precio = _tarifas.Calcular(dto.DistanciaKm, pesoTotal, dto.TipoServicio, dto.TipoVehiculo);

            var solicitud = new SolicitudFlete
            {
                ClienteId = dto.ClienteId,
                TipoServicio = dto.TipoServicio,
                FechaProgramada = dto.FechaProgramada?.ToUniversalTime(),
                DireccionOrigen = dto.DireccionOrigen.Trim(),
                LatitudOrigen = dto.LatitudOrigen,
                LongitudOrigen = dto.LongitudOrigen,
                DireccionDestino = dto.DireccionDestino.Trim(),
                LatitudDestino = dto.LatitudDestino,
                LongitudDestino = dto.LongitudDestino,
                DistanciaKm = dto.DistanciaKm,
                Precio = precio,
                Estado = EstadoSolicitud.Pendiente,
                Cargas = cargas,
                TipoVehiculo = dto.TipoVehiculo,
            };

            _context.SolicitudesFlete.Add(solicitud);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando nueva solicitud. Payload: {@dto}", dto);
                throw;
            }

            var resultado = await ObtenerDto(solicitud.SolicitudFleteId);

            // Notificar en tiempo real a conductores (vía SignalR) usando grupo 'drivers'
            try
            {
                await _hub.Clients.Group($"drivers-{solicitud.TipoVehiculo}").SendAsync("NuevaSolicitud", resultado);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo notificar nueva solicitud por SignalR");
            }

            return CreatedAtAction(nameof(ObtenerPorId),
                new { id = solicitud.SolicitudFleteId }, resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SolicitudFleteDto>> ObtenerPorId(int id)
        {
            var dto = await ObtenerDto(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        // Lo que un conductor ve para elegir qué aceptar
        [HttpGet("pendientes")]
        public async Task<ActionResult<IEnumerable<SolicitudFleteDto>>> ListarPendientes([FromQuery] TipoVehiculo? tipo)
        {
            var query = _context.SolicitudesFlete
                .Where(s => s.Estado == EstadoSolicitud.Pendiente);

            if (tipo.HasValue)
                query = query.Where(s => s.TipoVehiculo == tipo.Value);

            var ids = await query
                .OrderBy(s => s.FechaSolicitud)
                .Select(s => s.SolicitudFleteId)
                .ToListAsync();

            var resultado = new List<SolicitudFleteDto>();
            foreach (var id in ids)
            {
                var dto = await ObtenerDto(id);
                if (dto is not null) resultado.Add(dto);
            }
            return Ok(resultado);
        }

        // Historial de un cliente
        [HttpGet("cliente/{clienteId:int}")]
        public async Task<ActionResult<IEnumerable<SolicitudFleteDto>>> ListarPorCliente(int clienteId)
        {
            var ids = await _context.SolicitudesFlete
                .Where(s => s.ClienteId == clienteId)
                .OrderByDescending(s => s.FechaSolicitud)
                .Select(s => s.SolicitudFleteId)
                .ToListAsync();

            var resultado = new List<SolicitudFleteDto>();
            foreach (var id in ids)
            {
                var dto = await ObtenerDto(id);
                if (dto is not null) resultado.Add(dto);
            }
            return Ok(resultado);
        }

        [HttpPut("{id:int}/aceptar")]
        public async Task<ActionResult<SolicitudFleteDto>> Aceptar(int id, AceptarSolicitudDto dto)
        {
            var solicitud = await _context.SolicitudesFlete.FindAsync(id);
            if (solicitud is null)
                return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
                return Conflict(new { mensaje = "La solicitud ya no está pendiente" });

            var vehiculo = await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.VehiculoId == dto.VehiculoId
                    && v.ConductorId == dto.ConductorId);
            if (vehiculo is null)
                return BadRequest(new { mensaje = "El vehículo no pertenece a ese conductor" });

            if (vehiculo.TipoVehiculo != solicitud.TipoVehiculo)
                return Conflict(new { mensaje = $"Esta solicitud pide un vehículo tipo {solicitud.TipoVehiculo}" });

            if (!vehiculo.Disponible)
                return Conflict(new { mensaje = "El vehículo no está disponible" });

            solicitud.ConductorId = dto.ConductorId;
            solicitud.VehiculoId = dto.VehiculoId;
            solicitud.Estado = EstadoSolicitud.Aceptada;
            vehiculo.Disponible = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aceptar solicitud {SolicitudId} con payload {@dto}", id, dto);
                throw;
            }

            var resultadoDto = await ObtenerDto(id);
            try
            {
                var groupName = $"solicitud-{id}";
                await _hub.Clients.Group(groupName).SendAsync("SolicitudAceptada", resultadoDto);
            }
            catch { }

            return Ok(resultadoDto);
        }

        [HttpPut("{id:int}/cancelar")]
        public async Task<ActionResult<SolicitudFleteDto>> Cancelar(int id)
        {
            var solicitud = await _context.SolicitudesFlete.FindAsync(id);
            if (solicitud is null)
                return NotFound();

            if (solicitud.Estado is EstadoSolicitud.Completada or EstadoSolicitud.Cancelada)
                return Conflict(new { mensaje = "La solicitud ya está cerrada" });

            if (solicitud.VehiculoId.HasValue)
            {
                var vehiculo = await _context.Vehiculos.FindAsync(solicitud.VehiculoId.Value);
                if (vehiculo is not null) vehiculo.Disponible = true;
            }

            solicitud.Estado = EstadoSolicitud.Cancelada;
            await _context.SaveChangesAsync();

            return Ok(await ObtenerDto(id));
        }
        [HttpGet("conductor/{conductorId:int}/activa")]
        public async Task<ActionResult<SolicitudFleteDto>> ObtenerActivaPorConductor(int conductorId)
        {
            var id = await _context.SolicitudesFlete
                .Where(s => s.ConductorId == conductorId &&
                    (s.Estado == EstadoSolicitud.Aceptada || s.Estado == EstadoSolicitud.EnCurso))
                .OrderByDescending(s => s.FechaSolicitud)
                .Select(s => s.SolicitudFleteId)
                .FirstOrDefaultAsync();

            if (id == 0) return NotFound();

            var dto = await ObtenerDto(id);
            return dto is null ? NotFound() : Ok(dto);
        }
        [HttpPut("{id:int}/completar")]
        public async Task<ActionResult<SolicitudFleteDto>> Completar(int id)
        {
            var solicitud = await _context.SolicitudesFlete.FindAsync(id);
            if (solicitud is null)
                return NotFound();

            if (solicitud.Estado != EstadoSolicitud.Aceptada && solicitud.Estado != EstadoSolicitud.EnCurso)
                return Conflict(new { mensaje = "La solicitud no está en curso" });

            if (solicitud.VehiculoId.HasValue)
            {
                var vehiculo = await _context.Vehiculos.FindAsync(solicitud.VehiculoId.Value);
                if (vehiculo is not null) vehiculo.Disponible = true;
            }

            solicitud.Estado = EstadoSolicitud.Completada;
            await _context.SaveChangesAsync();

            return Ok(await ObtenerDto(id));
        }
        private async Task<SolicitudFleteDto?> ObtenerDto(int id)
        {
            var s = await _context.SolicitudesFlete
                .AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.Cargas)
                    .ThenInclude(c => c.TipoCarga)
                .FirstOrDefaultAsync(x => x.SolicitudFleteId == id);

            if (s is null) return null;

            return new SolicitudFleteDto
            {
                SolicitudFleteId = s.SolicitudFleteId,
                ClienteId = s.ClienteId,
                ClienteNombre = $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim(),
                ConductorId = s.ConductorId,
                VehiculoId = s.VehiculoId,
                TipoServicio = s.TipoServicio,
                FechaSolicitud = s.FechaSolicitud,
                FechaProgramada = s.FechaProgramada,
                DireccionOrigen = s.DireccionOrigen,
                LatitudOrigen = s.LatitudOrigen,
                LongitudOrigen = s.LongitudOrigen,
                DireccionDestino = s.DireccionDestino,
                LatitudDestino = s.LatitudDestino,
                LongitudDestino = s.LongitudDestino,
                DistanciaKm = s.DistanciaKm,
                Precio = s.Precio,
                Estado = s.Estado,
                TipoVehiculo = s.TipoVehiculo,
                Cargas = s.Cargas.Select(c => new CargaDto
                {
                    TipoCargaId = c.TipoCargaId,
                    TipoCargaNombre = c.TipoCarga.Nombre,
                    Cantidad = c.Cantidad,
                    PesoKg = c.PesoKg,
                    VolumenM3 = c.VolumenM3
                }).ToList()
            };
        }
    }
}