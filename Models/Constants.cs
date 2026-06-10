namespace MobileSLI.Models;

/// <summary>
/// Defines constants representing the status of a delivery attempt (statut de passage).
/// </summary>
public static class StatutPassageConstants
{
    /// <summary>
    /// Status indicating the stop is still to be done (à faire).
    /// </summary>
    public const string AFaire = "A_FAIRE";

    /// <summary>
    /// Status indicating the stop has been completed (fait).
    /// </summary>
    public const string Fait = "FAIT";

    /// <summary>
    /// Status indicating the stop was not performed (non fait).
    /// </summary>
    public const string NonFait = "NON_FAIT";

    /// <summary>
    /// Status indicating an anomaly occurred.
    /// </summary>
    public const string Anomalie = "ANOMALIE";

    /// <summary>
    /// Collection of all possible passage statuses. Useful for validation or enumeration.
    /// </summary>
    public static readonly string[] All =
    {
        AFaire,
        Fait,
        NonFait,
        Anomalie
    };
}

/// <summary>
/// Represents the possible local states of a delivery tour (tournée) on the device.
/// These values track the lifecycle of a tour from not loaded to synchronized or abandoned.
/// </summary>
public static class TourneeLocalStatus
{
    /// <summary>
    /// Tour has not been loaded yet.
    /// </summary>
    public const string NonChargee = "NON_CHARGEE";
    /// <summary>
    /// Tour has been loaded onto the device.
    /// </summary>
    public const string Chargee = "CHARGEE";
    /// <summary>
    /// Tour is currently in progress.
    /// </summary>
    public const string EnCours = "EN_COURS";
    /// <summary>
    /// Tour is finished locally and ready to be synchronized with the server.
    /// </summary>
    public const string PreteASynchroniser = "PRETE_A_SYNCHRONISER";
    /// <summary>
    /// Tour has been successfully synchronized with the server.
    /// </summary>
    public const string Synchronisee = "SYNCHRONISEE";
    /// <summary>
    /// An error occurred during synchronization.
    /// </summary>
    public const string ErreurSynchronisation = "ERREUR_SYNCHRONISATION";
    /// <summary>
    /// Tour was already synchronized previously.
    /// </summary>
    public const string DejaSynchronisee = "DEJA_SYNCHRONISEE";
    /// <summary>
    /// Tour has expired and is no longer valid.
    /// </summary>
    public const string Expiree = "EXPIREE";

    /*
     * Tournée volontairement abandonnée sur le téléphone.
     * Elle n'est plus considérée comme active et ne bloque plus le chargement
     * d'une nouvelle tournée, mais elle reste traçable dans SQLite pendant les tests.
     */
    public const string AbandonneeLocale = "ABANDONNEE_LOCALE";
}

/// <summary>
/// Defines codes representing different types of articles/items handled during deliveries.
/// </summary>
public static class ArticleCodes
{
    /// <summary>
    /// Code for rolls (chariots).
    /// </summary>
    public const string Rolls = "ROLLS";
    /// <summary>
    /// Code for conveyor belts (tapis).
    /// </summary>
    public const string Tapis = "TAPIS";
    /// <summary>
    /// Code for bags (sacs).
    /// </summary>
    public const string Sacs = "SACS";

    /*
     * Règle métier mise à jour :
     * ROLLS = Chariots.
     * ROLLS_VIDES = Chariots vides.
     * ROLLS_VIDES peut maintenant porter une quantité livrée prévue,
     * une quantité livrée et une quantité récupérée.
     */
    public const string RollsVides = "ROLLS_VIDES";
}
