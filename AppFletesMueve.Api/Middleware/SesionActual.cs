namespace AppFletesMueve.Api.Middleware;

public static class SesionActual
{
    internal const string UsuarioIdItem = "Sesion.UsuarioId";

    public static bool EsUsuarioActual(HttpContext context, int usuarioId) =>
        context.Items.TryGetValue(UsuarioIdItem, out var actual) && actual is int id && id == usuarioId;

    public static bool TryObtenerUsuarioId(HttpContext context, out int usuarioId)
    {
        if (context.Items.TryGetValue(UsuarioIdItem, out var actual) && actual is int id)
        {
            usuarioId = id;
            return true;
        }

        usuarioId = 0;
        return false;
    }
}
