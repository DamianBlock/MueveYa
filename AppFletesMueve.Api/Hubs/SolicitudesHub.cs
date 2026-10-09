using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Hubs;

public class SolicitudesHub : Hub
{
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

        // Intentar persistir el mensaje en la base de datos si el contexto está disponible via DI
        try
        {
            var httpContext = Context.GetHttpContext();
            var db = httpContext?.RequestServices.GetService(typeof(AppFletesMueve.Api.Data.MueveDbContext)) as AppFletesMueve.Api.Data.MueveDbContext;
            if (db != null)
            {
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
                    db.ChatMessages.Add(chat);
                    await db.SaveChangesAsync();

                    // Calcular conteo de mensajes no leídos para otros usuarios y notificar al grupo
                    try
                    {
                        var unreadCount = await db.ChatMessages.Where(m => m.SolicitudFleteId == sid && !m.IsRead && m.Sender != user).CountAsync();
                        await Clients.Group(groupName).SendAsync("UnreadCountUpdated", unreadCount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error enviando UnreadCountUpdated: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"No se pudo guardar mensaje chat: {ex}");
        }
    }
}
