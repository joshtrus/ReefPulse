using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ReefPulse.Infrastructure.Ingestion;

public sealed record MarineSnapshot(
    DateTimeOffset ObservedAt,
    double SeaSurfaceTemperatureCelsius,
    double WaveHeightMeters);

public interface IOpenMeteoClient
{
    Task<MarineSnapshot?> GetCurrentAsync(double latitude, double longitude, CancellationToken ct = default);
}

public sealed class OpenMeteoClient : IOpenMeteoClient
{
    private readonly HttpClient _http;

    public OpenMeteoClient(HttpClient http) => _http = http;

    public async Task<MarineSnapshot?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"v1/marine?latitude={lat}&longitude={lon}&current=sea_surface_temperature,wave_height";

        var response = await _http.GetFromJsonAsync<MarineResponse>(url, ct);
        var current = response?.Current;
        if (current?.Time is null || current.SeaSurfaceTemperature is null || current.WaveHeight is null)
            return null;

        // API returns GMT with no offset in the string, so interpret it as UTC.
        var observedAt = DateTimeOffset.Parse(
            current.Time, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        return new MarineSnapshot(
            observedAt, current.SeaSurfaceTemperature.Value, current.WaveHeight.Value);
    }

    private sealed record MarineResponse(
        [property: JsonPropertyName("current")] CurrentBlock? Current);

    private sealed record CurrentBlock(
        [property: JsonPropertyName("time")] string? Time,
        [property: JsonPropertyName("sea_surface_temperature")] double? SeaSurfaceTemperature,
        [property: JsonPropertyName("wave_height")] double? WaveHeight);
}
