using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MobileSLI.Services;

namespace MobileSLI.Services.Api;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;

    public ApiClient(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string BaseUrl => NormalizeBaseUrl(_settingsService.ApiBaseUrl);

    public async Task<T?> GetAsync<T>(
        string route,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildUri(route),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateException(response.StatusCode, route, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ApiClientException(
                $"La réponse JSON de l'API est invalide : {exception.Message}",
                (int)response.StatusCode,
                route,
                body);
        }
    }

    public async Task<ApiRawResponse> GetRawAsync(
        string route,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildUri(route),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ApiRawResponse(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            route,
            body);
    }

    public async Task<ApiRawResponse> PostAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUri(route),
            request,
            JsonOptions,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ApiRawResponse(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            route,
            body);
    }

    public T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public string BuildRoute(
        string path,
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        var query = string.Join(
            "&",
            queryParameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!.Trim())}"));

        return string.IsNullOrWhiteSpace(query)
            ? path
            : $"{path}?{query}";
    }

    public string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private Uri BuildUri(string route)
    {
        var normalizedRoute = route.StartsWith("/", StringComparison.Ordinal)
            ? route
            : "/" + route;

        return new Uri(BaseUrl + normalizedRoute, UriKind.Absolute);
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "http://127.0.0.1:5000";
        }

        var normalized = baseUrl.Trim().TrimEnd('/');

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "http://" + normalized;
        }

        return normalized;
    }

    private static ApiClientException CreateException(
        HttpStatusCode statusCode,
        string route,
        string body)
    {
        var status = (int)statusCode;

        var message = statusCode switch
        {
            HttpStatusCode.NotFound
                => $"Ressource introuvable : {route}",

            HttpStatusCode.BadRequest
                => $"Requête invalide vers l'API : {route}",

            HttpStatusCode.Conflict
                => $"Conflit métier ou technique détecté par l'API : {route}",

            HttpStatusCode.InternalServerError
                => $"Erreur technique côté API : {route}",

            _
                => $"Erreur API HTTP {status} sur {route}"
        };

        return new ApiClientException(message, status, route, body);
    }
}

public sealed record ApiRawResponse(
    bool IsSuccess,
    int StatusCode,
    string Route,
    string Body);

public sealed class ApiClientException : Exception
{
    public ApiClientException(
        string message,
        int statusCode,
        string route,
        string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        Route = route;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string Route { get; }

    public string ResponseBody { get; }
}
