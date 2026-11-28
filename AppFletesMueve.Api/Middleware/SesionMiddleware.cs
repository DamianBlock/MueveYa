using System.Security.Cryptography;
using System.Text;
using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AppFletesMueve.Api.Middleware
{
    // Token de sesión: se entrega UNA vez al iniciar sesión y en la base solo se guarda su hash,
    // así una filtración de la base no permite usar sesiones activas.
    public static class SesionTokens
    {
        public static string Generar() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        public static bool Coinciden(string esperado, string recibido) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(recibido));
    }

    // Solo vale la última sesión iniciada por cada usuario: al entrar desde otro dispositivo,
    // el token anterior deja de funcionar y la app vieja recibe un 401.
    public class SesionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly bool _obligatoria;

        public SesionMiddleware(RequestDelegate next, IConfiguration configuracion)
        {
            _next = next;

            // En producción no se admite el modo de transición: todo endpoint protegido
            // requiere una sesión vigente. Development puede configurarlo explícitamente.
            _obligatoria = configuracion.GetValue("Sesion:Obligatoria", true);
        }

        public async Task InvokeAsync(HttpContext context, MueveDbContext db)
        {
            var ruta = (context.Request.Path.Value ?? string.Empty).TrimEnd('/').ToLowerInvariant();

            var protegida = ruta.StartsWith("/api/") || ruta.StartsWith("/hubs/");
            var publica = ruta == "/api/usuario/login"
                          || (ruta == "/api/usuario" && HttpMethods.IsPost(context.Request.Method))
                          || ruta == "/api/usuario/recuperar-password"
                          || ruta == "/api/usuario/restablecer-password";

            if (!protegida || publica || HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var usuarioTexto = context.Request.Headers["X-Usuario-Id"].ToString();
            var token = context.Request.Headers["X-Sesion"].ToString();
            var hayCredenciales = int.TryParse(usuarioTexto, out var usuarioId) && !string.IsNullOrEmpty(token);

            if (!hayCredenciales)
            {
                if (_obligatoria)
                {
                    await RechazarAsync(context);
                    return;
                }

                await _next(context);
                return;
            }

            var hashGuardado = await db.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => u.TokenSesionHash)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(hashGuardado) || !SesionTokens.Coinciden(hashGuardado, SesionTokens.Hash(token)))
            {
                await RechazarAsync(context);
                return;
            }

            context.Items[SesionActual.UsuarioIdItem] = usuarioId;

            await _next(context);
        }

        private static Task RechazarAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.WriteAsJsonAsync(new
            {
                codigo = "SESION_INVALIDA",
                mensaje = "Tu sesión se cerró porque iniciaste sesión en otro dispositivo."
            });
        }
    }
}
