using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AppFletesMueve.Api.Hubs;

public class SolicitudesHub : Hub
{
    private readonly ILogger<SolicitudesHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SolicitudesHub(ILogger<SolicitudesHub> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    // Método que puede ser invocado por un conductor para enviar su ubicación
    public async Task ActualizarUbicacion(int solicitudId, double lat, double lon)
    {
        // Enviar la ubicación solamente al grupo de la solicitud
        var groupName = $"solicitud-{solicitudId}";
        await Clients.Group(groupName).SendAsync("UbicacionConductor", solicitudId, lat, lon);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    // Enviar mensaje de chat al grupo de la solicitud
    public async Task SendMessageToGroup(string groupName, string user, string message)
    {
        // Enviar mensaje en tiempo real
        await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message, DateTime.UtcNow);

        // Intentar persistir el mensaje en la base de datos usando un scope DI
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppFletesMueve.Api.Data.MueveDbContext>();
            if (db == null)
            {
                _logger.LogWarning("No DbContext disponible para persistir chat");
                return;
            }

            // extraer solicitud id del groupName si sigue el formato 'solicitud-{id}'
            if (groupName.StartsWith("solicitud-") && int.TryParse(groupName.Substring("solicitud-".Length), out var sid))
            {
                var chat = new AppFletesMueve.Api.Models.ChatMessage
                {
                    SolicitudFleteId = sid,
                    Sender = user,
                    Message = message,
                    TimestampUtc = DateTime.UtcNow,
                    IsRead = false
                };
                try
                {
                    db.ChatMessages.Add(chat);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando ChatMessage para solicitud {SolicitudId}", sid);
                    // no rethrow: no queremos que un error de persistencia rompa la comunicación en tiempo real
                }

                // Calcular conteo de mensajes no leídos para otros usuarios y notificar al grupo
                try
                {
                    var unreadCount = await db.ChatMessages.Where(m => m.SolicitudFleteId == sid && !m.IsRead && m.Sender != user).CountAsync();
                    await Clients.Group(groupName).SendAsync("UnreadCountUpdated", unreadCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error enviando UnreadCountUpdated para solicitud {SolicitudId}", sid);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo guardar mensaje chat para group {Group}", groupName);
        }
    }
}
