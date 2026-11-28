using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AppFletesMueve.Api.Middleware;
using AppFletesMueve.Api.Models;

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
        if (double.IsNaN(lat) || double.IsNaN(lon) ||
            double.IsInfinity(lat) || double.IsInfinity(lon) ||
            lat is < -90 or > 90 || lon is < -180 or > 180)
            throw new HubException("La ubicación enviada no es válida.");

        if (!await EsConductorDelViajeAsync(solicitudId))
            throw new HubException("No estás autorizado para actualizar esta ubicación.");

        // Persistir la última posición permite verificar en la API que el conductor
        // realmente llegó al punto de recogida antes de iniciar la carga.
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppFletesMueve.Api.Data.MueveDbContext>();
            var conductor = await db.SolicitudesFlete
                .Where(s => s.SolicitudFleteId == solicitudId)
                .Select(s => s.Conductor)
                .FirstOrDefaultAsync();

            if (conductor is null)
                throw new HubException("No se encontró el conductor del viaje.");

            conductor.Latitud = lat;
            conductor.Longitud = lon;
            await db.SaveChangesAsync();
        }

        // Enviar la ubicación solamente al grupo de la solicitud
        var groupName = $"solicitud-{solicitudId}";
        await Clients.Group(groupName).SendAsync("UbicacionConductor", solicitudId, lat, lon);
    }

    public async Task JoinGroup(string groupName)
    {
        if (!await PuedeUnirseAlGrupoAsync(groupName))
            throw new HubException("No estás autorizado para este grupo.");

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    // Enviar mensaje de chat al grupo de la solicitud
    public async Task SendMessageToGroup(string groupName, string user, string message)
    {
        if (!await PuedeUnirseAlGrupoAsync(groupName))
            throw new HubException("No estás autorizado para este chat.");
        if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
            throw new HubException("El mensaje no es válido.");

        var nombre = await NombreUsuarioActualAsync();
        if (nombre is null)
            throw new HubException("Sesión inválida.");

        // Enviar mensaje en tiempo real
        await Clients.Group(groupName).SendAsync("ReceiveMessage", nombre, message.Trim(), DateTime.UtcNow);

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
                    Sender = nombre,
                    Message = message.Trim(),
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
                    var unreadCount = await db.ChatMessages.Where(m => m.SolicitudFleteId == sid && !m.IsRead && m.Sender != nombre).CountAsync();
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

    private bool TryUsuarioActual(out int usuarioId) =>
        SesionActual.TryObtenerUsuarioId(Context.GetHttpContext()!, out usuarioId);

    private async Task<bool> PuedeUnirseAlGrupoAsync(string groupName)
    {
        if (!TryUsuarioActual(out var usuarioId)) return false;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppFletesMueve.Api.Data.MueveDbContext>();

        if (groupName.StartsWith("solicitud-") && int.TryParse(groupName["solicitud-".Length..], out var solicitudId))
            return await db.SolicitudesFlete.AsNoTracking().AnyAsync(s => s.SolicitudFleteId == solicitudId &&
                (s.ClienteId == usuarioId || (s.Conductor != null && s.Conductor.UsuarioId == usuarioId)));

        if (groupName.StartsWith("drivers-") && Enum.TryParse<TipoVehiculo>(groupName["drivers-".Length..], out var tipo))
            return await db.Conductores.AsNoTracking().AnyAsync(c => c.UsuarioId == usuarioId &&
                c.Vehiculos.Any(v => v.Activo && v.TipoVehiculo == tipo));

        return false;
    }

    private async Task<bool> EsConductorDelViajeAsync(int solicitudId)
    {
        if (!TryUsuarioActual(out var usuarioId)) return false;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppFletesMueve.Api.Data.MueveDbContext>();
        return await db.SolicitudesFlete.AsNoTracking().AnyAsync(s => s.SolicitudFleteId == solicitudId &&
            s.Conductor != null && s.Conductor.UsuarioId == usuarioId);
    }

    private async Task<string?> NombreUsuarioActualAsync()
    {
        if (!TryUsuarioActual(out var usuarioId)) return null;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppFletesMueve.Api.Data.MueveDbContext>();
        return await db.Usuarios.AsNoTracking().Where(u => u.UsuarioId == usuarioId)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync();
    }
}
