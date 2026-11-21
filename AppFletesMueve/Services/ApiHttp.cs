using System.Net;
using AppFletesMueve.Models;
using AppFletesMueve.Views;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace AppFletesMueve.Services
{
    // Agrega usuario + token de sesión a cada llamada a NUESTRA API.
    // No usarlo con servicios de terceros (Google, OSRM): el token no debe salir de la API.
    public class SesionHandler : DelegatingHandler
    {
        // No seguir redirecciones: evita reenviar los encabezados de sesión a otro origen.
        public SesionHandler() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = SesionUsuario.TokenSesion;
            var destinoConfiable = ApiHttp.EsOrigenApiConfiable(request.RequestUri);

            if (destinoConfiable && SesionUsuario.UsuarioId > 0 && !string.IsNullOrEmpty(token))
            {
                request.Headers.Remove("X-Usuario-Id");
                request.Headers.Remove("X-Sesion");
                request.Headers.TryAddWithoutValidation("X-Usuario-Id", SesionUsuario.UsuarioId.ToString());
                request.Headers.TryAddWithoutValidation("X-Sesion", token);
            }

            var respuesta = await base.SendAsync(request, cancellationToken);

            // Con un usuario ya iniciado, un 401 significa que su sesión dejó de valer
            // (por ejemplo, porque entró desde otro dispositivo).
            if (destinoConfiable && SesionUsuario.UsuarioId > 0 && respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                _ = SesionService.CerrarAsync(
                    "Tu sesión se cerró, por ejemplo porque iniciaste sesión en otro dispositivo. Volvé a ingresar.");
            }

            return respuesta;
        }
    }

    public static class ApiHttp
    {
        // Los valores en Preferences no determinan si un origen es confiable.
#if DEBUG
        private static readonly Uri[] OrigenesApiConfiables =
        {
            new("http://10.0.2.2:5051"),
            new("http://localhost:5051"),
            new("https://mueveya.onrender.com")
        };
#else
        private static readonly Uri[] OrigenesApiConfiables =
        {
            new("https://mueveya.onrender.com")
        };
#endif

        public static HttpClient CrearCliente() => new(new SesionHandler());

        // Para las conexiones SignalR: .WithUrl(TransporteService.HubUrl, ApiHttp.ConfigurarHub)
        public static void ConfigurarHub(HttpConnectionOptions opciones)
        {
            if (!EsOrigenApiConfiable(opciones.Url))
                return;

            var token = SesionUsuario.TokenSesion;

            if (SesionUsuario.UsuarioId > 0 && !string.IsNullOrEmpty(token))
            {
                opciones.Headers["X-Usuario-Id"] = SesionUsuario.UsuarioId.ToString();
                opciones.Headers["X-Sesion"] = token;
            }
        }

        internal static bool EsOrigenApiConfiable(Uri? destino)
        {
            if (destino is null || !destino.IsAbsoluteUri || !string.IsNullOrEmpty(destino.UserInfo))
                return false;

            foreach (var origen in OrigenesApiConfiables)
            {
                if (string.Equals(destino.Scheme, origen.Scheme, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(destino.Host, origen.Host, StringComparison.OrdinalIgnoreCase) &&
                    destino.Port == origen.Port)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Cierra la sesión local (y opcionalmente avisa al servidor) y vuelve al login
    public static class SesionService
    {
        private static int _cerrando;

        public static async Task CerrarAsync(string? mensaje = null, bool avisarAlServidor = false)
        {
            if (Interlocked.Exchange(ref _cerrando, 1) != 0)
                return;

            try
            {
                if (avisarAlServidor)
                {
                    try { await new UsuarioService().CerrarSesionAsync(); }
                    catch { }
                }

                Preferences.Clear();
                SesionUsuario.UsuarioId = 0;
                SesionUsuario.Nombre = string.Empty;
                SesionUsuario.TipoUsuario = string.Empty;
                SesionUsuario.TokenSesion = null;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var app = Application.Current;
                    if (app == null || app.Windows.Count == 0)
                        return;

                    app.Windows[0].Page = new NavigationPage(new LoginPage());

                    if (!string.IsNullOrEmpty(mensaje))
                    {
                        await Task.Delay(400);

                        var pagina = app.Windows[0].Page;
                        if (pagina != null)
                            await pagina.DisplayAlertAsync("MUEVE", mensaje, "Aceptar");
                    }
                });
            }
            finally
            {
                Volatile.Write(ref _cerrando, 0);
            }
        }
    }
}
