using System;
using System.Collections.Generic;
using System.Text;

namespace AppFletesMueve.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Dni { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // CLIENTE o CONDUCTOR
        public string TipoUsuario { get; set; } = string.Empty;
    }
}
