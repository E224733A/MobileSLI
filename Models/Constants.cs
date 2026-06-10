namespace MobileSLI.Models;

/// <summary>
/// Codes de statut de passage utilisés pour qualifier le résultat d'un arrêt de tournée.
/// Ces valeurs sont envoyées à l'API lors de la synchronisation.
/// </summary>
public static class StatutPassageConstants
{
    /// <summary>
    /// Arrêt pas encore traité par le livreur.
    /// </summary>
    public const string AFaire = "A_FAIRE";

    /// <summary>
    /// Arrêt réalisé normalement.
    /// </summary>
    public const string Fait = "FAIT";

    /// <summary>
    /// Arrêt non réalisé, par exemple dans le cas d'un client fermé.
    /// </summary>
    public const string NonFait = "NON_FAIT";

    /// <summary>
    /// Arrêt réalisé avec anomalie ou situation particulière.
    /// </summary>
    public const string Anomalie = "ANOMALIE";

    /// <summary>
    /// Liste des statuts autorisés pour les validations locales.
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
/// États locaux d'une tournée stockée sur le téléphone.
/// Ils décrivent le cycle de vie local : chargement, saisie, synchronisation, erreur ou abandon.
/// </summary>
public static class TourneeLocalStatus
{
    /// <summary>
    /// Tournée pas encore chargée sur le téléphone.
    /// </summary>
    public const string NonChargee = "NON_CHARGEE";

    /// <summary>
    /// Tournée chargée localement mais pas encore commencée.
    /// </summary>
    public const string Chargee = "CHARGEE";

    /// <summary>
    /// Tournée en cours de saisie sur le téléphone.
    /// </summary>
    public const string EnCours = "EN_COURS";

    /// <summary>
    /// Tournée terminée localement et prête à être envoyée à l'API.
    /// </summary>
    public const string PreteASynchroniser = "PRETE_A_SYNCHRONISER";

    /// <summary>
    /// Tournée envoyée et acceptée par l'API.
    /// </summary>
    public const string Synchronisee = "SYNCHRONISEE";

    /// <summary>
    /// Tournée en erreur après une tentative de synchronisation.
    /// </summary>
    public const string ErreurSynchronisation = "ERREUR_SYNCHRONISATION";

    /// <summary>
    /// Tournée déjà connue comme synchronisée côté API.
    /// </summary>
    public const string DejaSynchronisee = "DEJA_SYNCHRONISEE";

    /// <summary>
    /// Tournée expirée ou refusée car elle ne correspond plus à la date autorisée par l'API.
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
/// Codes articles manipulés par le mobile pour les quantités livrées et récupérées.
/// </summary>
public static class ArticleCodes
{
    /// <summary>
    /// Code des chariots.
    /// </summary>
    public const string Rolls = "ROLLS";

    /// <summary>
    /// Code des tapis.
    /// </summary>
    public const string Tapis = "TAPIS";

    /// <summary>
    /// Code des sacs.
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
