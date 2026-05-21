using System.Globalization;
using System.IO;
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
            // Le timeout global de 8 secondes provoquait des coupures réseau trop rapides
            // sur les chargements métier. Les délais sont maintenant pilotés appel par appel.
            Timeout = System.Threading.Timeout.InfiniteTimeSpan
        };
    }

    public string BaseUrl => NormalizeBaseUrl(_settingsService.ApiBaseUrl);

    public Task<T?> GetAsync<T>(
        string route,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<T>(
            route,
            ApiTimeouts.DefaultGet,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }

    public async Task<T?> GetAsync<T>(
        string route,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        var response = await GetRawAsync(
            route,
            timeout,
            retryCount,
            retryDelay,
            cancellationToken);

        if (!response.IsSuccess)
        {
            throw CreateException(response.StatusCode, route, response.Body);
        }

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(response.Body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ApiClientException(
                $"La réponse JSON de l'API est invalide : {exception.Message}",
                response.StatusCode,
                route,
                response.Body,
                exception);
        }
    }

    public Task<ApiRawResponse> GetRawAsync(
        string route,
        CancellationToken cancellationToken = default)
    {
        return GetRawAsync(
            route,
            ApiTimeouts.DefaultGet,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }

    public Task<ApiRawResponse> GetRawAsync(
        string route,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        return SendRawAsync(
            route,
            HttpMethod.Get,
            contentFactory: null,
            timeout,
            retryCount,
            retryDelay,
            cancellationToken);
    }

    public Task<ApiRawResponse> PostAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsJsonAsync(
            route,
            request,
            ApiTimeouts.DefaultPost,
            cancellationToken);
    }

    public Task<ApiRawResponse> PostAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return SendRawAsync(
            route,
            HttpMethod.Post,
            () => JsonContent.Create(request, options: JsonOptions),
            timeout,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
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

    private async Task<ApiRawResponse> SendRawAsync(
        string route,
        HttpMethod method,
        Func<HttpContent?>? contentFactory,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        var attempts = Math.Max(0, retryCount) + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                using var request = new HttpRequestMessage(method, BuildUri(route));
                request.Content = contentFactory?.Invoke();

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    timeoutCts.Token);

                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                return new ApiRawResponse(
                    response.IsSuccessStatusCode,
                    (int)response.StatusCode,
                    route,
                    body);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = CreateTimeoutException(route, timeout, exception);
            }
            catch (HttpRequestException exception)
            {
                lastException = CreateNetworkException(route, exception);
            }
            catch (IOException exception)
            {
                lastException = CreateNetworkException(route, exception);
            }

            if (attempt < attempts)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        if (lastException is ApiClientException apiClientException)
        {
            throw apiClientException;
        }

        throw new ApiClientException(
            $"Connexion API impossible sur {route}.",
            0,
            route,
            lastException?.Message ?? string.Empty,
            lastException);
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
        int statusCode,
        string route,
        string body)
    {
        var message = ((HttpStatusCode)statusCode) switch
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
                => $"Erreur API HTTP {statusCode} sur {route}"
        };

        return new ApiClientException(message, statusCode, route, body);
    }

    private static ApiClientException CreateTimeoutException(
        string route,
        TimeSpan timeout,
        Exception exception)
    {
        return new ApiClientException(
            $"Le serveur n'a pas répondu dans le délai prévu ({timeout.TotalSeconds:0} secondes) pour {route}. Vérifiez le Wi-Fi dépôt puis réessayez.",
            0,
            route,
            exception.Message,
            exception);
    }

    private static ApiClientException CreateNetworkException(
        string route,
        Exception exception)
    {
        return new ApiClientException(
            $"Connexion réseau interrompue pendant l'appel API {route}. Vérifiez le Wi-Fi dépôt puis réessayez.",
            0,
            route,
            exception.Message,
            exception);
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
        string responseBody,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Route = route;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string Route { get; }

    public string ResponseBody { get; }
}
