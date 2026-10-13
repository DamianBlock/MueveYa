namespace AppFletesMueve.Models
{
    public static class MotivosCancelacion
    {
        public static readonly string[] Cliente =
        {
            "Ya no necesito el flete",
            "Lo pedí por error",
            "El conductor tarda demasiado",
            "El conductor me pidió cancelar",
            "Cambié la dirección o la carga",
            "Otro motivo"
        };

        public static readonly string[] Conductor =
        {
            "Problema mecánico",
            "Accidente",
            "No encuentro la dirección",
            "El cliente no se presenta",
            "La carga no coincide con lo pedido",
            "Otro motivo"
        };
    }
}