using System;

namespace MobileSLI.Domain.Rules;

/// <summary>
/// Provides helper methods for handling deliveries when the client is closed (client fermé).
/// Contains constants for statuses and methods to normalize delivery line and quantity states.
/// </summary>
public static class ClientFermeRules
{
    /// <summary>
    /// Status used when a closed client was not visited.
    /// </summary>
    public const string StatutPassageClientFerme = "NON_FAIT";

    /// <summary>
    /// Automatic comment inserted by the delivery person when the client is closed.
    /// </summary>
    public const string CommentaireLivreurAutomatique = "Client fermé";

    /// <summary>
    /// Returns a normalized line state when the client is closed. If the client is not closed, the original state is returned.
    /// When closed, this method updates the status, marks the line as validated, sets the validation time, and applies a default comment.
    /// </summary>
    /// <param name="ligne">Current state of the delivery line.</param>
    /// <param name="now">Optional override for the current time. Used for testing.</param>
    /// <returns>The normalized line state.</returns>
    public static ClientFermeLineState NormalizeLine(
        ClientFermeLineState ligne,
        DateTime? now = null)
    {
        if (!ligne.EstFerme)
        {
            return ligne;
        }

        return ligne with
        {
            StatutPassage = StatutPassageClientFerme,
            EstValidee = true,
            HeureValidation = ligne.HeureValidation ?? now ?? DateTime.Now,
            CommentaireLivreur = CommentaireLivreurAutomatique
        };
    }

    /// <summary>
    /// Returns a normalized quantity state when the client is closed. If the client is not closed, the original state is returned.
    /// When closed, both delivered and recovered quantities are set to zero.
    /// </summary>
    /// <param name="quantite">Current quantity state.</param>
    /// <param name="estFerme">Whether the client is closed.</param>
    /// <returns>The normalized quantity state.</returns>
    public static ClientFermeQuantiteState NormalizeQuantite(
        ClientFermeQuantiteState quantite,
        bool estFerme)
    {
        if (!estFerme)
        {
            return quantite;
        }

        return quantite with
        {
            QuantiteLivree = 0,
            QuantiteRecuperee = 0
        };
    }
}

/// <summary>
/// Represents the state of a delivery line for a client.
/// </summary>
/// <param name="EstFerme">Indicates if the client is closed.</param>
/// <param name="StatutPassage">Delivery status code.</param>
/// <param name="EstValidee">Whether the delivery is validated.</param>
/// <param name="HeureValidation">Time of validation.</param>
/// <param name="CommentaireLivreur">Comment left by the delivery person.</param>
public readonly record struct ClientFermeLineState(
    bool EstFerme,
    string? StatutPassage,
    bool EstValidee,
    DateTime? HeureValidation,
    string? CommentaireLivreur);

/// <summary>
/// Represents the quantities delivered and recovered for a client.
/// </summary>
/// <param name="QuantiteLivree">Delivered quantity.</param>
/// <param name="QuantiteRecuperee">Recovered quantity.</param>
public readonly record struct ClientFermeQuantiteState(
    int QuantiteLivree,
    int QuantiteRecuperee);
