namespace MobileSLI.Models;

/// <summary>
/// Résultat applicatif commun pour les opérations métier : appel API, synchronisation,
/// contrôle de connexion ou traitement local.
/// Cette classe évite de faire remonter directement les exceptions techniques jusqu'aux écrans.
/// </summary>
public sealed class OperationResult
{
    /// <summary>
    /// Indique si l'opération s'est terminée correctement.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Indique que l'échec correspond à une tournée déjà synchronisée côté API.
    /// Ce cas est traité différemment d'une erreur réseau ou d'une erreur de validation.
    /// </summary>
    public bool AlreadySynchronized { get; init; }

    /// <summary>
    /// Code fonctionnel ou technique retourné par l'API ou par le traitement local.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Message affichable à l'utilisateur ou utilisé dans l'écran de résultat.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Détail technique conservé pour le diagnostic, sans être nécessairement affiché au livreur.
    /// </summary>
    public string TechnicalDetail { get; init; } = string.Empty;

    /// <summary>
    /// Date locale de production du résultat.
    /// </summary>
    public DateTime DateResult { get; init; } = DateTime.Now;

    /// <summary>
    /// Nombre de lignes envoyées lors d'une synchronisation réussie.
    /// </summary>
    public int LignesEnvoyees { get; init; }

    /// <summary>
    /// Construit un résultat de succès standard.
    /// </summary>
    public static OperationResult Ok(string message, int lignesEnvoyees) => new()
    {
        Success = true,
        Code = "SUCCESS",
        Message = message,
        LignesEnvoyees = lignesEnvoyees,
        DateResult = DateTime.Now
    };

    /// <summary>
    /// Construit un résultat d'échec standard en conservant le détail technique utile au diagnostic.
    /// </summary>
    public static OperationResult Fail(
        string message,
        string technicalDetail = "",
        bool alreadySynchronized = false,
        string code = "") => new()
    {
        Success = false,
        AlreadySynchronized = alreadySynchronized,
        Code = code,
        Message = message,
        TechnicalDetail = technicalDetail,
        DateResult = DateTime.Now
    };
}
