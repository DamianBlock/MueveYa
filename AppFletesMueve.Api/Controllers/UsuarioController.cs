using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;
using AppFletesMueve.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Cryptography;
using AppFletesMueve.Api.Services;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly MueveDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            MueveDbContext context,
            IEmailSender emailSender,
            ILogger<UsuarioController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("registro")]
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
                TipoUsuario = request.TipoUsuario.Trim(),
                FechaRegistroUtc = DateTime.UtcNow
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
        [EnableRateLimiting("login")]
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

        [HttpPost("recuperar-password")]
        [EnableRateLimiting("recuperacion-solicitud")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperacionPasswordRequest request)
        {
            // La respuesta es intencionalmente idéntica para no revelar qué correos tienen cuenta.
            const string mensaje = "Si existe una cuenta con ese correo, enviamos un código de recuperación.";
            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
                return Accepted(new { mensaje });

            var email = request.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (usuario == null)
                return Accepted(new { mensaje });

            var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            usuario.CodigoRecuperacionHash = SesionTokens.Hash(codigo);
            usuario.CodigoRecuperacionVenceUtc = DateTime.UtcNow.AddMinutes(15);
            usuario.IntentosCodigoRecuperacion = 0;
            await _context.SaveChangesAsync();

            var enviado = await _emailSender.EnviarCodigoRecuperacionAsync(usuario.Email, codigo);
            if (!enviado)
                _logger.LogWarning("No se pudo entregar el código de recuperación para una cuenta.");

            return Accepted(new { mensaje });
        }

        [HttpPost("restablecer-password")]
        [EnableRateLimiting("recuperacion-confirmacion")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Codigo) ||
                string.IsNullOrWhiteSpace(request.PasswordNueva) ||
                !new EmailAddressAttribute().IsValid(request.Email) ||
                request.Codigo.Length != 6 || !request.Codigo.All(char.IsAsciiDigit) ||
                request.PasswordNueva.Length < 6 || request.PasswordNueva.Length > 72)
            {
                return BadRequest(new { mensaje = "Verificá el código y usá una contraseña de entre 6 y 72 caracteres." });
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            var ahora = DateTime.UtcNow;

            if (usuario == null ||
                string.IsNullOrEmpty(usuario.CodigoRecuperacionHash) ||
                usuario.CodigoRecuperacionVenceUtc <= ahora ||
                usuario.IntentosCodigoRecuperacion >= 5 ||
                !SesionTokens.Coinciden(usuario.CodigoRecuperacionHash, SesionTokens.Hash(request.Codigo)))
            {
                if (usuario != null && usuario.CodigoRecuperacionHash != null)
                {
                    usuario.IntentosCodigoRecuperacion++;
                    if (usuario.IntentosCodigoRecuperacion >= 5 || usuario.CodigoRecuperacionVenceUtc <= ahora)
                        LimpiarCodigoRecuperacion(usuario);
                    await _context.SaveChangesAsync();
                }

                return BadRequest(new { mensaje = "El código es inválido, venció o superó el máximo de intentos." });
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
            usuario.TokenSesionHash = null;
            usuario.SesionIniciadaUtc = null;
            LimpiarCodigoRecuperacion(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("solicitar-eliminacion")]
        public async Task<IActionResult> SolicitarEliminacion()
        {
            var token = Request.Headers["X-Sesion"].ToString();
            if (!int.TryParse(Request.Headers["X-Usuario-Id"].ToString(), out var usuarioId) || string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { mensaje = "Necesitás una sesión válida para solicitar la eliminación." });

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null || string.IsNullOrEmpty(usuario.TokenSesionHash) ||
                !SesionTokens.Coinciden(usuario.TokenSesionHash, SesionTokens.Hash(token)))
                return Unauthorized(new { mensaje = "Tu sesión ya no es válida." });

            if (usuario.SolicitudEliminacionUtc is null)
            {
                usuario.SolicitudEliminacionUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Accepted(new { mensaje = "Registramos tu solicitud. El equipo de MUEVE se contactará para confirmar la eliminación y atender obligaciones legales o reclamos pendientes." });
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

        private static void LimpiarCodigoRecuperacion(Usuario usuario)
        {
            usuario.CodigoRecuperacionHash = null;
            usuario.CodigoRecuperacionVenceUtc = null;
            usuario.IntentosCodigoRecuperacion = 0;
        }
    }
}
