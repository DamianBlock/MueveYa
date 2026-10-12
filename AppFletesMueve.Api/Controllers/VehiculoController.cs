using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : ControllerBase
    {
        private readonly MueveDbContext _context;

        public VehiculosController(MueveDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<VehiculoDto>> Crear(CrearVehiculoDto dto)
        {
            var conductorExiste = await _context.Conductores
                .AnyAsync(c => c.ConductorId == dto.ConductorId);
            if (!conductorExiste)
                return NotFound(new { mensaje = "El conductor no existe" });

            var patente = dto.Patente.Replace(" ", "").ToUpperInvariant();

            var existente = await _context.Vehiculos
                .FirstOrDefaultAsync(v => v.Patente == patente);

            if (existente != null)
            {
                // Misma patente que un vehículo que este conductor dio de baja: se reactiva
                if (existente.ConductorId == dto.ConductorId && !existente.Activo)
                {
                    existente.Marca = dto.Marca.Trim();
                    existente.Modelo = dto.Modelo.Trim();
                    existente.Anio = dto.Anio;
                    existente.TipoVehiculo = dto.TipoVehiculo;
                    existente.CapacidadKg = dto.CapacidadKg;
                    existente.VolumenM3 = dto.VolumenM3;
                    existente.Imagen = dto.Imagen ?? existente.Imagen;
                    existente.Activo = true;
                    existente.Disponible = false;

                    await _context.SaveChangesAsync();
                    return Ok(ToDto(existente));
                }

                return Conflict(new { mensaje = "Ya existe un vehículo con esa patente" });
            }

            var vehiculo = new Vehiculo
            {
                ConductorId = dto.ConductorId,
                Patente = patente,
                Marca = dto.Marca.Trim(),
                Modelo = dto.Modelo.Trim(),
                Anio = dto.Anio,
                TipoVehiculo = dto.TipoVehiculo,
                CapacidadKg = dto.CapacidadKg,
                VolumenM3 = dto.VolumenM3,
                // Un vehículo nuevo no está en servicio hasta que el conductor lo elige al iniciar
                Disponible = false,
                Activo = true,
                Imagen = dto.Imagen
            };

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ListarPorConductor),
                new { conductorId = vehiculo.ConductorId },
                ToDto(vehiculo));
        }

        [HttpGet("conductor/{conductorId:int}")]
        public async Task<ActionResult<IEnumerable<VehiculoDto>>> ListarPorConductor(int conductorId)
        {
            var vehiculos = await _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.ConductorId == conductorId && v.Activo)
                .OrderBy(v => v.Patente)
                .ToListAsync();

            return Ok(vehiculos.Select(ToDto));
        }

        // Vehículos que un cliente podría contratar ahora
        [HttpGet("disponibles")]
        public async Task<ActionResult<IEnumerable<VehiculoDto>>> ListarDisponibles(
            [FromQuery] TipoVehiculo? tipo)
        {
            var query = _context.Vehiculos
                .AsNoTracking()
                .Where(v => v.Activo && v.Disponible && v.Conductor.Disponible);

            if (tipo.HasValue)
                query = query.Where(v => v.TipoVehiculo == tipo.Value);

            var vehiculos = await query
                .OrderBy(v => v.CapacidadKg)
                .ToListAsync();

            return Ok(vehiculos.Select(ToDto));
        }

        // Baja lógica: los viajes anteriores siguen referenciando al vehículo
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Conductor)
                .FirstOrDefaultAsync(v => v.VehiculoId == id && v.Activo);

            if (vehiculo is null)
                return NotFound();

            var tieneViajeActivo = await _context.SolicitudesFlete.AnyAsync(s =>
                s.VehiculoId == id && EstadosViaje.Activos.Contains(s.Estado));
            if (tieneViajeActivo)
                return Conflict(new { mensaje = "El vehículo tiene un viaje en curso" });

            if (vehiculo.Conductor.Disponible && vehiculo.Disponible)
                return Conflict(new { mensaje = "Es el vehículo con el que estás trabajando. Detené el servicio antes de darlo de baja." });

            vehiculo.Activo = false;
            vehiculo.Disponible = false;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static VehiculoDto ToDto(Vehiculo v) => new()
        {
            VehiculoId = v.VehiculoId,
            ConductorId = v.ConductorId,
            Patente = v.Patente,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Anio = v.Anio,
            TipoVehiculo = v.TipoVehiculo,
            CapacidadKg = v.CapacidadKg,
            VolumenM3 = v.VolumenM3,
            Disponible = v.Disponible,
            Imagen = v.Imagen
        };
    }
}