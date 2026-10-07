using System.Net.Http.Json;
using AppFletesMueve.Models;

namespace AppFletesMueve.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public UsuarioService()
        {
            _httpClient = new HttpClient();

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

                // Devolver el mensaje de error del API si está presente
                return (false, !string.IsNullOrWhiteSpace(contenido) ? contenido : "Error en la respuesta del servidor.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR API: {ex}");

                // No propagar la excepción al UI. Devolver detalle del error para mostrarlo.
                return (false, ex.Message);
            }
        }

        public async Task<Usuario?> Login(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                _apiUrl + "Usuario/login",
                new
                {
                    Email = email,
                    Password = password
                });

            // Log status and response body for debugging
            try
            {
                var contenido = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"API STATUS: {(int)response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"API RESPUESTA: {contenido}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR leyendo respuesta API: {ex}");
            }

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<Usuario>();
        }
    }
}