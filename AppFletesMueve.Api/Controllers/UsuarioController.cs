using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;
using AppFletesMueve.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly MueveDbContext _context;

        public UsuarioController(MueveDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] RegistroUsuarioRequest request)
        {
            var error = ValidarRegistro(request);
            if (error != null)
                return BadRequest(new { mensaje = error });

            var email = request.Email.Trim().ToLowerInvariant();
            var existeEmail = await _context.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == email);
            if (existeEmail)
                return Conflict(new { mensaje = "Ya existe una cuenta registrada con ese correo electrónico." });

            var usuario = new Usuario
            {
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                Dni = request.Dni.Trim(),
                Telefono = request.Telefono.Trim(),
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                TipoUsuario = request.TipoUsuario.Trim()
            };

            _context.Usuarios.Add(usuario);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { mensaje = "No se pudo crear la cuenta. Verificá que los datos no estén repetidos." });
            }

            return Ok(UsuarioDto.FromEntity(usuario));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (usuario == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password))
            {
                return Unauthorized(new
                {
                    mensaje = "Credenciales incorrectas"
                });
            }

            if (!string.IsNullOrEmpty(usuario.TokenSesionHash) && !request.ConfirmarReemplazoSesion)
            {
                return Conflict(new
                {
                    codigo = "SESION_ACTIVA",
                    mensaje = "Ya hay una sesión activa en otro dispositivo."
                });
            }

            // Una nueva sesión de conductor comienza fuera de servicio, salvo que tenga
            // un viaje activo que debe conservarse hasta que se finalice.
            var conductor = await _context.Conductores
                .Include(c => c.Vehiculos)
                .FirstOrDefaultAsync(c => c.UsuarioId == usuario.UsuarioId);
            if (conductor != null)
            {
                var tieneViajeActivo = await _context.SolicitudesFlete.AnyAsync(s =>
                    s.ConductorId == conductor.ConductorId && EstadosViaje.Activos.Contains(s.Estado));

                if (!tieneViajeActivo)
                {
                    conductor.Disponible = false;
                    foreach (var vehiculo in conductor.Vehiculos)
                        vehiculo.Disponible = false;
                }
            }

            var token = SesionTokens.Generar();
            usuario.TokenSesionHash = SesionTokens.Hash(token);
            usuario.SesionIniciadaUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var respuesta = UsuarioDto.FromEntity(usuario);
            respuesta.TokenSesion = token;
            return Ok(respuesta);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["X-Sesion"].ToString();

            if (int.TryParse(Request.Headers["X-Usuario-Id"].ToString(), out var usuarioId) && !string.IsNullOrEmpty(token))
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario != null && usuario.TokenSesionHash == SesionTokens.Hash(token))
                {
                    usuario.TokenSesionHash = null;
                    usuario.SesionIniciadaUtc = null;
                    await _context.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        private static string? ValidarRegistro(RegistroUsuarioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.Apellido) ||
                string.IsNullOrWhiteSpace(request.Dni) ||
                string.IsNullOrWhiteSpace(request.Telefono) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.TipoUsuario))
                return "Completá todos los campos para crear tu cuenta.";

            if (!new EmailAddressAttribute().IsValid(request.Email))
                return "Ingresá un correo electrónico válido.";

            if (request.Password.Length < 6 || request.Password.Length > 72)
                return "La contraseña debe tener entre 6 y 72 caracteres.";

            if (request.TipoUsuario is not "CLIENTE" and not "CONDUCTOR")
                return "Seleccioná si vas a usar MUEVE como cliente o conductor.";

            return null;
        }
    }
}
