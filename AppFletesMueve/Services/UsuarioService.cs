using System.Net.Http.Json;
using AppFletesMueve.Models;
using AppFletesMueve.Services;
using System.Text.Json;


namespace AppFletesMueve.Services
{
    public sealed record ResultadoLogin(Usuario? Usuario, bool RequiereConfirmacionSesion);

    public class UsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        public async Task CerrarSesionAsync()
        {
            try { await _httpClient.PostAsync(_apiUrl + "Usuario/logout", null); }
            catch { }
        }
        public UsuarioService()
        {
            _httpClient = ApiHttp.CrearCliente();

            // Priorizar valor manual si fue guardado en Preferences (útil para pruebas)
            var pref = Preferences.Get("ApiBaseUrl", string.Empty);
            if (!string.IsNullOrWhiteSpace(pref))
            {
                _apiUrl = pref.TrimEnd('/') + "/";
                return;
            }

            // Elegir URL por plataforma y entorno de depuración
#if DEBUG
            // En desarrollo preferimos apuntar al backend local.
            try
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    // El emulador Android usa 10.0.2.2 para acceder al host
                    _apiUrl = "http://10.0.2.2:5051/api/";
                }
                else
                {
                    // Simuladores de iOS/Windows/Mac pueden usar localhost
                    _apiUrl = "http://localhost:5051/api/";
                }
            }
            catch
            {
                _apiUrl = "https://mueveya.onrender.com/api/";
            }
#else
            _apiUrl = "https://mueveya.onrender.com/api/";
#endif
        }

        public async Task<(bool Success, string? ErrorMessage)> RegistrarUsuario(Usuario usuario)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    _apiUrl + "Usuario",
                    usuario);

                var contenido = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"API STATUS: {(int)response.StatusCode}");

                System.Diagnostics.Debug.WriteLine(
                    $"API RESPUESTA: {contenido}");

                if (response.IsSuccessStatusCode)
                    return (true, null);

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        using var json = JsonDocument.Parse(contenido);
                        if (json.RootElement.TryGetProperty("mensaje", out var mensaje))
                            return (false, mensaje.GetString());
                    }
                    catch { }
                }

                return (false, "No se pudo crear la cuenta. Intentá de nuevo.");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR de red al registrar: {ex}");
                return (false, "No se pudo conectar con el servidor. Verificá tu conexión e intentá de nuevo.");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tiempo de espera al registrar: {ex}");
                return (false, "El servidor tardó demasiado en responder. Intentá nuevamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR API: {ex}");

                return (false, "No se pudo crear la cuenta. Intentá de nuevo.");
            }
        }

        public async Task<ResultadoLogin> Login(string email, string password, bool confirmarReemplazoSesion = false)
        {
            var response = await _httpClient.PostAsJsonAsync(
                _apiUrl + "Usuario/login",
                new
                {
                    Email = email,
                    Password = password,
                    ConfirmarReemplazoSesion = confirmarReemplazoSesion
                });

            string contenido = string.Empty;
            try
            {
                contenido = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"API STATUS: {(int)response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"API RESPUESTA: {contenido}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR leyendo respuesta API: {ex}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return new ResultadoLogin(null, true);

            if (!response.IsSuccessStatusCode)
                return new ResultadoLogin(null, false);

            var usuario = JsonSerializer.Deserialize<Usuario>(contenido, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return new ResultadoLogin(usuario, false);
        }
    }
}
