using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AppFletesMueve.Services;

public class PlaceSuggestion
{
    public string Description { get; set; } = string.Empty;
    public string PlaceId { get; set; } = string.Empty;
}

public class PlaceDetalle
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
}

public class PlacesService
{
    private readonly HttpClient _httpClient = ApiHttp.CrearCliente();

#if DEBUG
    private const string ApiUrl = "http://10.0.2.2:5051/api/Places/";
#else
    private const string ApiUrl = "https://mueveya.onrender.com/api/Places/";
#endif

    public async Task<List<PlaceSuggestion>> BuscarSugerenciasAsync(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3) return new();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AutocompleteResponse>(
                ApiUrl + "autocomplete?input=" + Uri.EscapeDataString(texto.Trim()));
            return response?.Status == "OK" && response.Predictions is not null
                ? response.Predictions.Select(p => new PlaceSuggestion { Description = p.Description, PlaceId = p.PlaceId }).ToList()
                : new();
        }
        catch { return new(); }
    }

    public async Task<PlaceDetalle?> ObtenerDetalleAsync(string placeId)
    {
        if (string.IsNullOrWhiteSpace(placeId)) return null;
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DetailsResponse>(ApiUrl + "details/" + Uri.EscapeDataString(placeId));
            var location = response?.Result?.Geometry?.Location;
            return location is null ? null : new PlaceDetalle { Latitud = location.Lat, Longitud = location.Lng };
        }
        catch { return null; }
    }

    private sealed class AutocompleteResponse { [JsonPropertyName("status")] public string Status { get; set; } = string.Empty; [JsonPropertyName("predictions")] public List<Prediction>? Predictions { get; set; } }
    private sealed class Prediction { [JsonPropertyName("description")] public string Description { get; set; } = string.Empty; [JsonPropertyName("place_id")] public string PlaceId { get; set; } = string.Empty; }
    private sealed class DetailsResponse { [JsonPropertyName("result")] public PlaceResult? Result { get; set; } }
    private sealed class PlaceResult { [JsonPropertyName("geometry")] public PlaceGeometry? Geometry { get; set; } }
    private sealed class PlaceGeometry { [JsonPropertyName("location")] public PlaceLocation? Location { get; set; } }
    private sealed class PlaceLocation { [JsonPropertyName("lat")] public double Lat { get; set; } [JsonPropertyName("lng")] public double Lng { get; set; } }
}
