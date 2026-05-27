using System.Globalization;

namespace SafeSight.Api.Common;

// Geohash simplificado: redondeo de coordenadas a 2 decimales (~1 km² por celda).
// Criterio elegido por simplicidad para el MVP sin dependencias externas.
// En producción se puede reemplazar por un geohash estándar (Ngeohash, etc.) para mayor precisión.
public static class GeoHashHelper
{
    private const int Precision = 2;

    public static string Encode(double latitude, double longitude)
    {
        double roundedLat = Math.Round(latitude, Precision);
        double roundedLng = Math.Round(longitude, Precision);
        return $"{roundedLat.ToString("F2", CultureInfo.InvariantCulture)}:{roundedLng.ToString("F2", CultureInfo.InvariantCulture)}";
    }

    public static (double CenterLat, double CenterLng) Decode(string geohash)
    {
        string[] parts = geohash.Split(':');
        double lat = double.Parse(parts[0], CultureInfo.InvariantCulture);
        double lng = double.Parse(parts[1], CultureInfo.InvariantCulture);
        return (lat, lng);
    }
}
