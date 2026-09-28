using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TiposCargaController : ControllerBase
    {
        private readonly MueveDbContext _context;

        public TiposCargaController(MueveDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TipoCargaDto>>> Listar()
        {
            var tipos = await _context.TiposCarga
                .AsNoTracking()
                .OrderBy(t => t.Nombre)
                .Select(t => new TipoCargaDto
                {
                    TipoCargaId = t.TipoCargaId,
                    Nombre = t.Nombre,
                    PesoEstimadoKg = t.PesoEstimadoKg,
                    VolumenEstimadoM3 = t.VolumenEstimadoM3
                })
                .ToListAsync();

            return Ok(tipos);
        }
    }
}