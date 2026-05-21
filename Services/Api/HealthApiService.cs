using System.Text.Json.Serialization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class HealthApiService
{
    private readonly ApiClient _apiClient;

    public HealthApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<OperationResult> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await GetHealthRawAsync(
                "/api/health",
                cancellationToken);

            if (response.IsSuccess)
            {
                return new OperationResult
                {
                    Success = true,
                    Message = $"API accessible : {_apiClient.BaseUrl}"
                };
            }

            return new OperationResult
            {
                Success = false,
                Message = $"API inaccessible. HTTP {response.StatusCode}. {response.Body}"
            };
        }
        catch (Exception exception)
        {
            return new OperationResult
            {
                Success = false,
                Message = $"Connexion API impossible : {exception.Message}"
            };
        }
    }

    public async Task<ApiHealthResult> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetHealthRawAsync(
            "/api/health",
            cancellationToken);

        return new ApiHealthResult
        {
            IsSuccess = response.IsSuccess,
            StatusCode = response.StatusCode,
            RawBody = response.Body
        };
    }

    public async Task<ApiHealthResult> GetAbssoluteHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetHealthRawAsync(
            "/api/health/abssolute",
            cancellationToken);

        return new ApiHealthResult
        {
            IsSuccess = response.IsSuccess,
            StatusCode = response.StatusCode,
            RawBody = response.Body
        };
    }

    public async Task<ApiHealthResult> GetMobileHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetHealthRawAsync(
            "/api/health/mobile",
            cancellationToken);

        return new ApiHealthResult
        {
            IsSuccess = response.IsSuccess,
            StatusCode = response.StatusCode,
            RawBody = response.Body
        };
    }

    private Task<ApiRawResponse> GetHealthRawAsync(
        string route,
        CancellationToken cancellationToken)
    {
        return _apiClient.GetRawAsync(
            route,
            ApiTimeouts.HealthCheck,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }
}

public sealed class ApiHealthResult
{
    public bool IsSuccess { get; set; }

    public int StatusCode { get; set; }

    public string RawBody { get; set; } = string.Empty;
}
