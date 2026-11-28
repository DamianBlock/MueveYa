using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AppFletesMueve.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public PlacesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 120) return BadRequest(new { mensaje = "La búsqueda no es válida." });
        if (!EstaConfigurado()) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = "El buscador no está configurado." });
        using var request = CrearRequest(HttpMethod.Post, "places:autocomplete", "suggestions.placePrediction.placeId,suggestions.placePrediction.text.text");
        request.Content = JsonContent.Create(new { input = input.Trim(), includedRegionCodes = new[] { "ar" }, languageCode = "es" });
        using var response = await _httpClientFactory.CreateClient("GooglePlaces").SendAsync(request, HttpContext.RequestAborted);
        if (!response.IsSuccessStatusCode) return ErrorGoogle();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted));
        var predictions = new List<object>();
        if (json.RootElement.TryGetProperty("suggestions", out var suggestions))
        {
            foreach (var suggestion in suggestions.EnumerateArray())
            {
                if (!suggestion.TryGetProperty("placePrediction", out var prediction)) continue;
                predictions.Add(new
                {
                    description = prediction.GetProperty("text").GetProperty("text").GetString(),
                    place_id = prediction.GetProperty("placeId").GetString()
                });
            }
        }
        return Ok(new { status = "OK", predictions });
    }

    [HttpGet("details/{placeId}")]
    public async Task<IActionResult> Details(string placeId)
    {
        if (string.IsNullOrWhiteSpace(placeId) || placeId.Length > 200) return BadRequest(new { mensaje = "El lugar no es válido." });
        if (!EstaConfigurado()) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = "El buscador no está configurado." });
        using var request = CrearRequest(HttpMethod.Get, "places/" + Uri.EscapeDataString(placeId), "location");
        using var response = await _httpClientFactory.CreateClient("GooglePlaces").SendAsync(request, HttpContext.RequestAborted);
        if (!response.IsSuccessStatusCode) return ErrorGoogle();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted));
        if (!json.RootElement.TryGetProperty("location", out var location)) return NotFound();
        return Ok(new { result = new { geometry = new { location = new { lat = location.GetProperty("latitude").GetDouble(), lng = location.GetProperty("longitude").GetDouble() } } } });
    }

    private HttpRequestMessage CrearRequest(HttpMethod method, string recurso, string fieldMask)
    {
        var apiKey = _configuration["GoogleMaps:PlacesApiKey"]!;
        var request = new HttpRequestMessage(method, recurso);
        request.Headers.Add("X-Goog-Api-Key", apiKey);
        request.Headers.Add("X-Goog-FieldMask", fieldMask);
        return request;
    }

    private ObjectResult ErrorGoogle() => StatusCode(StatusCodes.Status502BadGateway, new { mensaje = "No se pudo consultar el buscador." });

    private bool EstaConfigurado() => !string.IsNullOrWhiteSpace(_configuration["GoogleMaps:PlacesApiKey"]);
}
