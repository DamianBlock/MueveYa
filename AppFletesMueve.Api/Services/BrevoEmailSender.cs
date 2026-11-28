using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AppFletesMueve.Api.Services
{
    public sealed class BrevoEmailSender : IEmailSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BrevoEmailSender> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> EnviarCodigoRecuperacionAsync(string destinatario, string codigo)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderEmail = _configuration["Brevo:SenderEmail"];
            var senderName = _configuration["Brevo:SenderName"] ?? "MUEVE";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogWarning("Brevo no está configurado; no se envió un correo de recuperación.");
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = destinatario } },
                subject = "Código para recuperar tu contraseña de MUEVE",
                textContent = $"Tu código de recuperación de MUEVE es: {codigo}. Vence en 15 minutos. Si no lo solicitaste, ignorá este mensaje."
            });

            try
            {
                var client = _httpClientFactory.CreateClient("Brevo");
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return true;

                _logger.LogWarning("Brevo rechazó un correo de recuperación con estado {StatusCode}.", (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar un correo de recuperación mediante Brevo.");
            }

            return false;
        }
    }
}
