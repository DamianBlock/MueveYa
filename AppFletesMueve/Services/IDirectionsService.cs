using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Maps;

namespace AppFletesMueve.Services
{
    public interface IDirectionsService
    {
        Task<List<Location>?> GetRoutePointsAsync(double lat1, double lon1, double lat2, double lon2);
    }
}
