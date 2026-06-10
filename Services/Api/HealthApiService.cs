using System.Text.Json.Serialization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

/// <summary>
/// Service dédié aux endpoints de santé de l'API.
/// Il permet au mobile de distinguer l'accessibilité générale de l'API,
/// l'accès à ABSSolute côté serveur et les contrôles spécifiques au module mobile.
/// </summary>
public sealed class HealthApiService
{
    private readonly ApiClient _apiClient;

    public HealthApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Teste rapidement la disponibilité de l'API principale.
    /// Cette méthode retourne un <see cref="OperationResult"/> lisible par les écrans,
    /// au lieu de propager directement les exceptions réseau à l'interface utilisateur.
    /// </summary>
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

    /// <summary>
    /// Interroge le health check général de l'API.
    /// Le corps brut est conservé pour l'affichage ou le diagnostic réseau.
    /// </summary>
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

    /// <summary>
    /// Interroge le health check ABSSolute exposé par l'API.
    /// Ce contrôle vérifie indirectement que l'API peut accéder aux données centrales nécessaires au chargement des tournées.
    /// </summary>
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

    /// <summary>
    /// Interroge le health check propre au module mobile.
    /// À utiliser pour diagnostiquer spécifiquement la disponibilité des fonctions consommées par l'application Android.
    /// </summary>
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

    /// <summary>
    /// Exécute les health checks avec un timeout court et sans retry.
    /// Un health check doit répondre vite : multiplier les tentatives masquerait un vrai problème réseau ou serveur.
    /// </summary>
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

/// <summary>
/// Résultat brut d'un contrôle de santé API.
/// Le corps de réponse n'est pas interprété ici afin de rester compatible avec différents formats de diagnostic serveur.
/// </summary>
public sealed class ApiHealthResult
{
    public bool IsSuccess { get; set; }

    public int StatusCode { get; set; }

    public string RawBody { get; set; } = string.Empty;
}
