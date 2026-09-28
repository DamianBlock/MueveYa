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

            var patenteUsada = await _context.Vehiculos
                .AnyAsync(v => v.Patente == patente);
            if (patenteUsada)
                return Conflict(new { mensaje = "Ya existe un vehículo con esa patente" });

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
                Disponible = true,
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
                .Where(v => v.ConductorId == conductorId)
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
                .Where(v => v.Disponible && v.Conductor.Disponible);

            if (tipo.HasValue)
                query = query.Where(v => v.TipoVehiculo == tipo.Value);

            var vehiculos = await query
                .OrderBy(v => v.CapacidadKg)
                .ToListAsync();

            return Ok(vehiculos.Select(ToDto));
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