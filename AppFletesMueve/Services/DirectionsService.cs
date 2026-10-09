using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Maps;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

namespace AppFletesMueve.Services
{
    public class DirectionsService : IDirectionsService
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string OsrmBase = "https://router.project-osrm.org";
        public static DirectionsService Instance { get; set; } = new DirectionsService();

        public async Task<List<Location>?> GetRoutePointsAsync(double lat1, double lon1, double lat2, double lon2)
        {
            try
            {
                var url = $"{OsrmBase}/route/v1/driving/{lon1.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lat1.ToString(System.Globalization.CultureInfo.InvariantCulture)};{lon2.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lat2.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&geometries=polyline6";

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
                        if (geom.ValueKind == JsonValueKind.String)
                        {
                            var poly = geom.GetString();
                            if (!string.IsNullOrEmpty(poly))
                            {
                                try
                                {
                                    var pts = DecodePolyline(poly);
                                    return pts;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"DecodePolyline failed for poly='{poly}': {ex}");
                                    try { _ = LocalCrashLogger.LogAsync($"DecodePolyline failed: {ex}\npoly:{poly}"); } catch { }
                                    return null;
                                }
                            }
                        }
                        else if (geom.ValueKind == JsonValueKind.Object)
                        {
                            try
                            {
                                if (geom.TryGetProperty("coordinates", out var coords) && coords.ValueKind == JsonValueKind.Array)
                                {
                                    var pts = new List<Location>();
                                    foreach (var c in coords.EnumerateArray())
                                    {
                                        if (c.ValueKind == JsonValueKind.Array && c.GetArrayLength() >= 2)
                                        {
                                            var lon = c[0].GetDouble();
                                            var lat = c[1].GetDouble();
                                            pts.Add(new Location(lat, lon));
                                        }
                                    }
                                    if (pts.Count > 0) return pts;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fallback geojson parse failed: {ex}");
                            }
                        }
                    }

                    if (first.TryGetProperty("legs", out var legs) && legs.ValueKind == JsonValueKind.Array)
                    {
                        var pts = new List<Location>();
                        foreach (var leg in legs.EnumerateArray())
                        {
                            if (leg.ValueKind != JsonValueKind.Object) continue;
                            if (leg.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var step in steps.EnumerateArray())
                                {
                                    if (step.ValueKind != JsonValueKind.Object) continue;
                                    if (step.TryGetProperty("geometry", out var sgeom))
                                    {
                                        if (sgeom.ValueKind == JsonValueKind.String)
                                        {
                                            var spoly = sgeom.GetString();
                                            if (!string.IsNullOrEmpty(spoly))
                                            {
                                                try
                                                {
                                                    var decoded = DecodePolyline(spoly);
                                                    if (decoded != null && decoded.Count > 0) pts.AddRange(decoded);
                                                }
                                                catch (Exception ex)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"DecodePolyline for step failed: {ex}");
                                                }
                                            }
                                        }
                                        else if (sgeom.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var c in sgeom.EnumerateArray())
                                            {
                                                if (c.ValueKind == JsonValueKind.Array && c.GetArrayLength() >= 2)
                                                {
                                                    var lon = c[0].GetDouble();
                                                    var lat = c[1].GetDouble();
                                                    pts.Add(new Location(lat, lon));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (pts.Count > 0) return pts;
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

        private static List<Location> DecodePolyline(string encoded)
        {
            var poly = new List<Location>();
            if (string.IsNullOrEmpty(encoded)) return poly;

            int index = 0, len = encoded.Length;
            long lat = 0, lng = 0;

            while (index < len)
            {
                long result = 0;
                int shift = 0;
                int b;
                do
                {
                    if (index >= len) return poly; // malformed string
                    b = encoded[index++] - 63;
                    result |= (long)(b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);

                long deltaLat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += deltaLat;

                result = 0;
                shift = 0;
                do
                {
                    if (index >= len) return poly;
                    b = encoded[index++] - 63;
                    result |= (long)(b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);

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
