// Models/UsuarioDto.cs
using AppFletesMueve.Api.Models;

namespace AppFletesMueve.Api.Models
{
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;

        public static UsuarioDto FromEntity(Usuario u) => new()
        {
            UsuarioId = u.UsuarioId,
            Nombre = u.Nombre,
            Apellido = u.Apellido,
            Email = u.Email,
            TipoUsuario = u.TipoUsuario
        };
    }
}
