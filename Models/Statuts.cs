namespace TourneesMobile.Models;

public static class StatutPassage
{
    public const string AFaire = "A_FAIRE";
    public const string Fait = "FAIT";
    public const string NonFait = "NON_FAIT";
    public const string Anomalie = "ANOMALIE";

    public static readonly string[] Tous = [AFaire, Fait, NonFait, Anomalie];

    public static bool DemandeCommentaire(string statut) =>
        string.Equals(statut, NonFait, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(statut, Anomalie, StringComparison.OrdinalIgnoreCase);
}

public static class StatutSynchronisation
{
    public const string NonEnvoyee = "NON_ENVOYEE";
    public const string Envoyee = "ENVOYEE";
    public const string Erreur = "ERREUR_ENVOI";
}
