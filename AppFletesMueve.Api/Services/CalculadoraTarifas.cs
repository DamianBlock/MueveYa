using AppFletesMueve.Api.Models;

namespace AppFletesMueve.Api.Services
{
    public record PerfilVehiculo(
        TipoVehiculo Tipo,
        string Nombre,
        double CapacidadKg,
        double CapacidadM3,
        decimal TarifaBase,
        decimal PrecioPorKm,
        decimal PrecioMinutoEspera);

    public interface ICalculadoraTarifas
    {
        int MinutosEsperaCarga { get; }
        int MinutosEsperaDescarga { get; }

        // De menor a mayor capacidad
        IReadOnlyList<PerfilVehiculo> Perfiles { get; }

        PerfilVehiculo ObtenerPerfil(TipoVehiculo tipo);

        decimal Calcular(double distanciaKm, double pesoTotalKg, TipoServicio tipoServicio, TipoVehiculo tipoVehiculo);
    }

    public class CalculadoraTarifas : ICalculadoraTarifas
    {
        private const decimal PrecioPorKg = 15m;
        private const decimal RecargoProgramado = 1.15m;

        public int MinutosEsperaCarga => 15;
        public int MinutosEsperaDescarga => 15;

        // VALORES DE EJEMPLO (ARS): ajustalos acá.
        //                          tipo                       nombre                          kg     m3   base   $/km  $/min espera
        public IReadOnlyList<PerfilVehiculo> Perfiles { get; } = new List<PerfilVehiculo>
        {
            new(TipoVehiculo.Utilitario,    "Utilitario",                 500,   2,  1500,  350,  40),
            new(TipoVehiculo.Camioneta,     "Camioneta / Pick-up",       1000,   5,  2200,  450,  55),
            new(TipoVehiculo.CamionChico,   "Camión chico",              2500,  12,  3500,  600,  80),
            new(TipoVehiculo.CamionMediano, "Camión mediano",            5000,  25,  5000,  800, 110),
            new(TipoVehiculo.CamionGrande,  "Camión grande / Mudanza",  10000,  45,  7500, 1100, 150)
        };

        public PerfilVehiculo ObtenerPerfil(TipoVehiculo tipo)
        {
            return Perfiles.First(p => p.Tipo == tipo);
        }

        public decimal Calcular(double distanciaKm, double pesoTotalKg, TipoServicio tipoServicio, TipoVehiculo tipoVehiculo)
        {
            var perfil = ObtenerPerfil(tipoVehiculo);

            var espera = (MinutosEsperaCarga + MinutosEsperaDescarga) * perfil.PrecioMinutoEspera;

            var precio = perfil.TarifaBase
                + (decimal)distanciaKm * perfil.PrecioPorKm
                + (decimal)pesoTotalKg * PrecioPorKg
                + espera;

            if (tipoServicio == TipoServicio.Programado)
                precio *= RecargoProgramado;

            return Math.Round(precio, 0, MidpointRounding.AwayFromZero);
        }
    }
}