namespace AppFletesMueve.Api.Models
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

        public string TipoUsuario { get; set; } = string.Empty;
        // Solo se completa para las cuentas creadas después de incorporar promociones.
        // Las cuentas históricas no reciben una fecha artificial de alta.
        public DateTime? FechaRegistroUtc { get; set; }
        public DateTime? SolicitudEliminacionUtc { get; set; }
        public string? TokenSesionHash { get; set; }
        public DateTime? SesionIniciadaUtc { get; set; }
        public string? TokenSesion { get; set; }

        // El código nunca se persiste en texto plano. Sólo existe durante 15 minutos.
        public string? CodigoRecuperacionHash { get; set; }
        public DateTime? CodigoRecuperacionVenceUtc { get; set; }
        public int IntentosCodigoRecuperacion { get; set; }
    }
}
