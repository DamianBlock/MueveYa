using AppFletesMueve.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // GET: api/Chat/unread-count/123?forUser=Nombre
        [HttpGet("unread-count/{solicitudId}")]
        public async Task<IActionResult> GetUnreadCount(int solicitudId, [FromQuery] string? forUser)
        {
            var query = _context.ChatMessages.AsQueryable()
                .Where(m => m.SolicitudFleteId == solicitudId && !m.IsRead);

            if (!string.IsNullOrWhiteSpace(forUser))
            {
                query = query.Where(m => m.Sender != forUser);
            }

            var count = await query.CountAsync();
            return Ok(new { count });
        }

        // GET: api/Chat/solicitud/123?skip=0&take=50
        [HttpGet("solicitud/{solicitudId}")]
        public async Task<IActionResult> GetMessages(int solicitudId, int skip = 0, int take = 50)
        {
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
        public async Task<IActionResult> MarkRead(int chatMessageId, [FromQuery] string readBy)
        {
            var msg = await _context.ChatMessages.FindAsync(chatMessageId);
            if (msg == null) return NotFound();

            msg.IsRead = true;
            msg.ReadTimestampUtc = DateTime.UtcNow;
            msg.ReadBy = readBy;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/Chat/mark-read-by-solicitud/123?before=2026-01-01T00:00:00Z&readBy=Nombre
        [HttpPost("mark-read-by-solicitud/{solicitudId}")]
        public async Task<IActionResult> MarkReadBySolicitud(int solicitudId, [FromQuery] DateTime before, [FromQuery] string readBy)
        {
            // Normalizar a UTC
            var beforeUtc = DateTime.SpecifyKind(before, DateTimeKind.Utc);

            var mensajes = await _context.ChatMessages
                .Where(m => m.SolicitudFleteId == solicitudId && !m.IsRead && m.TimestampUtc <= beforeUtc)
                .ToListAsync();

            if (!mensajes.Any()) return Ok(new { updated = 0 });

            foreach (var m in mensajes)
            {
                m.IsRead = true;
                m.ReadTimestampUtc = DateTime.UtcNow;
                m.ReadBy = readBy;
            }

            await _context.SaveChangesAsync();
            return Ok(new { updated = mensajes.Count });
        }
    }
}
