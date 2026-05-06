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
}
