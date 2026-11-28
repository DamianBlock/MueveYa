using AppFletesMueve.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppFletesMueve.Api.Middleware;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly MueveDbContext _context;

        public ChatController(MueveDbContext context)
        {
            _context = context;
        }

        [HttpGet("unread-count/{solicitudId}")]
        public async Task<IActionResult> GetUnreadCount(int solicitudId)
        {
            var nombre = await NombreUsuarioActualAsync();
            if (nombre is null || !await PuedeAccederSolicitudAsync(solicitudId)) return Forbid();

            var query = _context.ChatMessages.AsQueryable()
                .Where(m => m.SolicitudFleteId == solicitudId && !m.IsRead && m.Sender != nombre);

            var count = await query.CountAsync();
            return Ok(new { count });
        }

        // GET: api/Chat/solicitud/123?skip=0&take=50
        [HttpGet("solicitud/{solicitudId}")]
        public async Task<IActionResult> GetMessages(int solicitudId, int skip = 0, int take = 50)
        {
            if (!await PuedeAccederSolicitudAsync(solicitudId)) return Forbid();
            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 100);
            var total = await _context.ChatMessages.CountAsync(m => m.SolicitudFleteId == solicitudId);

            // Obtener mensajes más recientes primero, aplicar skip/take y luego devolver en orden ascendente
            var items = await _context.ChatMessages
                .Where(m => m.SolicitudFleteId == solicitudId)
                .OrderByDescending(m => m.TimestampUtc)
                .Skip(skip)
                .Take(take)
                .Select(m => new { m.ChatMessageId, m.Sender, m.Message, m.TimestampUtc, m.IsRead, m.ReadTimestampUtc, m.ReadBy })
                .ToListAsync();

            items.Reverse();

            return Ok(new { total, items });
        }

        // POST: api/Chat/mark-read/{chatMessageId}
        [HttpPost("mark-read/{chatMessageId}")]
        public async Task<IActionResult> MarkRead(int chatMessageId)
        {
            var msg = await _context.ChatMessages.FindAsync(chatMessageId);
            if (msg == null) return NotFound();
            var nombre = await NombreUsuarioActualAsync();
            if (nombre is null || !await PuedeAccederSolicitudAsync(msg.SolicitudFleteId)) return Forbid();

            msg.IsRead = true;
            msg.ReadTimestampUtc = DateTime.UtcNow;
            msg.ReadBy = nombre;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("mark-read-by-solicitud/{solicitudId}")]
        public async Task<IActionResult> MarkReadBySolicitud(int solicitudId, [FromQuery] DateTime before)
        {
            var nombre = await NombreUsuarioActualAsync();
            if (nombre is null || !await PuedeAccederSolicitudAsync(solicitudId)) return Forbid();
            // Normalizar a UTC
            var beforeUtc = DateTime.SpecifyKind(before, DateTimeKind.Utc);

            var mensajes = await _context.ChatMessages
                .Where(m => m.SolicitudFleteId == solicitudId && !m.IsRead && m.TimestampUtc <= beforeUtc && m.Sender != nombre)
                .ToListAsync();

            if (!mensajes.Any()) return Ok(new { updated = 0 });

            foreach (var m in mensajes)
            {
                m.IsRead = true;
                m.ReadTimestampUtc = DateTime.UtcNow;
                m.ReadBy = nombre;
            }

            await _context.SaveChangesAsync();
            return Ok(new { updated = mensajes.Count });
        }

        private async Task<bool> PuedeAccederSolicitudAsync(int solicitudId)
        {
            if (!SesionActual.TryObtenerUsuarioId(HttpContext, out var usuarioId)) return false;
            return await _context.SolicitudesFlete.AsNoTracking().AnyAsync(s => s.SolicitudFleteId == solicitudId &&
                (s.ClienteId == usuarioId || (s.Conductor != null && s.Conductor.UsuarioId == usuarioId)));
        }

        private async Task<string?> NombreUsuarioActualAsync()
        {
            if (!SesionActual.TryObtenerUsuarioId(HttpContext, out var usuarioId)) return null;
            return await _context.Usuarios.AsNoTracking().Where(u => u.UsuarioId == usuarioId)
                .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync();
        }
    }
}
