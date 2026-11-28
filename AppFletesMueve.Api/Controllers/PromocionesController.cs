using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Services;
using AppFletesMueve.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace AppFletesMueve.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromocionesController : ControllerBase
{
    private readonly MueveDbContext _context;
    private readonly IPromocionesService _promociones;

    public PromocionesController(MueveDbContext context, IPromocionesService promociones)
    {
        _context = context;
        _promociones = promociones;
    }

    [HttpGet("primer-viaje/{clienteId:int}")]
    public async Task<ActionResult<PromocionPrimerViajeDto>> ObtenerPrimerViaje(int clienteId)
    {
        if (!SesionActual.EsUsuarioActual(HttpContext, clienteId))
            return Forbid();

        var cliente = await _context.Usuarios.FindAsync(clienteId);
        return cliente is null ? NotFound() : Ok(await _promociones.ObtenerPrimerViajeAsync(cliente));
    }
}
