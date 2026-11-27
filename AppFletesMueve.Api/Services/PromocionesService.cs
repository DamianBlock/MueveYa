using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Services;

public interface IPromocionesService
{
    Task<PromocionPrimerViajeDto> ObtenerPrimerViajeAsync(Usuario cliente);
    decimal CalcularDescuentoPrimerViaje(decimal precioTarifa);
}

public sealed class PromocionesService : IPromocionesService
{
    public const decimal PorcentajePrimerViaje = 0.50m;
    public const decimal TopePrimerViaje = 10_000m;
    public const int VigenciaPrimerViajeDias = 30;

    private readonly MueveDbContext _context;

    public PromocionesService(MueveDbContext context) => _context = context;

    public async Task<PromocionPrimerViajeDto> ObtenerPrimerViajeAsync(Usuario cliente)
    {
        var resultado = new PromocionPrimerViajeDto
        {
            PorcentajeDescuento = PorcentajePrimerViaje * 100,
            TopeDescuento = TopePrimerViaje
        };

        if (!string.Equals(cliente.TipoUsuario, "CLIENTE", StringComparison.Ordinal))
        {
            resultado.Mensaje = "Este beneficio es exclusivo para clientes.";
            return resultado;
        }

        // No se inventa una fecha para cuentas creadas antes de esta promoción.
        if (cliente.FechaRegistroUtc is not DateTime fechaRegistro)
        {
            resultado.Mensaje = "La promoción se aplica a cuentas nuevas registradas desde su lanzamiento.";
            return resultado;
        }

        resultado.VenceUtc = fechaRegistro.AddDays(VigenciaPrimerViajeDias);
        if (DateTime.UtcNow >= resultado.VenceUtc.Value)
        {
            resultado.Mensaje = "La vigencia de 30 días de esta promoción ya finalizó.";
            return resultado;
        }

        var yaCompletoViaje = await _context.SolicitudesFlete
            .AsNoTracking()
            .AnyAsync(s => s.ClienteId == cliente.UsuarioId && s.Estado == EstadoSolicitud.Completada);
        if (yaCompletoViaje)
        {
            resultado.Mensaje = "La promoción es válida únicamente para tu primer viaje completado.";
            return resultado;
        }

        resultado.Disponible = true;
        resultado.Mensaje = "Tenés 50% de descuento en tu primer viaje, con un tope de $10.000. MUEVE cubre la diferencia y el conductor recibe la tarifa completa.";
        return resultado;
    }

    public decimal CalcularDescuentoPrimerViaje(decimal precioTarifa) =>
        Math.Min(decimal.Round(precioTarifa * PorcentajePrimerViaje, 0, MidpointRounding.AwayFromZero), TopePrimerViaje);
}
