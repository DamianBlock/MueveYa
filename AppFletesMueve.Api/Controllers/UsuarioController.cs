using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;
using AppFletesMueve.Api.Middleware;

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
        public async Task<IActionResult> Registrar([FromBody] Usuario usuario)
        {
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);
            usuario.TokenSesionHash = null;
            usuario.SesionIniciadaUtc = null;

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(UsuarioDto.FromEntity(usuario));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var usuario = _context.Usuarios
                .FirstOrDefault(x => x.Email == request.Email);

            if (usuario == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password))
            {
                return Unauthorized(new
                {
                    mensaje = "Credenciales incorrectas"
                });
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
    }
}