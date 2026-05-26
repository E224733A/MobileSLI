namespace MobileSLI.Models;

public static class StatutPassageConstants
{
    public const string AFaire = "A_FAIRE";
    public const string Fait = "FAIT";
    public const string NonFait = "NON_FAIT";
    public const string Anomalie = "ANOMALIE";

    public static readonly string[] All =
    {
        AFaire,
        Fait,
        NonFait,
        Anomalie
    };
}

public static class TourneeLocalStatus
{
    public const string NonChargee = "NON_CHARGEE";
    public const string Chargee = "CHARGEE";
    public const string EnCours = "EN_COURS";
    public const string PreteASynchroniser = "PRETE_A_SYNCHRONISER";
    public const string Synchronisee = "SYNCHRONISEE";
    public const string ErreurSynchronisation = "ERREUR_SYNCHRONISATION";
    public const string DejaSynchronisee = "DEJA_SYNCHRONISEE";
    public const string Expiree = "EXPIREE";

    /*
     * Tournée volontairement abandonnée sur le téléphone.
     * Elle n'est plus considérée comme active et ne bloque plus le chargement
     * d'une nouvelle tournée, mais elle reste traçable dans SQLite pendant les tests.
     */
    public const string AbandonneeLocale = "ABANDONNEE_LOCALE";
}

public static class ArticleCodes
{
    public const string Rolls = "ROLLS";
    public const string Tapis = "TAPIS";
    public const string Sacs = "SACS";

    /*
     * Article de récupération uniquement.
     * Le livreur ne livre pas de rolls vides au client.
     * Côté mobile, QuantiteLivree doit toujours rester à 0.
     */
    public const string RollsVides = "ROLLS_VIDES";
}
