namespace AppFletesMueve.Api.Services
{
    public interface ICalculadoraTarifas
    {
        decimal Calcular(double distanciaKm, double pesoTotalKg, Models.TipoServicio tipoServicio);
    }

    public class CalculadoraTarifas : ICalculadoraTarifas
    {
        private const decimal PrecioBase = 1500m;
        private const decimal PrecioPorKm = 350m;
        private const decimal PrecioPorKg = 15m;
        private const decimal RecargoProgramado = 1.15m;

        public decimal Calcular(double distanciaKm, double pesoTotalKg, Models.TipoServicio tipoServicio)
        {
            var precio = PrecioBase
                + (decimal)distanciaKm * PrecioPorKm
                + (decimal)pesoTotalKg * PrecioPorKg;

            if (tipoServicio == Models.TipoServicio.Programado)
                precio *= RecargoProgramado;

            return Math.Round(precio, 2);
        }
    }
}