namespace AppFletesMueve.Api.Models
{
    public static class EstadosViaje
    {
        // Viaje aceptado por un conductor y todavía sin terminar
        public static readonly EstadoSolicitud[] Activos =
        {
            EstadoSolicitud.Aceptada,
            EstadoSolicitud.EnOrigen,
            EstadoSolicitud.EnCurso,
            EstadoSolicitud.EnDestino
        };
    }
}