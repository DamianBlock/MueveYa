namespace AppFletesMueve.Api.Models
{
    public enum TipoVehiculo
    {
        Utilitario,
        Camioneta,
        CamionChico,
        CamionMediano,
        CamionGrande
    }

    public enum TipoServicio
    {
        Inmediato,
        Programado
    }

    public enum EstadoSolicitud
    {
        Pendiente,
        BuscandoConductor,
        Aceptada,
        EnCurso,
        Completada,
        Cancelada
    }
}
