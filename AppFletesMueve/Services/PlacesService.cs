using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AppFletesMueve.Services
{
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
        private readonly HttpClient _httpClient = new();

        // Key SEPARADA de la del Maps SDK: esta no puede restringirse por app Android.
        private const string PlacesApiKey = "TU_KEY_NUEVA_SIN_RESTRICCION_DE_APP";

        public async Task<List<PlaceSuggestion>> BuscarSugerenciasAsync(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
                return new List<PlaceSuggestion>();

            try
            {
                var url = "https://maps.googleapis.com/maps/api/place/autocomplete/json" +
                          $"?input={Uri.EscapeDataString(texto)}" +
                          "&language=es" +
                          "&components=country:ar" +
                          $"&key={PlacesApiKey}";

                var response = await _httpClient.GetFromJsonAsync<AutocompleteResponse>(url);

                if (response?.Status != "OK" || response.Predictions is null)
                    return new List<PlaceSuggestion>();

                return response.Predictions
                    .Select(p => new PlaceSuggestion { Description = p.Description, PlaceId = p.PlaceId })
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en autocomplete: {ex}");
                return new List<PlaceSuggestion>();
            }
        }

        public async Task<PlaceDetalle?> ObtenerDetalleAsync(string placeId)
        {
            try
            {
                var url = "https://maps.googleapis.com/maps/api/place/details/json" +
                          $"?place_id={placeId}" +
                          "&fields=geometry" +
                          $"&key={PlacesApiKey}";

                var response = await _httpClient.GetFromJsonAsync<DetailsResponse>(url);
                var loc = response?.Result?.Geometry?.Location;
                return loc is null ? null : new PlaceDetalle { Latitud = loc.Lat, Longitud = loc.Lng };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en place details: {ex}");
                return null;
            }
        }

        private class AutocompleteResponse
        {
            [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
            [JsonPropertyName("predictions")] public List<Prediction>? Predictions { get; set; }
        }

        private class Prediction
        {
            [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
            [JsonPropertyName("place_id")] public string PlaceId { get; set; } = string.Empty;
        }

        private class DetailsResponse
        {
            [JsonPropertyName("result")] public PlaceResult? Result { get; set; }
        }

        private class PlaceResult
        {
            [JsonPropertyName("geometry")] public PlaceGeometry? Geometry { get; set; }
        }

        private class PlaceGeometry
        {
            [JsonPropertyName("location")] public PlaceLocation? Location { get; set; }
        }

        private class PlaceLocation
        {
            [JsonPropertyName("lat")] public double Lat { get; set; }
            [JsonPropertyName("lng")] public double Lng { get; set; }
        }
    }
}