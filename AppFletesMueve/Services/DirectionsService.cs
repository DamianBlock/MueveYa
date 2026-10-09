using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Maps;
using System.Collections.Generic;
using System;

namespace AppFletesMueve.Services
{
    public static class DirectionsService
    {
        private static readonly HttpClient _http = new HttpClient();

        /// <summary>
        /// Obtiene la ruta entre dos puntos usando OSRM public API. Devuelve lista de Location.
        /// En caso de error retorna null.
        /// </summary>
        public static async Task<List<Location>?> GetRoutePointsAsync(double lat1, double lon1, double lat2, double lon2)
        {
            try
            {
                // OSRM expects lon,lat pairs
                var url = $"https://router.project-osrm.org/route/v1/driving/{lon1.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lat1.ToString(System.Globalization.CultureInfo.InvariantCulture)};{lon2.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lat2.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&geometries=polyline6";

                using var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var first = routes[0];
                    if (first.TryGetProperty("geometry", out var geom))
                    {
                        var poly = geom.GetString();
                        if (!string.IsNullOrEmpty(poly))
                        {
                            var pts = DecodePolyline(poly);
                            return pts;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectionsService error: {ex}");
                return null;
            }
        }

        // Decodificador de polyline encoded con precision 1e6 (polyline6)
        private static List<Location> DecodePolyline(string encoded)
        {
            var poly = new List<Location>();
            int index = 0, len = encoded.Length;
            long lat = 0, lng = 0;

            while (index < len)
            {
                long result = 0;
                int shift = 0;
                int b;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (long)(b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20 && index < len);

                long deltaLat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += deltaLat;

                result = 0;
                shift = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (long)(b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20 && index < len);

                long deltaLon = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += deltaLon;

                // polyline6 uses 1e6 precision
                var latitude = lat / 1e6;
                var longitude = lng / 1e6;
                poly.Add(new Location(latitude, longitude));
            }

            return poly;
        }
    }
}
