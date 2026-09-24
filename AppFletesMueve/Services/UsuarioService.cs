using System.Net.Http.Json;
using AppFletesMueve.Models;

namespace AppFletesMueve.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;

        private const string ApiUrl = " https://narrow-drama-lloyd-however.trycloudflare.com/api/";

        public UsuarioService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> RegistrarUsuario(Usuario usuario)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ApiUrl + "Usuario",
                    usuario);

                var contenido = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"API STATUS: {(int)response.StatusCode}");

                System.Diagnostics.Debug.WriteLine(
                    $"API RESPUESTA: {contenido}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ERROR API: {ex}");

                throw;
            }
        }

        public async Task<Usuario?> Login(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                ApiUrl + "Usuario/login",
                new
                {
                    Email = email,
                    Password = password
                });

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content
                .ReadFromJsonAsync<Usuario>();
        }
    }
}