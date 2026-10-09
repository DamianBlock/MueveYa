using AppFletesMueve.Api.Data;
using AppFletesMueve.Api.Dtos;
using AppFletesMueve.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarifasController : ControllerBase
    {
        private readonly MueveDbContext _context;
        private readonly ICalculadoraTarifas _tarifas;

        public TarifasController(MueveDbContext context, ICalculadoraTarifas tarifas)
        {
            _context = context;
            _tarifas = tarifas;
        }

        [HttpPost("cotizar")]
        public async Task<ActionResult<CotizacionDto>> Cotizar(CotizarTarifaDto dto)
        {
            var ids = dto.Cargas.Select(c => c.TipoCargaId).Distinct().ToList();

            var tipos = await _context.TiposCarga
                .AsNoTracking()
                .Where(t => ids.Contains(t.TipoCargaId))
                .ToDictionaryAsync(t => t.TipoCargaId);

            if (tipos.Count != ids.Count)
                return BadRequest(new { mensaje = "Hay un tipo de carga inválido" });

            var pesoTotal = dto.Cargas.Sum(c => tipos[c.TipoCargaId].PesoEstimadoKg * c.Cantidad);
            var volumenTotal = dto.Cargas.Sum(c => tipos[c.TipoCargaId].VolumenEstimadoM3 * c.Cantidad);

            var opciones = _tarifas.Perfiles.Select(p => new OpcionTarifaDto
            {
                TipoVehiculo = p.Tipo,
                Nombre = p.Nombre,
                CapacidadKg = p.CapacidadKg,
                CapacidadM3 = p.CapacidadM3,
                Precio = _tarifas.Calcular(dto.DistanciaKm, pesoTotal, dto.TipoServicio, p.Tipo),
                Entra = pesoTotal <= p.CapacidadKg && volumenTotal <= p.CapacidadM3
            }).ToList();

            // Sugerido: el más chico donde entra la carga
            var sugerida = opciones.FirstOrDefault(o => o.Entra);
            if (sugerida != null)
                sugerida.Sugerido = true;

            return Ok(new CotizacionDto
            {
                PesoTotalKg = pesoTotal,
                VolumenTotalM3 = volumenTotal,
                MinutosEspera = _tarifas.MinutosEsperaCarga + _tarifas.MinutosEsperaDescarga,
                Opciones = opciones
            });
        }
    }
}