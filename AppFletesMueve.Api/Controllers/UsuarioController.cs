using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Models;
using Microsoft.AspNetCore.Mvc;

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

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(UsuarioDto.FromEntity(usuario));
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
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

            return Ok(UsuarioDto.FromEntity(usuario));
        }
    }
}