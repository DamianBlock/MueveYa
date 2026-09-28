using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConductoresController : ControllerBase
    {
        private readonly MueveDbContext _context;

        public ConductoresController(MueveDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ConductorDto>> Crear(CrearConductorDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
            if (usuario is null)
                return NotFound(new { mensaje = "El usuario no existe" });

            var yaExiste = await _context.Conductores
                .AnyAsync(c => c.UsuarioId == dto.UsuarioId);
            if (yaExiste)
                return Conflict(new { mensaje = "Ese usuario ya tiene un perfil de conductor" });

            var conductor = new Conductor
            {
                UsuarioId = dto.UsuarioId,
                Licencia = dto.Licencia.Trim(),
                Disponible = false
            };

            _context.Conductores.Add(conductor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ObtenerPorUsuario),
                new { usuarioId = conductor.UsuarioId },
                ToDto(conductor, usuario));
        }

        [HttpGet("por-usuario/{usuarioId:int}")]
        public async Task<ActionResult<ConductorDto>> ObtenerPorUsuario(int usuarioId)
        {
            var conductor = await _context.Conductores
                .AsNoTracking()
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

            return conductor is null
                ? NotFound()
                : Ok(ToDto(conductor, conductor.Usuario));
        }

        [HttpPut("{id:int}/estado")]
        public async Task<ActionResult<ConductorDto>> ActualizarEstado(
            int id, ActualizarEstadoConductorDto dto)
        {
            var conductor = await _context.Conductores
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.ConductorId == id);

            if (conductor is null)
                return NotFound();

            conductor.Disponible = dto.Disponible;
            if (dto.Latitud.HasValue) conductor.Latitud = dto.Latitud.Value;
            if (dto.Longitud.HasValue) conductor.Longitud = dto.Longitud.Value;

            await _context.SaveChangesAsync();
            return Ok(ToDto(conductor, conductor.Usuario));
        }

        private static ConductorDto ToDto(Conductor c, Usuario u) => new()
        {
            ConductorId = c.ConductorId,
            UsuarioId = c.UsuarioId,
            Nombre = $"{u.Nombre} {u.Apellido}".Trim(),
            Licencia = c.Licencia,
            Disponible = c.Disponible,
            Latitud = c.Latitud,
            Longitud = c.Longitud
        };
    }
}